// File    : AutoSaveService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Debounced auto-save implementation — chờ 3 giây sau thay đổi cuối cùng rồi save.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Auto-save với debounce. Flow:
/// 1. NotifyDirty() → start/reset timer (3 giây)
/// 2. Sau 3 giây không có thay đổi mới → gọi saveCallback
/// 3. Hiện trạng thái: Pending → Saving → Saved / Error
/// </summary>
public sealed class AutoSaveService : IAutoSaveService
{
    private readonly Func<CancellationToken, Task> _saveCallback;
    private readonly TimeSpan _debounceDelay;

    private CancellationTokenSource? _debounceCts;
    private bool _isPaused;

    private AutoSaveStatus _status = AutoSaveStatus.Idle;
    public AutoSaveStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public DateTime? LastSavedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event EventHandler? StatusChanged;

    /// <param name="saveCallback">Async callback thực hiện save — nhận CancellationToken.</param>
    /// <param name="debounceDelay">Thời gian chờ debounce. Mặc định 3 giây.</param>
    public AutoSaveService(
        Func<CancellationToken, Task> saveCallback,
        TimeSpan? debounceDelay = null)
    {
        _saveCallback = saveCallback;
        _debounceDelay = debounceDelay ?? TimeSpan.FromSeconds(3);
    }

    /// <inheritdoc />
    public void NotifyDirty()
    {
        if (_isPaused) return;

        // Reset debounce timer
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        Status = AutoSaveStatus.Pending;
        ErrorMessage = null;

        var cts = _debounceCts;
        _ = DebounceSaveAsync(cts);
    }

    /// <inheritdoc />
    public async Task SaveNowAsync()
    {
        // Hủy debounce pending
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;

        await ExecuteSaveAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public void Pause()
    {
        _isPaused = true;
        _debounceCts?.Cancel();
    }

    /// <inheritdoc />
    public void Resume()
    {
        _isPaused = false;
    }

    /// <summary>Chờ debounce delay rồi save.</summary>
    private async Task DebounceSaveAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_debounceDelay, cts.Token);

            if (cts.IsCancellationRequested || _isPaused) return;

            await ExecuteSaveAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Debounce bị cancel — user tiếp tục sửa, timer sẽ reset
        }
    }

    /// <summary>Thực hiện save thật sự.</summary>
    private async Task ExecuteSaveAsync(CancellationToken ct)
    {
        Status = AutoSaveStatus.Saving;

        try
        {
            await _saveCallback(ct);

            if (!ct.IsCancellationRequested)
            {
                LastSavedAt = DateTime.Now;
                ErrorMessage = null;
                Status = AutoSaveStatus.Saved;

                // Sau 2 giây → chuyển về Idle (UI indicator tự ẩn)
                _ = ResetToIdleAfterDelayAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Status = AutoSaveStatus.Idle;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Status = AutoSaveStatus.Error;
        }
    }

    /// <summary>Sau 2 giây trạng thái Saved → chuyển về Idle.</summary>
    private async Task ResetToIdleAfterDelayAsync()
    {
        await Task.Delay(2000);
        if (Status == AutoSaveStatus.Saved)
            Status = AutoSaveStatus.Idle;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }
}
