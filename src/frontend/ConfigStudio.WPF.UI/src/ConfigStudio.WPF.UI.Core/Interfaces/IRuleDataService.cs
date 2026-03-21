// File    : IRuleDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface CRUD validation rules — Val_Rule (sau Migration 003: không còn Val_Rule_Field).

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD validation rules. Sau Migration 003, Val_Rule chứa Field_Id trực tiếp —
/// không còn bảng junction Val_Rule_Field.
/// </summary>
public interface IRuleDataService
{
    /// <summary>Lấy tất cả rules của 1 field, sắp xếp theo Order_No.</summary>
    Task<IReadOnlyList<RuleItemRecord>> GetRulesByFieldAsync(int fieldId, CancellationToken ct = default);

    /// <summary>Danh sách loại rule từ Val_Rule_Type.</summary>
    Task<IReadOnlyList<RuleTypeRecord>> GetRuleTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Thêm mới hoặc cập nhật 1 rule. FieldId lấy từ <see cref="RuleItemRecord.FieldId"/>.
    /// Trả về Rule_Id sau khi lưu.
    /// </summary>
    Task<int> SaveRuleAsync(RuleItemRecord rule, CancellationToken ct = default);

    /// <summary>Xóa rule khỏi Val_Rule theo Rule_Id.</summary>
    Task DeleteRuleAsync(int ruleId, CancellationToken ct = default);
}
