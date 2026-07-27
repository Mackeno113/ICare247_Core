// File    : ErrorLogBackgroundService.cs
// Module  : Errors
// Layer   : Infrastructure
// Purpose : Tiến trình nền tiêu thụ IErrorLogQueue → ghi thẳng NK_LoiHeThong (không qua Redis —
//           lỗi hiếm hơn audit trail nhiều nên không cần hạ tầng bền/scale-out của AuditBackgroundService).

using ICare247.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Errors;

/// <summary>Hosted service tiêu thụ <see cref="IErrorLogQueue"/> và ghi xuống NK_LoiHeThong.</summary>
public sealed class ErrorLogBackgroundService : BackgroundService
{
    private const int BatchSize = 100;

    private readonly IErrorLogQueue _queue;
    private readonly ErrorLogNkWriter _writer;
    private readonly ILogger<ErrorLogBackgroundService> _logger;

    public ErrorLogBackgroundService(IErrorLogQueue queue, ErrorLogNkWriter writer, ILogger<ErrorLogBackgroundService> logger)
    {
        _queue = queue;
        _writer = writer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(stoppingToken))
            {
                var batch = new List<ErrorLogEvent>(BatchSize);
                while (batch.Count < BatchSize && _queue.Reader.TryRead(out var e))
                    batch.Add(e);

                if (batch.Count > 0)
                    await _writer.WriteAsync(batch, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* dừng app */ }
        catch (Exception ex) { _logger.LogError(ex, "Error log drain (→DB) lỗi."); }
    }
}
