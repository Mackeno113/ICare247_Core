// File    : UserNotifier.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Implementation IUserNotifier — phát thông báo cho người dùng thấy trên shell.
//           Marshal về UI thread (Dispatcher) vì thường được gọi từ Task async/background.

using System.Windows;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Bộ phát thông báo dùng chung (singleton). Raise <see cref="Raised"/> trên UI thread
/// để ShellViewModel bind vào banner. Không tự ghi file — caller ghi log qua IAppLogger.
/// </summary>
public sealed class UserNotifier : IUserNotifier
{
    /// <inheritdoc />
    public event Action<UserNotification>? Raised;

    /// <inheritdoc />
    public void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info, string? detail = null)
        => Raise(new UserNotification(message, severity, detail));

    /// <inheritdoc />
    public void NotifyError(string message, Exception? ex = null)
        => Raise(new UserNotification(message, NotificationSeverity.Error, ex?.Message));

    /// <summary>
    /// Raise sự kiện trên UI thread. Nếu đang ở thread khác → dispatch qua Application.Current.
    /// Không có Application (unit test) → raise trực tiếp.
    /// </summary>
    private void Raise(UserNotification n)
    {
        var app = Application.Current;
        if (app?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.BeginInvoke(new Action(() => Raised?.Invoke(n)));
        else
            Raised?.Invoke(n);
    }
}
