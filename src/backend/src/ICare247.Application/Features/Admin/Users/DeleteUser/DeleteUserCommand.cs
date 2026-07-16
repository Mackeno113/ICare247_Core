// File    : DeleteUserCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command xóa mềm 1 người dùng (IsDeleted=1). Chặn tự xóa chính mình.

using FluentValidation;
using FluentValidation.Results;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.DeleteUser;

/// <param name="Id">User cần xóa.</param>
/// <param name="ActorId">Người thao tác (claim sub) — không được trùng Id (tự xóa mình).</param>
public sealed record DeleteUserCommand(long Id, long ActorId) : IRequest<Unit>;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserAdminRepository _repo;

    public DeleteUserCommandHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Xóa mềm user (chặn tự xóa). Sự kiện theo sau: FE reload lưới; user hết đăng nhập được.</summary>
    public async Task<Unit> Handle(DeleteUserCommand r, CancellationToken ct)
    {
        if (r.Id == r.ActorId)
            throw new ValidationException(
                [new ValidationFailure(nameof(r.Id), "Không thể xóa tài khoản đang đăng nhập.")]);

        var ok = await _repo.DeleteUserAsync(r.Id, r.ActorId, ct);
        if (!ok) throw new KeyNotFoundException($"Người dùng Id={r.Id} không tồn tại.");
        return Unit.Value;
    }
}
