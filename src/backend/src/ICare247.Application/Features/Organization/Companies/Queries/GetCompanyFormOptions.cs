// File    : GetCompanyFormOptions.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Query + Handler — bộ option tham chiếu nhỏ cho form (cấp công ty + ngân hàng).

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Queries;

public sealed record GetCompanyFormOptionsQuery : IRequest<CompanyFormOptionsDto>;

public sealed class GetCompanyFormOptionsQueryHandler
    : IRequestHandler<GetCompanyFormOptionsQuery, CompanyFormOptionsDto>
{
    private readonly ICongTyRepository _repo;
    public GetCompanyFormOptionsQueryHandler(ICongTyRepository repo) => _repo = repo;

    public async Task<CompanyFormOptionsDto> Handle(GetCompanyFormOptionsQuery r, CancellationToken ct)
        => new(
            await _repo.GetCapCongTyOptionsAsync(ct),
            await _repo.GetNganHangOptionsAsync(ct));
}
