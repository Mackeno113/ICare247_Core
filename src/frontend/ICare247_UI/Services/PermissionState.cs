// File    : PermissionState.cs
// Module  : ICare247_UI
// Purpose : Lưu cờ quyền của user (từ /me/navigation) trong phiên để màn nghiệp vụ ẩn/hiện nút.
//           Tra theo đối tượng engine (Form/View). Chưa map (không node nào gắn) → cho phép
//           (khớp enforce-if-mapped ở server). Nạp 1 lần, dùng lại nhiều màn.

using ICare247_UI.Navigation;
using Microsoft.AspNetCore.Components.Authorization;

namespace ICare247_UI.Services;

/// <summary>5 cờ quyền trên 1 đối tượng. Mặc định cho phép hết (khi chưa cấu hình quyền).</summary>
public readonly record struct PermFlags(bool Xem, bool Them, bool Sua, bool Xoa, bool InAn)
{
    public static readonly PermFlags All = new(true, true, true, true, true);
}

/// <summary>Trạng thái quyền theo phiên (scoped). Inject vào màn cần ẩn nút theo quyền.</summary>
public sealed class PermissionState
{
    /// <summary>Mã vai trò super-admin — khớp <c>RequirePermissionForTargetAttribute.SuperAdminRole</c> ở backend.</summary>
    private const string SuperAdminRole = "SUPERADMIN";

    private readonly NavigationApiService _navApi;
    private readonly AuthenticationStateProvider _authProvider;
    private IReadOnlyList<MeNavNode>? _nodes;
    private bool? _isSuperAdmin;

    public PermissionState(NavigationApiService navApi, AuthenticationStateProvider authProvider)
    {
        _navApi = navApi;
        _authProvider = authProvider;
    }

    /// <summary>Nạp cờ quyền 1 lần/phiên (idempotent). Gọi đầu màn trước khi tra cứu.</summary>
    public async Task EnsureLoadedAsync()
    {
        _nodes ??= await _navApi.GetNavigationAsync();
    }

    /// <summary>
    /// True nếu user hiện tại có vai trò super-admin (role claim = "SUPERADMIN").
    /// Dùng để ẩn/hiện công cụ quản trị (vd nút Xóa cache) khỏi người dùng cuối. Cache trong phiên.
    /// </summary>
    public async Task<bool> IsSuperAdminAsync()
    {
        if (_isSuperAdmin is { } cached) return cached;
        var state = await _authProvider.GetAuthenticationStateAsync();
        _isSuperAdmin = state.User.FindAll("role")
            .Any(c => string.Equals(c.Value, SuperAdminRole, StringComparison.OrdinalIgnoreCase));
        return _isSuperAdmin.Value;
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
