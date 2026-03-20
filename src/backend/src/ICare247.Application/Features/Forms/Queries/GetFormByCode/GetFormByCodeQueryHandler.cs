// File    : GetFormByCodeQueryHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho GetFormByCodeQuery — cache-aside pattern (L1 → L2 → DB).

using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Form;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Queries.GetFormByCode;

/// <summary>
/// Cache-aside: kiểm tra cache trước → miss thì gọi repository → ghi cache → trả kết quả.
/// </summary>
public sealed class GetFormByCodeQueryHandler
    : IRequestHandler<GetFormByCodeQuery, FormMetadata?>
{
    private readonly IFormRepository _formRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetFormByCodeQueryHandler> _logger;

    public GetFormByCodeQueryHandler(
        IFormRepository formRepository,
        ICacheService cache,
        ILogger<GetFormByCodeQueryHandler> logger)
    {
        _formRepository = formRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FormMetadata?> Handle(
        GetFormByCodeQuery request, CancellationToken ct)
    {
        // ── Bước 1: Thử lấy từ cache ────────────────────────────────────────
        // Chưa biết version → dùng version=0 làm "latest" key
        var cacheKey = CacheKeys.Form(
            request.FormCode, 0, request.LangCode, request.Platform, request.TenantId);

        var cached = await _cache.GetAsync<FormMetadata>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug(
                "Cache hit — FormCode={FormCode}, TenantId={TenantId}",
                request.FormCode, request.TenantId);
            return cached;
        }

        // ── Bước 2: Cache miss → đọc DB ─────────────────────────────────────
        _logger.LogDebug(
            "Cache miss — FormCode={FormCode}, TenantId={TenantId} → đọc DB",
            request.FormCode, request.TenantId);

        var form = await _formRepository.GetByCodeAsync(
            request.FormCode, request.TenantId, ct);

        if (form is null)
        {
            _logger.LogWarning(
                "Form không tồn tại — FormCode={FormCode}, TenantId={TenantId}",
                request.FormCode, request.TenantId);
            return null;
        }

        // ── Bước 3: Ghi cache ────────────────────────────────────────────────
        await _cache.SetAsync(cacheKey, form, ct: ct);

        return form;
    }
}
