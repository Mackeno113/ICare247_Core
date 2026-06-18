// File    : GetLanguagesQuery.cs
// Module  : Languages
// Layer   : Application
// Purpose : Lấy danh sách ngôn ngữ hệ thống (Sys_Language) cho client dựng bộ chuyển ngôn ngữ +
//           các ô nhập bản dịch. Thêm ngôn ngữ = 1 dòng Sys_Language, không cần sửa file/đổi code.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Languages.GetLanguages;

/// <summary>1 ngôn ngữ hệ thống.</summary>
/// <param name="Code">Mã ngôn ngữ (vd "vi", "en", "ja").</param>
/// <param name="Name">Tên hiển thị (vd "Tiếng Việt").</param>
/// <param name="IsDefault">Là ngôn ngữ mặc định/gốc.</param>
public sealed record LanguageDto(string Code, string Name, bool IsDefault);

/// <summary>Lấy mọi ngôn ngữ trong Sys_Language (mặc định lên đầu).</summary>
public sealed record GetLanguagesQuery() : IRequest<IReadOnlyList<LanguageDto>>;

public sealed class GetLanguagesQueryHandler
    : IRequestHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    private readonly IResourceRepository _resources;

    public GetLanguagesQueryHandler(IResourceRepository resources) => _resources = resources;

    public async Task<IReadOnlyList<LanguageDto>> Handle(GetLanguagesQuery r, CancellationToken ct)
        => await _resources.GetLanguagesAsync(ct);
}
