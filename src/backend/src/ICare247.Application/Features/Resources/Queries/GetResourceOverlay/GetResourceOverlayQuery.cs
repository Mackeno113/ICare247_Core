// File    : GetResourceOverlayQuery.cs
// Module  : Resources
// Layer   : Application
// Purpose : Lấy toàn bộ overlay i18n (key→value) của 1 ngôn ngữ (lọc tùy chọn theo prefix) — cho
//           LocalizationService gộp bản dịch DB lên trên lớp tĩnh JSON.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Resources.Queries.GetResourceOverlay;

/// <summary>Overlay i18n của một ngôn ngữ; <paramref name="KeyPrefix"/> null = mọi key.</summary>
public sealed record GetResourceOverlayQuery(string LangCode, string? KeyPrefix = null)
    : IRequest<IReadOnlyDictionary<string, string>>;

public sealed class GetResourceOverlayQueryHandler
    : IRequestHandler<GetResourceOverlayQuery, IReadOnlyDictionary<string, string>>
{
    private readonly IResourceRepository _resources;

    public GetResourceOverlayQueryHandler(IResourceRepository resources) => _resources = resources;

    public async Task<IReadOnlyDictionary<string, string>> Handle(GetResourceOverlayQuery r, CancellationToken ct)
        => await _resources.GetOverlayAsync(r.LangCode, r.KeyPrefix, ct);
}
