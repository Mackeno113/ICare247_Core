// File    : FieldConfigRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO chi tiết field cho FieldConfigView (edit mode).

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FieldConfigRecord
{
    public int FieldId { get; init; }
    public int FormId { get; init; }
    public int? SectionId { get; init; }
    public int ColumnId { get; init; }
    public string ColumnCode { get; init; } = "";
    /// <summary>
    /// Field_Code lưu trực tiếp trên Ui_Field — dùng cho virtual field không có Sys_Column.
    /// FieldCode hiệu lực = COALESCE(FieldCode, ColumnCode).
    /// </summary>
    public string? FieldCode { get; init; }
    public string SectionCode { get; init; } = "";
    public string EditorType { get; init; } = "TextBox";
    public string LabelKey { get; init; } = "";
    public string? PlaceholderKey { get; init; }
    public string? TooltipKey { get; init; }
    public bool IsVisible { get; init; } = true;
    public bool IsReadOnly { get; init; }
    /// <summary>Không cho phép để trống khi submit. Lưu vào Ui_Field.Is_Required.</summary>
    public bool IsRequired { get; init; }
    /// <summary>
    /// i18n key cho thông báo lỗi khi để trống (chỉ dùng khi IsRequired = true).
    /// Pattern: {tableCode}.val.{fieldCode}.required. Lưu vào Ui_Field.Required_Error_Key.
    /// </summary>
    public string? RequiredErrorKey { get; init; }
    /// <summary>
    /// Khóa field khi form mở ở chế độ Edit (record đã có Id). Lưu vào Ui_Field.Lock_On_Edit (ADR-017).
    /// Dùng cho key/code/audit field: cho nhập lúc tạo mới, không cho sửa khi update.
    /// EffectiveReadOnly = IsReadOnly OR (LockOnEdit AND FormMode=Edit).
    /// </summary>
    public bool LockOnEdit { get; init; }
    /// <summary>
    /// Field UI-only, không map tới cột DB (Ui_Field.Is_Virtual).
    /// Dùng cho helper field như TinhThanh lọc cascading XaPhuong mà không cần lưu DB.
    /// </summary>
    public bool IsVirtual { get; init; }
    public int OrderNo { get; init; }
    public string? ControlPropsJson { get; init; }
    /// <summary>Độ rộng grid 4-column: 1 = 1/4, 2 = 2/4(half), 3 = 3/4, 4 = full.</summary>
    public byte ColSpan { get; init; } = 1;

    /// <summary>null = thường | "static" = Sys_Lookup | "dynamic" = Ui_Field_Lookup</summary>
    public string? LookupSource { get; init; }

    /// <summary>Lookup code trong Sys_Lookup. Chỉ có giá trị khi LookupSource = "static".</summary>
    public string? LookupCode { get; init; }

    public int Version { get; init; }
    public string? Description { get; init; }
}
