// File    : GetResourceTranslationsQuery.cs
// Module  : Resources
// Layer   : Application
// Purpose : Lấy bản dịch của 1 key theo MỌI ngôn ngữ (Lang_Code→value) — cho màn sửa bản dịch inline.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Resources.Queries.GetResourceTranslations;

/// <summary>Bản dịch của 1 key theo mọi ngôn ngữ.</summary>
public sealed record GetResourceTranslationsQuery(string Key)
    : IRequest<IReadOnlyDictionary<string, string>>;

public sealed class GetResourceTranslationsQueryHandler
    : IRequestHandler<GetResourceTranslationsQuery, IReadOnlyDictionary<string, string>>
{
    private readonly IResourceRepository _resources;

    public GetResourceTranslationsQueryHandler(IResourceRepository resources) => _resources = resources;

    public async Task<IReadOnlyDictionary<string, string>> Handle(GetResourceTranslationsQuery r, CancellationToken ct)
        => string.IsNullOrWhiteSpace(r.Key)
            ? new Dictionary<string, string>()
            : await _resources.GetByKeyAllLangsAsync(r.Key, ct);
}
