// File    : GetMyNavigationQueryHandler.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Handler — delegate sang INavigationRepository, bọc kết quả vào MeNavigationDto.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Navigation.Queries.GetMyNavigation;

public sealed class GetMyNavigationQueryHandler
    : IRequestHandler<GetMyNavigationQuery, MeNavigationDto>
{
    private readonly INavigationRepository _repo;
    private readonly INavigationCache _cache;
    private readonly ITenantContext _tenant;

    public GetMyNavigationQueryHandler(INavigationRepository repo, INavigationCache cache, ITenantContext tenant)
    {
        _repo = repo;
        _cache = cache;
        _tenant = tenant;
    }

    /// <summary>Lấy node menu theo quyền (qua cache tenant+user). Sự kiện theo sau: API trả cây cho NavMenu.</summary>
    public Task<MeNavigationDto> Handle(GetMyNavigationQuery r, CancellationToken ct)
        => _cache.GetOrLoadAsync(_tenant.TenantId, r.UserId,
            async () => new MeNavigationDto(await _repo.GetForUserAsync(r.UserId, ct)));
}
