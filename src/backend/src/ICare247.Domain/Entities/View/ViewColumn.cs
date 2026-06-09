// File    : ViewColumn.cs
// Module  : View
// Layer   : Domain
// Purpose : Một cột hiển thị của View — bảng Ui_View_Column (render + export + format).

namespace ICare247.Domain.Entities.View;

/// <summary>
/// Một cột của View (<c>Ui_View_Column</c>). Text i18n (<see cref="Caption"/>) đã resolve theo langCode.
/// </summary>
public sealed class ViewColumn
{
    public int ViewColumnId { get; init; }

    /// <summary>Map Sys_Column (null = cột unbound/computed).</summary>
    public int? ColumnId { get; init; }

    /// <summary>FieldName trên control (bắt buộc).</summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>Resource key tiêu đề cột.</summary>
    public string? CaptionKey { get; init; }

    /// <summary>
    /// Tiêu đề cột đã resolve theo langCode. Null = caller fallback Label_Key field → Field_Name.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>Data | Selection | Command | TreeSpin.</summary>
    public string ColumnKind { get; init; } = "Data";

    // ── Hiển thị ──────────────────────────────────────────────
    public string? Width { get; init; }
    public int? MinWidth { get; init; }

    /// <summary>left | center | right.</summary>
    public string? TextAlign { get; init; }
    public string? DisplayFormat { get; init; }

    /// <summary>Text | Html | Image | Link | Badge | Boolean | Template.</summary>
    public string RenderMode { get; init; } = "Text";
    public string? CellTemplateKey { get; init; }
    public bool IsVisible { get; init; } = true;
    public int OrderNo { get; init; }

    /// <summary>none | left | right (frozen).</summary>
    public string? FixedPosition { get; init; }

    // ── Hành vi cột ───────────────────────────────────────────
    public bool AllowSort { get; init; } = true;

    /// <summary>asc | desc.</summary>
    public string? SortOrder { get; init; }
    public int? SortIndex { get; init; }
    public bool AllowFilter { get; init; } = true;
    public bool AllowGroup { get; init; }
    public int? GroupIndex { get; init; }

    /// <summary>count | sum | avg | min | max.</summary>
    public string? SummaryType { get; init; }

    // ── Export (giá trị thuần — KHÔNG xuất HTML) ──────────────
    public bool AllowExport { get; init; } = true;
    public string? ExportFormat { get; init; }
    public string? ExportCaptionKey { get; init; }

    /// <summary>Tiêu đề cột khi xuất, đã resolve theo langCode (null = dùng Caption).</summary>
    public string? ExportCaption { get; init; }

    /// <summary>Điều kiện (Grammar V1 AST) → style ô.</summary>
    public string? StyleRuleJson { get; init; }
}
