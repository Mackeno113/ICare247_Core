// File    : PermissionAdminDtos.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : DTO cho màn Phân quyền (admin): danh sách vai trò + ma trận quyền theo vai trò.

namespace ICare247.Application.Features.Admin.Permissions;

/// <summary>Một vai trò để chọn ở màn Phân quyền.</summary>
/// <param name="Id">HT_VaiTro.Id.</param>
/// <param name="Ma">Mã vai trò (ADMIN, KE_TOAN…).</param>
/// <param name="Ten">Tên vai trò.</param>
/// <param name="MoTa">Mô tả.</param>
/// <param name="LaHeThong">Vai trò hệ thống (không cho xóa).</param>
public sealed record RoleDto(long Id, string Ma, string Ten, string? MoTa, bool LaHeThong);

/// <summary>
/// Một node trong ma trận quyền: toàn bộ cây chức năng + cờ quyền HIỆN TẠI của vai trò
/// được chọn (LEFT JOIN — node chưa cấp = false).
/// </summary>
/// <param name="Id">HT_ChucNang.Id (dùng khi lưu).</param>
/// <param name="Ma">Khóa chức năng.</param>
/// <param name="Ten">Tên hiển thị.</param>
/// <param name="ChaId">Id node cha (null = gốc) — client dựng cây.</param>
/// <param name="Loai">Menu / ManHinh / ChucNangCon.</param>
/// <param name="ThuTu">Thứ tự.</param>
public sealed record RolePermNodeDto(
    long Id, string Ma, string Ten, long? ChaId, string Loai, int ThuTu,
    bool Xem, bool Them, bool Sua, bool Xoa, bool InAn);

/// <summary>1 dòng quyền cần lưu (gửi từ màn Phân quyền).</summary>
public sealed record SavePermItem(long ChucNangId, bool Xem, bool Them, bool Sua, bool Xoa, bool InAn);

/// <summary>
/// Một node cây công ty ở phần "Phạm vi công ty" của màn Phân quyền: toàn bộ cây TC_CongTy
/// + cờ đã gán vào vai trò (HT_VaiTro_CongTy). User thuộc vai trò kế thừa ĐỘNG các công ty này.
/// </summary>
/// <param name="ParentId">CongTy_Cha_Id (null = gốc) — client dựng cây.</param>
/// <param name="DaGan">Công ty đang thuộc phạm vi của vai trò.</param>
public sealed record RoleCompanyNodeDto(long Id, string? Ma, string Ten, long? ParentId, bool DaGan);
