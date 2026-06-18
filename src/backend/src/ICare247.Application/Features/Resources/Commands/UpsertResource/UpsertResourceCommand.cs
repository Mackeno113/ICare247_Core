// File    : UpsertResourceCommand.cs
// Module  : Resources
// Layer   : Application
// Purpose : Thêm/sửa 1 bản dịch i18n (Sys_Resource) — ghi inline từ màn quản trị (vd Quản lý menu).
//           Validate key/lang không rỗng; ủy quyền MERGE cho repository.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Resources.Commands.UpsertResource;

/// <summary>Thêm/sửa bản dịch cho (Key, LangCode). Value rỗng = xóa nội dung bản dịch (vẫn giữ dòng).</summary>
public sealed record UpsertResourceCommand(string Key, string LangCode, string Value) : IRequest;

public sealed class UpsertResourceCommandHandler : IRequestHandler<UpsertResourceCommand>
{
    private readonly IResourceRepository _resources;

    public UpsertResourceCommandHandler(IResourceRepository resources) => _resources = resources;

    public async Task Handle(UpsertResourceCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Key))
            throw new ArgumentException("Resource key là bắt buộc.");
        if (string.IsNullOrWhiteSpace(r.LangCode))
            throw new ArgumentException("Lang code là bắt buộc.");

        await _resources.UpsertAsync(r.Key.Trim(), r.LangCode.Trim(), r.Value ?? string.Empty, ct);
    }
}
