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
    private readonly IConfigCache          _config;
    private readonly IAuditWriter          _audit;
    private readonly ILogger<SaveMasterDataCommandHandler> _logger;

    public SaveMasterDataCommandHandler(
        IMasterDataRepository repo,
        IValidationEngine validation,
        IConfigCache config,
        IAuditWriter audit,
        ILogger<SaveMasterDataCommandHandler> logger)
    {
        _repo       = repo;
        _validation = validation;
        _config     = config;
        _audit      = audit;
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
        // Resource map lấy qua facade (ADR-014) → message validation resolve i18n đúng,
        // trước đây truyền null nên rule message hiện ra raw Error_Key.
        var context     = new EvaluationContext(r.Values);
        var resourceMap = await _config.GetResourceMapAsync(r.FormCode, "vi", r.TenantId, ct);
        var vr = await _validation.ValidateFormAsync(
            info.FormId, context, r.TenantId, langCode: "vi",
            resourceMap: resourceMap, formCode: r.FormCode, ct: ct);

        var errors = vr.Where(kv => !kv.Value.IsValid)
                       .Select(kv => new MasterDataFieldError(
                           kv.Key,
                           kv.Value.Results.FirstOrDefault()?.Message ?? "Giá trị không hợp lệ."))
                       .ToList();

        // ── Check trùng (field Is_Unique) — query DB trước khi ghi ──────────────
        // Resolve message trùng qua IConfigCache (ADR-014) — KHÔNG chọc IResourceRepository thẳng.
        foreach (var col in info.Columns.Where(c => c.IsUnique))
        {
            if (!r.Values.TryGetValue(col.ColumnCode, out var val)
                || val is null || string.IsNullOrWhiteSpace(val.ToString()))
                continue;

            if (await _repo.ExistsValueAsync(r.FormCode, r.TenantId, col.ColumnCode, val, r.Id, ct))
            {
                var key = $"{info.TableName.ToLowerInvariant()}.val.{col.ColumnCode.ToLowerInvariant()}.unique";
                var label = col.Label.Length > 0 ? col.Label : col.ColumnCode;
                var enteredValue = val.ToString() ?? "";
                // 1. per-field key → 2. sys.val.unique template → 3. hardcoded.
                // Token i18n: {0} = giá trị người dùng nhập · {1} = nhãn field (thay ở CẢ per-field lẫn template).
                var template = await _config.ResolveKeyAsync(key, "vi", r.TenantId, ct)
                    ?? await _config.ResolveKeyAsync("sys.val.unique", "vi", r.TenantId, ct);
                var msg = template is not null
                    ? template.Replace("{0}", enteredValue).Replace("{1}", label)
                    : $"{label} \"{enteredValue}\" đã tồn tại";
                errors.Add(new MasterDataFieldError(col.ColumnCode, msg));
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogDebug("SaveMasterData '{Form}' validation fail: {Count} field.", r.FormCode, errors.Count);
            return new MasterDataSaveResult(Success: false, Id: null, Errors: errors);
        }

        // ── Ghi DB ────────────────────────────────────────────────────────────
        if (r.Id is null)
        {
            var newId = await _repo.InsertAsync(r.FormCode, r.TenantId, r.Values, r.UserId, ct);
            _logger.LogInformation("SaveMasterData INSERT '{Form}' → Id={Id}.", r.FormCode, newId);
            AuditData(r, AuditAction.DataCreate, newId);
            return new MasterDataSaveResult(Success: true, Id: newId, Errors: []);
        }

        await _repo.UpdateAsync(r.FormCode, r.TenantId, r.Id, r.Values, r.UserId, ct);
        _logger.LogInformation("SaveMasterData UPDATE '{Form}' Id={Id}.", r.FormCode, r.Id);
        AuditData(r, AuditAction.DataUpdate, r.Id);
        return new MasterDataSaveResult(Success: true, Id: r.Id, Errors: []);
    }

    /// <summary>Ghi nhật ký 1 thao tác ghi danh mục (non-blocking). GiaTriMoi = JSON các giá trị gửi lên.</summary>
    private void AuditData(SaveMasterDataCommand r, string action, object? id)
        => _audit.Enqueue(new AuditEvent
        {
            TenantId = r.TenantId,
            Category = AuditCategory.MasterData,
            Action = action,
            Result = "Success",
            ObjectType = r.FormCode,
            ObjectId = id?.ToString(),
            NewValueJson = System.Text.Json.JsonSerializer.Serialize(r.Values)
        });
}
