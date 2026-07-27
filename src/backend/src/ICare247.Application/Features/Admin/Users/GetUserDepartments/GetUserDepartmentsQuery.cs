// File    : GetUserDepartmentsQuery.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Query cây phòng ban + trạng thái quyền của 1 user (tab "Phòng ban truy cập").

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.GetUserDepartments;

/// <param name="Id">User cần xem phân công phòng ban.</param>
public sealed record GetUserDepartmentsQuery(long Id) : IRequest<IReadOnlyList<UserDepartmentNodeDto>>;

public sealed class GetUserDepartmentsQueryHandler
    : IRequestHandler<GetUserDepartmentsQuery, IReadOnlyList<UserDepartmentNodeDto>>
{
    private readonly IUserAdminRepository _repo;

    public GetUserDepartmentsQueryHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Đọc cây phòng ban + cờ quyền. Sự kiện theo sau: FE render cây checkbox (không cascade).</summary>
    public Task<IReadOnlyList<UserDepartmentNodeDto>> Handle(GetUserDepartmentsQuery r, CancellationToken ct)
        => _repo.GetUserDepartmentsAsync(r.Id, ct);
}
