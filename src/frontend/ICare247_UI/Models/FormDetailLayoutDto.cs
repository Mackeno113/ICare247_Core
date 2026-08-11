// File    : FormDetailLayoutDto.cs
// Module  : ICare247_UI
// Purpose : DTO nhận response từ GET /api/v1/forms/{code}/details — cấu hình master-detail
//           (rail workspace). Mirror của Domain FormDetailLayout/FormDetailPane (NS-MASTERDETAIL).

namespace ICare247_UI.Models;

/// <summary>
/// Cấu hình master-detail của một form: kiểu bố cục + danh sách pane chi tiết.
/// <see cref="Layout"/>='Inline' + <see cref="Panes"/> rỗng = form thường (tenant chưa migrate db/106
/// hoặc form không có chi tiết) → runtime render form phẳng như cũ.
/// </summary>
public sealed class FormDetailLayoutDto
{
    /// <summary>'Inline' = lưới chi tiết chèn trong thân form; 'Rail' = workspace rail điều hướng con.</summary>
    public string Layout { get; set; } = "Inline";

    /// <summary>Danh sách pane theo thứ tự OrderNo. Rỗng = không có chi tiết.</summary>
    public List<FormDetailPaneDto> Panes { get; set; } = [];

    /// <summary>True nếu bố cục là Rail và có ít nhất 1 pane → dựng rail workspace.</summary>
    public bool IsRail =>
        Layout.Equals("Rail", StringComparison.OrdinalIgnoreCase) && Panes.Count > 0;
}

/// <summary>Một pane chi tiết trên rail — 1 dòng Ui_Form_Detail.</summary>
public sealed class FormDetailPaneDto
{
    /// <summary>Khóa chính Ui_Form_Detail.Detail_Id.</summary>
    public int DetailId { get; set; }

    /// <summary>Mã định danh pane trong form master (unique theo Form_Id) — dùng làm key rail.</summary>
    public string DetailCode { get; set; } = "";

    /// <summary>Kiểu pane: 'Grid' (lưới CRUD) hoặc 'Timeline' (dòng thời gian — hoãn phase sau).</summary>
    public string PaneType { get; set; } = "Grid";

    /// <summary>Form_Code của form CON định nghĩa cột lưới (khi PaneType='Grid').</summary>
    public string? DetailFormCode { get; set; }

    /// <summary>Cột FK trên bảng con trỏ về master (vd 'NhanVien_Id'). Lọc/gán khi CRUD.</summary>
    public string? ParentKeyColumn { get; set; }

    /// <summary>'Immediate' = mỗi dòng lưu ngay (hồ sơ NS_) · 'WithMaster' = gom lưu 1 transaction (chứng từ).</summary>
    public string SaveMode { get; set; } = "WithMaster";

    /// <summary>Nhãn hiển thị (đã resolve i18n theo lang); fallback DetailCode.</summary>
    public string Title { get; set; } = "";

    /// <summary>Icon mục rail (tên icon Feather — khớp component &lt;Icon&gt;).</summary>
    public string? Icon { get; set; }

    /// <summary>Nhóm rail (vd 'RELATED' / 'HISTORY') — KEY gom mục cùng nhóm.</summary>
    public string? GroupKey { get; set; }

    /// <summary>Nhãn nhóm đã resolve i18n (Sys_Resource, key tự sinh) theo lang; fallback <see cref="GroupKey"/>.</summary>
    public string? GroupTitle { get; set; }

    /// <summary>Chế độ nhập lưới: EntryPanel | CellInline | RowPopup.</summary>
    public string EditMode { get; set; } = "EntryPanel";

    /// <summary>Cho phép thêm dòng.</summary>
    public bool AllowAdd { get; set; } = true;

    /// <summary>Cho phép xóa dòng.</summary>
    public bool AllowDelete { get; set; } = true;

    /// <summary>Cho phép kéo sắp thứ tự dòng.</summary>
    public bool AllowReorder { get; set; }

    /// <summary>Thứ tự pane trên rail.</summary>
    public int OrderNo { get; set; }
}
