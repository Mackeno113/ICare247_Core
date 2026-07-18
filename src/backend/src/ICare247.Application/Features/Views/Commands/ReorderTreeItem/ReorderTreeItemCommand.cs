// File    : ReorderTreeItemCommand.cs
// Module  : Views
// Layer   : Application
// Purpose : Command kéo-thả sắp xếp 1 node trong TreeList (ADR-027).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Commands.ReorderTreeItem;

/// <summary>
/// Chuyển cha (nếu đổi) + renumber ThuTu tập anh em mới theo vị trí thả, rồi recompute cache cây.
/// Trả <c>null</c> nếu View không tồn tại.
/// </summary>
public sealed record ReorderTreeItemCommand(
    string ViewCode,
    int TenantId,
    long Id,
    long? NewParentId,
    long? TargetId,
    string DropPosition
) : IRequest<TreeReorderResult?>;
