// File    : IImpactPreviewService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Interface cho Impact Preview — phân tích ảnh hưởng khi thay đổi field/rule/event.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Service phân tích impact khi thay đổi cấu hình form.
/// Trả về danh sách fields/rules/events bị ảnh hưởng.
/// </summary>
public interface IImpactPreviewService
{
    /// <summary>
    /// Phân tích ảnh hưởng khi thay đổi một field.
    /// Trả về danh sách rules, events, fields khác bị ảnh hưởng.
    /// </summary>
    /// <param name="fieldCode">Field code đang sửa.</param>
    /// <param name="formId">Form chứa field.</param>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImpactAnalysis> AnalyzeFieldImpactAsync(
        string fieldCode, int formId, int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Phân tích ảnh hưởng khi thay đổi expression.
    /// Parse AST để tìm fields được tham chiếu.
    /// </summary>
    /// <param name="expressionJson">Expression JSON hiện tại.</param>
    /// <param name="formId">Form chứa expression.</param>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImpactAnalysis> AnalyzeExpressionImpactAsync(
        string? expressionJson, int formId, int tenantId,
        CancellationToken ct = default);
}

/// <summary>Kết quả phân tích impact.</summary>
public sealed class ImpactAnalysis
{
    /// <summary>Danh sách fields bị ảnh hưởng.</summary>
    public IReadOnlyList<ImpactItem> AffectedFields { get; init; } = [];

    /// <summary>Danh sách rules bị ảnh hưởng.</summary>
    public IReadOnlyList<ImpactItem> AffectedRules { get; init; } = [];

    /// <summary>Danh sách events bị ảnh hưởng.</summary>
    public IReadOnlyList<ImpactItem> AffectedEvents { get; init; } = [];

    /// <summary>Tổng số items bị ảnh hưởng.</summary>
    public int TotalAffected =>
        AffectedFields.Count + AffectedRules.Count + AffectedEvents.Count;

    /// <summary>Có circular dependency không.</summary>
    public bool HasCircularDependency { get; init; }

    /// <summary>Impact analysis rỗng — không có ảnh hưởng.</summary>
    public static ImpactAnalysis Empty { get; } = new();
}

/// <summary>Một item bị ảnh hưởng trong impact analysis.</summary>
/// <param name="Code">Code của item (FieldCode, RuleId, EventId).</param>
/// <param name="Name">Tên hiển thị.</param>
/// <param name="Type">Loại: field | rule | event.</param>
/// <param name="Reason">Lý do bị ảnh hưởng.</param>
public sealed record ImpactItem(
    string Code,
    string Name,
    string Type,
    string Reason);
