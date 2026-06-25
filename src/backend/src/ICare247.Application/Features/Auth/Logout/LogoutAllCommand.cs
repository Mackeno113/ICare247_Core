// File    : LogoutAllCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : SEC2-3 (spec 20) — Command "đăng xuất mọi thiết bị": thu hồi TẤT CẢ refresh token
//           đang sống của 1 người dùng. Dùng khi user chủ động hoặc khi đổi mật khẩu/khóa tài khoản.

using MediatR;

namespace ICare247.Application.Features.Auth.Logout;

/// <summary>
/// Thu hồi mọi refresh token còn hiệu lực của <paramref name="UserId"/> → mọi phiên phải đăng nhập lại.
/// </summary>
/// <param name="UserId">HT_NguoiDung.Id (lấy từ claim sub của token hiện tại).</param>
/// <param name="TenantId">Tenant hiện tại.</param>
public sealed record LogoutAllCommand(long UserId, int TenantId) : IRequest<Unit>;
