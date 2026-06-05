// File    : InsertLookupCommandHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler cho InsertLookupCommand — delegate sang IDynamicLookupRepository.InsertAsync.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Lookups.Commands.InsertLookup;

public sealed class InsertLookupCommandHandler
    : IRequestHandler<InsertLookupCommand, IDictionary<string, object?>?>
{
    private readonly IDynamicLookupRepository              _repo;
    private readonly ILogger<InsertLookupCommandHandler>   _logger;

    public InsertLookupCommandHandler(
        IDynamicLookupRepository repo,
        ILogger<InsertLookupCommandHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
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

        var result = await _repo.InsertAsync(
            request.FieldId, request.TenantId, request.Values, ct);

        _logger.LogDebug("InsertLookup OK — FieldId={FieldId} NewValue={Value}",
            request.FieldId, result?.TryGetValue("value", out var v) == true ? v : null);

        return result;
    }
}
