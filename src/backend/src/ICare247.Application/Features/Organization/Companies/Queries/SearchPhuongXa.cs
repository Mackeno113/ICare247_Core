// File    : SearchPhuongXa.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Query + Handler — tìm phường-xã theo từ khóa (cho cascade địa chỉ, suy Tỉnh).

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Queries;

/// <param name="Term">Từ khóa tìm (theo tên phường-xã hoặc tên tỉnh). Null/rỗng = TOP đầu.</param>
public sealed record SearchPhuongXaQuery(string? Term) : IRequest<IReadOnlyList<LookupOptionDto>>;

public sealed class SearchPhuongXaQueryHandler
    : IRequestHandler<SearchPhuongXaQuery, IReadOnlyList<LookupOptionDto>>
{
    private readonly ICongTyRepository _repo;
    public SearchPhuongXaQueryHandler(ICongTyRepository repo) => _repo = repo;

    public Task<IReadOnlyList<LookupOptionDto>> Handle(SearchPhuongXaQuery r, CancellationToken ct)
        => _repo.SearchPhuongXaAsync(r.Term, ct);
}
