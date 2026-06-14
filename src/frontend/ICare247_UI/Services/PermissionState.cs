// File    : PermissionState.cs
// Module  : ICare247_UI
// Purpose : Lưu cờ quyền của user (từ /me/navigation) trong phiên để màn nghiệp vụ ẩn/hiện nút.
//           Tra theo đối tượng engine (Form/View). Chưa map (không node nào gắn) → cho phép
//           (khớp enforce-if-mapped ở server). Nạp 1 lần, dùng lại nhiều màn.

using ICare247_UI.Navigation;

namespace ICare247_UI.Services;

/// <summary>5 cờ quyền trên 1 đối tượng. Mặc định cho phép hết (khi chưa cấu hình quyền).</summary>
public readonly record struct PermFlags(bool Xem, bool Them, bool Sua, bool Xoa, bool InAn)
{
    public static readonly PermFlags All = new(true, true, true, true, true);
}

/// <summary>Trạng thái quyền theo phiên (scoped). Inject vào màn cần ẩn nút theo quyền.</summary>
public sealed class PermissionState
{
    private readonly NavigationApiService _navApi;
    private IReadOnlyList<MeNavNode>? _nodes;

    public PermissionState(NavigationApiService navApi) => _navApi = navApi;

    /// <summary>Nạp cờ quyền 1 lần/phiên (idempotent). Gọi đầu màn trước khi tra cứu.</summary>
    public async Task EnsureLoadedAsync()
    {
        _nodes ??= await _navApi.GetNavigationAsync();
    }

    /// <summary>
    /// Cờ quyền cho 1 đối tượng engine. type = "Form"/"View", code = formCode/viewCode.
    /// Không tìm thấy node gắn → trả <see cref="PermFlags.All"/> (chưa cấu hình = không khóa).
    /// </summary>
    public PermFlags ForTarget(string type, string code)
    {
        if (_nodes is null) return PermFlags.All;

        var n = _nodes.FirstOrDefault(x =>
            string.Equals(x.LoaiDoiTuong, type, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.DoiTuong, code, StringComparison.OrdinalIgnoreCase));

        return n is null
            ? PermFlags.All
            : new PermFlags(n.Xem, n.Them, n.Sua, n.Xoa, n.InAn);
    }
}
