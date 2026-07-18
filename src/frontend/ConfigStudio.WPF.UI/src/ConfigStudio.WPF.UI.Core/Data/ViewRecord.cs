// File    : ViewRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO map header bảng Ui_View — dùng cho lưới danh sách + nạp editor.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi header một View hiển thị danh sách (Grid/TreeList/Cards) trong <c>dbo.Ui_View</c>.
/// Alias SQL phải khớp tên property (View_Id AS ViewId, ...).
/// </summary>
public sealed class ViewRecord
{
    public int ViewId { get; init; }
    public string ViewCode { get; init; } = "";
    public string ViewType { get; init; } = "Grid";          // Grid | TreeList | Cards
    public int TableId { get; init; }
    public string TableCode { get; init; } = "";              // join Sys_Table để hiển thị
    public string SourceType { get; init; } = "Table";        // Table | View | Sp | Api
    public string? SourceObject { get; init; }
    public string? TitleKey { get; init; }
    public int? EditFormId { get; init; }

    // Hành vi lưới
    public int PageSize { get; init; } = 20;
    public bool AllowPaging { get; init; } = true;
    public bool VirtualScroll { get; init; }
    public bool ShowFilterRow { get; init; } = true;
    public bool ShowGroupPanel { get; init; }
    public bool ShowSearchBox { get; init; } = true;
    public bool ShowColumnChooser { get; init; }
    public string SelectionMode { get; init; } = "none";      // none | single | multiple
    public bool AllowAdd { get; init; } = true;
    public bool AllowEdit { get; init; } = true;
    public bool AllowDelete { get; init; } = true;

    // Export / Print
    public bool AllowExport { get; init; } = true;
    public string? ExportFormats { get; init; }               // 'xlsx,csv,pdf,docx'
    public string? ExportFileNameKey { get; init; }
    public bool AllowPrint { get; init; }

    // TreeList
    public string? KeyField { get; init; }
    public string? ParentField { get; init; }
    public int? ExpandLevel { get; init; }
    public bool AllowReorder { get; init; }

    // Panel lọc trái (lưới nâng cao)
    public bool FilterPanelEnabled { get; init; }
    public string FilterPanelPosition { get; init; } = "left";   // left | top
    public bool FilterCollapsible { get; init; } = true;
    public bool AutoSearchOnLoad { get; init; }
    public string? SearchLabelKey { get; init; }
    public string? ResetLabelKey { get; init; }

    // Master-detail / lọc mặc định
    public int? DetailViewId { get; init; }
    public string? DefaultFilterJson { get; init; }

    public string? OptionsJson { get; init; }
    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? Description { get; init; }
}
