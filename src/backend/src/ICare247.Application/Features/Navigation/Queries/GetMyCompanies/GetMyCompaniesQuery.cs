// File    : GetMyCompaniesQuery.cs
// Module  : Navigation
// Layer   : Application
// Purpose : VFILTER-ACTIVE — danh sách công ty user được phép chọn ở company-switcher.
//           Nguồn cho header X-Active-CongTy (token @CongTyID_Active, ADR-030). userId từ claim, KHÔNG nhận client.

using MediatR;

namespace ICare247.Application.Features.Navigation.Queries.GetMyCompanies;

/// <param name="UserId">NguoiDung_Id của user đăng nhập (claim sub).</param>
public sealed record GetMyCompaniesQuery(long UserId) : IRequest<IReadOnlyList<MyCompanyDto>>;

/// <summary>Một công ty trong danh sách switcher.</summary>
/// <param name="Id">CongTy_Id (bigint) — gửi qua header X-Active-CongTy.</param>
/// <param name="Code">Mã công ty (hiển thị phụ).</param>
/// <param name="Name">Tên công ty.</param>
/// <param name="IsDefault">Công ty mặc định của user (LaMacDinh) → chọn sẵn.</param>
public sealed record MyCompanyDto(long Id, string? Code, string Name, bool IsDefault);
