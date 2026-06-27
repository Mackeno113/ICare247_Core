// File    : IMeCompanyRepository.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Đọc công ty mà user đăng nhập được phép chọn (company-switcher) — từ Data DB tenant.

using ICare247.Application.Features.Navigation.Queries.GetMyCompanies;

namespace ICare247.Application.Interfaces;

/// <summary>Truy vấn công ty theo user (HT_NguoiDung_CongTy ⨝ TC_CongTy, fallback mọi công ty active).</summary>
public interface IMeCompanyRepository
{
    /// <summary>Danh sách công ty user được phân công; rỗng phân công/chưa có bảng → mọi công ty active.</summary>
    Task<IReadOnlyList<MyCompanyDto>> GetForUserAsync(long userId, CancellationToken ct = default);
}
