// File    : FieldMetadata.cs
// Module  : Form
// Layer   : Domain
// Purpose : Metadata của một field trong form — cấu hình hiển thị, kiểu dữ liệu, default value.

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Metadata của một field trong form.
/// Maps từ bảng <c>Ui_Field</c>.
/// FieldType lưu dạng string để hỗ trợ thêm type mới qua DB mà không cần deploy lại code.
/// </summary>
public sealed class FieldMetadata
{
    /// <summary>Khóa chính trong bảng Ui_Field.</summary>
    public int FieldId { get; init; }

    /// <summary>Form chứa field này.</summary>
    public int FormId { get; init; }

    /// <summary>Section chứa field — null nếu field không thuộc section nào.</summary>
    public int? SectionId { get; init; }

    /// <summary>Tenant sở hữu.</summary>
    public int TenantId { get; init; }

    /// <summary>
    /// Mã kỹ thuật duy nhất trong form — lấy từ Sys_Column.Column_Code qua Column_Id.
    /// Dùng làm key trong <see cref="ValueObjects.EvaluationContext"/>.
    /// </summary>
    public string FieldCode { get; init; } = string.Empty;

    /// <summary>
    /// Kiểu editor lưu trong DB: 'TextBox', 'TextArea', 'NumberEdit', 'DateEdit',
    /// 'DateTimeEdit', 'CheckBox', 'ComboBox', 'LookupEdit',...
    /// Blazor cần normalize về lowercase trước khi render.
    /// </summary>
    public string FieldType { get; init; } = "TextBox";

    /// <summary>
    /// Nhãn hiển thị cho người dùng.
    /// FormRepository: resolve qua Sys_Resource (COALESCE), fallback về Label_Key.
    /// FieldRepository: resolve qua Sys_Resource (langCode param), fallback về Label_Key.
    /// Không bao giờ là null — worst case trả Label_Key thô.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Cấu hình UI dạng JSON (Ui_Field.Control_Props_Json).
    /// Ví dụ: {"lookupCode":"GENDER"}, {"format":"dd/MM/yyyy","minDate":"01/01/2000"}.
    /// </summary>
    public string? ControlPropsJson { get; init; }

    /// <summary>
    /// Giá trị mặc định dạng JSON string.
    /// Null nếu không có default (bảng Ui_Field hiện chưa có cột Default_Value_Json).
    /// </summary>
    public string? DefaultValueJson { get; init; }

    /// <summary>
    /// Field hiển thị hay ẩn theo cấu hình ban đầu (Ui_Field.Is_Visible).
    /// Có thể thay đổi runtime qua UiDelta SET_VISIBLE.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Field read-only theo cấu hình ban đầu (Ui_Field.Is_ReadOnly).
    /// Có thể thay đổi runtime qua UiDelta SET_READONLY.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Field bắt buộc nhập theo cấu hình tĩnh (Ui_Field.Is_Required — ADR-011).
    /// Có thể override runtime qua UiDelta SET_REQUIRED.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Field có thể tương tác hay không (Ui_Field.Is_Enabled — ADR-012).
    /// false = grayout, không nhập được, không submit giá trị.
    /// Có thể override runtime qua UiDelta SET_ENABLED.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Độ rộng field trong grid layout 4-column (Ui_Field.Col_Span).
    /// 1 = 1/4 width (narrow, default), 2 = 2/4 (half),
    /// 3 = 3/4 width, 4 = full width (textarea, subgrid).
    /// FormRunner dùng trực tiếp để build CSS grid-column: span X.
    /// </summary>
    public byte ColSpan { get; init; } = 1;

    /// <summary>
    /// Phân loại nguồn dữ liệu lookup (Ui_Field.Lookup_Source).
    /// Null    = field thường (TextBox, DateEdit,...).
    /// "static"  = đọc từ Sys_Lookup theo <see cref="LookupCode"/>.
    /// "dynamic" = đọc theo cấu hình trong <see cref="LookupConfig"/>.
    /// </summary>
    public string? LookupSource { get; init; }

    /// <summary>
    /// Mã lookup trong Sys_Lookup — chỉ có giá trị khi <see cref="LookupSource"/> = "static".
    /// Tham chiếu logic (không FK vật lý) để linh hoạt theo tenant.
    /// </summary>
    public string? LookupCode { get; init; }

    /// <summary>
    /// Cấu hình FK lookup động — chỉ có giá trị khi <see cref="LookupSource"/> = "dynamic".
    /// Maps từ bảng Ui_Field_Lookup (quan hệ 1-1).
    /// </summary>
    public FieldLookupConfig? LookupConfig { get; init; }

    /// <summary>Thứ tự hiển thị trong section — tăng dần.</summary>
    public int SortOrder { get; init; }
}
