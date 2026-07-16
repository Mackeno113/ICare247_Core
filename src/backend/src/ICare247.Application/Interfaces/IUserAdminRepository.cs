// File    : IUserAdminRepository.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Hợp đồng truy cập dữ liệu màn Người dùng (Data DB tenant): CRUD HT_NguoiDung,
//           gán vai trò (HT_NguoiDung_VaiTro), gán công ty riêng (HT_NguoiDung_CongTy).

using ICare247.Application.Features.Admin.Users;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc/ghi người dùng + phân công vai trò/công ty trên Data DB tenant.</summary>
public interface IUserAdminRepository
{
    /// <summary>Danh sách người dùng (chưa xóa) kèm tên vai trò gộp cho lưới.</summary>
    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>Chi tiết user + toàn bộ vai trò với cờ đã gán. Null = không tồn tại.</summary>
    Task<UserDetailDto?> GetUserDetailAsync(long id, CancellationToken ct = default);

    /// <summary>Kiểm tra trùng Ma / TenDangNhap (loại trừ chính mình khi sửa).</summary>
    Task<(bool MaTrung, bool TenDangNhapTrung)> CheckDuplicateAsync(
        string ma, string tenDangNhap, long? excludeId, CancellationToken ct = default);

    /// <summary>Tạo user mới (LoaiTaiKhoan='Local', MatKhauHash đã băm sẵn). Trả Id mới.</summary>
    Task<long> CreateUserAsync(
        string ma, string tenDangNhap, string matKhauHash, string trangThai, bool laQuanTri,
        bool kichHoatMobile, DateTime? hetHanTaiKhoan, bool doiMatKhauLanSau, long actorId,
        CancellationToken ct = default);

    /// <summary>Cập nhật thông tin user (không đụng mật khẩu). False = user không tồn tại.</summary>
    Task<bool> UpdateUserAsync(
        long id, string ma, string tenDangNhap, string trangThai, bool laQuanTri,
        bool kichHoatMobile, DateTime? hetHanTaiKhoan, bool doiMatKhauLanSau, long actorId,
        CancellationToken ct = default);

    /// <summary>Đặt lại mật khẩu (hash sẵn) + cờ bắt đổi lần sau. False = user không tồn tại.</summary>
    Task<bool> ResetPasswordAsync(
        long id, string matKhauHash, bool doiMatKhauLanSau, long actorId, CancellationToken ct = default);

    /// <summary>Xóa mềm user (IsDeleted=1). False = user không tồn tại.</summary>
    Task<bool> DeleteUserAsync(long id, long actorId, CancellationToken ct = default);

    /// <summary>Ghi lại danh sách vai trò của user: thêm thiếu, xóa mềm thừa (1 transaction).</summary>
    Task SaveUserRolesAsync(long id, IReadOnlyList<long> roleIds, long actorId, CancellationToken ct = default);

    /// <summary>Toàn bộ cây công ty + trạng thái quyền (gán riêng / theo vai trò / mặc định) của user.</summary>
    Task<IReadOnlyList<UserCompanyNodeDto>> GetUserCompaniesAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Ghi lại tập công ty GÁN RIÊNG của user (thêm thiếu, xóa mềm thừa) + đặt công ty mặc định
    /// (LaMacDinh duy nhất 1 dòng), 1 transaction. Không đụng quyền kế thừa theo vai trò.
    /// </summary>
    Task SaveUserCompaniesAsync(
        long id, IReadOnlyList<long> congTyIds, long? macDinhCongTyId, long actorId,
        CancellationToken ct = default);
}
