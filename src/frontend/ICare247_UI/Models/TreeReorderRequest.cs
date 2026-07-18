// File    : TreeReorderRequest.cs
// Module  : ICare247_UI
// Purpose : Yêu cầu kéo-thả sắp xếp 1 node TreeList (ADR-027). DataView gom (node đang kéo + cha
//           mới suy ra + mốc thả + vị trí) → ViewPage gọi ViewApiService.ReorderAsync + reload.

namespace ICare247_UI.Models;

/// <summary><see cref="DropPosition"/>: "Append" | "Before" | "After" | "Inside".</summary>
public sealed record TreeReorderRequest(
    long Id,
    long? NewParentId,
    long? TargetId,
    string DropPosition);
