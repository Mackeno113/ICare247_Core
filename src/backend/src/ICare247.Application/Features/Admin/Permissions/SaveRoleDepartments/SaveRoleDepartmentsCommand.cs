// File    : SaveRoleDepartmentsCommand.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Command ghi lại tập phòng ban của 1 vai trò (HT_VaiTro_PhongBan). Kế thừa ĐỘNG:
//           mọi user thuộc vai trò thấy thay đổi ngay — không copy, không rác. Trục độc lập với
//           phạm vi công ty (HT_VaiTro_CongTy) dù mỗi phòng ban thuộc đúng 1 công ty.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.SaveRoleDepartments;

/// <param name="RoleId">Vai trò được cấu hình.</param>
/// <param name="PhongBanIds">Toàn bộ phòng ban thuộc phạm vi vai trò sau khi lưu (WYSIWYG từ cây).</param>
/// <param name="UserId">Người thao tác (claim sub).</param>
public sealed record SaveRoleDepartmentsCommand(
    long RoleId, IReadOnlyList<long> PhongBanIds, long UserId) : IRequest<Unit>;

public sealed class SaveRoleDepartmentsCommandHandler : IRequestHandler<SaveRoleDepartmentsCommand, Unit>
{
    private readonly IPermissionAdminRepository _repo;

    public SaveRoleDepartmentsCommandHandler(IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Ghi diff tập phòng ban vai trò. Sự kiện theo sau: user thuộc vai trò áp ngay.</summary>
    public async Task<Unit> Handle(SaveRoleDepartmentsCommand r, CancellationToken ct)
    {
        await _repo.SaveRoleDepartmentsAsync(r.RoleId, r.PhongBanIds, r.UserId, ct);
        return Unit.Value;
    }
}
