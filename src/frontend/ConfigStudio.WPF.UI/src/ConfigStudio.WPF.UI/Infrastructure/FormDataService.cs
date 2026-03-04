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

        // ── 1. Dò schema để tránh lỗi cột không tồn tại ──────
        var formCols = await GetTableColumnsAsync(conn, "dbo", "Ui_Form", ct);
        if (formCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Ui_Form.");
        if (!formCols.Contains("Form_Id") || !formCols.Contains("Form_Code"))
            throw new InvalidOperationException("Bảng dbo.Ui_Form thiếu cột bắt buộc Form_Id/Form_Code.");

        var sectionCols = await GetTableColumnsAsync(conn, "dbo", "Ui_Section", ct);
        var fieldCols = await GetTableColumnsAsync(conn, "dbo", "Ui_Field", ct);
        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);

        var useTenantFromForm = formCols.Contains("Tenant_Id");
        var useTenantFromSysTable = !useTenantFromForm
            && formCols.Contains("Table_Id")
            && sysTableCols.Contains("Table_Id")
            && sysTableCols.Contains("Tenant_Id");

        if (!useTenantFromForm && !useTenantFromSysTable)
            throw new InvalidOperationException(
                "Không tìm được cột tenant cho Ui_Form. Cần Ui_Form.Tenant_Id hoặc mapping Ui_Form.Table_Id -> Sys_Table.Tenant_Id.");

        var formNameExpr = formCols.Contains("Form_Name")
            ? "f.Form_Name AS FormName"
            : formCols.Contains("Description")
                ? "ISNULL(f.Description, f.Form_Code) AS FormName"
                : "CAST('' AS nvarchar(255)) AS FormName";

        var versionExpr = formCols.Contains("Version")
            ? "f.Version AS Version"
            : "CAST(1 AS int) AS Version";

        var platformExpr = formCols.Contains("Platform")
            ? "f.Platform AS Platform"
            : "CAST('web' AS nvarchar(50)) AS Platform";

        var isActiveExpr = formCols.Contains("Is_Active")
            ? "f.Is_Active AS IsActive"
            : "CAST(1 AS bit) AS IsActive";

        var updatedAtExpr = formCols.Contains("Updated_At")
            ? "ISNULL(f.Updated_At, GETDATE()) AS UpdatedAt"
            : formCols.Contains("Created_At")
                ? "ISNULL(f.Created_At, GETDATE()) AS UpdatedAt"
                : "GETDATE() AS UpdatedAt";

        var updatedByExpr = formCols.Contains("Updated_By")
            ? "ISNULL(f.Updated_By, '') AS UpdatedBy"
            : formCols.Contains("Created_By")
                ? "ISNULL(f.Created_By, '') AS UpdatedBy"
                : "CAST('' AS nvarchar(255)) AS UpdatedBy";

        var sectionCountExpr = BuildCountExpr("Ui_Section", "s", sectionCols, "SectionCount");
        var fieldCountExpr = BuildCountExpr("Ui_Field", "fi", fieldCols, "FieldCount");

        var fromClause = useTenantFromSysTable
            ? "dbo.Ui_Form f\n            INNER JOIN dbo.Sys_Table st ON st.Table_Id = f.Table_Id"
            : "dbo.Ui_Form f";

        var whereParts = new List<string>
        {
            useTenantFromSysTable ? "st.Tenant_Id = @TenantId" : "f.Tenant_Id = @TenantId"
        };
        if (formCols.Contains("Is_Active"))
            whereParts.Add("(@IncludeInactive = 1 OR f.Is_Active = 1)");
        if (useTenantFromSysTable && sysTableCols.Contains("Is_Active"))
            whereParts.Add("st.Is_Active = 1");

        var sql = $"""
            SELECT f.Form_Id       AS FormId,
                   f.Form_Code     AS FormCode,
                   {formNameExpr},
                   {versionExpr},
                   {platformExpr},
                   {isActiveExpr},
                   {updatedAtExpr},
                   {updatedByExpr},
                   {sectionCountExpr},
                   {fieldCountExpr}
            FROM   {fromClause}
            WHERE  {string.Join("\n              AND  ", whereParts)}
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
    public async Task<IReadOnlyList<TableLookupRecord>> GetTablesByTenantAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (sysTableCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Sys_Table.");
        if (!sysTableCols.Contains("Table_Id") || !sysTableCols.Contains("Tenant_Id"))
            throw new InvalidOperationException("Bảng dbo.Sys_Table thiếu cột bắt buộc Table_Id/Tenant_Id.");

        var tableCodeExpr = sysTableCols.Contains("Table_Code")
            ? "st.Table_Code AS TableCode"
            : "CAST(CONCAT('TABLE_', st.Table_Id) AS nvarchar(128)) AS TableCode";

        var tableNameExpr = sysTableCols.Contains("Table_Name")
            ? "ISNULL(st.Table_Name, '') AS TableName"
            : "CAST('' AS nvarchar(255)) AS TableName";

        var schemaExpr = sysTableCols.Contains("Schema_Name")
            ? "ISNULL(st.Schema_Name, '') AS SchemaName"
            : "CAST('dbo' AS nvarchar(128)) AS SchemaName";

        var descriptionExpr = sysTableCols.Contains("Description")
            ? "ISNULL(st.Description, '') AS Description"
            : "CAST('' AS nvarchar(4000)) AS Description";

        var whereParts = new List<string> { "st.Tenant_Id = @TenantId" };
        if (sysTableCols.Contains("Is_Active"))
            whereParts.Add("st.Is_Active = 1");
        if (sysTableCols.Contains("Is_Tenant"))
            whereParts.Add("st.Is_Tenant = 1");

        var orderBy = sysTableCols.Contains("Table_Code")
            ? "st.Table_Code"
            : "st.Table_Id";

        var sql = $"""
            SELECT st.Table_Id AS TableId,
                   {tableCodeExpr},
                   {tableNameExpr},
                   {schemaExpr},
                   {descriptionExpr}
            FROM   dbo.Sys_Table st
            WHERE  {string.Join("\n              AND  ", whereParts)}
            ORDER BY {orderBy}
            """;

        var result = await conn.QueryAsync<TableLookupRecord>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId },
                cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysTableRecord>> GetSysTablesAsync(
        int tenantId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (sysTableCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Sys_Table.");
        if (!sysTableCols.Contains("Table_Id") || !sysTableCols.Contains("Tenant_Id"))
            throw new InvalidOperationException("Bảng dbo.Sys_Table thiếu cột bắt buộc Table_Id/Tenant_Id.");

        var tableCodeExpr = sysTableCols.Contains("Table_Code")
            ? "st.Table_Code AS TableCode"
            : "CAST(CONCAT('TABLE_', st.Table_Id) AS nvarchar(128)) AS TableCode";
        var tableNameExpr = sysTableCols.Contains("Table_Name")
            ? "ISNULL(st.Table_Name, '') AS TableName"
            : "CAST('' AS nvarchar(255)) AS TableName";
        var schemaExpr = sysTableCols.Contains("Schema_Name")
            ? "ISNULL(st.Schema_Name, 'dbo') AS SchemaName"
            : "CAST('dbo' AS nvarchar(128)) AS SchemaName";
        var isTenantExpr = sysTableCols.Contains("Is_Tenant")
            ? "st.Is_Tenant AS IsTenant"
            : "CAST(1 AS bit) AS IsTenant";
        var versionExpr = sysTableCols.Contains("Version")
            ? "st.Version AS Version"
            : "CAST(1 AS int) AS Version";
        var checksumExpr = sysTableCols.Contains("Checksum")
            ? "ISNULL(st.Checksum, '') AS Checksum"
            : "CAST('' AS nvarchar(255)) AS Checksum";
        var isActiveExpr = sysTableCols.Contains("Is_Active")
            ? "st.Is_Active AS IsActive"
            : "CAST(1 AS bit) AS IsActive";
        var createdAtExpr = sysTableCols.Contains("Created_At")
            ? "st.Created_At AS CreatedAt"
            : "CAST(NULL AS datetime) AS CreatedAt";
        var updatedAtExpr = sysTableCols.Contains("Updated_At")
            ? "st.Updated_At AS UpdatedAt"
            : "CAST(NULL AS datetime) AS UpdatedAt";
        var descriptionExpr = sysTableCols.Contains("Description")
            ? "ISNULL(st.Description, '') AS Description"
            : "CAST('' AS nvarchar(max)) AS Description";

        var whereParts = new List<string>
        {
            "st.Tenant_Id = @TenantId"
        };
        if (!includeInactive && sysTableCols.Contains("Is_Active"))
            whereParts.Add("st.Is_Active = 1");

        var orderBy = sysTableCols.Contains("Table_Code")
            ? "st.Table_Code"
            : "st.Table_Id";

        var sql = $"""
            SELECT st.Table_Id AS TableId,
                   {tableCodeExpr},
                   {tableNameExpr},
                   {schemaExpr},
                   {isTenantExpr},
                   st.Tenant_Id AS TenantId,
                   {versionExpr},
                   {checksumExpr},
                   {isActiveExpr},
                   {createdAtExpr},
                   {updatedAtExpr},
                   {descriptionExpr}
            FROM   dbo.Sys_Table st
            WHERE  {string.Join("\n              AND  ", whereParts)}
            ORDER BY {orderBy}
            """;

        var result = await conn.QueryAsync<SysTableRecord>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId },
                cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> CreateSysTableAsync(
        string tableCode,
        string tableName,
        string schemaName,
        bool isTenant,
        int tenantId,
        string? description = null,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");

        var normalizedCode = tableCode.Trim();
        var normalizedName = tableName.Trim();
        var normalizedSchema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName.Trim();
        var normalizedDescription = description?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
            throw new InvalidOperationException("Table_Code không được để trống.");
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new InvalidOperationException("Table_Name không được để trống.");

        await using var conn = new SqlConnection(_config.ConnectionString);

        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (sysTableCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Sys_Table.");
        if (!sysTableCols.Contains("Table_Code") || !sysTableCols.Contains("Tenant_Id"))
            throw new InvalidOperationException("Bảng dbo.Sys_Table thiếu cột bắt buộc Table_Code/Tenant_Id.");

        var duplicateSql = """
            SELECT TOP (1) 1
            FROM   dbo.Sys_Table
            WHERE  Table_Code = @TableCode
              AND  Tenant_Id  = @TenantId
            """;
        var duplicate = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                duplicateSql,
                new { TableCode = normalizedCode, TenantId = tenantId },
                cancellationToken: ct));
        if (duplicate.HasValue)
            throw new InvalidOperationException(
                $"Table_Code '{normalizedCode}' đã tồn tại trong tenant {tenantId}.");

        var insertCols = new List<string> { "Table_Code", "Tenant_Id" };
        var insertVals = new List<string> { "@TableCode", "@TenantId" };

        if (sysTableCols.Contains("Table_Name"))
        {
            insertCols.Add("Table_Name");
            insertVals.Add("@TableName");
        }
        if (sysTableCols.Contains("Schema_Name"))
        {
            insertCols.Add("Schema_Name");
            insertVals.Add("@SchemaName");
        }
        if (sysTableCols.Contains("Is_Tenant"))
        {
            insertCols.Add("Is_Tenant");
            insertVals.Add("@IsTenant");
        }
        if (sysTableCols.Contains("Version"))
        {
            insertCols.Add("Version");
            insertVals.Add("1");
        }
        if (sysTableCols.Contains("Checksum"))
        {
            insertCols.Add("Checksum");
            insertVals.Add("''");
        }
        if (sysTableCols.Contains("Is_Active"))
        {
            insertCols.Add("Is_Active");
            insertVals.Add("1");
        }
        if (sysTableCols.Contains("Created_At"))
        {
            insertCols.Add("Created_At");
            insertVals.Add("GETDATE()");
        }
        if (sysTableCols.Contains("Updated_At"))
        {
            insertCols.Add("Updated_At");
            insertVals.Add("GETDATE()");
        }
        if (sysTableCols.Contains("Description"))
        {
            insertCols.Add("Description");
            insertVals.Add("@Description");
        }

        var sql = $"""
            INSERT INTO dbo.Sys_Table
                ({string.Join(", ", insertCols)})
            VALUES
                ({string.Join(", ", insertVals)});

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    TableCode = normalizedCode,
                    TableName = normalizedName,
                    SchemaName = normalizedSchema,
                    IsTenant = isTenant,
                    TenantId = tenantId,
                    Description = normalizedDescription,
                },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateSysTableAsync(
        int tableId,
        string tableCode,
        string tableName,
        string schemaName,
        bool isTenant,
        bool isActive,
        int tenantId,
        string? description = null,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");

        var normalizedCode = tableCode.Trim();
        var normalizedName = tableName.Trim();
        var normalizedSchema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName.Trim();
        var normalizedDescription = description?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
            throw new InvalidOperationException("Table_Code không được để trống.");
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new InvalidOperationException("Table_Name không được để trống.");

        await using var conn = new SqlConnection(_config.ConnectionString);

        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (sysTableCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Sys_Table.");
        if (!sysTableCols.Contains("Table_Id") || !sysTableCols.Contains("Tenant_Id"))
            throw new InvalidOperationException("Bảng dbo.Sys_Table thiếu cột bắt buộc Table_Id/Tenant_Id.");
        if (!sysTableCols.Contains("Table_Code"))
            throw new InvalidOperationException("Bảng dbo.Sys_Table thiếu cột bắt buộc Table_Code.");

        var duplicateSql = """
            SELECT TOP (1) 1
            FROM   dbo.Sys_Table
            WHERE  Table_Id   <> @TableId
              AND  Table_Code = @TableCode
              AND  Tenant_Id  = @TenantId
            """;
        var duplicate = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                duplicateSql,
                new { TableId = tableId, TableCode = normalizedCode, TenantId = tenantId },
                cancellationToken: ct));
        if (duplicate.HasValue)
            throw new InvalidOperationException(
                $"Table_Code '{normalizedCode}' đã tồn tại trong tenant {tenantId}.");

        var setParts = new List<string>();
        if (sysTableCols.Contains("Table_Code"))
            setParts.Add("Table_Code = @TableCode");
        if (sysTableCols.Contains("Table_Name"))
            setParts.Add("Table_Name = @TableName");
        if (sysTableCols.Contains("Schema_Name"))
            setParts.Add("Schema_Name = @SchemaName");
        if (sysTableCols.Contains("Is_Tenant"))
            setParts.Add("Is_Tenant = @IsTenant");
        if (sysTableCols.Contains("Is_Active"))
            setParts.Add("Is_Active = @IsActive");
        if (sysTableCols.Contains("Description"))
            setParts.Add("Description = @Description");
        if (sysTableCols.Contains("Updated_At"))
            setParts.Add("Updated_At = GETDATE()");

        if (setParts.Count == 0)
            throw new InvalidOperationException("Không tìm thấy cột hợp lệ để update Sys_Table.");

        var sql = $"""
            UPDATE dbo.Sys_Table
            SET    {string.Join(", ", setParts)}
            WHERE  Table_Id = @TableId
              AND  Tenant_Id = @TenantId
            """;

        var affected = await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    TableId = tableId,
                    TableCode = normalizedCode,
                    TableName = normalizedName,
                    SchemaName = normalizedSchema,
                    IsTenant = isTenant,
                    IsActive = isActive,
                    TenantId = tenantId,
                    Description = normalizedDescription,
                },
                cancellationToken: ct));

        if (affected == 0)
            throw new InvalidOperationException(
                $"Không tìm thấy Sys_Table.Table_Id={tableId} trong tenant {tenantId} để cập nhật.");
    }

    /// <inheritdoc />
    public async Task<int> CreateFormAsync(
        string formCode,
        string formName,
        string platform,
        int tenantId,
        int? tableId = null,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");

        await using var conn = new SqlConnection(_config.ConnectionString);

        var formCols = await GetTableColumnsAsync(conn, "dbo", "Ui_Form", ct);
        if (formCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Ui_Form.");
        if (!formCols.Contains("Form_Code"))
            throw new InvalidOperationException("Bảng dbo.Ui_Form thiếu cột bắt buộc Form_Code.");

        var insertCols = new List<string> { "Form_Code" };
        var insertVals = new List<string> { "@FormCode" };
        int? tableIdForInsert = null;

        if (formCols.Contains("Tenant_Id"))
        {
            insertCols.Insert(0, "Tenant_Id");
            insertVals.Insert(0, "@TenantId");
        }
        else if (formCols.Contains("Table_Id"))
        {
            // NOTE: Ưu tiên Table_Id từ UI để tránh tạo sai bảng metadata.
            if (tableId.HasValue)
            {
                var isValidTable = await IsTableInTenantAsync(conn, tableId.Value, tenantId, ct);
                if (!isValidTable)
                {
                    throw new InvalidOperationException(
                        $"Table_Id={tableId.Value} không thuộc tenant {tenantId} hoặc không active.");
                }
                tableIdForInsert = tableId.Value;
            }
            else
            {
                // NOTE: Backward-compatible cho flow cũ chưa truyền Table_Id.
                tableIdForInsert = await ResolveTableIdForTenantAsync(conn, tenantId, ct);
            }

            if (!tableIdForInsert.HasValue)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy Sys_Table hợp lệ cho tenant {tenantId}. Vui lòng chọn Table_Id trước khi tạo form.");
            }

            insertCols.Insert(0, "Table_Id");
            insertVals.Insert(0, "@TableId");
        }

        if (formCols.Contains("Form_Name"))
        {
            insertCols.Add("Form_Name");
            insertVals.Add("@FormName");
        }
        else if (formCols.Contains("Description"))
        {
            insertCols.Add("Description");
            insertVals.Add("@FormName");
        }

        if (formCols.Contains("Version"))
        {
            insertCols.Add("Version");
            insertVals.Add("1");
        }

        if (formCols.Contains("Platform"))
        {
            insertCols.Add("Platform");
            insertVals.Add("@Platform");
        }

        if (formCols.Contains("Is_Active"))
        {
            insertCols.Add("Is_Active");
            insertVals.Add("1");
        }

        if (formCols.Contains("Created_At"))
        {
            insertCols.Add("Created_At");
            insertVals.Add("GETDATE()");
        }

        if (formCols.Contains("Created_By"))
        {
            insertCols.Add("Created_By");
            insertVals.Add("'system'");
        }

        if (formCols.Contains("Updated_At"))
        {
            insertCols.Add("Updated_At");
            insertVals.Add("GETDATE()");
        }

        if (formCols.Contains("Updated_By"))
        {
            insertCols.Add("Updated_By");
            insertVals.Add("'system'");
        }

        var sql = $"""
            INSERT INTO dbo.Ui_Form
                ({string.Join(", ", insertCols)})
            VALUES
                ({string.Join(", ", insertVals)});

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, FormCode = formCode, FormName = formName, Platform = platform, TableId = tableIdForInsert },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsFormCodeAsync(
        string formCode,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            return false;

        await using var conn = new SqlConnection(_config.ConnectionString);

        var formCols = await GetTableColumnsAsync(conn, "dbo", "Ui_Form", ct);
        if (formCols.Count == 0)
            throw new InvalidOperationException("Không tìm thấy bảng dbo.Ui_Form.");
        if (!formCols.Contains("Form_Code"))
            throw new InvalidOperationException("Bảng dbo.Ui_Form thiếu cột bắt buộc Form_Code.");

        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        var useTenantFromForm = formCols.Contains("Tenant_Id");
        var useTenantFromSysTable = !useTenantFromForm
            && formCols.Contains("Table_Id")
            && sysTableCols.Contains("Table_Id")
            && sysTableCols.Contains("Tenant_Id");

        if (!useTenantFromForm && !useTenantFromSysTable)
            throw new InvalidOperationException(
                "Không tìm được cột tenant cho check duplicate Form_Code. Cần Ui_Form.Tenant_Id hoặc mapping qua Sys_Table.");

        // ── Check duplicate Form_Code trong tenant hiện tại ─────
        var whereParts = new List<string>
        {
            "f.Form_Code = @FormCode",
            useTenantFromSysTable ? "st.Tenant_Id = @TenantId" : "f.Tenant_Id = @TenantId",
        };

        if (formCols.Contains("Is_Active"))
            whereParts.Add("f.Is_Active = 1");
        if (useTenantFromSysTable && sysTableCols.Contains("Is_Active"))
            whereParts.Add("st.Is_Active = 1");

        var fromClause = useTenantFromSysTable
            ? "dbo.Ui_Form f INNER JOIN dbo.Sys_Table st ON st.Table_Id = f.Table_Id"
            : "dbo.Ui_Form f";

        var sql = $"""
            SELECT TOP (1) 1
            FROM   {fromClause}
            WHERE  {string.Join("\n              AND  ", whereParts)}
            """;

        var exists = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, FormCode = formCode },
                cancellationToken: ct));

        return exists.HasValue;
    }

    /// <summary>
    /// Resolve Table_Id đầu tiên của tenant khi schema đặt tenant ở Sys_Table.
    /// </summary>
    private static async Task<int?> ResolveTableIdForTenantAsync(
        SqlConnection conn,
        int tenantId,
        CancellationToken ct)
    {
        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (!sysTableCols.Contains("Table_Id") || !sysTableCols.Contains("Tenant_Id"))
            return null;

        var whereParts = new List<string> { "Tenant_Id = @TenantId" };
        if (sysTableCols.Contains("Is_Active"))
            whereParts.Add("Is_Active = 1");
        if (sysTableCols.Contains("Is_Tenant"))
            whereParts.Add("Is_Tenant = 1");

        var sql = $"""
            SELECT TOP (1) Table_Id
            FROM   dbo.Sys_Table
            WHERE  {string.Join("\n              AND  ", whereParts)}
            ORDER BY Table_Id
            """;

        return await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId },
                cancellationToken: ct));
    }

    /// <summary>
    /// Kiểm tra Table_Id có thuộc tenant hiện tại và còn active hay không.
    /// </summary>
    private static async Task<bool> IsTableInTenantAsync(
        SqlConnection conn,
        int tableId,
        int tenantId,
        CancellationToken ct)
    {
        var sysTableCols = await GetTableColumnsAsync(conn, "dbo", "Sys_Table", ct);
        if (!sysTableCols.Contains("Table_Id") || !sysTableCols.Contains("Tenant_Id"))
            return false;

        var whereParts = new List<string>
        {
            "Table_Id = @TableId",
            "Tenant_Id = @TenantId",
        };
        if (sysTableCols.Contains("Is_Active"))
            whereParts.Add("Is_Active = 1");
        if (sysTableCols.Contains("Is_Tenant"))
            whereParts.Add("Is_Tenant = 1");

        var sql = $"""
            SELECT TOP (1) 1
            FROM   dbo.Sys_Table
            WHERE  {string.Join("\n              AND  ", whereParts)}
            """;

        var exists = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { TableId = tableId, TenantId = tenantId },
                cancellationToken: ct));

        return exists.HasValue;
    }

    /// <summary>
    /// Lấy danh sách cột của bảng theo schema. Nếu bảng không tồn tại thì trả rỗng.
    /// </summary>
    private static async Task<HashSet<string>> GetTableColumnsAsync(
        SqlConnection conn,
        string schemaName,
        string tableName,
        CancellationToken ct)
    {
        const string sql = """
            SELECT c.name
            FROM   sys.columns c
            INNER JOIN sys.tables t
                    ON c.object_id = t.object_id
            INNER JOIN sys.schemas s
                    ON t.schema_id = s.schema_id
            WHERE  s.name = @SchemaName
              AND  t.name = @TableName
            """;

        var colNames = await conn.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new { SchemaName = schemaName, TableName = tableName },
                cancellationToken: ct));

        return new HashSet<string>(colNames, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build biểu thức COUNT an toàn theo schema thật của bảng con.
    /// </summary>
    private static string BuildCountExpr(
        string tableName,
        string alias,
        HashSet<string> columns,
        string outputAlias)
    {
        if (!columns.Contains("Form_Id"))
            return $"CAST(0 AS int) AS {outputAlias}";

        var activeFilter = columns.Contains("Is_Active")
            ? $" AND {alias}.Is_Active = 1"
            : string.Empty;

        return $"(SELECT COUNT(*) FROM dbo.{tableName} {alias} WHERE {alias}.Form_Id = f.Form_Id{activeFilter}) AS {outputAlias}";
    }
}
