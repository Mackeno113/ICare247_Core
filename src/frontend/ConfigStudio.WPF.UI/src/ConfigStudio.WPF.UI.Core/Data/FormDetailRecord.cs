// File    : FormDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO chi tiết form (header) cho FormDetailView.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FormDetailRecord
{
    public int FormId { get; init; }
    public string FormCode { get; init; } = "";
    public string TableName { get; init; } = "";
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string LayoutEngine { get; init; } = "Grid";
    /// <summary>Cách mở form detail: "Popup" (dialog) hoặc "Tab" (tab mới). Từ Ui_Form.Display_Mode.</summary>
    public string DisplayMode { get; init; } = "Popup";
    public int Version { get; init; }
    public string? Checksum { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Description { get; init; }
    /// <summary>Bề rộng tối đa form (px). NULL = mặc định. Từ Ui_Form.Max_Width.</summary>
    public int? MaxWidth { get; init; }
    /// <summary>Số cột lưới (1..4). NULL = mặc định 4. Từ Ui_Form.Form_Columns.</summary>
    public int? FormColumns { get; init; }
    /// <summary>Resource key tiêu đề hiển thị form (đa ngôn ngữ). Từ Ui_Form.Title_Key.</summary>
    public string? TitleKey { get; init; }
}
