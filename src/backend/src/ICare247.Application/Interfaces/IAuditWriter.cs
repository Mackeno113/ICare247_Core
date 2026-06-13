// File    : IAuditWriter.cs
// Module  : Audit
// Layer   : Application
// Purpose : Hợp đồng ghi nhật ký hoạt động KHÔNG chặn — enqueue rồi trả ngay, việc ghi DB
//           do tiến trình nền xử lý (gộp lô). Mục tiêu: không ảnh hưởng độ trễ request/UX.

namespace ICare247.Application.Interfaces;

/// <summary>Nhóm nhật ký.</summary>
public static class AuditCategory
{
    public const string Auth = "Auth";
    public const string MasterData = "MasterData";
}

/// <summary>Mã hành động nhật ký (ổn định để truy vấn/thống kê).</summary>
public static class AuditAction
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginLocked = "LOGIN_LOCKED";
    public const string Logout = "LOGOUT";
    public const string TokenRefresh = "TOKEN_REFRESH";
    public const string DataCreate = "DATA_CREATE";
    public const string DataUpdate = "DATA_UPDATE";
    public const string DataDelete = "DATA_DELETE";
}

/// <summary>
/// 1 bản ghi nhật ký. Mang <see cref="TenantId"/> (KHÔNG mang connection string — tránh lộ
/// secret khi đi qua Redis); tiến trình ghi sẽ resolve connection từ Tenant_Id.
/// </summary>
public sealed class AuditEvent
{
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public int TenantId { get; init; }
    public string Category { get; init; } = "";
    public string Action { get; init; } = "";
    public string? Result { get; init; }            // "Success" | "Failed"
    public long? UserId { get; init; }
    public string? Username { get; init; }
    public string? ObjectType { get; init; }
    public string? ObjectId { get; init; }
    public string? OldValueJson { get; init; }
    public string? NewValueJson { get; init; }
    public string? IpAddress { get; init; }
    public string? Device { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Ghi nhật ký hoạt động (non-blocking). Implementation chỉ đẩy vào hàng đợi in-memory
/// (bounded, drop khi đầy) — KHÔNG mở DB/Redis trên luồng request.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Enqueue 1 sự kiện. Tự bổ sung TenantId/CorrelationId/actor từ context hiện tại nếu thiếu.
    /// Sự kiện theo sau: tiến trình nền sẽ ghi xuống NK_NhatKyHoatDong (gộp lô). Không ném lỗi.
    /// </summary>
    void Enqueue(AuditEvent e);
}
