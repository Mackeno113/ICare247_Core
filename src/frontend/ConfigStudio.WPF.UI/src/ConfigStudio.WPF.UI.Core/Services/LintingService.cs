// File    : LintingService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Live linting implementation — debounce 500ms, chạy validation callback, collect issues.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Live linting với debounce 500ms.
/// Flow: NotifyChanged() → chờ 500ms → gọi lintCallback → cập nhật Issues → fire IssuesChanged.
/// </summary>
public sealed class LintingService : ILintingService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<LintIssue>>> _lintCallback;
    private readonly TimeSpan _debounceDelay;

    private CancellationTokenSource? _debounceCts;
    private IReadOnlyList<LintIssue> _issues = [];

    /// <inheritdoc />
    public IReadOnlyList<LintIssue> Issues => _issues;

    /// <inheritdoc />
    public bool HasErrors => _issues.Any(i => i.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public bool HasWarnings => _issues.Any(i => i.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public int IssueCount => _issues.Count;

    /// <inheritdoc />
    public event EventHandler? IssuesChanged;

    /// <param name="lintCallback">Async callback trả về danh sách lint issues.</param>
    /// <param name="debounceDelay">Thời gian debounce. Mặc định 500ms.</param>
    public LintingService(
        Func<CancellationToken, Task<IReadOnlyList<LintIssue>>> lintCallback,
        TimeSpan? debounceDelay = null)
    {
        _lintCallback = lintCallback;
        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(500);
    }

    /// <inheritdoc />
    public void NotifyChanged()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        var cts = _debounceCts;
        _ = DebounceLintAsync(cts);
    }

    /// <inheritdoc />
    public async Task LintNowAsync()
    {
        _debounceCts?.Cancel();

        try
        {
            var results = await _lintCallback(CancellationToken.None);
            UpdateIssues(results);
        }
        catch
        {
            // Lint fail → không crash, giữ issues cũ
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _issues = [];
        IssuesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Chờ debounce rồi lint.</summary>
    private async Task DebounceLintAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_debounceDelay, cts.Token);
            if (cts.IsCancellationRequested) return;

            var results = await _lintCallback(cts.Token);
            if (!cts.IsCancellationRequested)
                UpdateIssues(results);
        }
        catch (OperationCanceledException) { }
        catch
        {
            // Lint callback fail → không crash
        }
    }

    /// <summary>Cập nhật issues và fire event.</summary>
    private void UpdateIssues(IReadOnlyList<LintIssue> newIssues)
    {
        _issues = newIssues;
        IssuesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }
}
