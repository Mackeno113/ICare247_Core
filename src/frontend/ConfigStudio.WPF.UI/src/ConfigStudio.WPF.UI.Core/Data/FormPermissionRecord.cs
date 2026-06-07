// File    : FormPermissionRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : Bản ghi phân quyền form theo role (Sys_Permission, Object_Type='Form').

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Quyền của một role trên một form. Map từ <c>Sys_Permission</c>
/// với Object_Type = 'Form', Object_Id = Form_Id.
/// Can_Write = quyền thêm mới / sửa (gồm tính năng "Thêm mới").
/// </summary>
public sealed record FormPermissionRecord(
    int  RoleId,
    bool CanRead,
    bool CanWrite,
    bool CanSubmit
);
