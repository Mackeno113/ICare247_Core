// File    : FormPermission.cs
// Module  : Permission
// Layer   : Domain
// Purpose : Entity quyền truy cập form theo tenant — phục vụ runtime enforce (xem/thêm/sửa/xóa).

namespace ICare247.Domain.Entities.Permission;

/// <summary>
/// Quyền của một form trong phạm vi tenant — đọc từ <c>Sys_Permission</c>, cache qua <c>IConfigCache</c>.
/// <para>
/// Là <b>config</b> (đổi hiếm) nên được cache theo <c>form + tenant</c>; runtime đọc để enforce
/// nút thêm/sửa/xóa và chặn truy cập màn hình.
/// </para>
/// </summary>
/// <remarks>
/// Khởi tạo bởi <c>ConfigCache</c> (CC-3) sau khi load từ repository permission.
/// Mọi flag mặc định <c>false</c> — không cấu hình = không có quyền (deny-by-default).
/// </remarks>
public sealed record FormPermission
{
    /// <summary>Ui_Form.Form_Id mà quyền này áp dụng.</summary>
    public int FormId { get; init; }

    /// <summary>Tenant sở hữu cấu hình quyền.</summary>
    public int TenantId { get; init; }

    /// <summary>Được phép xem/mở form (truy cập màn hình List + Form).</summary>
    public bool CanView { get; init; }

    /// <summary>Được phép thêm mới bản ghi.</summary>
    public bool CanCreate { get; init; }

    /// <summary>Được phép sửa bản ghi hiện có.</summary>
    public bool CanEdit { get; init; }

    /// <summary>Được phép xóa bản ghi.</summary>
    public bool CanDelete { get; init; }
}
