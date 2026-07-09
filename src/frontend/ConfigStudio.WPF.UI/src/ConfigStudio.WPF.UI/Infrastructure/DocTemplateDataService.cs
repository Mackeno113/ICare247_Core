// File    : DocTemplateDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper impl IDocTemplateDataService — CRUD Doc_Template*/nạp-lưu bytes fragment (Config DB).
//           ConfigStudio nối thẳng DB. CreatedBy = SystemUserId (tool không có phiên người dùng).
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §4, §8.

using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>CRUD bộ mẫu tài liệu trên Config DB (Ui_* cùng nhóm). Lọc theo Tenant_Id hiện tại.</summary>
public sealed class DocTemplateDataService : IDocTemplateDataService
{
    private const long SystemUserId = 1;   // sentinel CreatedBy — ConfigStudio không có phiên người dùng

    private readonly IAppConfigService _config;

    public DocTemplateDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocTemplateListItem>> GetTemplatesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];
        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<DocTemplateListItem>(new CommandDefinition(
            "SELECT Id, Ma, Ten FROM dbo.Doc_Template WHERE Tenant_Id=@TenantId AND IsDeleted=0 ORDER BY Ma",
            new { TenantId = _config.TenantId }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task<long> CreateTemplateAsync(
        string ma, string ten, string masterProc, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO dbo.Doc_Template (Tenant_Id, Ma, Ten, Master_Proc, Is_Active, CreatedBy, CreatedAt) " +
            "VALUES (@TenantId, @Ma, @Ten, @MasterProc, 1, @By, SYSUTCDATETIME()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
            new { TenantId = _config.TenantId, Ma = ma, Ten = ten, MasterProc = masterProc, By = SystemUserId },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocDetailListItem>> GetDetailsAsync(
        long templateId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];
        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<DocDetailListItem>(new CommandDefinition(
            "SELECT Id, Ma, Ten FROM dbo.Doc_Template_Detail " +
            "WHERE Template_Id=@TemplateId AND IsDeleted=0 ORDER BY Thu_Tu, Id",
            new { TemplateId = templateId }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task<long> CreateDetailAsync(
        long templateId, string ma, string ten, string detailProc, int thuTu, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO dbo.Doc_Template_Detail (Template_Id, Ma, Ten, Detail_Proc, Thu_Tu, Is_Active, CreatedBy, CreatedAt) " +
            "VALUES (@TemplateId, @Ma, @Ten, @DetailProc, @ThuTu, 1, @By, SYSUTCDATETIME()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
            new { TemplateId = templateId, Ma = ma, Ten = ten, DetailProc = detailProc, ThuTu = thuTu, By = SystemUserId },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetMasterDocxAsync(long templateId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<byte[]?>(new CommandDefinition(
            "SELECT Master_Docx FROM dbo.Doc_Template WHERE Id=@Id AND Tenant_Id=@TenantId AND IsDeleted=0",
            new { Id = templateId, TenantId = _config.TenantId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SaveMasterDocxAsync(long templateId, byte[] docx, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Doc_Template SET Master_Docx=@Docx, UpdatedBy=@By, UpdatedAt=SYSUTCDATETIME(), Ver=Ver+1 " +
            "WHERE Id=@Id AND Tenant_Id=@TenantId",
            new { Docx = docx, Id = templateId, TenantId = _config.TenantId, By = SystemUserId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetDetailDocxAsync(long detailId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<byte[]?>(new CommandDefinition(
            "SELECT Detail_Docx FROM dbo.Doc_Template_Detail WHERE Id=@Id AND IsDeleted=0",
            new { Id = detailId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SaveDetailDocxAsync(long detailId, byte[] docx, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Doc_Template_Detail SET Detail_Docx=@Docx, UpdatedBy=@By, UpdatedAt=SYSUTCDATETIME(), Ver=Ver+1 " +
            "WHERE Id=@Id",
            new { Docx = docx, Id = detailId, By = SystemUserId }, cancellationToken: ct));
    }
}
