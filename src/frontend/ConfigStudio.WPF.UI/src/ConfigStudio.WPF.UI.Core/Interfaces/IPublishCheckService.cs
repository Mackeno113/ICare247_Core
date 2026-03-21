// File    : IPublishCheckService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Interface kiểm tra tính hợp lệ của form trước khi publish.

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Kết quả của một check đơn lẻ trong publish checklist.
/// </summary>
/// <param name="Passed">true = pass, false = fail.</param>
/// <param name="IsWarning">true = warning (không block publish), false = error.</param>
/// <param name="Detail">Chi tiết lỗi hoặc null nếu pass.</param>
public sealed record CheckResult(bool Passed, bool IsWarning = false, string? Detail = null);

/// <summary>
/// Service kiểm tra tính hợp lệ của form trước khi publish.
/// Thực hiện qua Dapper trực tiếp vào DB.
/// </summary>
public interface IPublishCheckService
{
    /// <summary>CHECK 1: Tất cả field có Label_Key hợp lệ (không null/empty).</summary>
    Task<CheckResult> CheckLabelKeysAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 2: Tất cả Expression_Json parse được (JSON hợp lệ).</summary>
    Task<CheckResult> CheckExpressionsParseAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 3: Tất cả function trong expression có trong Gram_Function whitelist.</summary>
    Task<CheckResult> CheckFunctionWhitelistAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 4: Tất cả operator trong expression có trong Gram_Operator whitelist.</summary>
    Task<CheckResult> CheckOperatorWhitelistAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 5: Return type của rule expression = Boolean.</summary>
    Task<CheckResult> CheckRuleReturnTypeAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 6: Return type của calculate = compatible với target field.</summary>
    Task<CheckResult> CheckCalculateReturnTypeAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 7: Không có circular dependency giữa fields/rules/events.</summary>
    Task<CheckResult> CheckCircularDependencyAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 8: Tất cả AST depth ≤ 20.</summary>
    Task<CheckResult> CheckAstDepthAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 9: Tất cả Error_Key có bản dịch đầy đủ cho mọi ngôn ngữ active.</summary>
    Task<CheckResult> CheckI18nCompletenessAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 10: Tất cả CallAPI action có URL format hợp lệ.</summary>
    Task<CheckResult> CheckCallApiUrlsAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>CHECK 11: Sys_Dependency có entries cho form (graph đã được build).</summary>
    Task<CheckResult> CheckDependencyGraphAsync(int formId, int tenantId, CancellationToken ct = default);
}
