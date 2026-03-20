// File    : ILintingService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Interface cho Live Linting service — validate form/field metadata realtime.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Service kiểm tra lỗi cấu hình realtime (live linting).
/// Debounce validate sau mỗi thay đổi, trả về danh sách lint issues.
/// </summary>
public interface ILintingService : IDisposable
{
    /// <summary>Danh sách lint issues hiện tại.</summary>
    IReadOnlyList<LintIssue> Issues { get; }

    /// <summary>Có lỗi (Error severity) không.</summary>
    bool HasErrors { get; }

    /// <summary>Có warning không.</summary>
    bool HasWarnings { get; }

    /// <summary>Tổng số issues.</summary>
    int IssueCount { get; }

    /// <summary>
    /// Đánh dấu cần re-lint. Debounce 500ms.
    /// </summary>
    void NotifyChanged();

    /// <summary>Lint ngay lập tức (bỏ qua debounce).</summary>
    Task LintNowAsync();

    /// <summary>Xóa tất cả issues.</summary>
    void Clear();

    /// <summary>Event khi danh sách issues thay đổi.</summary>
    event EventHandler? IssuesChanged;
}

/// <summary>Một issue phát hiện bởi linting.</summary>
/// <param name="Code">Mã lỗi: LINT001, LINT002, ...</param>
/// <param name="Severity">error | warning | info.</param>
/// <param name="Message">Mô tả lỗi.</param>
/// <param name="Source">Nguồn lỗi: field code, section code, expression, etc.</param>
/// <param name="Category">Phân loại: naming | type | expression | dependency | required.</param>
public sealed record LintIssue(
    string Code,
    string Severity,
    string Message,
    string? Source = null,
    string? Category = null);
