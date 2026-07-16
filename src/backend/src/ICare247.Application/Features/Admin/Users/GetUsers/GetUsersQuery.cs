// File    : GetUsersQuery.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Query danh sách người dùng cho lưới màn Người dùng (admin).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.GetUsers;

/// <summary>Danh sách người dùng (chưa xóa) kèm tên vai trò gộp.</summary>
public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserListItemDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItemDto>>
{
    private readonly IUserAdminRepository _repo;

    public GetUsersQueryHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Đọc danh sách user. Sự kiện theo sau: FE đổ vào lưới màn Người dùng.</summary>
    public Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersQuery r, CancellationToken ct)
        => _repo.GetUsersAsync(ct);
}
