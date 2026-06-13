// File    : NguoiDung.cs
// Module  : Auth
// Layer   : Domain
// Purpose : Entity người dùng hệ thống (HT_NguoiDung — Data DB) phục vụ xác thực.
//           Chỉ giữ các cột liên quan đăng nhập/khóa/2FA; hồ sơ (họ tên, email) lấy
//           qua NhanVien ở đợt NS_.

namespace ICare247.Domain.Entities.Auth;

/// <summary>
/// Người dùng hệ thống — ánh xạ bảng <c>dbo.HT_NguoiDung</c> (Data DB của tenant).
/// Dùng cho luồng đăng nhập: verify mật khẩu PBKDF2, kiểm trạng thái/khóa/hết hạn.
/// </summary>
public sealed class NguoiDung
{
    /// <summary>Khóa chính.</summary>
    public long Id { get; init; }

    /// <summary>Mã người dùng (bất biến nghiệp vụ).</summary>
    public string Ma { get; init; } = "";

    /// <summary>Tên đăng nhập — duy nhất trong tenant.</summary>
    public string TenDangNhap { get; init; } = "";

    /// <summary>Loại tài khoản: Local/AD/SSO/Portal. Chỉ Local mới verify mật khẩu nội bộ.</summary>
    public string LoaiTaiKhoan { get; init; } = "Local";

    /// <summary>Hash mật khẩu PBKDF2 (Identity v3). NULL khi AD/SSO.</summary>
    public string? MatKhauHash { get; init; }

    /// <summary>Công ty mặc định (phạm vi dữ liệu khi đăng nhập).</summary>
    public long? CongTyMacDinh_Id { get; init; }

    /// <summary>Trạng thái: HoatDong/NgungHoatDong/... (mã bất biến, label i18n ở Config DB).</summary>
    public string TrangThai { get; init; } = "HoatDong";

    /// <summary>True nếu là tài khoản quản trị (toàn quyền).</summary>
    public bool LaQuanTri { get; init; }

    /// <summary>Hạn dùng tài khoản (UTC). NULL = không giới hạn.</summary>
    public DateTime? HetHanTaiKhoan { get; init; }

    /// <summary>Hình thức 2FA: None/App/Email/SMS.</summary>
    public string HinhThuc2FA { get; init; } = "None";

    /// <summary>Số lần đăng nhập sai liên tiếp (reset khi thành công).</summary>
    public int SoLanDangNhapSai { get; init; }

    /// <summary>Khóa đăng nhập đến thời điểm này (UTC). NULL = không bị khóa.</summary>
    public DateTime? KhoaDenKhi { get; init; }

    /// <summary>Bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp.</summary>
    public bool DoiMatKhauLanSau { get; init; }

    /// <summary>True nếu bản ghi đã xóa mềm.</summary>
    public bool IsDeleted { get; init; }
}
