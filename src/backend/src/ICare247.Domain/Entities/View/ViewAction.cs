// File    : ViewAction.cs
// Module  : View
// Layer   : Domain
// Purpose : Một nút toolbar/row của View — bảng Ui_View_Action (CRUD, in, xuất, custom).

namespace ICare247.Domain.Entities.View;

/// <summary>
/// Một hành động (nút) của View (<c>Ui_View_Action</c>). Text i18n đã resolve theo langCode.
/// </summary>
public sealed class ViewAction
{
    public int ActionId { get; init; }

    /// <summary>add | edit | delete | export | print | refresh | column-chooser | custom.</summary>
    public string ActionCode { get; init; } = string.Empty;

    /// <summary>BuiltIn | Export | Print | Navigate | Event | Api.</summary>
    public string ActionType { get; init; } = "BuiltIn";

    /// <summary>Toolbar | Row | Both.</summary>
    public string Scope { get; init; } = "Toolbar";

    public string? LabelKey { get; init; }

    /// <summary>Nhãn nút đã resolve theo langCode.</summary>
    public string? Label { get; init; }

    public string? TooltipKey { get; init; }

    /// <summary>Tooltip đã resolve theo langCode.</summary>
    public string? Tooltip { get; init; }

    public string? ConfirmKey { get; init; }

    /// <summary>Câu xác nhận (vd Xóa) đã resolve theo langCode.</summary>
    public string? Confirm { get; init; }

    /// <summary>Unicode/tên icon — KHÔNG dịch.</summary>
    public string? Icon { get; init; }

    /// <summary>xlsx | xls | csv | pdf | docx (khi Action_Type='Export').</summary>
    public string? ExportFormat { get; init; }

    /// <summary>Grid (client) | Server (template).</summary>
    public string? ExportEngine { get; init; }

    /// <summary>url | event_code | api endpoint | report template.</summary>
    public string? Target { get; init; }

    public bool RequireSelection { get; init; }
    public int OrderNo { get; init; }
}
