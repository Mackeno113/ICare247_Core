// File    : UserAdminDtos.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : DTO cho màn Người dùng (admin): danh sách, chi tiết + gán vai trò, cây công ty truy cập.

namespace ICare247.Application.Features.Admin.Users;

/// <summary>Một dòng trên lưới Người dùng.</summary>
/// <param name="Id">HT_NguoiDung.Id.</param>
/// <param name="Ma">Mã người dùng.</param>
/// <param name="TenDangNhap">Username đăng nhập.</param>
/// <param name="LoaiTaiKhoan">Local/AD/SSO/Portal (label qua i18n shell).</param>
/// <param name="TrangThai">HoatDong/TamKhoa/NgungHoatDong (label qua i18n shell).</param>
/// <param name="LaQuanTri">Super-admin tenant (bỏ qua check quyền).</param>
/// <param name="KichHoatMobile">Được đăng nhập app mobile.</param>
/// <param name="HetHanTaiKhoan">Hạn dùng tài khoản (null = không hạn).</param>
/// <param name="LanDangNhapCuoi">Lần đăng nhập gần nhất.</param>
/// <param name="VaiTro">Tên các vai trò đã gán, nối bằng ", " (hiển thị lưới).</param>
public sealed record UserListItemDto(
    long Id, string Ma, string TenDangNhap, string LoaiTaiKhoan, string TrangThai,
    bool LaQuanTri, bool KichHoatMobile, DateTime? HetHanTaiKhoan, DateTime? LanDangNhapCuoi,
    string? VaiTro);

/// <summary>Chi tiết 1 người dùng cho tab Thông tin + tab Vai trò (kèm toàn bộ vai trò để tick).</summary>
public sealed record UserDetailDto(
    long Id, string Ma, string TenDangNhap, string LoaiTaiKhoan, string TrangThai,
    bool LaQuanTri, bool KichHoatMobile, DateTime? HetHanTaiKhoan, bool DoiMatKhauLanSau,
    IReadOnlyList<UserRoleItemDto> VaiTro);

/// <summary>1 vai trò trong tab Vai trò của màn Người dùng (toàn bộ vai trò + cờ đã gán).</summary>
/// <param name="DaGan">User đang thuộc vai trò này (HT_NguoiDung_VaiTro).</param>
public sealed record UserRoleItemDto(long Id, string Ma, string Ten, string? MoTa, bool DaGan);

/// <summary>
/// Một node cây công ty ở tab "Công ty truy cập": toàn bộ cây TC_CongTy + trạng thái quyền
/// hiện tại của user. Quyền hiệu lực = GanRieng ∪ TheoVaiTro (kế thừa động, chỉ hiển thị).
/// </summary>
/// <param name="GanRieng">Có dòng gán trực tiếp HT_NguoiDung_CongTy (admin tick/bỏ tick được).</param>
/// <param name="TheoVaiTro">Được kế thừa từ ít nhất 1 vai trò (readonly — sửa ở màn vai trò).</param>
/// <param name="LaMacDinh">Công ty mặc định khi đăng nhập (chỉ có ở gán riêng).</param>
public sealed record UserCompanyNodeDto(
    long Id, string? Ma, string Ten, long? ParentId,
    bool GanRieng, bool TheoVaiTro, bool LaMacDinh);
