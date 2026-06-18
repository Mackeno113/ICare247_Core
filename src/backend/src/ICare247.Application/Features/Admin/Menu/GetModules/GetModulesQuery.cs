// File    : GetModulesQuery.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : Đọc danh sách phân hệ (HT_PhanHe) đang bật cho dropdown Module ở Menu Builder.
//           Tách khỏi cây menu — danh mục thuần để admin gán Module cho node.

using ICare247.Application.Features.Admin.Menu;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Menu.GetModules;

/// <summary>Lấy danh sách phân hệ (module) đang bật của tenant — cho dropdown chọn Module.</summary>
public sealed record GetModulesQuery() : IRequest<IReadOnlyList<ModuleOptionDto>>;

public sealed class GetModulesQueryHandler
    : IRequestHandler<GetModulesQuery, IReadOnlyList<ModuleOptionDto>>
{
    private readonly IMenuAdminRepository _repo;

    public GetModulesQueryHandler(IMenuAdminRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ModuleOptionDto>> Handle(GetModulesQuery r, CancellationToken ct)
        => await _repo.GetModulesAsync(ct);
}
