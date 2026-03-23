// File    : LookupItemEditRecord.cs
// Module  : Core
// Layer   : Data
// Purpose : DTO dùng trong Sys_Lookup Manager — đọc/ghi đầy đủ một item lookup.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Đại diện một item trong <c>Sys_Lookup</c> phục vụ màn hình quản lý.
/// Bao gồm cả label vi/en để edit trực tiếp không cần mở i18n Manager.
/// </summary>
public sealed class LookupItemEditRecord
{
    public int    LookupId   { get; set; }
    public int    TenantId   { get; set; }
    public string LookupCode { get; set; } = "";
    public string ItemCode   { get; set; } = "";
    public string LabelKey   { get; set; } = "";
    public string LabelVi    { get; set; } = "";
    public string LabelEn    { get; set; } = "";
    public int    SortOrder  { get; set; }
    public bool   IsActive   { get; set; } = true;
}
