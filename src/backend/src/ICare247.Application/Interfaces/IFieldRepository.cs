// File    : IFieldRepository.cs
// Module  : Form
// Layer   : Application
// Purpose : Repository interface cho bảng Ui_Field — đọc fields theo form.

using ICare247.Domain.Entities.Form;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho bảng <c>Ui_Field</c>.
/// Query luôn resolve tenant qua Form → Sys_Table.Tenant_Id.
/// </summary>
public interface IFieldRepository
{
    /// <summary>
    /// Lấy tất cả fields (active) của một form, sắp xếp theo Order_No.
    /// </summary>
    Task<IReadOnlyList<FieldMetadata>> GetByFormIdAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Lấy field đơn lẻ theo Field_Id.
    /// </summary>
    Task<FieldMetadata?> GetByIdAsync(int fieldId, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả fields thuộc một section.
    /// </summary>
    Task<IReadOnlyList<FieldMetadata>> GetBySectionIdAsync(int sectionId, int tenantId, CancellationToken ct = default);
}
