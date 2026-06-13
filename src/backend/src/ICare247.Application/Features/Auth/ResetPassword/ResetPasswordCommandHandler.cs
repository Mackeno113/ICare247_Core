// File    : ResetPasswordCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : Xử lý đặt lại mật khẩu (STUB) — chưa hỗ trợ; trả false để controller báo "đang hoàn thiện".

using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.ResetPassword;

/// <summary>
/// Handler STUB cho <see cref="ResetPasswordCommand"/>. Trả false (chưa hỗ trợ).
/// TODO(phase-email): verify token trong kho, băm mật khẩu mới, cập nhật + thu hồi phiên cũ.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(ILogger<ResetPasswordCommandHandler> logger)
        => _logger = logger;

    /// <summary>Chưa hỗ trợ đặt lại thật ở pha này.</summary>
    public Task<bool> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Đặt lại mật khẩu (STUB, chưa hỗ trợ) — TenantId={TenantId}", request.TenantId);
        return Task.FromResult(false);
    }
}
