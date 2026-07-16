// File    : GetUserDetailQuery.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Query chi tiết 1 người dùng (tab Thông tin + tab Vai trò với cờ đã gán).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.GetUserDetail;

/// <param name="Id">HT_NguoiDung.Id cần xem.</param>
public sealed record GetUserDetailQuery(long Id) : IRequest<UserDetailDto>;

public sealed class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto>
{
    private readonly IUserAdminRepository _repo;

    public GetUserDetailQueryHandler(IUserAdminRepository repo) => _repo = repo;

    /// <summary>Đọc chi tiết user; không tồn tại → 404 (middleware). Sự kiện theo sau: FE mở form detail.</summary>
    public async Task<UserDetailDto> Handle(GetUserDetailQuery r, CancellationToken ct)
        => await _repo.GetUserDetailAsync(r.Id, ct)
           ?? throw new KeyNotFoundException($"Người dùng Id={r.Id} không tồn tại.");
}
