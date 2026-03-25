// File    : SectionMetadata.cs
// Module  : Form
// Layer   : Domain
// Purpose : Metadata của một section trong form, chứa danh sách fields thuộc section.

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Metadata của một section (nhóm fields) trong form.
/// Maps từ bảng <c>Ui_Section</c>.
/// </summary>
public sealed class SectionMetadata
{
    /// <summary>Khóa chính trong bảng Ui_Section.</summary>
    public int SectionId { get; init; }

    /// <summary>Form chứa section này.</summary>
    public int FormId { get; init; }

    /// <summary>Tenant sở hữu.</summary>
    public int TenantId { get; init; }

    /// <summary>Mã kỹ thuật duy nhất trong form (Ui_Section.Section_Code).</summary>
    public string SectionCode { get; init; } = string.Empty;

    /// <summary>Tên hiển thị của section.</summary>
    public string SectionName { get; init; } = string.Empty;

    /// <summary>
    /// Tab chứa section này (FK → Ui_Tab.Tab_Id).
    /// Null = section không thuộc tab nào → FormRunner render phẳng (backward compat).
    /// </summary>
    public int? TabId { get; init; }

    /// <summary>Thứ tự hiển thị trong form — tăng dần.</summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Danh sách fields thuộc section này, đã sắp xếp theo Sort_Order.
    /// </summary>
    public IReadOnlyList<FieldMetadata> Fields { get; init; } = [];
}
