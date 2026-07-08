// File    : ImportMetadataProvider.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : Tổng hợp metadata import 1 View: cột nhập (kiểu/bắt buộc/masking) + bảng đích + khoá ghép.
//           Spec 25 §11–§14, ADR-034.

using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Import;

/// <summary>
/// Dựng <see cref="ImportContext"/>: đọc View (IConfigCache) → Edit_Form (IMasterDataRepository) → cột nhập
/// (Ui_Field visible/không-ảo/không-readonly), gắn kiểu (Sys_Column net type), cờ masking (Sys_Column),
/// và khoá ghép (Ui_View.Import_Key_Fields). Cột mới (masking/key) đọc phòng thủ — DB chưa migrate thì bỏ qua.
/// </summary>
public sealed class ImportMetadataProvider : IImportMetadataProvider
{
    private readonly IConfigCache _config;
    private readonly IMasterDataRepository _master;
    private readonly IDbConnectionFactory _configDb;
    private readonly ILogger<ImportMetadataProvider> _logger;

    public ImportMetadataProvider(
        IConfigCache config, IMasterDataRepository master,
        IDbConnectionFactory configDb, ILogger<ImportMetadataProvider> logger)
    {
        _config = config;
        _master = master;
        _configDb = configDb;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportContext?> BuildAsync(
        string viewCode, int tenantId, string langCode, CancellationToken ct = default)
    {
        var view = await _config.GetViewAsync(viewCode, langCode, tenantId, ct);
        if (view is null || string.IsNullOrWhiteSpace(view.EditFormCode))
            return null;   // không có form Thêm/Sửa → không import được

        var info = await _master.GetFormInfoAsync(view.EditFormCode!, tenantId, ct);
        if (info is null || string.IsNullOrWhiteSpace(info.TableName))
            return null;

        var form = await _config.GetFormMetadataAsync(view.EditFormCode!, langCode, "web", tenantId, ct);

        // Kiểu .NET theo cột (từ MasterDataColumn) + cờ masking (Sys_Column) — đọc phòng thủ.
        var netTypeByCol = info.Columns.ToDictionary(
            c => c.ColumnCode, c => c.NetType, StringComparer.OrdinalIgnoreCase);
        var maskByCol = await LoadMaskingAsync(info.TableId, ct);
        var keyFields = await LoadKeyFieldsAsync(view.ViewId, ct);
        var globalCodeFieldIds = await LoadGlobalCodeFieldIdsAsync(info.FormId, ct);

        // Cột nhập = field form hiển thị, không ảo, không read-only, không phải khóa chính.
        // Cột FK (LookupConfig động, Query_Mode=table) → dựng FkLookupDefinition từ chính Ui_Field_Lookup
        // của field (nguồn sự thật spec 25) — chạy đúng cho MỌI màn, kể cả màn đọc view JOIN tay.
        var fields = new List<ImportFieldSpec>();
        var fkColumns = new List<FkLookupDefinition>();
        if (form is not null)
        {
            foreach (var f in form.Fields)
            {
                if (!f.IsVisible || f.IsVirtual || f.IsReadOnly) continue;
                if (string.Equals(f.FieldCode, info.PkColumn, StringComparison.OrdinalIgnoreCase)) continue;

                var netType = netTypeByCol.GetValueOrDefault(f.FieldCode, "string");
                var masked = maskByCol.TryGetValue(f.FieldCode, out var mode);
                fields.Add(new ImportFieldSpec(
                    FieldName: f.FieldCode,
                    Caption: string.IsNullOrWhiteSpace(f.Label) ? f.FieldCode : f.Label,
                    Required: f.IsRequired,
                    NetType: netType,
                    IsMasked: masked,
                    MaskMode: masked ? mode : null));

                if (f.LookupConfig is { } lc
                    && string.Equals(lc.QueryMode ?? "table", "table", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(lc.SourceName)
                    && !string.IsNullOrWhiteSpace(lc.ValueColumn)
                    && !string.IsNullOrWhiteSpace(lc.DisplayColumn))
                {
                    fkColumns.Add(new FkLookupDefinition(
                        FieldName: f.FieldCode,
                        FieldId: lc.FieldId,
                        SourceName: lc.SourceName,
                        ValueColumn: lc.ValueColumn,
                        DisplayColumn: lc.DisplayColumn,
                        CodeField: lc.CodeField,
                        FilterSql: lc.FilterSql,
                        OrderBy: lc.OrderBy,
                        ImportGlobalCode: globalCodeFieldIds.Contains(lc.FieldId)));
                }
            }
        }

        // Chỉ giữ khoá ghép trỏ tới field thực có trong danh sách nhập.
        var validKeys = keyFields
            .Where(k => fields.Any(f => string.Equals(f.FieldName, k, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var sheetName = string.IsNullOrWhiteSpace(view.Title) ? view.ViewCode : view.Title!;
        return new ImportContext(
            view, view.EditFormCode!, info.SchemaName, info.TableName, info.PkColumn,
            sheetName, fields, validKeys, fkColumns);
    }

    /// <summary>Đọc cờ masking cột (Is_Log_Masked=1) → map Column_Code → Mask_Mode (Full nếu null). Phòng thủ.</summary>
    private async Task<Dictionary<string, string>> LoadMaskingAsync(int tableId, CancellationToken ct)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const string sql = """
            SELECT Column_Code AS ColumnCode, ISNULL(Log_Mask_Mode, N'Full') AS MaskMode
            FROM   dbo.Sys_Column
            WHERE  Table_Id = @TableId AND Is_Log_Masked = 1
            """;
        try
        {
            using var conn = _configDb.CreateConnection();
            var rows = await conn.QueryAsync<(string ColumnCode, string MaskMode)>(
                new CommandDefinition(sql, new { TableId = tableId }, cancellationToken: ct));
            foreach (var r in rows)
                map[r.ColumnCode] = r.MaskMode;
        }
        catch (SqlException ex)
        {
            // Cột Is_Log_Masked/Log_Mask_Mode chưa migrate (db/071) → không masking (an toàn).
            _logger.LogDebug(ex, "Import: bỏ qua masking — Sys_Column chưa có cột Is_Log_Masked?");
        }
        return map;
    }

    /// <summary>Đọc Field_Id các FK bật Import_Global_Code=1 (bỏ lọc cha khi import). Phòng thủ (cột mới db/074).</summary>
    private async Task<HashSet<int>> LoadGlobalCodeFieldIdsAsync(int formId, CancellationToken ct)
    {
        var set = new HashSet<int>();
        const string sql = """
            SELECT fl.Field_Id
            FROM   dbo.Ui_Field_Lookup fl
            JOIN   dbo.Ui_Field fi ON fi.Field_Id = fl.Field_Id
            WHERE  fi.Form_Id = @FormId AND fl.Import_Global_Code = 1
            """;
        try
        {
            using var conn = _configDb.CreateConnection();
            var ids = await conn.QueryAsync<int>(
                new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
            foreach (var id in ids) set.Add(id);
        }
        catch (SqlException ex)
        {
            _logger.LogDebug(ex, "Import: bỏ qua Import_Global_Code — Ui_Field_Lookup chưa có cột (db/074)?");
        }
        return set;
    }

    /// <summary>Đọc Ui_View.Import_Key_Fields (CSV) → danh sách field-code khoá ghép. Phòng thủ.</summary>
    private async Task<IReadOnlyList<string>> LoadKeyFieldsAsync(int viewId, CancellationToken ct)
    {
        const string sql = "SELECT Import_Key_Fields FROM dbo.Ui_View WHERE View_Id = @ViewId";
        try
        {
            using var conn = _configDb.CreateConnection();
            var csv = await conn.ExecuteScalarAsync<string?>(
                new CommandDefinition(sql, new { ViewId = viewId }, cancellationToken: ct));
            if (string.IsNullOrWhiteSpace(csv))
                return [];
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (SqlException ex)
        {
            _logger.LogDebug(ex, "Import: bỏ qua khoá ghép — Ui_View chưa có cột Import_Key_Fields?");
            return [];
        }
    }
}
