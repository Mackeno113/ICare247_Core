// File    : FormMasterDetailDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Truy vấn / ghi Ui_Form_Detail (pane master-detail / rail) + Ui_Form.Detail_Layout
//           qua Dapper trên Config DB (db/106). Mọi query parameterized; guard schema trước khi ghi.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Implementation <see cref="IFormMasterDetailDataService"/> dùng Dapper trên Config DB.
/// Kiểm tra bảng <c>Ui_Form_Detail</c> (db/106) tồn tại trước khi đọc/ghi pane.
/// </summary>
public sealed class FormMasterDetailDataService : IFormMasterDetailDataService
{
    private readonly IAppConfigService _config;

    /// <summary>Khởi tạo với cấu hình DB hiện hành (ConnectionString Config DB).</summary>
    public FormMasterDetailDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FormLookupItem>> GetFormsAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        const string sql =
            "SELECT Form_Id AS FormId, Form_Code AS FormCode\n" +
            "FROM   dbo.Ui_Form\n" +
            "WHERE  Is_Active = 1\n" +
            "ORDER BY Form_Code";

        var result = await conn.QueryAsync<FormLookupItem>(
            new CommandDefinition(sql, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<string> GetDetailLayoutAsync(int formId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || formId <= 0) return "Inline";

        await using var conn = new SqlConnection(_config.ConnectionString);

        // Đọc phòng thủ: DB chưa chạy db/106 → cột Detail_Layout chưa có → mặc định 'Inline'.
        const string colSql =
            "SELECT COUNT(*) FROM sys.columns\n" +
            "WHERE object_id = OBJECT_ID('dbo.Ui_Form') AND name = 'Detail_Layout'";
        var hasCol = await conn.ExecuteScalarAsync<int>(new CommandDefinition(colSql, cancellationToken: ct));
        if (hasCol == 0) return "Inline";

        const string sql = "SELECT ISNULL(Detail_Layout, 'Inline') FROM dbo.Ui_Form WHERE Form_Id = @FormId";
        var layout = await conn.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return string.IsNullOrWhiteSpace(layout) ? "Inline" : layout;
    }

    /// <inheritdoc />
    public async Task SaveDetailLayoutAsync(int formId, string layout, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình.");
        if (formId <= 0)
            throw new InvalidOperationException("Chưa chọn form master.");
        var norm = layout is "Rail" ? "Rail" : "Inline";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSchemaAsync(conn, ct);

        const string sql =
            "UPDATE dbo.Ui_Form SET Detail_Layout = @Layout, Version = Version + 1, Updated_At = GETDATE()\n" +
            "WHERE Form_Id = @FormId";
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Layout = norm, FormId = formId }, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy Form_Id={formId} để cập nhật bố cục.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FormMasterDetailRecord>> GetPanesAsync(
        int formId, bool includeInactive = false, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || formId <= 0) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);
        if (!await SchemaExistsAsync(conn, ct)) return [];

        var whereActive = includeInactive ? "" : "  AND d.Is_Active = 1\n";

        var sql =
            "SELECT d.Detail_Id AS DetailId, d.Form_Id AS FormId, d.Detail_Code AS DetailCode,\n" +
            "       d.Pane_Type AS PaneType, d.Detail_Form_Id AS DetailFormId, cf.Form_Code AS DetailFormCode,\n" +
            "       d.Parent_Key_Column AS ParentKeyColumn, d.Save_Mode AS SaveMode, d.Title_Key AS TitleKey,\n" +
            "       d.Icon, d.Group_Key AS GroupKey, d.Edit_Mode AS EditMode,\n" +
            "       d.Allow_Add AS AllowAdd, d.Allow_Delete AS AllowDelete, d.Allow_Reorder AS AllowReorder,\n" +
            "       d.Min_Rows AS MinRows, d.Order_No AS OrderNo, d.Is_Active AS IsActive\n" +
            "FROM   dbo.Ui_Form_Detail d\n" +
            "LEFT JOIN dbo.Ui_Form cf ON cf.Form_Id = d.Detail_Form_Id\n" +
            "WHERE  d.Form_Id = @FormId\n" +
            whereActive +
            "ORDER BY d.Order_No, d.Detail_Id";

        var result = await conn.QueryAsync<FormMasterDetailRecord>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> SavePaneAsync(FormMasterDetailRecord r, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");
        if (r.FormId <= 0)
            throw new InvalidOperationException("Phải chọn form master.");
        if (string.IsNullOrWhiteSpace(r.DetailCode))
            throw new InvalidOperationException("Phải nhập mã pane (Detail_Code).");

        var paneType = r.PaneType is "Timeline" ? "Timeline" : "Grid";
        if (paneType == "Grid")
        {
            if (r.DetailFormId is null or <= 0)
                throw new InvalidOperationException("Pane 'Grid' phải chọn form con (Detail_Form).");
            if (string.IsNullOrWhiteSpace(r.ParentKeyColumn))
                throw new InvalidOperationException("Pane 'Grid' phải nhập cột khóa cha (Parent_Key_Column).");
        }

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSchemaAsync(conn, ct);

        // Chống trùng (Form_Id + Detail_Code).
        const string dupSql =
            "SELECT TOP (1) 1 FROM dbo.Ui_Form_Detail\n" +
            "WHERE Form_Id = @FormId AND Detail_Code = @DetailCode AND Detail_Id <> @DetailId";
        var dup = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
            dupSql, new { r.FormId, DetailCode = r.DetailCode.Trim(), r.DetailId }, cancellationToken: ct));
        if (dup.HasValue)
            throw new InvalidOperationException($"Mã pane '{r.DetailCode}' đã tồn tại trong form này.");

        var p = new
        {
            r.FormId,
            DetailCode = r.DetailCode.Trim(),
            PaneType = paneType,
            DetailFormId = paneType == "Grid" ? r.DetailFormId : null,
            ParentKeyColumn = NullIfEmpty(r.ParentKeyColumn),
            SaveMode = r.SaveMode is "Immediate" ? "Immediate" : "WithMaster",
            TitleKey = NullIfEmpty(r.TitleKey),
            Icon = NullIfEmpty(r.Icon),
            GroupKey = NullIfEmpty(r.GroupKey),
            EditMode = string.IsNullOrWhiteSpace(r.EditMode) ? "EntryPanel" : r.EditMode,
            r.AllowAdd,
            r.AllowDelete,
            r.AllowReorder,
            MinRows = r.MinRows < 0 ? 0 : r.MinRows,
            r.OrderNo,
            r.IsActive,
            r.DetailId,
        };

        if (r.DetailId <= 0)
        {
            const string insertSql =
                "INSERT INTO dbo.Ui_Form_Detail (Form_Id, Detail_Code, Pane_Type, Detail_Form_Id,\n" +
                "    Parent_Key_Column, Save_Mode, Title_Key, Icon, Group_Key, Edit_Mode,\n" +
                "    Allow_Add, Allow_Delete, Allow_Reorder, Min_Rows, Order_No, Is_Active, Is_Customized)\n" +
                "VALUES (@FormId, @DetailCode, @PaneType, @DetailFormId, @ParentKeyColumn, @SaveMode,\n" +
                "    @TitleKey, @Icon, @GroupKey, @EditMode, @AllowAdd, @AllowDelete, @AllowReorder,\n" +
                "    @MinRows, @OrderNo, @IsActive, 1);\n" +   // Is_Customized=1: bản tenant/DEV tự thêm → ConfigSync bỏ qua
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(insertSql, p, cancellationToken: ct));
        }

        const string updateSql =
            "UPDATE dbo.Ui_Form_Detail SET Detail_Code = @DetailCode, Pane_Type = @PaneType,\n" +
            "    Detail_Form_Id = @DetailFormId, Parent_Key_Column = @ParentKeyColumn, Save_Mode = @SaveMode,\n" +
            "    Title_Key = @TitleKey, Icon = @Icon, Group_Key = @GroupKey, Edit_Mode = @EditMode,\n" +
            "    Allow_Add = @AllowAdd, Allow_Delete = @AllowDelete, Allow_Reorder = @AllowReorder,\n" +
            "    Min_Rows = @MinRows, Order_No = @OrderNo, Is_Active = @IsActive,\n" +
            "    Is_Customized = 1, Version = Version + 1, Updated_At = GETDATE()\n" +
            "WHERE Detail_Id = @DetailId";
        var affected = await conn.ExecuteAsync(new CommandDefinition(updateSql, p, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy Detail_Id={r.DetailId} để cập nhật.");
        return r.DetailId;
    }

    /// <inheritdoc />
    public async Task DeactivatePaneAsync(int detailId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình.");

        await using var conn = new SqlConnection(_config.ConnectionString);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Ui_Form_Detail SET Is_Active = 0, Updated_At = GETDATE() WHERE Detail_Id = @DetailId",
            new { DetailId = detailId }, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy Detail_Id={detailId} để ẩn.");
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>Bảng Ui_Form_Detail (db/106) đã tồn tại chưa (không ném — cho luồng đọc).</summary>
    private static async Task<bool> SchemaExistsAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = "SELECT CASE WHEN OBJECT_ID('dbo.Ui_Form_Detail', 'U') IS NULL THEN 0 ELSE 1 END";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct)) == 1;
    }

    /// <summary>Guard cho luồng GHI: chưa chạy db/106 → ném lỗi rõ ràng.</summary>
    private static async Task EnsureSchemaAsync(SqlConnection conn, CancellationToken ct)
    {
        if (!await SchemaExistsAsync(conn, ct))
            throw new InvalidOperationException(
                "Bảng Ui_Form_Detail chưa tồn tại. Cần chạy migration db/106_create_ui_form_detail.sql trước.");
    }

    /// <summary>Chuẩn hóa chuỗi rỗng/space về null để cột nullable lưu NULL thay vì ''.</summary>
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
