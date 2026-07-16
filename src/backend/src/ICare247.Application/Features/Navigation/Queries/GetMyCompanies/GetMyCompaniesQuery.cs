// File    : GetMyCompaniesQuery.cs
// Module  : Navigation
// Layer   : Application
// Purpose : VFILTER-ACTIVE — danh sách công ty user được phép chọn ở company-switcher.
//           Nguồn cho header X-Active-CongTy (token @CongTyID_Active, ADR-030). userId từ claim, KHÔNG nhận client.

using MediatR;

namespace ICare247.Application.Features.Navigation.Queries.GetMyCompanies;

/// <param name="UserId">NguoiDung_Id của user đăng nhập (claim sub).</param>
public sealed record GetMyCompaniesQuery(long UserId) : IRequest<IReadOnlyList<MyCompanyDto>>;

/// <summary>Một node trong cây công ty của switcher (gồm cả node tổ tiên chỉ-để-giữ-cấu-trúc).</summary>
/// <param name="Id">CongTy_Id (bigint) — gửi qua header X-Active-CongTy.</param>
/// <param name="Code">Mã công ty (hiển thị phụ).</param>
/// <param name="Name">Tên công ty.</param>
/// <param name="ParentId">CongTy_Cha_Id — NULL = gốc; FE dựng cây theo cột này.</param>
/// <param name="CanAccess">User có quyền chọn node này không. False = tổ tiên trả kèm để cây
/// không đứt nhánh (hiển thị disabled, không chọn được).</param>
/// <param name="IsDefault">Công ty mặc định của user (LaMacDinh, chỉ ở gán riêng) → chọn sẵn.</param>
public sealed record MyCompanyDto(long Id, string? Code, string Name, long? ParentId, bool CanAccess, bool IsDefault);
