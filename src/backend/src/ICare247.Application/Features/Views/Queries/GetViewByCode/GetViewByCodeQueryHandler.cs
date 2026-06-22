// File    : GetViewByCodeQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler cho GetViewByCodeQuery — ủy quyền facade IConfigCache (ADR-014), không chọc repo.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Lấy ViewMetadata qua facade <see cref="IConfigCache"/> (cache-aside L1+L2) rồi map sang
/// <see cref="ViewInfoResponse"/> để LOẠI <c>Lookup_Sql</c> trước khi trả client. Entity trong cache
/// giữ nguyên (filter-options vẫn dùng được). Sự kiện theo sau: controller serialize response trả client.
/// </summary>
public sealed class GetViewByCodeQueryHandler
    : IRequestHandler<GetViewByCodeQuery, ViewInfoResponse?>
{
    private readonly IConfigCache _configCache;

    public GetViewByCodeQueryHandler(IConfigCache configCache)
        => _configCache = configCache;

    public async Task<ViewInfoResponse?> Handle(GetViewByCodeQuery request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(
            request.ViewCode, request.LangCode, request.TenantId, ct);

        return view is null ? null : ViewInfoResponse.FromEntity(view);
    }
}
