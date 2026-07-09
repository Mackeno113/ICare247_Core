// File    : DocTemplateRepository.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents)
// Purpose : Đọc cấu hình Doc_Template* trên Config DB (Dapper). Chỉ đọc — không ghi, không phát event.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §4, §5.3.

using System.Data;
using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents.Internal;

/// <summary>Repository đọc bộ mẫu + detail + tham số + whitelist proc (Config DB).</summary>
internal sealed class DocTemplateRepository
{
    private readonly IDbConnectionFactory _config;

    public DocTemplateRepository(IDbConnectionFactory config) => _config = config;

    /// <summary>
    /// Lấy 1 bộ mẫu master theo Id + tenant (chỉ bản Active, chưa xóa).
    /// Sự kiện theo sau: null nếu không tồn tại/không thuộc tenant → caller trả 404.
    /// </summary>
    public async Task<DocTemplateRow?> GetTemplateAsync(long id, int tenantId, CancellationToken ct)
    {
        using var conn = _config.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<DocTemplateRow>(new CommandDefinition(
            "SELECT Id, Ma, Ten, Master_Proc AS MasterProc, Master_Docx AS MasterDocx " +
            "FROM dbo.Doc_Template WHERE Id=@id AND Tenant_Id=@tenantId AND IsDeleted=0 AND Is_Active=1",
            new { id, tenantId }, cancellationToken: ct));
    }

    /// <summary>Lấy các mảnh detail của bộ mẫu, sắp theo Thu_Tu (thứ tự ghép/in). Không phát event.</summary>
    public async Task<IReadOnlyList<DocDetailRow>> GetDetailsAsync(long templateId, CancellationToken ct)
    {
        using var conn = _config.CreateConnection();
        var rows = await conn.QueryAsync<DocDetailRow>(new CommandDefinition(
            "SELECT Id, Ma, Ten, Detail_Proc AS DetailProc, Detail_Docx AS DetailDocx, Thu_Tu AS ThuTu " +
            "FROM dbo.Doc_Template_Detail WHERE Template_Id=@templateId AND IsDeleted=0 AND Is_Active=1 " +
            "ORDER BY Thu_Tu, Id", new { templateId }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>Lấy ánh xạ tham số của bộ mẫu (cả master lẫn detail). Không phát event.</summary>
    public async Task<IReadOnlyList<DocParamRow>> GetParamsAsync(long templateId, CancellationToken ct)
    {
        using var conn = _config.CreateConnection();
        var rows = await conn.QueryAsync<DocParamRow>(new CommandDefinition(
            "SELECT Detail_Id AS DetailId, Param_Name AS ParamName, Nguon, Nguon_Key AS NguonKey, Kieu " +
            "FROM dbo.Doc_Template_Param WHERE Template_Id=@templateId AND IsDeleted=0", new { templateId },
            cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Kiểm 1 proc có nằm trong whitelist <c>Doc_Proc_Registry</c> (Active) của tenant không.
    /// Sự kiện theo sau: false → caller từ chối render (chặn gọi proc tùy tiện).
    /// </summary>
    public async Task<bool> IsProcRegisteredAsync(string procName, int tenantId, CancellationToken ct)
    {
        using var conn = _config.CreateConnection();
        var ok = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Doc_Proc_Registry " +
            "WHERE Proc_Name=@procName AND Tenant_Id=@tenantId AND IsDeleted=0 AND Is_Active=1) THEN 1 ELSE 0 END",
            new { procName, tenantId }, cancellationToken: ct));
        return ok == 1;
    }
}
