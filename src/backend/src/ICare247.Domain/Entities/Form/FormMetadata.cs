// File    : FormMetadata.cs
// Module  : Form
// Layer   : Domain
// Purpose : Aggregate root đại diện cho một form và toàn bộ metadata con của nó.

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Aggregate root chứa toàn bộ metadata của một form (sections, fields).
/// Load một lần từ DB rồi cache — không lazy-load từng phần.
/// </summary>
public sealed class FormMetadata
{
    /// <summary>Khóa chính trong bảng Ui_Form.</summary>
    public int FormId { get; init; }

    /// <summary>Tenant sở hữu form này — bắt buộc trong mọi query.</summary>
    public int TenantId { get; init; }

    /// <summary>Mã kỹ thuật duy nhất trong tenant (Ui_Form.Form_Code).</summary>
    public string FormCode { get; init; } = string.Empty;

    /// <summary>Tên hiển thị cho người dùng.</summary>
    public string FormName { get; init; } = string.Empty;

    /// <summary>Phiên bản metadata — dùng làm cache key.</summary>
    public int Version { get; init; }

    /// <summary>Nền tảng hiển thị: 'web' hoặc 'mobile'.</summary>
    public string Platform { get; init; } = "web";

    /// <summary>
    /// Danh sách sections theo thứ tự Sort_Order.
    /// Mỗi section chứa danh sách fields của section đó.
    /// </summary>
    public IReadOnlyList<SectionMetadata> Sections { get; init; } = [];

    /// <summary>
    /// Toàn bộ fields của form (flat list) — dùng để lookup nhanh theo FieldCode.
    /// Bao gồm cả fields không có section (SectionId = null).
    /// </summary>
    public IReadOnlyList<FieldMetadata> Fields { get; init; } = [];

    /// <summary>
    /// Lookup field theo FieldCode (OrdinalIgnoreCase).
    /// Trả null nếu không tìm thấy — không throw.
    /// </summary>
    public FieldMetadata? GetField(string fieldCode) =>
        Fields.FirstOrDefault(f =>
            string.Equals(f.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
}
