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
    private readonly IResourceRepository                   _resources;
    private readonly ILogger<InsertLookupCommandHandler>   _logger;

    public InsertLookupCommandHandler(
        IDynamicLookupRepository repo,
        IResourceRepository resources,
        ILogger<InsertLookupCommandHandler> logger)
    {
        _repo      = repo;
        _resources = resources;
        _logger    = logger;
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
                request.FieldId, request.TenantId, request.Values, ct);
        }
        catch (DuplicateValueException dup)
        {
            // Resolve key {table}.val.{column}.unique → text qua resource map (cache).
            var map = await _resources.GetByKeysAsync([dup.ResourceKey, "sys.val.unique"], "vi", ct);
            var msg = map.TryGetValue(dup.ResourceKey, out var v) && !string.IsNullOrWhiteSpace(v)
                ? v
                : map.TryGetValue("sys.val.unique", out var tpl) && !string.IsNullOrWhiteSpace(tpl)
                    ? tpl.Replace("{0}", dup.Column)
                    : $"{dup.Column} đã tồn tại";
            throw new InvalidOperationException(msg);
        }

        _logger.LogDebug("InsertLookup OK — FieldId={FieldId} NewValue={Value}",
            request.FieldId, result?.TryGetValue("value", out var v) == true ? v : null);

        return result;
    }
}
