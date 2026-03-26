// File    : RoleLookupRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO cho danh sách tra cứu Sys_Role khi load quyền form.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi role từ <c>dbo.Sys_Role</c>.
/// Dùng để build <see cref="ConfigStudio.WPF.UI.Modules.Forms.Models.FormPermissionRow"/>.
/// </summary>
public sealed class RoleLookupRecord
{
    public int    RoleId   { get; init; }
    public string RoleCode { get; init; } = "";
    public string RoleName { get; init; } = "";
}
