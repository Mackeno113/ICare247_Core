// File    : ResetPasswordCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : Command đặt lại mật khẩu bằng token (STUB — chưa có kho token ở pha này).

using MediatR;

namespace ICare247.Application.Features.Auth.ResetPassword;

/// <summary>
/// Đặt lại mật khẩu từ token nhận qua email. STUB: chưa có kho token + SMTP nên trả false.
/// TODO(phase-email): verify token, đổi MatKhauHash, thu hồi mọi refresh token.
/// </summary>
/// <param name="Token">Token đặt lại (từ email).</param>
/// <param name="NewPassword">Mật khẩu mới.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
public sealed record ResetPasswordCommand(string Token, string NewPassword, int TenantId)
    : IRequest<bool>;
