// File    : IPermissionAdminRepository.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Đọc/ghi cấu hình phân quyền (HT_VaiTro / HT_ChucNang / HT_VaiTro_Quyen) cho màn Phân quyền.

using ICare247.Application.Features.Admin.Permissions;

namespace ICare247.Application.Interfaces;

/// <summary>Repository cấu hình phân quyền (Data DB tenant) — phục vụ màn admin.</summary>
public interface IPermissionAdminRepository
{
    /// <summary>Danh sách vai trò (HT_VaiTro) đang hiệu lực.</summary>
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);

    /// <summary>Toàn bộ cây chức năng + cờ quyền hiện tại của 1 vai trò (node chưa cấp = false).</summary>
    Task<IReadOnlyList<RolePermNodeDto>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default);

    /// <summary>Lưu (upsert) quyền của vai trò theo danh sách node; ghi CreatedBy/UpdatedBy = userId.</summary>
    Task SaveRolePermissionsAsync(long roleId, IReadOnlyList<SavePermItem> items, long userId, CancellationToken ct = default);
}
