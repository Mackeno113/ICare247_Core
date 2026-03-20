// File    : IAutoSaveService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Interface cho auto-save service — debounce save khi IsDirty thay đổi.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Service auto-save — debounce save sau khi user ngừng sửa một khoảng thời gian.
/// Mỗi editor tạo instance riêng.
/// </summary>
public interface IAutoSaveService : IDisposable
{
    /// <summary>Trạng thái auto-save: Idle | Pending | Saving | Saved | Error.</summary>
    AutoSaveStatus Status { get; }

    /// <summary>Thời gian save thành công gần nhất.</summary>
    DateTime? LastSavedAt { get; }

    /// <summary>Message lỗi nếu save thất bại.</summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Đánh dấu có thay đổi cần save. Bắt đầu đếm debounce.
    /// Nếu gọi lại trước khi timeout → reset đồng hồ.
    /// </summary>
    void NotifyDirty();

    /// <summary>Trigger save ngay lập tức (bỏ qua debounce).</summary>
    Task SaveNowAsync();

    /// <summary>Tạm dừng auto-save (ví dụ khi đang trong dialog).</summary>
    void Pause();

    /// <summary>Resume auto-save sau khi pause.</summary>
    void Resume();

    /// <summary>Event khi Status thay đổi.</summary>
    event EventHandler? StatusChanged;
}

/// <summary>Trạng thái của auto-save service.</summary>
public enum AutoSaveStatus
{
    /// <summary>Không có thay đổi pending.</summary>
    Idle,
    /// <summary>Có thay đổi, đang chờ debounce timeout.</summary>
    Pending,
    /// <summary>Đang thực hiện save.</summary>
    Saving,
    /// <summary>Save thành công.</summary>
    Saved,
    /// <summary>Save thất bại.</summary>
    Error
}
