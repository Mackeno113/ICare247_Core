// File    : ViewMetadata.cs
// Module  : View
// Layer   : Domain
// Purpose : Aggregate root cấu hình hiển thị danh sách (Grid/TreeList) — Ui_View + cột + action.

namespace ICare247.Domain.Entities.View;

/// <summary>
/// Aggregate root mô tả một View hiển thị danh sách (Grid/TreeList/Cards) — bảng <c>Ui_View</c>.
/// Tách khỏi form sửa (<c>Ui_Form</c>); trỏ <see cref="EditFormCode"/> để mở popup/tab Thêm/Sửa.
/// Load một lần rồi cache; text i18n đã resolve theo langCode (xem <see cref="Title"/>).
/// </summary>
public sealed class ViewMetadata
{
    /// <summary>Khóa chính Ui_View.View_Id.</summary>
    public int ViewId { get; init; }

    /// <summary>Mã kỹ thuật duy nhất (route <c>/view/{code}</c>).</summary>
    public string ViewCode { get; init; } = string.Empty;

    /// <summary>Grid | TreeList | Cards.</summary>
    public string ViewType { get; init; } = "Grid";

    /// <summary>Bảng nguồn (Sys_Table.Table_Id).</summary>
    public int TableId { get; init; }

    /// <summary>Mã bảng nguồn (join Sys_Table) — dùng làm scope i18n + truy data.</summary>
    public string TableCode { get; init; } = string.Empty;

    /// <summary>Table | View | Sp | Api.</summary>
    public string SourceType { get; init; } = "Table";

    /// <summary>Tên view/SP/SQL/endpoint khi Source_Type ≠ Table.</summary>
    public string? SourceObject { get; init; }

    /// <summary>Resource key tiêu đề màn (để tham chiếu/invalidate).</summary>
    public string? TitleKey { get; init; }

    /// <summary>Tiêu đề màn đã resolve theo langCode (null = không có bản dịch).</summary>
    public string? Title { get; init; }

    /// <summary>Ui_Form Thêm/Sửa (null = chỉ đọc).</summary>
    public int? EditFormId { get; init; }

    /// <summary>Mã form Thêm/Sửa (join Ui_Form) — Blazor mở popup/tab theo code.</summary>
    public string? EditFormCode { get; init; }

    // ── Hành vi lưới ──────────────────────────────────────────
    public int PageSize { get; init; } = 20;
    public bool AllowPaging { get; init; } = true;
    public bool VirtualScroll { get; init; }
    public bool ShowFilterRow { get; init; } = true;
    public bool ShowGroupPanel { get; init; }
    public bool ShowSearchBox { get; init; } = true;
    public bool ShowColumnChooser { get; init; }

    /// <summary>none | single | multiple.</summary>
    public string SelectionMode { get; init; } = "none";
    public bool AllowAdd { get; init; } = true;
    public bool AllowEdit { get; init; } = true;
    public bool AllowDelete { get; init; } = true;

    // ── Export / Print ────────────────────────────────────────
    public bool AllowExport { get; init; } = true;

    /// <summary>'xlsx,csv,pdf,docx'.</summary>
    public string? ExportFormats { get; init; }

    /// <summary>Resource key tên file xuất.</summary>
    public string? ExportFileNameKey { get; init; }

    /// <summary>Tên file xuất đã resolve theo langCode (null = dùng View_Code).</summary>
    public string? ExportFileName { get; init; }
    public bool AllowPrint { get; init; }

    // ── TreeList ──────────────────────────────────────────────
    public string? KeyField { get; init; }
    public string? ParentField { get; init; }
    public int? ExpandLevel { get; init; }

    /// <summary>Cho phép kéo-thả sắp xếp (ADR-027) — chỉ hiệu lực khi <see cref="ViewType"/>=TreeList.</summary>
    public bool AllowReorder { get; init; }

    // ── Panel lọc trái (lưới nâng cao) — chỉ hiệu lực khi SourceType ∈ {Sp, Sql} ──
    /// <summary>Bật panel lọc trái (Ui_View.Filter_Panel_Enabled).</summary>
    public bool FilterPanelEnabled { get; init; }

    /// <summary>left | top — vị trí panel lọc.</summary>
    public string FilterPanelPosition { get; init; } = "left";

    /// <summary>Cho thu gọn panel.</summary>
    public bool FilterCollapsible { get; init; } = true;

    /// <summary>Tự Tìm khi mở (false = chờ người dùng bấm Tìm — mặc định, tránh SP nặng).</summary>
    public bool AutoSearchOnLoad { get; init; }

    public string? SearchLabelKey { get; init; }

    /// <summary>Nhãn nút Tìm đã resolve theo langCode (null = dùng key chung common.filter.search).</summary>
    public string? SearchLabel { get; init; }

    public string? ResetLabelKey { get; init; }

    /// <summary>Nhãn nút Đặt lại đã resolve theo langCode (null = dùng key chung common.filter.reset).</summary>
    public string? ResetLabel { get; init; }

    /// <summary>Phiên bản metadata — dùng làm cache key/slot.</summary>
    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public string? Description { get; init; }

    /// <summary>Cột hiển thị theo thứ tự Order_No.</summary>
    public IReadOnlyList<ViewColumn> Columns { get; init; } = [];

    /// <summary>Nút toolbar/row theo thứ tự Order_No.</summary>
    public IReadOnlyList<ViewAction> Actions { get; init; } = [];

    /// <summary>Control lọc trên panel trái theo thứ tự Order_No (rỗng nếu không dùng panel).</summary>
    public IReadOnlyList<ViewFilter> Filters { get; init; } = [];

    /// <summary>Cờ View dạng cây.</summary>
    public bool IsTreeList =>
        string.Equals(ViewType, "TreeList", StringComparison.OrdinalIgnoreCase);

    /// <summary>Nguồn là Stored Procedure hoặc SQL tùy chỉnh (cho phép panel lọc tham số).</summary>
    public bool IsQuerySource =>
        string.Equals(SourceType, "Sp", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SourceType, "Sql", StringComparison.OrdinalIgnoreCase);

    /// <summary>Panel lọc trái thực sự hiển thị: được bật, nguồn query, và có ≥1 control.</summary>
    public bool HasFilterPanel => FilterPanelEnabled && IsQuerySource && Filters.Count > 0;
}
