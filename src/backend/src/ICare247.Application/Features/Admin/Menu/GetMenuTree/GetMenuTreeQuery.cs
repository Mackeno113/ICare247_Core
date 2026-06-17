// File    : GetMenuTreeQuery.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : Đọc toàn bộ cây menu (HT_ChucNang) cho màn Menu Builder — gồm node ẩn (KichHoat=0)
//           để admin thấy & bật lại. Khác /me/navigation (chỉ node user có quyền Xem).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Menu.GetMenuTree;

/// <summary>Lấy toàn bộ node menu của tenant hiện tại (để dựng cây + chọn cha).</summary>
public sealed record GetMenuTreeQuery() : IRequest<IReadOnlyList<MenuNodeDto>>;

public sealed class GetMenuTreeQueryHandler
    : IRequestHandler<GetMenuTreeQuery, IReadOnlyList<MenuNodeDto>>
{
    private readonly IMenuAdminRepository _repo;

    public GetMenuTreeQueryHandler(IMenuAdminRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<MenuNodeDto>> Handle(GetMenuTreeQuery r, CancellationToken ct)
        => await _repo.GetTreeAsync(ct);
}
