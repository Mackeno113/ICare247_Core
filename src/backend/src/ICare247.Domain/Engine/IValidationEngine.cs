// File    : IValidationEngine.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Interface validate field hoặc toàn bộ form theo rule list với dependency order.

using ICare247.Domain.Engine.Models;
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engine;

/// <summary>
/// Engine validate field/form theo danh sách rules, tôn trọng dependency order từ Sys_Dependency.
/// <para>
/// Flow: Load rules → topological sort theo dependency → evaluate từng rule (IAstEngine) →
/// collect kết quả fail → trả ValidationResponse.
/// </para>
/// </summary>
public interface IValidationEngine
{
    /// <summary>
    /// Validate một field đơn lẻ với giá trị mới.
    /// </summary>
    /// <param name="formId">Form chứa field — dùng để load rules.</param>
    /// <param name="fieldCode">Field_Code cần validate.</param>
    /// <param name="value">Giá trị mới của field cần validate.</param>
    /// <param name="context">
    /// Snapshot giá trị toàn bộ field hiện tại — cần thiết vì rule có thể tham chiếu field khác.
    /// </param>
    /// <param name="tenantId">Tenant — bắt buộc.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="ValidationResponse"/> với danh sách rules fail.
    /// IsValid = false khi có ít nhất một rule severity 'error' fail.
    /// </returns>
    Task<ValidationResponse> ValidateFieldAsync(
        int formId,
        string fieldCode,
        object? value,
        EvaluationContext context,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Validate toàn bộ form — gọi trước khi submit.
    /// </summary>
    /// <param name="formId">Form cần validate.</param>
    /// <param name="context">Snapshot đầy đủ giá trị form tại thời điểm submit.</param>
    /// <param name="tenantId">Tenant — bắt buộc.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Dictionary field → ValidationResponse cho mỗi field có rule.
    /// Fields không có rule hoặc rule đều pass → không có trong kết quả (hoặc IsValid = true).
    /// </returns>
    Task<IReadOnlyDictionary<string, ValidationResponse>> ValidateFormAsync(
        int formId,
        EvaluationContext context,
        int tenantId,
        CancellationToken ct = default);
}
