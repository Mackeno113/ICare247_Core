// File    : GetResourcesQuery.cs
// Module  : Resources
// Layer   : Application
// Purpose : Query batch-load resource value theo tập key + ngôn ngữ (cho i18n chuỗi UI tĩnh client).

using MediatR;

namespace ICare247.Application.Features.Resources.Queries.GetResources;

/// <summary>
/// Lấy map Resource_Key → Resource_Value cho danh sách key theo <paramref name="LangCode"/>.
/// Key không tồn tại sẽ không có trong kết quả (client tự fallback).
/// </summary>
public sealed record GetResourcesQuery(
    IReadOnlyList<string> Keys,
    string LangCode = "vi"
) : IRequest<IReadOnlyDictionary<string, string>>;
