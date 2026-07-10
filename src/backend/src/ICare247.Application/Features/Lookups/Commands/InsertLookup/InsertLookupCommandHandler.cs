// File    : InsertLookupCommandHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler cho InsertLookupCommand — delegate sang IDynamicLookupRepository.InsertAsync.

using ICare247.Application.Interfaces;
using ICare247.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Lookups.Commands.InsertLookup;

public sealed class InsertLookupCommandHandler
    : IRequestHandler<InsertLookupCommand, IDictionary<string, object?>?>
{
    private readonly IDynamicLookupRepository              _repo;
    private readonly IConfigCache                          _config;
    private readonly ILogger<InsertLookupCommandHandler>   _logger;

    public InsertLookupCommandHandler(
        IDynamicLookupRepository repo,
        IConfigCache config,
        ILogger<InsertLookupCommandHandler> logger)
    {
        _repo    = repo;
        _config  = config;
        _logger  = logger;
    }

    /// <summary>
    /// Thực thi insert entity mới qua repository.
    /// Sự kiện theo sau: API trả về khóa + display để LookupBox auto-select.
    /// </summary>
    public async Task<IDictionary<string, object?>?> Handle(
        InsertLookupCommand request, CancellationToken ct)
    {
        _logger.LogDebug(
            "InsertLookup — FieldId={FieldId} TenantId={TenantId} Columns=[{Cols}]",
            request.FieldId, request.TenantId, string.Join(", ", request.Values.Keys));

        IDictionary<string, object?>? result;
        try
        {
            result = await _repo.InsertAsync(
                request.FieldId, request.TenantId, request.Values, request.UserId, ct);
        }
        catch (DuplicateValueException dup)
        {
            // Resolve message trùng qua IConfigCache (ADR-014) — KHÔNG chọc IResourceRepository thẳng.
            // 1. per-field key {table}.val.{column}.unique → 2. sys.val.unique template → 3. hardcode.
            // Token i18n: {0} = giá trị nhập · {1} = cột (thay ở CẢ per-field lẫn template).
            var enteredValue = request.Values.TryGetValue(dup.Column, out var dv) ? dv?.ToString() ?? "" : "";
            var template = await _config.ResolveKeyAsync(dup.ResourceKey, "vi", request.TenantId, ct)
                ?? await _config.ResolveKeyAsync("sys.val.unique", "vi", request.TenantId, ct);
            var msg = template is not null
                ? template.Replace("{0}", enteredValue).Replace("{1}", dup.Column)
                : $"{dup.Column} \"{enteredValue}\" đã tồn tại";
            throw new InvalidOperationException(msg);
        }

        _logger.LogDebug("InsertLookup OK — FieldId={FieldId} NewValue={Value}",
            request.FieldId, result?.TryGetValue("value", out var newValue) == true ? newValue : null);

        return result;
    }
}
