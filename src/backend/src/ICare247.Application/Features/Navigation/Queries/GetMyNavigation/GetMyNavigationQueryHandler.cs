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

    public GetMyNavigationQueryHandler(INavigationRepository repo) => _repo = repo;

    /// <summary>Lấy node menu theo quyền. Sự kiện theo sau: API trả cây cho NavMenu vẽ.</summary>
    public async Task<MeNavigationDto> Handle(GetMyNavigationQuery r, CancellationToken ct)
    {
        var nodes = await _repo.GetForUserAsync(r.UserId, ct);
        return new MeNavigationDto(nodes);
    }
}
