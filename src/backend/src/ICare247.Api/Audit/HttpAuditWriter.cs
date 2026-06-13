// File    : HttpAuditWriter.cs
// Module  : Audit
// Layer   : Api
// Purpose : Cài đặt IAuditWriter ở tầng Api — bổ sung ngữ cảnh request (tenant, actor, IP,
//           thiết bị, correlation) rồi đẩy vào IAuditQueue (non-blocking). Đặt ở Api vì cần
//           HttpContext; tầng Infrastructure không phụ thuộc HTTP.

using System.Security.Claims;
using ICare247.Application.Interfaces;

namespace ICare247.Api.Audit;

/// <summary>
/// Ghi nhật ký: làm giàu <see cref="AuditEvent"/> từ HttpContext hiện tại (nếu trường còn trống)
/// rồi enqueue. KHÔNG I/O, KHÔNG ném lỗi.
/// </summary>
public sealed class HttpAuditWriter : IAuditWriter
{
    private readonly IAuditQueue _queue;
    private readonly IHttpContextAccessor _http;
    private readonly ITenantContext _tenant;

    public HttpAuditWriter(IAuditQueue queue, IHttpContextAccessor http, ITenantContext tenant)
    {
        _queue = queue;
        _http = http;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public void Enqueue(AuditEvent e)
    {
        var ctx = _http.HttpContext;
        var user = ctx?.User;

        // Actor: ưu tiên giá trị handler đã set (vd login biết rõ user); thiếu thì lấy từ JWT claim.
        var userId = e.UserId ?? ParseUserId(user);
        var username = e.Username
            ?? user?.FindFirst("unique_name")?.Value
            ?? user?.Identity?.Name;

        var enriched = new AuditEvent
        {
            OccurredAtUtc = e.OccurredAtUtc == default ? DateTime.UtcNow : e.OccurredAtUtc,
            TenantId      = e.TenantId != 0 ? e.TenantId : _tenant.TenantId,
            Category      = e.Category,
            Action        = e.Action,
            Result        = e.Result,
            UserId        = userId,
            Username      = username,
            ObjectType    = e.ObjectType,
            ObjectId      = e.ObjectId,
            OldValueJson  = e.OldValueJson,
            NewValueJson  = e.NewValueJson,
            IpAddress     = e.IpAddress ?? ctx?.Connection.RemoteIpAddress?.ToString(),
            Device        = e.Device ?? Trim(ctx?.Request.Headers.UserAgent.ToString(), 300),
            CorrelationId = e.CorrelationId ?? ctx?.Items["CorrelationId"]?.ToString()
        };

        _queue.TryWrite(enriched);   // đầy thì drop, không chặn
    }

    private static long? ParseUserId(ClaimsPrincipal? user)
    {
        var s = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : null;
    }

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : (s.Length > max ? s[..max] : s);
}
