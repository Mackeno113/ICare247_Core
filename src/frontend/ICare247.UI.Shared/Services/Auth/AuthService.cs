// File    : AuthService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Cài đặt tạm (khung) cho IAuthService. Chưa gọi API thật — chỉ giữ
//           trạng thái trong bộ nhớ để shell/login chạy được trong giai đoạn dựng
//           khung. Sẽ thay bằng JWT + endpoint /api/auth ở phase Auth.

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Cài đặt khung của <see cref="IAuthService"/>. TODO(phase-auth): thay bằng gọi
/// POST /api/auth/login (JWT) + lưu token vào localStorage qua JSInterop.
/// </summary>
public sealed class AuthService : IAuthService
{
    /// <inheritdoc />
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Đăng nhập (khung). Hiện chấp nhận mọi tài khoản không rỗng để khung shell
    /// chạy được. Sự kiện theo sau: đặt <see cref="IsAuthenticated"/> = true.
    /// </summary>
    public Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        // TODO(phase-auth): gọi backend, nhận JWT, lưu token, đọc claim quyền.
        IsAuthenticated = !string.IsNullOrWhiteSpace(username);
        return Task.FromResult(IsAuthenticated);
    }

    /// <summary>
    /// Đăng xuất (khung). Sự kiện theo sau: <see cref="IsAuthenticated"/> = false.
    /// </summary>
    public Task LogoutAsync(CancellationToken ct = default)
    {
        // TODO(phase-auth): xóa token khỏi localStorage + revoke nếu cần.
        IsAuthenticated = false;
        return Task.CompletedTask;
    }
}
