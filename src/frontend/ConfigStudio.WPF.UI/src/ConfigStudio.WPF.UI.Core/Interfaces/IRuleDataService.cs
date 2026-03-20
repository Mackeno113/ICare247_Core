// File    : IRuleDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface CRUD validation rules cho ValidationRuleEditorView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD validation rules (Val_Rule + Val_Rule_Field).
/// </summary>
public interface IRuleDataService
{
    Task<IReadOnlyList<RuleItemRecord>> GetRulesByFieldAsync(int fieldId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleTypeRecord>> GetRuleTypesAsync(CancellationToken ct = default);
    Task<int> SaveRuleAsync(RuleItemRecord rule, int fieldId, CancellationToken ct = default);
    Task DeleteRuleFieldAsync(int fieldId, int ruleId, CancellationToken ct = default);
}
