// File    : GetFormsListQueryHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho GetFormsListQuery — delegate xuống FormRepository.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Forms.Queries.GetFormsList;

/// <summary>
/// Lấy danh sách form từ DB. Không cache vì list thay đổi thường xuyên.
/// </summary>
public sealed class GetFormsListQueryHandler
    : IRequestHandler<GetFormsListQuery, GetFormsListResult>
{
    private readonly IFormRepository _formRepository;

    public GetFormsListQueryHandler(IFormRepository formRepository)
    {
        _formRepository = formRepository;
    }

    public async Task<GetFormsListResult> Handle(
        GetFormsListQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _formRepository.GetListAsync(
            request.TenantId,
            request.Platform,
            request.TableId,
            request.IsActive,
            request.SearchText,
            request.Page,
            request.PageSize,
            ct);

        return new GetFormsListResult(items, totalCount, request.Page, request.PageSize);
    }
}
