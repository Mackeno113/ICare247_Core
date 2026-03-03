// File    : RuleMetadata.cs
// Module  : Rule
// Layer   : Domain
// Purpose : Metadata của một rule validation/visibility/required — chứa expression dạng JSON string.

namespace ICare247.Domain.Entities.Rule;

/// <summary>
/// Metadata của một rule.
/// Expression lưu dạng JSON string (<c>Expression_Json</c>) — parse bởi AstEngine khi evaluate.
/// Maps từ bảng <c>Sys_Rule</c>.
/// </summary>
public sealed class RuleMetadata
{
    /// <summary>Khóa chính trong bảng Sys_Rule.</summary>
    public int RuleId { get; init; }

    /// <summary>Form chứa rule này.</summary>
    public int FormId { get; init; }

    /// <summary>Field áp dụng rule (Ui_Field.Field_Code).</summary>
    public string FieldCode { get; init; } = string.Empty;

    /// <summary>Tenant sở hữu.</summary>
    public int TenantId { get; init; }

    /// <summary>
    /// Loại rule: 'validation' | 'visibility' | 'required'.
    /// Dùng string để dễ mở rộng qua DB.
    /// </summary>
    public string RuleType { get; init; } = "validation";

    /// <summary>
    /// Mức độ: 'error' | 'warning' | 'info'.
    /// Chỉ có ý nghĩa khi RuleType = 'validation'.
    /// </summary>
    public string Severity { get; init; } = "error";

    /// <summary>
    /// Expression dạng JSON string (Sys_Rule.Expression_Json).
    /// Parse bởi <c>IAstEngine.Parse()</c> — không parse trong Domain.
    /// </summary>
    public string ExpressionJson { get; init; } = string.Empty;

    /// <summary>
    /// Thông báo lỗi khi rule fail (Sys_Rule.Error_Message).
    /// Có thể chứa placeholder như {FieldName}.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Thứ tự evaluate khi không có dependency constraint.</summary>
    public int SortOrder { get; init; }
}
