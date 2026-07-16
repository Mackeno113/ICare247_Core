// File    : ResetUserPasswordCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command đặt lại mật khẩu 1 người dùng từ màn Người dùng (admin) — băm PBKDF2 mới.

using FluentValidation;
using FluentValidation.Results;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.ResetUserPassword;

/// <param name="Id">User cần đặt lại mật khẩu.</param>
/// <param name="MatKhauMoi">Mật khẩu mới (thô — băm ở handler).</param>
/// <param name="DoiMatKhauLanSau">Bắt user đổi mật khẩu ở lần đăng nhập kế.</param>
/// <param name="ActorId">Người thao tác (claim sub).</param>
public sealed record ResetUserPasswordCommand(
    long Id, string MatKhauMoi, bool DoiMatKhauLanSau, long ActorId) : IRequest<Unit>;

public sealed class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, Unit>
{
    private readonly IUserAdminRepository _repo;
    private readonly IPasswordHasher _hasher;

    public ResetUserPasswordCommandHandler(IUserAdminRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    /// <summary>Băm mật khẩu mới rồi ghi đè. Sự kiện theo sau: user đăng nhập bằng mật khẩu mới.</summary>
    public async Task<Unit> Handle(ResetUserPasswordCommand r, CancellationToken ct)
    {
        if (r.MatKhauMoi is null || r.MatKhauMoi.Length < 6)
            throw new ValidationException(
                [new ValidationFailure(nameof(r.MatKhauMoi), "Mật khẩu tối thiểu 6 ký tự.")]);

        var ok = await _repo.ResetPasswordAsync(r.Id, _hasher.Hash(r.MatKhauMoi), r.DoiMatKhauLanSau, r.ActorId, ct);
        if (!ok) throw new KeyNotFoundException($"Người dùng Id={r.Id} không tồn tại.");
        return Unit.Value;
    }
}
