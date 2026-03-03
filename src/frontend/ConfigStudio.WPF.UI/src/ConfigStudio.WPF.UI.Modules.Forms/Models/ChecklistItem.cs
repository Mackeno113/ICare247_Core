// File    : ChecklistItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Một mục kiểm tra trong Publish Checklist — hiển thị trạng thái pass/fail.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Trạng thái của một mục kiểm tra.
/// </summary>
public enum CheckStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Warning
}

/// <summary>
/// Một mục kiểm tra trong Publish Checklist.
/// Mỗi item đại diện cho 1 validation check trước khi publish form.
/// </summary>
public sealed class ChecklistItem : BindableBase
{
    public string Description { get; set; } = "";

    private CheckStatus _status = CheckStatus.Pending;
    public CheckStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(StatusIcon));
                RaisePropertyChanged(nameof(StatusColor));
                RaisePropertyChanged(nameof(IsFailed));
            }
        }
    }

    /// <summary>
    /// Chi tiết lỗi khi <see cref="Status"/> = Failed.
    /// </summary>
    private string? _detail;
    public string? Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    /// <summary>
    /// ViewName để navigate khi click "Jump To" — null nếu không có.
    /// </summary>
    public string? JumpToView { get; set; }

    /// <summary>
    /// Navigation params cho "Jump To".
    /// </summary>
    public Dictionary<string, object>? JumpToParams { get; set; }

    public bool IsFailed => Status is CheckStatus.Failed or CheckStatus.Warning;

    /// <summary>
    /// MaterialDesign PackIcon Kind dựa trên status.
    /// </summary>
    public string StatusIcon => Status switch
    {
        CheckStatus.Pending => "ClockOutline",
        CheckStatus.Running => "Loading",
        CheckStatus.Passed => "CheckCircle",
        CheckStatus.Failed => "CloseCircle",
        CheckStatus.Warning => "AlertCircle",
        _ => "Help"
    };

    /// <summary>
    /// Màu icon dựa trên status.
    /// </summary>
    public string StatusColor => Status switch
    {
        CheckStatus.Pending => "#9E9E9E",
        CheckStatus.Running => "#42A5F5",
        CheckStatus.Passed => "#66BB6A",
        CheckStatus.Failed => "#EF5350",
        CheckStatus.Warning => "#FFA726",
        _ => "#9E9E9E"
    };
}
