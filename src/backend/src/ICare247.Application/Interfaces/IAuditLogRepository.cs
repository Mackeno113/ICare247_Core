// File    : IAuditLogRepository.cs
// Module  : System
// Layer   : Application
// Purpose : Repository interface cho bảng Sys_Audit_Log — ghi và đọc audit trail.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Sys_Audit_Log</c>.
/// Ghi lại mọi thay đổi quan trọng (INSERT/UPDATE/DELETE) trên các object.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Ghi một audit log entry.
    /// </summary>
    Task InsertAsync(AuditLogEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Lấy audit log theo object (phân trang).
    /// </summary>
    Task<(IReadOnlyList<AuditLogItem> Items, int TotalCount)> GetByObjectAsync(
        string objectType,
        int objectId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}

/// <summary>Params ghi audit log.</summary>
public sealed class AuditLogEntry
{
    public string ObjectType { get; init; } = string.Empty;
    public int ObjectId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public string? OldValueJson { get; init; }
    public string? NewValueJson { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>DTO đọc audit log.</summary>
public sealed class AuditLogItem
{
    public long AuditId { get; init; }
    public string ObjectType { get; init; } = string.Empty;
    public int ObjectId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? OldValueJson { get; init; }
    public string? NewValueJson { get; init; }
    public string? CorrelationId { get; init; }
}
