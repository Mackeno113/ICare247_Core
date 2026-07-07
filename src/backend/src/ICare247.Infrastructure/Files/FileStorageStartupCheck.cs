// File    : FileStorageStartupCheck.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Kiểm tra backend lưu trữ đã cấu hình có đọc/ghi được lúc KHỞI ĐỘNG. Provider != Db mà
//           không sẵn sàng → dừng app (fail-fast) thay vì âm thầm ghi local → tránh 404 ngẫu nhiên sau LB.

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Hosted service chạy 1 lần lúc khởi động: probe backend của <see cref="FileStorageOptions.Provider"/>.
/// Db luôn khỏe (bỏ qua). FileSystem/Object không đọc/ghi được → log Critical + dừng ứng dụng.
/// </summary>
public sealed class FileStorageStartupCheck : IHostedService
{
    private readonly IFileStoreSelector _selector;
    private readonly FileStorageOptions _opts;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<FileStorageStartupCheck> _logger;

    /// <summary>Khởi tạo với selector, options, lifetime (để dừng app) và logger.</summary>
    public FileStorageStartupCheck(
        IFileStoreSelector selector,
        IOptions<FileStorageOptions> opts,
        IHostApplicationLifetime lifetime,
        ILogger<FileStorageStartupCheck> logger)
    {
        _selector = selector;
        _opts = opts.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <summary>Probe backend cấu hình. Sự kiện theo sau: không sẵn sàng (Provider != Db) → StopApplication().</summary>
    /// <param name="ct">Cancellation token khởi động.</param>
    public async Task StartAsync(CancellationToken ct)
    {
        // Db: luôn node-safe (chung Data DB) — không cần probe.
        if (string.Equals(_opts.Provider, FileStorageProviders.Db, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("FileStorage: provider = Db (bytes trong DB). Bỏ qua probe.");
            return;
        }

        var store = _selector.SelectForKind(_opts.Provider);
        var health = await store.CheckHealthAsync(ct);

        if (health.IsHealthy)
        {
            _logger.LogInformation("FileStorage: provider = {Provider} sẵn sàng. {Detail}",
                _opts.Provider, health.Detail);
            return;
        }

        // Fail-fast: không để node chạy khi không truy cập được nơi lưu chung.
        _logger.LogCritical(
            "FileStorage: provider = {Provider} KHÔNG sẵn sàng — dừng ứng dụng để tránh ghi/đọc sai. {Detail}",
            _opts.Provider, health.Detail);
        _lifetime.StopApplication();
    }

    /// <summary>Không có tài nguyên nền cần giải phóng.</summary>
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
