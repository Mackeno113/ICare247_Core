// File    : ValidateImportFile.cs
// Module  : Import
// Layer   : Application
// Purpose : Command dry-run — đọc file + validate → preview NEW/UPDATE/ERROR (chưa ghi DB). Spec 25 §11, ADR-034.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Import;

/// <summary>Yêu cầu kiểm tra file import (không ghi). Trả null nếu View/Edit_Form không tồn tại.</summary>
public sealed record ValidateImportFileCommand(
    string ViewCode, int TenantId, string LangCode, byte[] FileBytes,
    ImportMode Mode = ImportMode.Upsert)
    : IRequest<ImportPreviewResult?>;

/// <summary>Chạy <see cref="IImportEngine"/> → dựng preview + resolve thông báo lỗi i18n.</summary>
public sealed class ValidateImportFileCommandHandler
    : IRequestHandler<ValidateImportFileCommand, ImportPreviewResult?>
{
    private readonly IImportMetadataProvider _meta;
    private readonly IImportEngine _engine;
    private readonly IConfigCache _config;

    public ValidateImportFileCommandHandler(
        IImportMetadataProvider meta, IImportEngine engine, IConfigCache config)
    {
        _meta = meta;
        _engine = engine;
        _config = config;
    }

    /// <summary>Phân tích + dựng preview. Sự kiện theo sau: controller trả preview cho client xác nhận.</summary>
    public async Task<ImportPreviewResult?> Handle(ValidateImportFileCommand r, CancellationToken ct)
    {
        var ctx = await _meta.BuildAsync(r.ViewCode, r.TenantId, r.LangCode, ct);
        if (ctx is null)
            return null;

        var req = new ImportPlanRequest(
            ctx.View, ctx.Schema, ctx.TargetTable, ctx.PkColumn, ctx.SheetName,
            ctx.Fields, ctx.KeyFields, ctx.FkColumns, r.Mode);

        using var ms = new MemoryStream(r.FileBytes, writable: false);
        var plan = await _engine.BuildPlanAsync(req, ms, ct);

        var fileErrors = new List<string>(plan.FileErrors.Count);
        foreach (var e in plan.FileErrors)
            fileErrors.Add(await ImportMessageResolver.ResolveAsync(_config, e, r.LangCode, r.TenantId, ct));

        var rows = new List<ImportPreviewRow>(plan.Rows.Count);
        foreach (var row in plan.Rows)
        {
            var errs = await ImportMessageResolver.ResolveRowAsync(_config, row.Errors, r.LangCode, r.TenantId, ct);
            rows.Add(new ImportPreviewRow(row.RowNumber, row.Operation.ToString(), row.MatchedId, errs));
        }

        return new ImportPreviewResult(ToSummary(plan.Summary), fileErrors, rows);
    }

    /// <summary>Chuyển summary engine → DTO API.</summary>
    private static ImportPreviewSummary ToSummary(ImportSummary s) =>
        new(s.Total, s.New, s.Update, s.Error, s.Skipped);
}
