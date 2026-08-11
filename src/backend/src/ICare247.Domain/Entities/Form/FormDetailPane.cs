// File    : FormDetailPane.cs
// Module  : Form
// Layer   : Domain
// Purpose : Một pane chi tiết (master-detail) gắn vào form master — 1 dòng Ui_Form_Detail.
//           Runtime dựng rail/lưới từ danh sách pane này (Spec 30 / rail workspace).

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Mô tả một pane chi tiết của form master (bảng <c>Ui_Form_Detail</c>).
/// <c>Pane_Type='Grid'</c> = lưới CRUD bảng con (dùng <see cref="DetailFormCode"/> +
/// <see cref="ParentKeyColumn"/>); <c>'Timeline'</c> = dòng thời gian đọc từ view/query khai trong
/// <see cref="OptionsJson"/>. Load một lần cùng form rồi cache — không lazy-load.
/// </summary>
public sealed class FormDetailPane
{
    /// <summary>Khóa chính Ui_Form_Detail.Detail_Id.</summary>
    public int DetailId { get; init; }

    /// <summary>Mã định danh pane trong form master (unique theo Form_Id).</summary>
    public string DetailCode { get; init; } = string.Empty;

    /// <summary>Kiểu pane: <c>'Grid'</c> (lưới CRUD) hoặc <c>'Timeline'</c> (dòng thời gian).</summary>
    public string PaneType { get; init; } = "Grid";

    /// <summary>
    /// Form_Code của form CON định nghĩa cột lưới (khi <see cref="PaneType"/>='Grid').
    /// <c>null</c> với Timeline (nguồn khai trong <see cref="OptionsJson"/>).
    /// </summary>
    public string? DetailFormCode { get; init; }

    /// <summary>Cột FK trên bảng con trỏ về master (vd 'NhanVien_Id'). Lọc/gán khi CRUD.</summary>
    public string? ParentKeyColumn { get; init; }

    /// <summary>
    /// <c>'Immediate'</c> = mỗi dòng lưu ngay (tái dùng MasterData CRUD, hồ sơ) ·
    /// <c>'WithMaster'</c> = gom lưu 1 transaction cùng master (chứng từ, Spec 30).
    /// </summary>
    public string SaveMode { get; init; } = "WithMaster";

    /// <summary>Nhãn hiển thị (đã resolve i18n theo lang); fallback <see cref="DetailCode"/>.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Icon mục rail (tùy chọn).</summary>
    public string? Icon { get; init; }

    /// <summary>Nhóm rail (vd 'RELATED' / 'HISTORY') — KEY gom mục cùng nhóm (không hiển thị trực tiếp).</summary>
    public string? GroupKey { get; init; }

    /// <summary>
    /// Nhãn nhóm ĐÃ RESOLVE i18n theo lang — Sys_Resource với key TỰ SINH
    /// <c>{form_code}.railgroup.{group_key}.title</c> (giống khuôn Title_Key); fallback chính
    /// <see cref="GroupKey"/> khi chưa dịch. Runtime hiển thị tiêu đề nhóm bằng trường này.
    /// </summary>
    public string? GroupTitle { get; init; }

    /// <summary>Chế độ nhập lưới: EntryPanel | CellInline | RowPopup (Spec 30 §3.1).</summary>
    public string EditMode { get; init; } = "EntryPanel";

    /// <summary>Cho phép thêm dòng.</summary>
    public bool AllowAdd { get; init; } = true;

    /// <summary>Cho phép xóa dòng.</summary>
    public bool AllowDelete { get; init; } = true;

    /// <summary>Cho phép kéo sắp thứ tự dòng.</summary>
    public bool AllowReorder { get; init; }

    /// <summary>Số dòng tối thiểu (validate; vd đơn hàng ≥1 dòng).</summary>
    public int MinRows { get; init; }

    /// <summary>Cấu hình footer tổng (JSON): [{"field":"ThanhTien","func":"SUM"}].</summary>
    public string? SummaryJson { get; init; }

    /// <summary>Cấu hình phụ (JSON). Timeline: map cột date/title/body/tag → view nguồn.</summary>
    public string? OptionsJson { get; init; }

    /// <summary>Thứ tự pane trên rail.</summary>
    public int OrderNo { get; init; }
}
