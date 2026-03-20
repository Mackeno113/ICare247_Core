// File    : AuditLogRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO audit log từ Sys_Audit_Log.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class AuditLogRecord
{
    public long AuditId { get; init; }
    public string Action { get; init; } = "";
    public string ChangedBy { get; init; } = "";
    public DateTime ChangedAt { get; init; }
    public string? CorrelationId { get; init; }
    public string? ChangeSummary { get; init; }
}
