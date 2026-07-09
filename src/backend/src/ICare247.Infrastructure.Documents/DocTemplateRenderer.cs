// File    : DocTemplateRenderer.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents)
// Purpose : Orchestrator IDocTemplateRenderer — đọc cấu hình (Config DB) → chạy proc (Data DB, check whitelist)
//           → mail-merge master + bảng detail → ghép fragment → xuất .docx/.pdf.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §7.2.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Documents.Internal;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Documents;

/// <summary>
/// Impl <see cref="IDocTemplateRenderer"/> — cầu nối DB ↔ <see cref="DocxRenderEngine"/>.
/// Chỉ đọc dữ liệu; không ghi DB, không phát event nghiệp vụ.
/// </summary>
internal sealed class DocTemplateRenderer : IDocTemplateRenderer
{
    private readonly DocTemplateRepository _repo;
    private readonly DocProcRunner _proc;
    private readonly DocxRenderEngine _engine;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DocTemplateRenderer> _logger;

    public DocTemplateRenderer(
        DocTemplateRepository repo, DocProcRunner proc, DocxRenderEngine engine,
        ITenantContext tenant, ILogger<DocTemplateRenderer> logger)
    {
        _repo = repo;
        _proc = proc;
        _engine = engine;
        _tenant = tenant;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocRenderResult> RenderAsync(
        long templateId, IReadOnlyDictionary<string, object?> keyParams,
        DocOutputFormat format, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;

        var tmpl = await _repo.GetTemplateAsync(templateId, tenantId, ct)
            ?? throw new InvalidOperationException($"Bộ mẫu #{templateId} không tồn tại hoặc chưa bật.");
        if (tmpl.MasterDocx is null || tmpl.MasterDocx.Length == 0)
            throw new InvalidOperationException($"Bộ mẫu '{tmpl.Ma}' chưa soạn fragment master.");

        var allParams = await _repo.GetParamsAsync(templateId, ct);

        // ── Master (A4 dọc): proc 1 dòng → mail-merge biến ──────────────────
        await EnsureRegisteredAsync(tmpl.MasterProc, tenantId, ct);
        var masterData = await _proc.ExecuteAsync(
            tmpl.MasterProc, BuildParams(allParams, detailId: null, keyParams, tenantId), ct);
        var masterMerged = _engine.MailMergeFragment(tmpl.MasterDocx, masterData);

        // ── Detail (A4 ngang): mỗi proc N dòng → nạp nguyên bảng ────────────
        var details = await _repo.GetDetailsAsync(templateId, ct);
        var detailBytes = new List<byte[]>(details.Count);
        foreach (var d in details)
        {
            if (d.DetailDocx is null || d.DetailDocx.Length == 0)
            {
                _logger.LogWarning("Bỏ qua detail '{Ma}' (chưa soạn fragment).", d.Ma);
                continue;
            }
            await EnsureRegisteredAsync(d.DetailProc, tenantId, ct);
            var data = await _proc.ExecuteAsync(
                d.DetailProc, BuildParams(allParams, detailId: d.Id, keyParams, tenantId), ct);
            detailBytes.Add(_engine.BuildDetailTable(d.DetailDocx, data));
        }

        // ── Ghép master + detail → xuất ─────────────────────────────────────
        var bytes = _engine.Assemble(masterMerged, detailBytes, format);
        return format == DocOutputFormat.Pdf
            ? new DocRenderResult(bytes, "application/pdf", $"{tmpl.Ma}.pdf")
            : new DocRenderResult(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{tmpl.Ma}.docx");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocVariable>> DescribeVariablesAsync(
        string procName, CancellationToken ct = default)
    {
        await EnsureRegisteredAsync(procName, _tenant.TenantId, ct);
        var cols = await _proc.DescribeAsync(procName, ct);
        return cols.Select(c => new DocVariable(c.Name, c.DbType)).ToList();
    }

    /// <summary>Chặn nếu proc không nằm trong whitelist Doc_Proc_Registry. Sự kiện theo sau: ném nếu không hợp lệ.</summary>
    private async Task EnsureRegisteredAsync(string procName, int tenantId, CancellationToken ct)
    {
        if (!await _repo.IsProcRegisteredAsync(procName, tenantId, ct))
            throw new InvalidOperationException($"Stored proc '{procName}' chưa đăng ký trong Doc_Proc_Registry.");
    }

    /// <summary>Dựng tham số proc từ ánh xạ Doc_Template_Param (theo master/detail). Không phát event.</summary>
    private static DynamicParameters BuildParams(
        IReadOnlyList<DocParamRow> allParams, long? detailId,
        IReadOnlyDictionary<string, object?> keyParams, int tenantId)
    {
        var dp = new DynamicParameters();
        foreach (var p in allParams.Where(x => x.DetailId == detailId))
        {
            object? value = p.Nguon switch
            {
                "key"     => keyParams.TryGetValue(p.NguonKey ?? p.ParamName.TrimStart('@'), out var v) ? v : null,
                "context" => string.Equals(p.NguonKey, "Tenant_Id", StringComparison.OrdinalIgnoreCase) ? tenantId : null,
                "const"   => p.NguonKey,
                _         => null
            };
            var name = p.ParamName.StartsWith('@') ? p.ParamName : "@" + p.ParamName;
            dp.Add(name, value);
        }
        return dp;
    }
}
