// File    : IAuthRepository.cs
// Module  : Auth
// Layer   : Application
// Purpose : Hợp đồng truy cập dữ liệu người dùng (HT_NguoiDung — Data DB) cho luồng xác thực.

using ICare247.Domain.Entities.Auth;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository đọc/ghi dữ liệu xác thực người dùng trong Data DB của tenant hiện tại.
/// Mọi connection lấy qua <see cref="IDataDbConnectionFactory"/> (tenant-aware).
/// </summary>
public interface IAuthRepository
{
    /// <summary>Lấy người dùng theo tên đăng nhập (bỏ qua bản ghi đã xóa). Null nếu không có.</summary>
    Task<NguoiDung?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Lấy người dùng theo Id (bỏ qua bản ghi đã xóa). Null nếu không có.</summary>
    Task<NguoiDung?> GetByIdAsync(long userId, CancellationToken ct = default);

    /// <summary>Danh sách mã vai trò (HT_VaiTro.Ma) đã gán cho người dùng.</summary>
    Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Ghi nhận đăng nhập thành công. Sự kiện theo sau: reset SoLanDangNhapSai = 0,
    /// xóa KhoaDenKhi, cập nhật LanDangNhapCuoi = now (UTC).
    /// </summary>
    Task RecordLoginSuccessAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Ghi nhận đăng nhập sai. Sự kiện theo sau: tăng SoLanDangNhapSai và (nếu vượt ngưỡng)
    /// set KhoaDenKhi để chặn tạm thời.
    /// </summary>
    /// <param name="userId">Người dùng.</param>
    /// <param name="newFailCount">Số lần sai mới (đã tính).</param>
    /// <param name="lockUntilUtc">Thời điểm hết khóa (UTC) hoặc null nếu chưa khóa.</param>
    Task RecordLoginFailureAsync(long userId, int newFailCount, DateTime? lockUntilUtc, CancellationToken ct = default);
}
