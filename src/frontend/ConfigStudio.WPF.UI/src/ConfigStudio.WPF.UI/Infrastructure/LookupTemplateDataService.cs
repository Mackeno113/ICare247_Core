// File    : LookupTemplateDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : CRUD bảng Ui_Lookup_Template trên Config DB bằng Dapper.

using System.Text.Json;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>Data service Dapper cho màn quản lý mẫu lookup.</summary>
public sealed class LookupTemplateDataService : ILookupTemplateDataService
{
    private static readonly HashSet<string> QueryModes =
        new(StringComparer.OrdinalIgnoreCase) { "table", "tvf", "custom_sql" };

    private readonly IAppConfigService _config;

    public LookupTemplateDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupTemplateRecord>> GetTemplatesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Template_Id        AS TemplateId,
                   Template_Code      AS TemplateCode,
                   Ten                AS Ten,
                   Mo_Ta              AS MoTa,
                   Query_Mode         AS QueryMode,
                   Source_Name        AS SourceName,
                   Value_Column       AS ValueColumn,
                   Display_Column     AS DisplayColumn,
                   Code_Field         AS CodeField,
                   Filter_Sql         AS FilterSql,
                   Order_By           AS OrderBy,
                   Popup_Columns_Json AS PopupColumnsJson,
                   Parent_Column      AS ParentColumn,
                   Canonical_Params   AS CanonicalParams,
                   Is_Active          AS IsActive,
                   Is_System          AS IsSystem,
                   Is_Customized      AS IsCustomized,
                   Synced_At          AS SyncedAt,
                   Source_Ver         AS SourceVer
            FROM dbo.Ui_Lookup_Template
            ORDER BY Template_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<LookupTemplateRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<int> SaveTemplateAsync(
        LookupTemplateUpsertRequest request,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        Validate(request);

        var code = request.TemplateCode.Trim();
        var parameters = new
        {
            TemplateId = request.TemplateId ?? 0,
            TemplateCode = code,
            Ten = request.Ten.Trim(),
            MoTa = NullIfEmpty(request.MoTa),
            QueryMode = request.QueryMode.Trim().ToLowerInvariant(),
            SourceName = request.SourceName.Trim(),
            ValueColumn = request.ValueColumn.Trim(),
            DisplayColumn = request.DisplayColumn.Trim(),
            CodeField = NullIfEmpty(request.CodeField),
            FilterSql = NullIfEmpty(request.FilterSql),
            OrderBy = NullIfEmpty(request.OrderBy),
            PopupColumnsJson = NullIfEmpty(request.PopupColumnsJson),
            ParentColumn = NullIfEmpty(request.ParentColumn),
            CanonicalParams = NullIfEmpty(request.CanonicalParams),
            request.IsActive,
        };

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);

        const string duplicateSql = """
            SELECT TOP (1) 1
            FROM dbo.Ui_Lookup_Template
            WHERE Template_Code = @TemplateCode
              AND Template_Id <> @TemplateId
            """;
        var duplicate = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(duplicateSql, parameters, cancellationToken: ct));
        if (duplicate.HasValue)
            throw new InvalidOperationException($"Template_Code '{code}' đã tồn tại.");

        if (request.TemplateId is null or 0)
        {
            const string insertSql = """
                INSERT INTO dbo.Ui_Lookup_Template
                    (Template_Code, Ten, Mo_Ta, Query_Mode, Source_Name, Value_Column,
                     Display_Column, Code_Field, Filter_Sql, Order_By, Popup_Columns_Json,
                     Parent_Column, Canonical_Params, Is_Active, Is_System, Is_Customized)
                OUTPUT INSERTED.Template_Id
                VALUES
                    (@TemplateCode, @Ten, @MoTa, @QueryMode, @SourceName, @ValueColumn,
                     @DisplayColumn, @CodeField, @FilterSql, @OrderBy, @PopupColumnsJson,
                     @ParentColumn, @CanonicalParams, @IsActive, 0, 1)
                """;
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(insertSql, parameters, cancellationToken: ct));
        }

        const string updateSql = """
            UPDATE dbo.Ui_Lookup_Template
            SET Ten                = @Ten,
                Mo_Ta              = @MoTa,
                Query_Mode         = @QueryMode,
                Source_Name        = @SourceName,
                Value_Column       = @ValueColumn,
                Display_Column     = @DisplayColumn,
                Code_Field         = @CodeField,
                Filter_Sql         = @FilterSql,
                Order_By           = @OrderBy,
                Popup_Columns_Json = @PopupColumnsJson,
                Parent_Column      = @ParentColumn,
                Canonical_Params   = @CanonicalParams,
                Is_Active          = @IsActive,
                Is_Customized      = 1
            WHERE Template_Id = @TemplateId
              AND Template_Code = @TemplateCode
            """;
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(updateSql, parameters, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException("Không tìm thấy mẫu lookup hoặc Template_Code đã thay đổi.");
        return request.TemplateId.Value;
    }

    /// <inheritdoc />
    public async Task<int> CountReferencesAsync(string templateCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || string.IsNullOrWhiteSpace(templateCode)) return 0;

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Ui_Field_Lookup
            WHERE Template_Code = @TemplateCode
            """;
        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { TemplateCode = templateCode.Trim() }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteTemplateAsync(int templateId, CancellationToken ct = default)
    {
        EnsureConfigured();

        const string sql = """
            DELETE tpl
            FROM dbo.Ui_Lookup_Template tpl
            WHERE tpl.Template_Id = @TemplateId
              AND tpl.Is_System = 0
              AND NOT EXISTS
                  (SELECT 1
                   FROM dbo.Ui_Field_Lookup fl
                   WHERE fl.Template_Code = tpl.Template_Code)
            """;
        await using var conn = new SqlConnection(_config.ConnectionString);
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { TemplateId = templateId }, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException(
                "Không thể xóa: mẫu là mẫu hệ thống, không tồn tại hoặc đang được field tham chiếu.");
    }

    private void EnsureConfigured()
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình. Vui lòng vào Settings.");
    }

    private static void Validate(LookupTemplateUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateCode))
            throw new InvalidOperationException("Template_Code không được để trống.");
        if (string.IsNullOrWhiteSpace(request.Ten))
            throw new InvalidOperationException("Tên mẫu không được để trống.");
        if (!QueryModes.Contains(request.QueryMode.Trim()))
            throw new InvalidOperationException("Query_Mode chỉ nhận table, tvf hoặc custom_sql.");
        if (string.IsNullOrWhiteSpace(request.SourceName))
            throw new InvalidOperationException("Source_Name không được để trống.");
        if (string.IsNullOrWhiteSpace(request.ValueColumn))
            throw new InvalidOperationException("Value_Column không được để trống.");
        if (string.IsNullOrWhiteSpace(request.DisplayColumn))
            throw new InvalidOperationException("Display_Column không được để trống.");

        ValidateJsonArray(request.PopupColumnsJson, "Popup_Columns_Json");
        ValidateJsonArray(request.CanonicalParams, "Canonical_Params");
    }

    private static void ValidateJsonArray(string? json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException($"{fieldName} phải là JSON array.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{fieldName} không phải JSON hợp lệ: {ex.Message}", ex);
        }
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
