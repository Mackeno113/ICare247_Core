// File    : SaveUserCompaniesCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command ghi lại tập công ty GÁN RIÊNG của user + công ty mặc định (tab Công ty truy cập).
//           WYSIWYG: FE gửi đúng tập node đang tick; server thêm thiếu, xóa mềm thừa. Quyền kế thừa
//           theo vai trò không nằm trong payload này (readonly ở màn user, sửa ở màn vai trò).

using FluentValidation;
using FluentValidation.Results;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.SaveUserCompanies;

/// <param name="Id">User được phân công.</param>
/// <param name="CongTyIds">Toàn bộ công ty gán riêng sau khi lưu.</param>
/// <param name="MacDinhCongTyId">Công ty mặc định khi đăng nhập (phải thuộc CongTyIds; null = không đặt).</param>
/// <param name="ActorId">Người thao tác (claim sub).</param>
public sealed record SaveUserCompaniesCommand(
    long Id, IReadOnlyList<long> CongTyIds, long? MacDinhCongTyId, long ActorId) : IRequest<Unit>;

public sealed class SaveUserCompaniesCommandHandler : IRequestHandler<SaveUserCompaniesCommand, Unit>
{
    private readonly IUserAdminRepository _repo;

    public SaveUserCompaniesCommandHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Validate công ty mặc định thuộc tập gán rồi ghi. Sự kiện theo sau: switcher của
    /// user áp danh sách mới ở lần tải kế (không cần user logout).</summary>
    public async Task<Unit> Handle(SaveUserCompaniesCommand r, CancellationToken ct)
    {
        if (r.MacDinhCongTyId is not null && !r.CongTyIds.Contains(r.MacDinhCongTyId.Value))
            throw new ValidationException(
                [new ValidationFailure(nameof(r.MacDinhCongTyId),
                    "Công ty mặc định phải nằm trong danh sách công ty được gán.")]);

        await _repo.SaveUserCompaniesAsync(r.Id, r.CongTyIds, r.MacDinhCongTyId, r.ActorId, ct);
        return Unit.Value;
    }
}
