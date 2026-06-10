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

    public int? TenantId { get; init; }

    /// <summary>Phiên bản metadata — dùng làm cache key/slot.</summary>
    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public string? Description { get; init; }

    /// <summary>Cột hiển thị theo thứ tự Order_No.</summary>
    public IReadOnlyList<ViewColumn> Columns { get; init; } = [];

    /// <summary>Nút toolbar/row theo thứ tự Order_No.</summary>
    public IReadOnlyList<ViewAction> Actions { get; init; } = [];

    /// <summary>Cờ View dạng cây.</summary>
    public bool IsTreeList =>
        string.Equals(ViewType, "TreeList", StringComparison.OrdinalIgnoreCase);
}
