// File    : FormDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Truy vấn Ui_Form qua Dapper, trả FormRecord cho ViewModels.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Implementation IFormDataService dùng Dapper query thẳng SQL Server.
/// Mọi query đều parameterized, filter Tenant_Id bắt buộc.
/// </summary>
public sealed class FormDataService : IFormDataService
{
    private readonly IAppConfigService _config;

    public FormDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FormRecord>> GetAllFormsAsync(
        int tenantId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        // NULL-SAFE: Chưa cấu hình DB → trả list rỗng, không throw
        if (!_config.IsConfigured) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        // ── 1. Build SQL ─────────────────────────────────────
        // NOTE: ISNULL để fallback an toàn nếu cột audit chưa có trong schema cũ
        const string sql = """
            SELECT f.Form_Id       AS FormId,
                   f.Form_Code     AS FormCode,
                   f.Form_Name     AS FormName,
                   f.Version,
                   f.Platform,
                   f.Is_Active     AS IsActive,
                   ISNULL(f.Updated_At, GETDATE()) AS UpdatedAt,
                   ISNULL(f.Updated_By, '')        AS UpdatedBy,
                   (SELECT COUNT(*)
                    FROM   dbo.Ui_Section s
                    WHERE  s.Form_Id = f.Form_Id
                      AND  s.Is_Active = 1)        AS SectionCount,
                   (SELECT COUNT(*)
                    FROM   dbo.Ui_Field fi
                    WHERE  fi.Form_Id = f.Form_Id
                      AND  fi.Is_Active = 1)       AS FieldCount
            FROM   dbo.Ui_Form f
            WHERE  f.Tenant_Id = @TenantId
              AND  (@IncludeInactive = 1 OR f.Is_Active = 1)
            ORDER BY f.Form_Code
            """;

        // ── 2. Execute ────────────────────────────────────────
        var result = await conn.QueryAsync<FormRecord>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, IncludeInactive = includeInactive ? 1 : 0 },
                cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> CreateFormAsync(
        string formCode,
        string formName,
        string platform,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");

        await using var conn = new SqlConnection(_config.ConnectionString);

        // ── INSERT + lấy lại Form_Id vừa tạo ────────────────
        const string sql = """
            INSERT INTO dbo.Ui_Form
                (Tenant_Id, Form_Code, Form_Name, Version, Platform,
                 Is_Active, Created_At, Created_By, Updated_At, Updated_By)
            VALUES
                (@TenantId, @FormCode, @FormName, 1, @Platform,
                 1, GETDATE(), 'system', GETDATE(), 'system');

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, FormCode = formCode, FormName = formName, Platform = platform },
                cancellationToken: ct));
    }
}
