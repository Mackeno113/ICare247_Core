// File    : FormRepository.cs
// Module  : Form
// Layer   : Infrastructure
// Purpose : Dapper implementation của IFormRepository — CRUD + clone cho Ui_Form.

using System.Security.Cryptography;
using System.Text;
using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Form;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Ui_Form</c> + <c>Ui_Section</c> + <c>Ui_Field</c>.
/// Mọi query resolve tenant qua <c>Sys_Table.Tenant_Id</c>.
/// </summary>
public sealed class FormRepository : IFormRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<FormRepository> _logger;

    public FormRepository(IDbConnectionFactory db, ILogger<FormRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<FormListItem> Items, int TotalCount)> GetListAsync(
        int tenantId,
        string? platform = null,
        int? tableId = null,
        bool? isActive = null,
        string? searchText = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        const string sql = """
            -- Đếm tổng số record
            SELECT COUNT(*)
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
              AND  (@Platform IS NULL OR f.Platform = @Platform)
              AND  (@TableId IS NULL OR f.Table_Id = @TableId)
              AND  (@IsActive IS NULL OR f.Is_Active = @IsActive)
              AND  (@Search IS NULL OR f.Form_Code LIKE '%' + @Search + '%');

            -- Lấy danh sách có phân trang
            SELECT f.Form_Id     AS FormId,
                   f.Form_Code   AS FormCode,
                   ''            AS FormName,
                   f.Platform,
                   t.Table_Name  AS TableName,
                   f.Table_Id    AS TableId,
                   f.Version,
                   f.Is_Active   AS IsActive,
                   f.Checksum,
                   f.Updated_At  AS UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Ui_Section s
                    WHERE s.Form_Id = f.Form_Id AND s.Is_Active = 1) AS SectionCount,
                   (SELECT COUNT(*) FROM dbo.Ui_Field fi
                    WHERE fi.Form_Id = f.Form_Id) AS FieldCount
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
              AND  (@Platform IS NULL OR f.Platform = @Platform)
              AND  (@TableId IS NULL OR f.Table_Id = @TableId)
              AND  (@IsActive IS NULL OR f.Is_Active = @IsActive)
              AND  (@Search IS NULL OR f.Form_Code LIKE '%' + @Search + '%')
            ORDER BY f.Form_Code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new
        {
            TenantId = tenantId,
            Platform = platform,
            TableId = tableId,
            IsActive = isActive,
            Search = string.IsNullOrWhiteSpace(searchText) ? null : searchText,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, param, cancellationToken: ct));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<FormListItem>()).AsList();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<FormMetadata?> GetByCodeAsync(
        string formCode, int tenantId, string langCode = "vi", CancellationToken ct = default)
    {
        const string sqlForm = """
            SELECT f.Form_Id    AS FormId,
                   f.Form_Code  AS FormCode,
                   ''           AS FormName,
                   f.Version,
                   f.Platform
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  f.Form_Code = @FormCode
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
              AND  f.Is_Active = 1
            """;

        const string sqlSections = """
            SELECT s.Section_Id   AS SectionId,
                   s.Form_Id      AS FormId,
                   s.Section_Code AS SectionCode,
                   ''             AS SectionName,
                   s.Order_No     AS SortOrder
            FROM   dbo.Ui_Section s
            WHERE  s.Form_Id = @FormId
              AND  s.Is_Active = 1
            ORDER BY s.Order_No
            """;

        // ── sqlFields: map đúng cột, resolve Label qua Sys_Resource ──────────
        // sc.Column_Code  → FieldCode (mã kỹ thuật, dùng làm key trong EvaluationContext)
        // Sys_Resource    → Label đã localize; fallback về Label_Key nếu chưa có resource
        // Is_Visible      → IsVisible (không phải IsRequired!)
        // Control_Props_Json → ControlPropsJson (cấu hình UI, không phải default value)
        const string sqlFields = """
            SELECT fi.Field_Id                               AS FieldId,
                   fi.Form_Id                                AS FormId,
                   fi.Section_Id                             AS SectionId,
                   sc.Column_Code                            AS FieldCode,
                   fi.Editor_Type                            AS FieldType,
                   COALESCE(r.Resource_Value, fi.Label_Key)  AS Label,
                   fi.Order_No                               AS SortOrder,
                   fi.Is_Visible                             AS IsVisible,
                   fi.Is_ReadOnly                            AS IsReadOnly,
                   fi.Control_Props_Json                     AS ControlPropsJson
            FROM       dbo.Ui_Field fi
            LEFT JOIN  dbo.Sys_Column   sc ON sc.Column_Id    = fi.Column_Id
            LEFT JOIN  dbo.Sys_Resource r  ON r.Resource_Key  = fi.Label_Key
                                          AND r.Lang_Code      = @LangCode
            WHERE  fi.Form_Id = @FormId
            ORDER BY fi.Order_No
            """;

        using var conn = _db.CreateConnection();

        // ── Lấy form ────────────────────────────────────────────────────────
        var formDto = await conn.QueryFirstOrDefaultAsync<FormMetadata>(
            new CommandDefinition(sqlForm, new { FormCode = formCode, TenantId = tenantId },
                cancellationToken: ct));

        if (formDto is null) return null;

        // ── Lấy sections + fields ───────────────────────────────────────────
        var sections = (await conn.QueryAsync<SectionMetadata>(
            new CommandDefinition(sqlSections, new { FormId = formDto.FormId },
                cancellationToken: ct))).AsList();

        var allFields = (await conn.QueryAsync<FieldMetadata>(
            new CommandDefinition(sqlFields, new { FormId = formDto.FormId, LangCode = langCode },
                cancellationToken: ct))).AsList();

        // ── Gán fields vào sections ─────────────────────────────────────────
        var sectionFieldsMap = allFields
            .Where(f => f.SectionId.HasValue)
            .GroupBy(f => f.SectionId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FieldMetadata>)g.ToList());

        var enrichedSections = sections.Select(s => new SectionMetadata
        {
            SectionId = s.SectionId,
            FormId = s.FormId,
            TenantId = tenantId,
            SectionCode = s.SectionCode,
            SectionName = s.SectionName,
            SortOrder = s.SortOrder,
            Fields = sectionFieldsMap.GetValueOrDefault(s.SectionId, [])
        }).ToList();

        // ── Trả aggregate root ──────────────────────────────────────────────
        return new FormMetadata
        {
            FormId = formDto.FormId,
            TenantId = tenantId,
            FormCode = formDto.FormCode,
            FormName = formDto.FormName,
            Version = formDto.Version,
            Platform = formDto.Platform,
            Sections = enrichedSections,
            Fields = allFields
        };
    }

    /// <inheritdoc />
    public async Task<FormMetadata?> GetByIdAsync(
        int formId, int tenantId, CancellationToken ct = default)
    {
        // Lấy Form_Code từ Id rồi delegate sang GetByCodeAsync
        const string sql = """
            SELECT f.Form_Code
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  f.Form_Id = @FormId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;

        using var conn = _db.CreateConnection();
        var formCode = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
                cancellationToken: ct));

        return formCode is null ? null : await GetByCodeAsync(formCode, tenantId, ct: ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCodeAsync(
        string formCode, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM   dbo.Ui_Form f
                JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
                WHERE  f.Form_Code = @FormCode
                  AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            ) THEN 1 ELSE 0 END
            """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<bool>(
            new CommandDefinition(sql, new { FormCode = formCode, TenantId = tenantId },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        FormCreateParams form, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Ui_Form
                   (Form_Code, Table_Id, Platform, Layout_Engine, Version, Checksum,
                    Is_Active, Description, Updated_At)
            OUTPUT INSERTED.Form_Id
            VALUES (@FormCode, @TableId, @Platform, @LayoutEngine, 1, @Checksum,
                    1, @Description, GETDATE());
            """;

        var checksum = ComputeChecksum(form.FormCode, 1);

        using var conn = _db.CreateConnection();
        var formId = await conn.QuerySingleAsync<int>(
            new CommandDefinition(sql, new
            {
                form.FormCode,
                form.TableId,
                form.Platform,
                form.LayoutEngine,
                Checksum = checksum,
                form.Description
            }, cancellationToken: ct));

        _logger.LogInformation(
            "Form tạo mới — FormId={FormId}, FormCode={FormCode}, TenantId={TenantId}",
            formId, form.FormCode, tenantId);

        return formId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        FormUpdateParams form, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Ui_Form
            SET    Table_Id      = @TableId,
                   Platform      = @Platform,
                   Layout_Engine = @LayoutEngine,
                   Description   = @Description,
                   Version       = Version + 1,
                   Checksum      = @Checksum,
                   Updated_At    = GETDATE()
            WHERE  Form_Id = @FormId;
            """;

        // Lấy version hiện tại để tính checksum
        const string sqlVersion = """
            SELECT f.Version, f.Form_Code
            FROM   dbo.Ui_Form f
            WHERE  f.Form_Id = @FormId
            """;

        using var conn = _db.CreateConnection();

        var current = await conn.QueryFirstOrDefaultAsync<(int Version, string FormCode)>(
            new CommandDefinition(sqlVersion, new { form.FormId }, cancellationToken: ct));

        var newVersion = current.Version + 1;
        var checksum = ComputeChecksum(current.FormCode, newVersion);

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                form.FormId,
                form.TableId,
                form.Platform,
                form.LayoutEngine,
                form.Description,
                Checksum = checksum
            }, cancellationToken: ct));

        _logger.LogInformation(
            "Form cập nhật — FormId={FormId}, Version={Version}, TenantId={TenantId}",
            form.FormId, newVersion, tenantId);
    }

    /// <inheritdoc />
    public async Task SetActiveAsync(
        int formId, bool isActive, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Ui_Form
            SET    Is_Active  = @IsActive,
                   Updated_At = GETDATE()
            WHERE  Form_Id = @FormId;
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { FormId = formId, IsActive = isActive },
                cancellationToken: ct));

        _logger.LogInformation(
            "Form {Action} — FormId={FormId}, TenantId={TenantId}",
            isActive ? "khôi phục" : "vô hiệu hóa", formId, tenantId);
    }

    /// <inheritdoc />
    public async Task SetActiveByCodeAsync(
        string formCode, bool isActive, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE f
            SET    f.Is_Active  = @IsActive,
                   f.Updated_At = GETDATE()
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  f.Form_Code = @FormCode
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL);
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { FormCode = formCode, IsActive = isActive, TenantId = tenantId },
                cancellationToken: ct));

        _logger.LogInformation(
            "Form {Action} — FormCode={FormCode}, TenantId={TenantId}",
            isActive ? "khôi phục" : "vô hiệu hóa", formCode, tenantId);
    }

    /// <inheritdoc />
    public async Task<int> CloneAsync(
        int sourceFormId, string newFormCode, int tenantId, CancellationToken ct = default)
    {
        // Clone trong 1 transaction: Form → Sections → Fields
        const string sqlCloneForm = """
            INSERT INTO dbo.Ui_Form
                   (Form_Code, Table_Id, Platform, Layout_Engine, Version, Checksum,
                    Is_Active, Description, Updated_At)
            SELECT @NewFormCode, Table_Id, Platform, Layout_Engine, 1, @Checksum,
                   1, Description, GETDATE()
            FROM   dbo.Ui_Form
            WHERE  Form_Id = @SourceFormId;

            SELECT SCOPE_IDENTITY();
            """;

        const string sqlCloneSections = """
            INSERT INTO dbo.Ui_Section
                   (Form_Id, Section_Code, Title_Key, Order_No, Layout_Json, Is_Active, Description)
            SELECT @NewFormId, Section_Code, Title_Key, Order_No, Layout_Json, Is_Active, Description
            FROM   dbo.Ui_Section
            WHERE  Form_Id = @SourceFormId AND Is_Active = 1;
            """;

        const string sqlCloneFields = """
            INSERT INTO dbo.Ui_Field
                   (Form_Id, Section_Id, Column_Id, Editor_Type, Label_Key, Placeholder_Key,
                    Tooltip_Key, Is_Visible, Is_ReadOnly, Order_No, Control_Props_Json,
                    Version, Updated_At, Description)
            SELECT @NewFormId,
                   -- Map section cũ → section mới theo Section_Code
                   (SELECT ns.Section_Id FROM dbo.Ui_Section ns
                    WHERE ns.Form_Id = @NewFormId AND ns.Section_Code =
                      (SELECT os.Section_Code FROM dbo.Ui_Section os WHERE os.Section_Id = f.Section_Id)),
                   f.Column_Id, f.Editor_Type, f.Label_Key, f.Placeholder_Key,
                   f.Tooltip_Key, f.Is_Visible, f.Is_ReadOnly, f.Order_No, f.Control_Props_Json,
                   1, GETDATE(), f.Description
            FROM   dbo.Ui_Field f
            WHERE  f.Form_Id = @SourceFormId;
            """;

        var checksum = ComputeChecksum(newFormCode, 1);

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Clone form
            var newFormId = await conn.QuerySingleAsync<int>(
                new CommandDefinition(sqlCloneForm,
                    new { SourceFormId = sourceFormId, NewFormCode = newFormCode, Checksum = checksum },
                    transaction: tx, cancellationToken: ct));

            // Clone sections
            await conn.ExecuteAsync(
                new CommandDefinition(sqlCloneSections,
                    new { NewFormId = newFormId, SourceFormId = sourceFormId },
                    transaction: tx, cancellationToken: ct));

            // Clone fields (map Section_Id qua Section_Code)
            await conn.ExecuteAsync(
                new CommandDefinition(sqlCloneFields,
                    new { NewFormId = newFormId, SourceFormId = sourceFormId },
                    transaction: tx, cancellationToken: ct));

            tx.Commit();

            _logger.LogInformation(
                "Form nhân bản — Source={SourceFormId}, New={NewFormId}, Code={NewFormCode}, TenantId={TenantId}",
                sourceFormId, newFormId, newFormCode, tenantId);

            return newFormId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>Tính SHA256 checksum từ FormCode + Version.</summary>
    private static string ComputeChecksum(string formCode, int version)
    {
        var input = $"{formCode}:v{version}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash)[..16];
    }
}
