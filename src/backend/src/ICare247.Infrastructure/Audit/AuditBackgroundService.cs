// File    : AuditBackgroundService.cs
// Module  : Audit
// Layer   : Infrastructure
// Purpose : Tiến trình nền ghi nhật ký. Có Redis → đẩy hàng đợi sang Redis Stream (bền) rồi
//           consumer group đọc → SqlBulkCopy NK_. Không có Redis → ghi thẳng DB (fallback).
//           Tách khỏi luồng request: mọi I/O DB/Redis nằm ở đây.

using System.Text.Json;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ICare247.Infrastructure.Audit;

/// <summary>
/// Hosted service tiêu thụ <see cref="IAuditQueue"/> và ghi xuống NK_NhatKyHoatDong.
/// </summary>
public sealed class AuditBackgroundService : BackgroundService
{
    private const string StreamKey = "ic247:audit";
    private const string GroupName = "ic247-audit";
    private const int BatchSize = 500;

    private readonly IAuditQueue _queue;
    private readonly AuditNkWriter _writer;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AuditBackgroundService> _logger;
    private readonly string _consumerName = $"{Environment.MachineName}-{Environment.ProcessId}";

    public AuditBackgroundService(
        IAuditQueue queue,
        AuditNkWriter writer,
        IServiceProvider sp,
        ILogger<AuditBackgroundService> logger)
    {
        _queue = queue;
        _writer = writer;
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Redis có thể không được đăng ký (chưa cấu hình) → GetService trả null.
        var mux = _sp.GetService<IConnectionMultiplexer>();

        if (mux is null)
        {
            _logger.LogInformation("Audit: chưa có Redis → ghi trực tiếp DB (fallback).");
            await DrainToDbAsync(stoppingToken);
            return;
        }

        _logger.LogInformation("Audit: dùng Redis Stream '{Stream}' (consumer {Consumer}).", StreamKey, _consumerName);
        var db = mux.GetDatabase();
        await EnsureGroupAsync(db);

        // Pump (hàng đợi → Redis) + Consumer (Redis → DB) chạy song song.
        await Task.WhenAll(
            PumpToRedisAsync(db, stoppingToken),
            ConsumeFromRedisAsync(db, stoppingToken));
    }

    // ── Chế độ Redis ──────────────────────────────────────────────────────────

    private async Task EnsureGroupAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamKey, GroupName, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Nhóm đã tồn tại — bình thường.
        }
    }

    /// <summary>Đẩy sự kiện từ hàng đợi in-memory sang Redis Stream (bền qua restart/scale-out).</summary>
    private async Task PumpToRedisAsync(IDatabase db, CancellationToken ct)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(ct))
            {
                while (_queue.Reader.TryRead(out var e))
                {
                    var json = JsonSerializer.Serialize(e);
                    // FireAndForget: tối đa throughput; mất 1 vài entry lúc Redis nghẽn là chấp nhận được.
                    await db.StreamAddAsync(StreamKey,
                        [new NameValueEntry("d", json)],
                        maxLength: 200_000, useApproximateMaxLength: true,
                        flags: CommandFlags.FireAndForget);
                }
            }
        }
        catch (OperationCanceledException) { /* dừng app */ }
        catch (Exception ex) { _logger.LogError(ex, "Audit pump (→Redis) lỗi."); }
    }

    /// <summary>Đọc Redis Stream theo consumer group → bulk insert DB → XACK.</summary>
    private async Task ConsumeFromRedisAsync(IDatabase db, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(StreamKey, GroupName, _consumerName, ">", BatchSize);
                if (entries.Length == 0)
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                var batch = new List<AuditEvent>(entries.Length);
                var ids = new List<RedisValue>(entries.Length);
                foreach (var entry in entries)
                {
                    string? json = entry.Values.FirstOrDefault(v => v.Name == "d").Value;
                    if (!string.IsNullOrEmpty(json) && JsonSerializer.Deserialize<AuditEvent>(json) is { } ev)
                        batch.Add(ev);
                    ids.Add(entry.Id);
                }

                if (batch.Count > 0)
                    await _writer.WriteAsync(batch, ct);

                await db.StreamAcknowledgeAsync(StreamKey, GroupName, ids.ToArray());
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit consumer (Redis→DB) lỗi.");
                await Task.Delay(1000, ct);
            }
        }
    }

    // ── Chế độ fallback (không Redis): hàng đợi → DB ───────────────────────────

    private async Task DrainToDbAsync(CancellationToken ct)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(ct))
            {
                var batch = new List<AuditEvent>(BatchSize);
                while (batch.Count < BatchSize && _queue.Reader.TryRead(out var e))
                    batch.Add(e);

                if (batch.Count > 0)
                    await _writer.WriteAsync(batch, ct);
            }
        }
        catch (OperationCanceledException) { /* dừng app */ }
        catch (Exception ex) { _logger.LogError(ex, "Audit drain (→DB) lỗi."); }
    }
}
