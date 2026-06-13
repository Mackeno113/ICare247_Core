// File    : ForgotPasswordCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : Command yêu cầu đặt lại mật khẩu (STUB — chưa gửi email thật ở pha này).

using MediatR;

namespace ICare247.Application.Features.Auth.ForgotPassword;

/// <summary>
/// Yêu cầu gửi link/đặt lại mật khẩu. STUB: chưa tích hợp SMTP — chỉ log, luôn trả thành
/// công để chống dò tài khoản. TODO(phase-email): sinh token, lưu DB, gửi email thật.
/// </summary>
/// <param name="UsernameOrEmail">Tên đăng nhập hoặc email người dùng nhập.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
public sealed record ForgotPasswordCommand(string UsernameOrEmail, int TenantId) : IRequest<Unit>;
