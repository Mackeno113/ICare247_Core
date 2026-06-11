// File    : ViewUpsertRequest.cs
// Module  : Data
// Layer   : Core
// Purpose : Payload tạo mới / cập nhật một View kèm toàn bộ cột + action (lưu nguyên khối).

using System.Collections.Generic;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Yêu cầu ghi một View. <see cref="ViewId"/> null/0 = tạo mới; ngược lại = cập nhật.
/// Cột và action được lưu nguyên khối (xóa cũ → ghi lại) trong cùng transaction.
/// </summary>
public sealed class ViewUpsertRequest
{
    public int? ViewId { get; set; }
    public string ViewCode { get; set; } = "";
    public string ViewType { get; set; } = "Grid";
    public int TableId { get; set; }
    public string SourceType { get; set; } = "Table";
    public string? SourceObject { get; set; }
    public string? TitleKey { get; set; }
    public int? EditFormId { get; set; }

    public int PageSize { get; set; } = 20;
    public bool AllowPaging { get; set; } = true;
    public bool VirtualScroll { get; set; }
    public bool ShowFilterRow { get; set; } = true;
    public bool ShowGroupPanel { get; set; }
    public bool ShowSearchBox { get; set; } = true;
    public bool ShowColumnChooser { get; set; }
    public string SelectionMode { get; set; } = "none";
    public bool AllowAdd { get; set; } = true;
    public bool AllowEdit { get; set; } = true;
    public bool AllowDelete { get; set; } = true;

    public bool AllowExport { get; set; } = true;
    public string? ExportFormats { get; set; }
    public string? ExportFileNameKey { get; set; }
    public bool AllowPrint { get; set; }

    public string? KeyField { get; set; }
    public string? ParentField { get; set; }
    public int? ExpandLevel { get; set; }

    // Panel lọc trái (lưới nâng cao)
    public bool FilterPanelEnabled { get; set; }
    public string FilterPanelPosition { get; set; } = "left";
    public bool FilterCollapsible { get; set; } = true;
    public bool AutoSearchOnLoad { get; set; }
    public string? SearchLabelKey { get; set; }
    public string? ResetLabelKey { get; set; }

    public int? DetailViewId { get; set; }
    public string? DefaultFilterJson { get; set; }
    public string? OptionsJson { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    /// <summary>Phiên bản hiện tại để kiểm tra optimistic concurrency khi update.</summary>
    public int Version { get; set; } = 1;

    public List<ViewColumnRecord> Columns { get; set; } = [];
    public List<ViewActionRecord> Actions { get; set; } = [];
    public List<ViewFilterRecord> Filters { get; set; } = [];
}
