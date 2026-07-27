// File    : SaveUserDepartmentsCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command ghi lại tập phòng ban GÁN RIÊNG của user (tab Phòng ban truy cập). WYSIWYG:
//           FE gửi đúng tập node đang tick; server thêm thiếu, xóa mềm thừa. Quyền kế thừa theo
//           vai trò không nằm trong payload này (readonly ở màn user, sửa ở màn vai trò).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.SaveUserDepartments;

/// <param name="Id">User được phân công.</param>
/// <param name="PhongBanIds">Toàn bộ phòng ban gán riêng sau khi lưu.</param>
/// <param name="ActorId">Người thao tác (claim sub).</param>
public sealed record SaveUserDepartmentsCommand(
    long Id, IReadOnlyList<long> PhongBanIds, long ActorId) : IRequest<Unit>;

public sealed class SaveUserDepartmentsCommandHandler : IRequestHandler<SaveUserDepartmentsCommand, Unit>
{
    private readonly IUserAdminRepository _repo;

    public SaveUserDepartmentsCommandHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Ghi diff tập phòng ban gán riêng. Sự kiện theo sau: quyền dữ liệu user áp ngay.</summary>
    public async Task<Unit> Handle(SaveUserDepartmentsCommand r, CancellationToken ct)
    {
        await _repo.SaveUserDepartmentsAsync(r.Id, r.PhongBanIds, r.ActorId, ct);
        return Unit.Value;
    }
}
