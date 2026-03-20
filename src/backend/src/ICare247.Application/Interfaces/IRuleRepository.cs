// File    : IRuleRepository.cs
// Module  : Validation
// Layer   : Application
// Purpose : Repository interface cho Val_Rule + Val_Rule_Field — đọc rules theo field/form.

using ICare247.Domain.Entities.Rule;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Val_Rule</c> + <c>Val_Rule_Field</c>.
/// Tenant resolve qua Form → Sys_Table.Tenant_Id.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Lấy tất cả rules (active) của một field, sắp xếp theo Order_No.
    /// JOIN Val_Rule_Field → Val_Rule.
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
