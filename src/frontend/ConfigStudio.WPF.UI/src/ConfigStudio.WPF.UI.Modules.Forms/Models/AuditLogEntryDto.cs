// File    : AuditLogEntryDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO hiển thị một dòng audit log trong tab Audit Log của FormDetailView.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị một entry trong tab Audit Log của Form Detail.
/// Map từ <c>Sys_Audit_Log</c> WHERE Object_Type = 'Form'.
/// </summary>
public sealed class AuditLogEntryDto
{
    public int      LogId          { get; set; }
    public string   ActionType     { get; set; } = "";   // INSERT | UPDATE
    public DateTime ChangedAt      { get; set; }
    public string   ChangedBy      { get; set; } = "";
    public string   CorrelationId  { get; set; } = "";
    public string   ChangeSummary  { get; set; } = "";   // mô tả ngắn thay đổi
}
