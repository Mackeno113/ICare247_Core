// File    : FormMasterDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO map 1 dòng Ui_Form_Detail (pane chi tiết master-detail / rail) + record phụ
//           FormLookupItem để chọn form master / form con trên editor. Alias SQL khớp property.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi một pane chi tiết trong <c>dbo.Ui_Form_Detail</c> (Spec 30 / rail workspace, db/106).
/// Dùng cho lưới danh sách + nạp editor + payload lưu.
/// </summary>
public sealed class FormMasterDetailRecord
{
    /// <summary>Khóa chính Detail_Id (0 = bản ghi mới chưa lưu).</summary>
    public int DetailId { get; init; }

    /// <summary>Form master chứa pane này (Ui_Form.Form_Id).</summary>
    public int FormId { get; init; }

    /// <summary>Mã định danh pane trong form master (unique theo Form_Id).</summary>
    public string DetailCode { get; init; } = "";

    /// <summary>Kiểu pane: 'Grid' (lưới CRUD bảng con) | 'Timeline' (dòng thời gian).</summary>
    public string PaneType { get; init; } = "Grid";

    /// <summary>Form con định nghĩa cột lưới (Ui_Form.Form_Id). NULL với Timeline.</summary>
    public int? DetailFormId { get; init; }

    /// <summary>Form_Code form con — join Ui_Form để hiển thị trên lưới.</summary>
    public string? DetailFormCode { get; init; }

    /// <summary>Cột FK bảng con trỏ về master (vd 'NhanVien_Id'). NULL với Timeline.</summary>
    public string? ParentKeyColumn { get; init; }

    /// <summary>'Immediate' (mỗi dòng lưu ngay) | 'WithMaster' (lưu gộp 1 transaction).</summary>
    public string SaveMode { get; init; } = "WithMaster";

    /// <summary>Key i18n nhãn pane / tiêu đề lưới.</summary>
    public string? TitleKey { get; init; }

    /// <summary>Icon mục rail.</summary>
    public string? Icon { get; init; }

    /// <summary>Nhóm rail (vd 'RELATED' / 'HISTORY').</summary>
    public string? GroupKey { get; init; }

    /// <summary>Chế độ nhập lưới: EntryPanel | CellInline | RowPopup.</summary>
    public string EditMode { get; init; } = "EntryPanel";

    /// <summary>Cho phép thêm dòng.</summary>
    public bool AllowAdd { get; init; } = true;

    /// <summary>Cho phép xóa dòng.</summary>
    public bool AllowDelete { get; init; } = true;

    /// <summary>Cho phép kéo sắp thứ tự dòng.</summary>
    public bool AllowReorder { get; init; }

    /// <summary>Số dòng tối thiểu (validate).</summary>
    public int MinRows { get; init; }

    /// <summary>Thứ tự pane trên rail.</summary>
    public int OrderNo { get; init; }

    /// <summary>Pane đang dùng hay đã ẩn.</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Record gọn để chọn form (master hoặc con) trên editor — Form_Id + Form_Code.
/// </summary>
public sealed class FormLookupItem
{
    /// <summary>Ui_Form.Form_Id.</summary>
    public int FormId { get; init; }

    /// <summary>Ui_Form.Form_Code — hiển thị trên dropdown.</summary>
    public string FormCode { get; init; } = "";
}
