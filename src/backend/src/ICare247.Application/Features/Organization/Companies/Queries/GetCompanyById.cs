// File    : GetCompanyById.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Query + Handler — lấy chi tiết 1 công ty cho form Sửa.

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Queries;

/// <param name="Id">Khóa công ty.</param>
public sealed record GetCompanyByIdQuery(long Id) : IRequest<CompanyDetailDto?>;

public sealed class GetCompanyByIdQueryHandler
    : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto?>
{
    private readonly ICongTyRepository _repo;
    public GetCompanyByIdQueryHandler(ICongTyRepository repo) => _repo = repo;

    public Task<CompanyDetailDto?> Handle(GetCompanyByIdQuery r, CancellationToken ct)
        => _repo.GetByIdAsync(r.Id, ct);
}
