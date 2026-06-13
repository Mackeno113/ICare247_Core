// File    : ForgotPasswordCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : Xử lý yêu cầu quên mật khẩu (STUB) — log yêu cầu, chưa gửi email.

using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.ForgotPassword;

/// <summary>
/// Handler STUB cho <see cref="ForgotPasswordCommand"/>. Không tiết lộ tài khoản có tồn tại
/// hay không (luôn trả Unit). TODO(phase-email): tích hợp service gửi mail + token reset.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(ILogger<ForgotPasswordCommandHandler> logger)
        => _logger = logger;

    /// <summary>Ghi nhận yêu cầu. Sự kiện theo sau (tương lai): gửi email chứa token đặt lại.</summary>
    public Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        // TODO(phase-email): tra HT_NguoiDung theo email/username → sinh token → lưu DB → gửi mail.
        _logger.LogInformation(
            "Yêu cầu quên mật khẩu (STUB, chưa gửi mail) — '{Input}', TenantId={TenantId}",
            request.UsernameOrEmail, request.TenantId);
        return Task.FromResult(Unit.Value);
    }
}
