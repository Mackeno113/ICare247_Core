// File    : CommitImport.cs
// Module  : Import
// Layer   : Application
// Purpose : Command ghi các dòng hợp lệ (partial commit) qua SaveMasterDataCommand (sp_AfterSave_ tự nổ
//           mỗi dòng) + ghi log + gọi sp_AfterImport_<Table>. Spec 25 §11–§13, ADR-034.

using System.Security.Cryptography;
using System.Text.Json;
using ICare247.Application.Features.MasterData.Commands.SaveMasterData;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Import;

/// <summary>Yêu cầu ghi thật file import (đã xem preview). Trả null nếu View/Edit_Form không tồn tại.</summary>
public sealed record CommitImportCommand(
    string ViewCode, int TenantId, long UserId, string LangCode,
    byte[] FileBytes, string? FileName, string? CorrelationId)
    : IRequest<ImportCommitResult?>;

/// <summary>
/// Phân tích lại file → ghi dòng New/Update qua <see cref="SaveMasterDataCommand"/> (hook <c>sp_AfterSave_</c>
/// tự nổ mỗi dòng, rollback-on-fail); dòng lỗi ghi <c>Sys_Import_Log_Detail</c>; cuối mẻ gọi <c>sp_AfterImport_</c>.
/// </summary>
public sealed class CommitImportCommandHandler
    : IRequestHandler<CommitImportCommand, ImportCommitResult?>
{
    private readonly IImportMetadataProvider _meta;
    private readonly IImportEngine _engine;
    private readonly IImportLogRepository _log;
    private readonly IPermissionService _perm;
    private readonly IMediator _mediator;
    private readonly IConfigCache _config;
    private readonly ILogger<CommitImportCommandHandler> _logger;

    public CommitImportCommandHandler(
        IImportMetadataProvider meta, IImportEngine engine, IImportLogRepository log,
        IPermissionService perm, IMediator mediator, IConfigCache config,
        ILogger<CommitImportCommandHandler> logger)
    {
        _meta = meta;
        _engine = engine;
        _log = log;
        _perm = perm;
        _mediator = mediator;
        _config = config;
        _logger = logger;
    }

    /// <summary>Ghi các dòng hợp lệ + log + hook. Sự kiện theo sau: controller trả kết quả commit cho client.</summary>
    public async Task<ImportCommitResult?> Handle(CommitImportCommand r, CancellationToken ct)
    {
        var ctx = await _meta.BuildAsync(r.ViewCode, r.TenantId, r.LangCode, ct);
        if (ctx is null)
            return null;

        var req = new ImportPlanRequest(
            ctx.View, ctx.Schema, ctx.TargetTable, ctx.PkColumn, ctx.SheetName,
            ctx.Fields, ctx.KeyFields, ctx.FkColumns);

        using var ms = new MemoryStream(r.FileBytes, writable: false);
        var plan = await _engine.BuildPlanAsync(req, ms, ct);

        // ── Kiểm quyền: New cần Form.Them · Update cần Form.Sua (deny-by-default) ──
        var hasNew = plan.Rows.Any(x => x.Operation == ImportRowOperation.New);
        var hasUpd = plan.Rows.Any(x => x.Operation == ImportRowOperation.Update);
        var permErrors = new List<string>();
        if (hasNew && !await _perm.HasPermissionForTargetAsync(r.UserId, "Form", ctx.FormCode, PermissionOp.Them, ct))
            permErrors.Add(await Msg("import.perm.no_add", "Bạn không có quyền thêm dữ liệu.", r, ct));
        if (hasUpd && !await _perm.HasPermissionForTargetAsync(r.UserId, "Form", ctx.FormCode, PermissionOp.Sua, ct))
            permErrors.Add(await Msg("import.perm.no_edit", "Bạn không có quyền cập nhật dữ liệu.", r, ct));
        if (plan.FileErrors.Count > 0 || permErrors.Count > 0)
        {
            var fe = new List<string>(permErrors);
            foreach (var e in plan.FileErrors)
                fe.Add(await ImportMessageResolver.ResolveAsync(_config, e, r.LangCode, r.TenantId, ct));
            return new ImportCommitResult(Guid.Empty, "Failed", ToSummary(plan.Summary), fe, []);
        }

        // ── Mở log mẻ ───────────────────────────────────────────────────────
        var started = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var mode = ctx.KeyFields.Count > 0 ? "upsert" : "insert";
        var logId = await _log.CreateAsync(new ImportLogHeader(
            sessionId, ctx.View.ViewCode, ctx.TargetTable, r.FileName, r.FileBytes.LongLength,
            HashFile(r.FileBytes), mode, "Validating", started, r.UserId, r.CorrelationId), ct);

        // ── Ghi từng dòng hợp lệ (partial commit) ───────────────────────────
        var details = new List<ImportLogDetail>();
        var recordIds = new List<long>();
        int inserted = 0, updated = 0, commitErrors = 0;

        foreach (var row in plan.Rows)
        {
            ct.ThrowIfCancellationRequested();

            if (row.Operation == ImportRowOperation.Error)
            {
                details.Add(ToErrorDetail(row));
                continue;
            }
            if (row.Operation != ImportRowOperation.New && row.Operation != ImportRowOperation.Update)
                continue;

            var values = new Dictionary<string, object?>(row.Values);
            object? id = row.Operation == ImportRowOperation.Update ? row.MatchedId : null;
            // Source="IMPORT" + sessionId → hook sp_AfterSave_ nhận ngữ cảnh import (§12.1).
            var save = await _mediator.Send(
                new SaveMasterDataCommand(ctx.FormCode, r.TenantId, id, values, r.UserId,
                    Source: "IMPORT", ImportSessionId: sessionId), ct);

            if (save.Success)
            {
                if (row.Operation == ImportRowOperation.Update) updated++;
                else inserted++;
                if (save.Id is not null && long.TryParse(save.Id.ToString(), out var rid))
                    recordIds.Add(rid);
            }
            else
            {
                // Lỗi lúc ghi (validation/store) → chuyển thành dòng lỗi (hook đã rollback dòng đó).
                commitErrors++;
                var msg = string.Join("; ", save.Errors.Select(e => e.Message));
                details.Add(new ImportLogDetail(
                    row.RowNumber, "ERROR", null, "sys.msg.raw",
                    JsonSerializer.Serialize(new[] { msg }),
                    save.Errors.FirstOrDefault()?.FieldCode, null));
            }
        }

        await _log.AddErrorDetailsAsync(logId, details, ct);

        // ── Hoàn tất + trạng thái ───────────────────────────────────────────
        var totalError = plan.Summary.Error + commitErrors;
        var status = totalError == 0 ? "Committed"
                   : (inserted + updated > 0 ? "PartialSuccess" : "Failed");
        var finished = DateTime.UtcNow;
        await _log.CompleteAsync(logId, new ImportLogCompletion(
            plan.Summary.Total, inserted, updated, totalError, plan.Summary.Skipped,
            status, finished, (int)(finished - started).TotalMilliseconds), ct);

        // ── Hook sau import (best-effort — lỗi KHÔNG rollback dữ liệu đã ghi) ─
        try
        {
            await _log.RunAfterImportAsync(new ImportAfterHookArgs(
                ctx.Schema, ctx.TargetTable, sessionId, r.UserId, r.TenantId,
                inserted, updated, totalError, recordIds, finished), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Import {View}: sp_AfterImport_{Table} lỗi (dữ liệu đã ghi, không rollback).",
                ctx.View.ViewCode, ctx.TargetTable);
        }

        _logger.LogInformation(
            "Import {View} session={Session} status={Status}: +{Ins} thêm, {Upd} sửa, {Err} lỗi.",
            ctx.View.ViewCode, sessionId, status, inserted, updated, totalError);

        // ── Dựng danh sách dòng lỗi trả về (đã dịch) ────────────────────────
        var errorRows = new List<ImportPreviewRow>();
        foreach (var row in plan.Rows.Where(x => x.Operation == ImportRowOperation.Error))
        {
            var errs = await ImportMessageResolver.ResolveRowAsync(_config, row.Errors, r.LangCode, r.TenantId, ct);
            errorRows.Add(new ImportPreviewRow(row.RowNumber, "Error", null, errs));
        }

        var summary = new ImportPreviewSummary(plan.Summary.Total, inserted, updated, totalError, plan.Summary.Skipped);
        return new ImportCommitResult(sessionId, status, summary, [], errorRows);
    }

    /// <summary>Chuyển 1 dòng lỗi (validate) → bản ghi log detail (lấy lỗi đầu tiên).</summary>
    private static ImportLogDetail ToErrorDetail(ImportRow row)
    {
        var first = row.Errors.Count > 0 ? row.Errors[0] : null;
        return new ImportLogDetail(
            row.RowNumber, "ERROR", null,
            first?.ErrorKey,
            first is null ? null : JsonSerializer.Serialize(first.Args),
            first?.FieldName,
            row.MaskedRowJson);
    }

    /// <summary>SHA-256 hex của file (File_Hash — chống import trùng file).</summary>
    private static string HashFile(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static ImportPreviewSummary ToSummary(ImportSummary s) =>
        new(s.Total, s.New, s.Update, s.Error, s.Skipped);

    /// <summary>Resolve message theo key (fallback text) cho lỗi cấp mẻ như thiếu quyền.</summary>
    private async Task<string> Msg(string key, string fallback, CommitImportCommand r, CancellationToken ct)
        => await _config.ResolveKeyAsync(key, r.LangCode, r.TenantId, ct) ?? fallback;
}
