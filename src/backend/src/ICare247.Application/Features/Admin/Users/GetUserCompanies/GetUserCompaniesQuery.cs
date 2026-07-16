// File    : GetUserCompaniesQuery.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Query cây công ty + trạng thái quyền của 1 user (tab "Công ty truy cập").

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.GetUserCompanies;

/// <param name="Id">User cần xem phân công công ty.</param>
public sealed record GetUserCompaniesQuery(long Id) : IRequest<IReadOnlyList<UserCompanyNodeDto>>;

public sealed class GetUserCompaniesQueryHandler
    : IRequestHandler<GetUserCompaniesQuery, IReadOnlyList<UserCompanyNodeDto>>
{
    private readonly IUserAdminRepository _repo;

    public GetUserCompaniesQueryHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Đọc cây công ty + cờ quyền. Sự kiện theo sau: FE render cây checkbox WYSIWYG.</summary>
    public Task<IReadOnlyList<UserCompanyNodeDto>> Handle(GetUserCompaniesQuery r, CancellationToken ct)
        => _repo.GetUserCompaniesAsync(r.Id, ct);
}
