// File    : IUserNotifier.cs
// Module  : Core
// Layer   : Abstraction
// Purpose : Cổng phát thông báo cho NGƯỜI DÙNG thấy trên UI (banner/toast ở shell).
//           Khác IAppLogger (ghi file, âm thầm): IUserNotifier hiện lỗi/cảnh báo NGAY
//           trên màn hình để user biết mà xử lý — vá lỗ hổng "WPF nuốt lỗi im lặng".

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>Mức độ của thông báo hiện cho người dùng — quyết định màu banner.</summary>
public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Một thông báo hiện cho người dùng. <paramref name="Detail"/> là chi tiết kỹ thuật
/// (vd message của exception) — hiện phụ dưới dòng chính, hỗ trợ debug.
/// </summary>
/// <param name="Message">Nội dung chính (tiếng Việt, dễ hiểu).</param>
/// <param name="Severity">Mức độ → màu banner.</param>
/// <param name="Detail">Chi tiết kỹ thuật tùy chọn (message lỗi gốc).</param>
public sealed record UserNotification(
    string Message,
    NotificationSeverity Severity = NotificationSeverity.Info,
    string? Detail = null);

/// <summary>
/// Cổng phát thông báo cho người dùng. Implementation (UserNotifier) marshal về UI thread
/// và raise <see cref="Raised"/> để shell (ShellViewModel) hiện banner.
/// Đây là "nơi báo lỗi" dùng chung cho toàn app — mọi catch trước đây nuốt lỗi im lặng
/// nên gọi <see cref="NotifyError"/> để user thấy được.
/// </summary>
public interface IUserNotifier
{
    /// <summary>Phát sinh khi có thông báo mới — shell subscribe để hiện banner.</summary>
    event Action<UserNotification>? Raised;

    /// <summary>Phát một thông báo bất kỳ (info/success/warning/error).</summary>
    void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info, string? detail = null);

    /// <summary>
    /// Phát thông báo lỗi. Nếu có <paramref name="ex"/> → đính message của exception vào Detail.
    /// Sự kiện theo sau: shell hiện banner đỏ; song song nên gọi IAppLogger.Capture để ghi file.
    /// </summary>
    void NotifyError(string message, Exception? ex = null);
}
