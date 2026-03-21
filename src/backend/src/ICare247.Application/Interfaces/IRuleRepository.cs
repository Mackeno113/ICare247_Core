// File    : IRuleRepository.cs
// Module  : Validation
// Layer   : Application
// Purpose : Repository interface cho Val_Rule — đọc rules theo field/form.
//           Sau Migration 003: Field_Id nằm trực tiếp trong Val_Rule.

using ICare247.Domain.Entities.Rule;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Val_Rule</c>.
/// Field_Id nằm trực tiếp trong Val_Rule (sau Migration 003 — bỏ bảng junction Val_Rule_Field).
/// Tenant resolve qua Ui_Field → Ui_Form → Sys_Table.Tenant_Id.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Lấy tất cả rules (active) của một field, sắp xếp theo Order_No.
    /// </summary>
    Task<IReadOnlyList<RuleMetadata>> GetByFieldAsync(
        int formId, string fieldCode, int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả rules (active) của toàn bộ form, group theo FieldCode.
    /// Dùng cho ValidateFormAsync — load 1 lần thay vì N lần.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>>> GetByFormAsync(
        int formId, int tenantId,
        CancellationToken ct = default);
}
