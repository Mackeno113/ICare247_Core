// File    : GetMyCompaniesQueryHandler.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Handler — delegate sang IMeCompanyRepository (đọc công ty user được phân công ở Data DB).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Navigation.Queries.GetMyCompanies;

public sealed class GetMyCompaniesQueryHandler
    : IRequestHandler<GetMyCompaniesQuery, IReadOnlyList<MyCompanyDto>>
{
    private readonly IMeCompanyRepository _repo;

    public GetMyCompaniesQueryHandler(IMeCompanyRepository repo) => _repo = repo;

    /// <summary>Lấy công ty user được chọn. Sự kiện theo sau: FE đổ vào company-switcher.</summary>
    public Task<IReadOnlyList<MyCompanyDto>> Handle(GetMyCompaniesQuery r, CancellationToken ct)
        => _repo.GetForUserAsync(r.UserId, ct);
}
