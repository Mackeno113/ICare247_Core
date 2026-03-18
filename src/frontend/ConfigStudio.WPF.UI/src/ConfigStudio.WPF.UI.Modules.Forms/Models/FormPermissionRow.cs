// File    : FormPermissionRow.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model cho một dòng trong DataGrid quyền form — hỗ trợ inline edit checkbox.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Một dòng quyền truy cập form theo role.
/// Dùng BindableBase để checkbox inline trong DataGrid tự notify.
/// </summary>
public sealed class FormPermissionRow : BindableBase
{
    public int RoleId { get; set; }

    private string _roleName = "";
    public string RoleName
    {
        get => _roleName;
        set => SetProperty(ref _roleName, value);
    }

    private string _roleDescription = "";
    /// <summary>Mô tả ngắn về role — hiển thị tooltip.</summary>
    public string RoleDescription
    {
        get => _roleDescription;
        set => SetProperty(ref _roleDescription, value);
    }

    private bool _canRead;
    public bool CanRead
    {
        get => _canRead;
        set => SetProperty(ref _canRead, value);
    }

    private bool _canWrite;
    public bool CanWrite
    {
        get => _canWrite;
        set => SetProperty(ref _canWrite, value);
    }

    private bool _canSubmit;
    public bool CanSubmit
    {
        get => _canSubmit;
        set => SetProperty(ref _canSubmit, value);
    }
}
