// File    : IErrorLogQueue.cs
// Module  : Errors
// Layer   : Application
// Purpose : Hàng đợi in-memory (bounded) cho lỗi 500 chưa xử lý — tách producer
//           (ExceptionHandlingMiddleware) khỏi consumer (nền ghi NK_LoiHeThong). Đầy thì DROP
//           (không chặn response lỗi vốn đã phải trả nhanh cho user).

using System.Threading.Channels;

namespace ICare247.Application.Interfaces;

/// <summary>
/// 1 bản ghi lỗi 500. Mang <see cref="TenantId"/> (KHÔNG mang connection string) — tiến trình
/// ghi nền resolve connection Audit DB từ Tenant_Id, giống <see cref="AuditEvent"/>.
/// </summary>
public sealed class ErrorLogEvent
{
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public int TenantId { get; init; }
    public string CorrelationId { get; init; } = "";
    public string? Source { get; init; }
    public string? Message { get; init; }
    public string? StackDetail { get; init; }
    public string? Path { get; init; }
    public string? HttpMethod { get; init; }
    public long? UserId { get; init; }
    public string? Username { get; init; }
    public string? IpAddress { get; init; }
    public string? Device { get; init; }
}

/// <summary>
/// Hàng đợi lỗi phi-chặn. Producer gọi <see cref="TryWrite"/> (O(1), không I/O); consumer nền
/// đọc qua <see cref="Reader"/>. Khi đầy: bỏ bản ghi mới + tăng <see cref="DroppedCount"/>.
/// </summary>
public interface IErrorLogQueue
{
    /// <summary>Đẩy 1 sự kiện lỗi. Trả false nếu hàng đợi đầy (đã bị drop). KHÔNG bao giờ chặn.</summary>
    bool TryWrite(ErrorLogEvent e);

    /// <summary>Đầu đọc cho tiến trình nền.</summary>
    ChannelReader<ErrorLogEvent> Reader { get; }

    /// <summary>Tổng số bản ghi bị bỏ do hàng đợi đầy.</summary>
    long DroppedCount { get; }
}
