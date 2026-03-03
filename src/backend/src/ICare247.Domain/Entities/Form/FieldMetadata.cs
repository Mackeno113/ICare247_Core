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
    /// Mã kỹ thuật duy nhất trong form (Ui_Field.Field_Code).
    /// Dùng làm key trong <see cref="ValueObjects.EvaluationContext"/>.
    /// </summary>
    public string FieldCode { get; init; } = string.Empty;

    /// <summary>
    /// Kiểu dữ liệu field: 'text', 'number', 'date', 'datetime', 'bool',
    /// 'select', 'multiselect', 'textarea', 'file'.
    /// Lưu dạng string — không dùng enum để tránh deploy lại khi thêm type mới.
    /// </summary>
    public string FieldType { get; init; } = "text";

    /// <summary>Nhãn hiển thị cho người dùng (đã localize theo langCode).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Giá trị mặc định dạng JSON string (Ui_Field.Default_Value_Json).
    /// Null nếu không có default. Parse bởi engine khi cần.
    /// </summary>
    public string? DefaultValueJson { get; init; }

    /// <summary>Field bắt buộc nhập hay không (Ui_Field.Is_Required).</summary>
    public bool IsRequired { get; init; }

    /// <summary>Thứ tự hiển thị trong section — tăng dần.</summary>
    public int SortOrder { get; init; }
}
