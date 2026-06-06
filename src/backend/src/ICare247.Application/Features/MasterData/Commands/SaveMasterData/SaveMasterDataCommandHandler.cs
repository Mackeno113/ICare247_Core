// File    : SaveMasterDataCommandHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler Insert/Update — chạy ValidationEngine server-side trước khi ghi DB.

using ICare247.Application.Features.MasterData.Models;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.MasterData.Commands.SaveMasterData;

public sealed class SaveMasterDataCommandHandler
    : IRequestHandler<SaveMasterDataCommand, MasterDataSaveResult>
{
    private readonly IMasterDataRepository _repo;
    private readonly IValidationEngine     _validation;
    private readonly ILogger<SaveMasterDataCommandHandler> _logger;

    public SaveMasterDataCommandHandler(
        IMasterDataRepository repo,
        IValidationEngine validation,
        ILogger<SaveMasterDataCommandHandler> logger)
    {
        _repo       = repo;
        _validation = validation;
        _logger     = logger;
    }

    /// <summary>
    /// Validate toàn form (server-side) → nếu fail trả lỗi; ngược lại Insert/Update.
    /// Sự kiện theo sau: API trả Id mới (Insert) hoặc danh sách lỗi field cho UI.
    /// </summary>
    public async Task<MasterDataSaveResult> Handle(SaveMasterDataCommand r, CancellationToken ct)
    {
        var info = await _repo.GetFormInfoAsync(r.FormCode, r.TenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{r.FormCode}' không tồn tại.");

        // ── Validation server-side (vá caveat session 36: trước đây chỉ check client) ──
        var context = new EvaluationContext(r.Values);
        var vr = await _validation.ValidateFormAsync(
            info.FormId, context, r.TenantId, langCode: "vi",
            resourceMap: null, formCode: r.FormCode, ct: ct);

        var errors = vr.Where(kv => !kv.Value.IsValid)
                       .Select(kv => new MasterDataFieldError(
                           kv.Key,
                           kv.Value.Results.FirstOrDefault()?.Message ?? "Giá trị không hợp lệ."))
                       .ToList();

        if (errors.Count > 0)
        {
            _logger.LogDebug("SaveMasterData '{Form}' validation fail: {Count} field.", r.FormCode, errors.Count);
            return new MasterDataSaveResult(Success: false, Id: null, Errors: errors);
        }

        // ── Ghi DB ────────────────────────────────────────────────────────────
        if (r.Id is null)
        {
            var newId = await _repo.InsertAsync(r.FormCode, r.TenantId, r.Values, ct);
            _logger.LogInformation("SaveMasterData INSERT '{Form}' → Id={Id}.", r.FormCode, newId);
            return new MasterDataSaveResult(Success: true, Id: newId, Errors: []);
        }

        await _repo.UpdateAsync(r.FormCode, r.TenantId, r.Id, r.Values, ct);
        _logger.LogInformation("SaveMasterData UPDATE '{Form}' Id={Id}.", r.FormCode, r.Id);
        return new MasterDataSaveResult(Success: true, Id: r.Id, Errors: []);
    }
}
