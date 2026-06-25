// File    : LogoutAllCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : SEC2-3 (spec 20) — thu hồi mọi refresh token của 1 người dùng (đăng xuất mọi thiết bị).

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.Logout;

/// <summary>Handler cho <see cref="LogoutAllCommand"/>. Idempotent — không lỗi nếu user không có phiên nào.</summary>
public sealed class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, Unit>
{
    private readonly IRefreshTokenRepository _refresh;
    private readonly IAuditWriter _audit;
    private readonly ILogger<LogoutAllCommandHandler> _logger;

    public LogoutAllCommandHandler(
        IRefreshTokenRepository refresh,
        IAuditWriter audit,
        ILogger<LogoutAllCommandHandler> logger)
    {
        _refresh = refresh;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>Thu hồi mọi refresh token của user. Sự kiện theo sau: mọi phiên hiện có phải đăng nhập lại.</summary>
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken ct)
    {
        if (request.UserId <= 0) return Unit.Value;

        await _refresh.RevokeAllForUserAsync(request.UserId, ct);
        _logger.LogInformation("Đăng xuất mọi thiết bị — thu hồi toàn bộ refresh token UserId={UserId}", request.UserId);
        _audit.Enqueue(new AuditEvent
        {
            TenantId = request.TenantId,
            Category = AuditCategory.Auth,
            Action = AuditAction.Logout,
            Result = "Success",
            UserId = request.UserId
        });

        return Unit.Value;
    }
}
