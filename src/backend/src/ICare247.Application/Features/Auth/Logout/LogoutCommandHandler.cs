// File    : LogoutCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : Xử lý đăng xuất — tra refresh token theo hash và thu hồi nếu còn.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.Logout;

/// <summary>Handler cho <see cref="LogoutCommand"/>. Không lỗi nếu token không tồn tại (idempotent).</summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRefreshTokenRepository _refresh;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditWriter _audit;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refresh,
        IJwtTokenService jwt,
        IAuditWriter audit,
        ILogger<LogoutCommandHandler> logger)
    {
        _refresh = refresh;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>Thu hồi refresh token. Sự kiện theo sau: token không dùng để làm mới được nữa.</summary>
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unit.Value;

        var hash = _jwt.HashRefreshToken(request.RefreshToken);
        var record = await _refresh.GetByHashAsync(hash, ct);
        if (record is not null && !record.DaThuHoi)
        {
            await _refresh.RevokeAsync(record.Id, ct);
            _logger.LogInformation("Đăng xuất — thu hồi refresh token UserId={UserId}", record.NguoiDungId);
            _audit.Enqueue(new AuditEvent
            {
                TenantId = request.TenantId,
                Category = AuditCategory.Auth,
                Action = AuditAction.Logout,
                Result = "Success",
                UserId = record.NguoiDungId
            });
        }

        return Unit.Value;
    }
}
