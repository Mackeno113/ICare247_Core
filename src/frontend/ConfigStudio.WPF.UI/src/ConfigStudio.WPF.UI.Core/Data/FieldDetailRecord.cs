// File    : FieldDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO field summary cho FormDetailView.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FieldDetailRecord
{
    public int FieldId { get; init; }
    public int OrderNo { get; init; }
    public string ColumnCode { get; init; } = "";
    public string SectionCode { get; init; } = "";
    public string EditorType { get; init; } = "";
    public string LabelKey { get; init; } = "";
    public bool IsVisible { get; init; }
    public bool IsReadOnly { get; init; }
    /// <summary>Field_Code lưu trực tiếp — dùng cho virtual field không có Sys_Column.</summary>
    public string? FieldCode { get; init; }
    /// <summary>Field ảo — không map cột DB (Is_Virtual). Column_Id = NULL.</summary>
    public bool IsVirtual { get; init; }
    public int RuleCount { get; init; }

    /// <summary>
    /// True khi field đã được cấu hình (Ui_Field.Is_Configured) — bật khi user bấm "Lưu Field".
    /// False → field mới tạo tự động, chưa mở ra lưu lần nào.
    /// </summary>
    public bool IsConfigured { get; init; }
}
