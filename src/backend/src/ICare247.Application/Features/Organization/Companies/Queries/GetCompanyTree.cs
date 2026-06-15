// File    : GetCompanyTree.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Query + Handler — lấy toàn bộ cây công ty (phẳng) cho lưới TreeList.

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Queries;

/// <summary>Lấy cây công ty của tenant hiện tại (tenant ngầm qua connection Data DB).</summary>
public sealed record GetCompanyTreeQuery : IRequest<IReadOnlyList<CompanyTreeNodeDto>>;

public sealed class GetCompanyTreeQueryHandler
    : IRequestHandler<GetCompanyTreeQuery, IReadOnlyList<CompanyTreeNodeDto>>
{
    private readonly ICongTyRepository _repo;
    public GetCompanyTreeQueryHandler(ICongTyRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CompanyTreeNodeDto>> Handle(GetCompanyTreeQuery r, CancellationToken ct)
        => _repo.GetTreeAsync(ct);
}
