// File    : ToastService.cs
// Module  : ICare247_UI
// Purpose : Dịch vụ thông báo nổi (toast) dùng chung — phát thông báo thoáng qua, tự tắt, xếp chồng.
//           Mỗi kiểu (success/error/warning/info) có phong cách riêng do ToastHost render.

namespace ICare247_UI.Services;

/// <summary>Phân loại toast — quyết định màu/icon/thời lượng mặc định (xem <see cref="ToastService"/>).</summary>
public enum ToastType
{
    /// <summary>Thành công (xanh lá) — vd "Đã lưu". Tự tắt nhanh.</summary>
    Success,
    /// <summary>Lỗi (đỏ) — thao tác thất bại. Ở lâu hơn để user kịp đọc.</summary>
    Error,
    /// <summary>Cảnh báo (cam) — hoàn tất một phần / cần lưu ý (vd lưu OK nhưng làm mới lỗi).</summary>
    Warning,
    /// <summary>Thông tin / nhắc nhỏ (xanh dương) — trung tính, không chặn.</summary>
    Info
}

/// <summary>Một thông báo toast đang hiển thị (bất biến — dựng khi phát, loại bỏ khi hết hạn/đóng tay).</summary>
public sealed record ToastMessage(Guid Id, ToastType Type, string Message, int DurationMs);

/// <summary>
/// Hàng đợi toast dùng chung toàn app: bất kỳ component/service nào cũng gọi
/// <see cref="Success"/>/<see cref="Error"/>/<see cref="Warning"/>/<see cref="Info"/> để phát thông báo.
/// <see cref="ToastHost"/> (đặt 1 lần ở MainLayout) lắng nghe <see cref="OnChange"/> và render.
/// Dùng cho thông báo THOÁNG QUA, tự tắt (Đã lưu, export xong…); lỗi validation trong form vẫn dùng
/// banner/inline tại chỗ (không tự tắt). Chuỗi truyền vào PHẢI đã i18n sẵn (service chỉ hiển thị).
/// </summary>
public sealed class ToastService
{
    private readonly List<ToastMessage> _items = [];

    /// <summary>Các toast đang hiển thị (mới nhất ở cuối) — ToastHost đọc để render.</summary>
    public IReadOnlyList<ToastMessage> Items => _items;

    /// <summary>Phát khi danh sách toast đổi (thêm/bớt) — ToastHost bắt để StateHasChanged.</summary>
    public event Action? OnChange;

    /// <summary>Thời lượng mặc định (ms) theo kiểu: lỗi/cảnh báo ở lâu hơn để kịp đọc.</summary>
    private static int DefaultDuration(ToastType type) => type switch
    {
        ToastType.Success => 3000,
        ToastType.Info    => 4000,
        ToastType.Warning => 5000,
        ToastType.Error   => 7000,
        _                 => 4000
    };

    /// <summary>Phát toast thành công (xanh lá). Sự kiện theo sau: hiển thị rồi tự tắt sau <paramref name="durationMs"/>.</summary>
    public void Success(string message, int? durationMs = null) => Show(ToastType.Success, message, durationMs);

    /// <summary>Phát toast lỗi (đỏ). Sự kiện theo sau: hiển thị (ở lâu) rồi tự tắt.</summary>
    public void Error(string message, int? durationMs = null) => Show(ToastType.Error, message, durationMs);

    /// <summary>Phát toast cảnh báo (cam). Sự kiện theo sau: hiển thị rồi tự tắt.</summary>
    public void Warning(string message, int? durationMs = null) => Show(ToastType.Warning, message, durationMs);

    /// <summary>Phát toast thông tin/nhắc nhỏ (xanh dương). Sự kiện theo sau: hiển thị rồi tự tắt.</summary>
    public void Info(string message, int? durationMs = null) => Show(ToastType.Info, message, durationMs);

    /// <summary>
    /// Lõi phát toast: thêm vào hàng đợi, báo host render, rồi hẹn tự loại bỏ sau thời lượng.
    /// Chuỗi rỗng → bỏ qua (không phát toast trống). Sự kiện theo sau: <see cref="OnChange"/>.
    /// </summary>
    public void Show(ToastType type, string message, int? durationMs = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var duration = durationMs ?? DefaultDuration(type);
        var toast = new ToastMessage(Guid.NewGuid(), type, message, duration);
        _items.Add(toast);
        OnChange?.Invoke();

        if (duration > 0)
            _ = DismissAfterAsync(toast.Id, duration);
    }

    /// <summary>Đóng 1 toast theo Id (nút ✕ hoặc hết hạn). Sự kiện theo sau: <see cref="OnChange"/> nếu có gỡ.</summary>
    public void Dismiss(Guid id)
    {
        var idx = _items.FindIndex(t => t.Id == id);
        if (idx < 0) return;
        _items.RemoveAt(idx);
        OnChange?.Invoke();
    }

    /// <summary>Chờ hết thời lượng rồi tự đóng toast (WASM đơn luồng — Task.Delay an toàn).</summary>
    private async Task DismissAfterAsync(Guid id, int durationMs)
    {
        await Task.Delay(durationMs);
        Dismiss(id);
    }
}
