// File    : ReorderTreeItemCommandHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler — nạp ViewMetadata qua facade rồi kéo-thả sắp xếp qua IViewRepository.ReorderAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Commands.ReorderTreeItem;

public sealed class ReorderTreeItemCommandHandler
    : IRequestHandler<ReorderTreeItemCommand, TreeReorderResult?>
{
    private readonly IConfigCache _configCache;
    private readonly IViewRepository _viewRepository;

    public ReorderTreeItemCommandHandler(IConfigCache configCache, IViewRepository viewRepository)
    {
        _configCache = configCache;
        _viewRepository = viewRepository;
    }

    public async Task<TreeReorderResult?> Handle(ReorderTreeItemCommand request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(request.ViewCode, "vi", request.TenantId, ct);
        if (view is null)
            return null;

        return await _viewRepository.ReorderAsync(
            view, request.Id, request.NewParentId, request.TargetId, request.DropPosition, ct);
    }
}
