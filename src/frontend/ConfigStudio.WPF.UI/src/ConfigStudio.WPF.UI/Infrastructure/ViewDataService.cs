// File    : ViewDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Truy vấn / ghi cụm bảng Ui_View, Ui_View_Column, Ui_View_Action qua Dapper.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Implementation <see cref="IViewDataService"/> dùng Dapper trên Config DB.
/// Mọi query parameterized; cột/action lưu nguyên khối theo transaction.
/// </summary>
public sealed class ViewDataService : IViewDataService
{
    private readonly IAppConfigService _config;

    /// <summary>Khởi tạo với cấu hình DB hiện hành.</summary>
    /// <param name="config">Dịch vụ cung cấp ConnectionString + Tenant_Id.</param>
    public ViewDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViewRecord>> GetViewsAsync(
        int tenantId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);
        await EnsureSchemaAsync(conn, ct);

        var whereActive = includeInactive ? "" : "  AND v.Is_Active = 1\n";

        var sql =
            "SELECT v.View_Id AS ViewId, v.View_Code AS ViewCode, v.View_Type AS ViewType,\n" +
            "       v.Table_Id AS TableId, ISNULL(st.Table_Code, '') AS TableCode,\n" +
            "       v.Source_Type AS SourceType, v.Source_Object AS SourceObject,\n" +
            "       v.Title_Key AS TitleKey, v.Edit_Form_Id AS EditFormId,\n" +
            "       v.Page_Size AS PageSize, v.Allow_Paging AS AllowPaging, v.Virtual_Scroll AS VirtualScroll,\n" +
            "       v.Show_Filter_Row AS ShowFilterRow, v.Show_Group_Panel AS ShowGroupPanel,\n" +
            "       v.Show_Search_Box AS ShowSearchBox, v.Show_Column_Chooser AS ShowColumnChooser,\n" +
            "       v.Selection_Mode AS SelectionMode, v.Allow_Add AS AllowAdd, v.Allow_Edit AS AllowEdit,\n" +
            "       v.Allow_Delete AS AllowDelete, v.Allow_Export AS AllowExport, v.Export_Formats AS ExportFormats,\n" +
            "       v.Export_File_Name_Key AS ExportFileNameKey, v.Allow_Print AS AllowPrint,\n" +
            "       v.Key_Field AS KeyField, v.Parent_Field AS ParentField, v.Expand_Level AS ExpandLevel,\n" +
            "       v.Filter_Panel_Enabled AS FilterPanelEnabled, v.Filter_Panel_Position AS FilterPanelPosition,\n" +
            "       v.Filter_Collapsible AS FilterCollapsible, v.Auto_Search_On_Load AS AutoSearchOnLoad,\n" +
            "       v.Search_Label_Key AS SearchLabelKey, v.Reset_Label_Key AS ResetLabelKey,\n" +
            "       v.Detail_View_Id AS DetailViewId, v.Default_Filter_Json AS DefaultFilterJson,\n" +
            "       v.Options_Json AS OptionsJson, v.Tenant_Id AS TenantId, v.Version AS Version,\n" +
            "       v.Is_Active AS IsActive, v.Created_At AS CreatedAt, v.Updated_At AS UpdatedAt,\n" +
            "       v.Description AS Description\n" +
            "FROM   dbo.Ui_View v\n" +
            "LEFT JOIN dbo.Sys_Table st ON st.Table_Id = v.Table_Id\n" +
            "WHERE  (v.Tenant_Id = @TenantId OR v.Tenant_Id IS NULL)\n" +
            whereActive +
            "ORDER BY v.View_Code";

        var result = await conn.QueryAsync<ViewRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<ViewDetailRecord?> GetViewDetailAsync(int viewId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await EnsureSchemaAsync(conn, ct);

        var headerSql =
            "SELECT v.View_Id AS ViewId, v.View_Code AS ViewCode, v.View_Type AS ViewType,\n" +
            "       v.Table_Id AS TableId, ISNULL(st.Table_Code, '') AS TableCode,\n" +
            "       v.Source_Type AS SourceType, v.Source_Object AS SourceObject,\n" +
            "       v.Title_Key AS TitleKey, v.Edit_Form_Id AS EditFormId,\n" +
            "       v.Page_Size AS PageSize, v.Allow_Paging AS AllowPaging, v.Virtual_Scroll AS VirtualScroll,\n" +
            "       v.Show_Filter_Row AS ShowFilterRow, v.Show_Group_Panel AS ShowGroupPanel,\n" +
            "       v.Show_Search_Box AS ShowSearchBox, v.Show_Column_Chooser AS ShowColumnChooser,\n" +
            "       v.Selection_Mode AS SelectionMode, v.Allow_Add AS AllowAdd, v.Allow_Edit AS AllowEdit,\n" +
            "       v.Allow_Delete AS AllowDelete, v.Allow_Export AS AllowExport, v.Export_Formats AS ExportFormats,\n" +
            "       v.Export_File_Name_Key AS ExportFileNameKey, v.Allow_Print AS AllowPrint,\n" +
            "       v.Key_Field AS KeyField, v.Parent_Field AS ParentField, v.Expand_Level AS ExpandLevel,\n" +
            "       v.Filter_Panel_Enabled AS FilterPanelEnabled, v.Filter_Panel_Position AS FilterPanelPosition,\n" +
            "       v.Filter_Collapsible AS FilterCollapsible, v.Auto_Search_On_Load AS AutoSearchOnLoad,\n" +
            "       v.Search_Label_Key AS SearchLabelKey, v.Reset_Label_Key AS ResetLabelKey,\n" +
            "       v.Detail_View_Id AS DetailViewId, v.Default_Filter_Json AS DefaultFilterJson,\n" +
            "       v.Options_Json AS OptionsJson, v.Tenant_Id AS TenantId, v.Version AS Version,\n" +
            "       v.Is_Active AS IsActive, v.Created_At AS CreatedAt, v.Updated_At AS UpdatedAt,\n" +
            "       v.Description AS Description\n" +
            "FROM   dbo.Ui_View v\n" +
            "LEFT JOIN dbo.Sys_Table st ON st.Table_Id = v.Table_Id\n" +
            "WHERE  v.View_Id = @ViewId";

        var header = await conn.QuerySingleOrDefaultAsync<ViewRecord>(
            new CommandDefinition(headerSql, new { ViewId = viewId }, cancellationToken: ct));
        if (header is null) return null;

        var columnsSql =
            "SELECT View_Column_Id AS ViewColumnId, Column_Id AS ColumnId, Field_Name AS FieldName,\n" +
            "       Caption_Key AS CaptionKey, Column_Kind AS ColumnKind, Width AS Width, Min_Width AS MinWidth,\n" +
            "       Text_Align AS TextAlign, Display_Format AS DisplayFormat, Render_Mode AS RenderMode,\n" +
            "       Cell_Template_Key AS CellTemplateKey, Is_Visible AS IsVisible, Order_No AS OrderNo,\n" +
            "       Fixed_Position AS FixedPosition, Allow_Sort AS AllowSort, Sort_Order AS SortOrder,\n" +
            "       Sort_Index AS SortIndex, Allow_Filter AS AllowFilter, Allow_Group AS AllowGroup,\n" +
            "       Group_Index AS GroupIndex, Summary_Type AS SummaryType, Allow_Export AS AllowExport,\n" +
            "       Export_Format AS ExportFormat, Export_Caption_Key AS ExportCaptionKey,\n" +
            "       Style_Rule_Json AS StyleRuleJson, Props_Json AS PropsJson, Is_Active AS IsActive\n" +
            "FROM   dbo.Ui_View_Column\n" +
            "WHERE  View_Id = @ViewId\n" +
            "ORDER BY Order_No, View_Column_Id";

        var columns = await conn.QueryAsync<ViewColumnRecord>(
            new CommandDefinition(columnsSql, new { ViewId = viewId }, cancellationToken: ct));

        var actionsSql =
            "SELECT Action_Id AS ActionId, Action_Code AS ActionCode, Action_Type AS ActionType,\n" +
            "       Scope AS Scope, Label_Key AS LabelKey, Tooltip_Key AS TooltipKey, Confirm_Key AS ConfirmKey,\n" +
            "       Icon AS Icon, Export_Format AS ExportFormat, Export_Engine AS ExportEngine, Target AS Target,\n" +
            "       Require_Selection AS RequireSelection, Order_No AS OrderNo, Props_Json AS PropsJson,\n" +
            "       Is_Active AS IsActive\n" +
            "FROM   dbo.Ui_View_Action\n" +
            "WHERE  View_Id = @ViewId\n" +
            "ORDER BY Order_No, Action_Id";

        var actions = await conn.QueryAsync<ViewActionRecord>(
            new CommandDefinition(actionsSql, new { ViewId = viewId }, cancellationToken: ct));

        var filtersSql =
            "SELECT Filter_Id AS FilterId, Filter_Code AS FilterCode, Control_Type AS ControlType,\n" +
            "       Label_Key AS LabelKey, Placeholder_Key AS PlaceholderKey, Tooltip_Key AS TooltipKey,\n" +
            "       Param_Name AS ParamName, Param_Type AS ParamType, Operator AS Operator,\n" +
            "       Default_Value AS DefaultValue, Is_Required AS IsRequired, Is_Visible AS IsVisible,\n" +
            "       Order_No AS OrderNo, Col_Span AS ColSpan, Lookup_Source AS LookupSource,\n" +
            "       Lookup_Code AS LookupCode, Lookup_Sql AS LookupSql, Props_Json AS PropsJson,\n" +
            "       Depends_On AS DependsOn, Default_To_Field AS DefaultToField, Default_Lock AS DefaultLock,\n" +
            "       Is_Active AS IsActive\n" +
            "FROM   dbo.Ui_View_Filter\n" +
            "WHERE  View_Id = @ViewId\n" +
            "ORDER BY Order_No, Filter_Id";

        var filters = await conn.QueryAsync<ViewFilterRecord>(
            new CommandDefinition(filtersSql, new { ViewId = viewId }, cancellationToken: ct));

        return new ViewDetailRecord
        {
            Header = header,
            Columns = columns.ToList(),
            Actions = actions.ToList(),
            Filters = filters.ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<int> SaveViewAsync(ViewUpsertRequest request, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");

        var code = request.ViewCode.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("View_Code không được để trống.");
        if (request.TableId <= 0)
            throw new InvalidOperationException("Phải chọn bảng nguồn (Table_Id).");

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSchemaAsync(conn, ct);

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // ── Chống trùng View_Code trong phạm vi tenant ────────
            var dupSql =
                "SELECT TOP (1) 1 FROM dbo.Ui_View\n" +
                "WHERE View_Code = @Code AND (Tenant_Id = @TenantId OR Tenant_Id IS NULL)\n" +
                "  AND View_Id <> @ViewId";
            var duplicate = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
                dupSql, new { Code = code, TenantId = tenantId, ViewId = request.ViewId ?? 0 },
                tx, cancellationToken: ct));
            if (duplicate.HasValue)
                throw new InvalidOperationException($"View_Code '{code}' đã tồn tại.");

            int viewId;
            var p = BuildHeaderParams(request, code, tenantId);

            if (request.ViewId is null or 0)
            {
                const string insertSql =
                    "INSERT INTO dbo.Ui_View (View_Code, View_Type, Table_Id, Source_Type, Source_Object,\n" +
                    "    Title_Key, Edit_Form_Id, Page_Size, Allow_Paging, Virtual_Scroll, Show_Filter_Row,\n" +
                    "    Show_Group_Panel, Show_Search_Box, Show_Column_Chooser, Selection_Mode, Allow_Add,\n" +
                    "    Allow_Edit, Allow_Delete, Allow_Export, Export_Formats, Export_File_Name_Key, Allow_Print,\n" +
                    "    Key_Field, Parent_Field, Expand_Level, Filter_Panel_Enabled, Filter_Panel_Position,\n" +
                    "    Filter_Collapsible, Auto_Search_On_Load, Search_Label_Key, Reset_Label_Key,\n" +
                    "    Detail_View_Id, Default_Filter_Json, Options_Json,\n" +
                    "    Tenant_Id, Version, Is_Active, Created_At, Updated_At, Description)\n" +
                    "VALUES (@ViewCode, @ViewType, @TableId, @SourceType, @SourceObject, @TitleKey, @EditFormId,\n" +
                    "    @PageSize, @AllowPaging, @VirtualScroll, @ShowFilterRow, @ShowGroupPanel, @ShowSearchBox,\n" +
                    "    @ShowColumnChooser, @SelectionMode, @AllowAdd, @AllowEdit, @AllowDelete, @AllowExport,\n" +
                    "    @ExportFormats, @ExportFileNameKey, @AllowPrint, @KeyField, @ParentField, @ExpandLevel,\n" +
                    "    @FilterPanelEnabled, @FilterPanelPosition, @FilterCollapsible, @AutoSearchOnLoad,\n" +
                    "    @SearchLabelKey, @ResetLabelKey,\n" +
                    "    @DetailViewId, @DefaultFilterJson, @OptionsJson, @TenantId, 1, @IsActive, GETDATE(), GETDATE(),\n" +
                    "    @Description);\n" +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);";
                viewId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(insertSql, p, tx, cancellationToken: ct));
            }
            else
            {
                viewId = request.ViewId.Value;
                p.Add("ViewId", viewId);
                p.Add("Version", request.Version);
                const string updateSql =
                    "UPDATE dbo.Ui_View SET View_Code = @ViewCode, View_Type = @ViewType, Table_Id = @TableId,\n" +
                    "    Source_Type = @SourceType, Source_Object = @SourceObject, Title_Key = @TitleKey,\n" +
                    "    Edit_Form_Id = @EditFormId, Page_Size = @PageSize, Allow_Paging = @AllowPaging,\n" +
                    "    Virtual_Scroll = @VirtualScroll, Show_Filter_Row = @ShowFilterRow,\n" +
                    "    Show_Group_Panel = @ShowGroupPanel, Show_Search_Box = @ShowSearchBox,\n" +
                    "    Show_Column_Chooser = @ShowColumnChooser, Selection_Mode = @SelectionMode,\n" +
                    "    Allow_Add = @AllowAdd, Allow_Edit = @AllowEdit, Allow_Delete = @AllowDelete,\n" +
                    "    Allow_Export = @AllowExport, Export_Formats = @ExportFormats,\n" +
                    "    Export_File_Name_Key = @ExportFileNameKey, Allow_Print = @AllowPrint, Key_Field = @KeyField,\n" +
                    "    Parent_Field = @ParentField, Expand_Level = @ExpandLevel,\n" +
                    "    Filter_Panel_Enabled = @FilterPanelEnabled, Filter_Panel_Position = @FilterPanelPosition,\n" +
                    "    Filter_Collapsible = @FilterCollapsible, Auto_Search_On_Load = @AutoSearchOnLoad,\n" +
                    "    Search_Label_Key = @SearchLabelKey, Reset_Label_Key = @ResetLabelKey,\n" +
                    "    Detail_View_Id = @DetailViewId,\n" +
                    "    Default_Filter_Json = @DefaultFilterJson, Options_Json = @OptionsJson,\n" +
                    "    Version = Version + 1, Is_Active = @IsActive, Updated_At = GETDATE(),\n" +
                    "    Description = @Description\n" +
                    "WHERE View_Id = @ViewId AND Version = @Version";
                var affected = await conn.ExecuteAsync(
                    new CommandDefinition(updateSql, p, tx, cancellationToken: ct));
                if (affected == 0)
                    throw new InvalidOperationException(
                        "View đã bị thay đổi bởi phiên khác (optimistic concurrency). Vui lòng tải lại.");
            }

            // ── Ghi lại toàn bộ cột + action + filter (xóa cũ → insert mới) ──
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM dbo.Ui_View_Column WHERE View_Id = @ViewId;\n" +
                "DELETE FROM dbo.Ui_View_Action WHERE View_Id = @ViewId;\n" +
                "DELETE FROM dbo.Ui_View_Filter WHERE View_Id = @ViewId;",
                new { ViewId = viewId }, tx, cancellationToken: ct));

            await InsertColumnsAsync(conn, tx, viewId, request.Columns, ct);
            await InsertActionsAsync(conn, tx, viewId, request.Actions, ct);
            await InsertFiltersAsync(conn, tx, viewId, request.Filters, ct);

            await tx.CommitAsync(ct);
            return viewId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeactivateViewAsync(int viewId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình.");

        await using var conn = new SqlConnection(_config.ConnectionString);
        await EnsureSchemaAsync(conn, ct);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Ui_View SET Is_Active = 0, Updated_At = GETDATE()\n" +
            "WHERE View_Id = @ViewId AND (Tenant_Id = @TenantId OR Tenant_Id IS NULL)",
            new { ViewId = viewId, TenantId = tenantId }, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy View_Id={viewId} để ẩn.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FkLookupFieldOption>> GetFormLookupFieldsAsync(
        int formId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || formId <= 0) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);
        await EnsureSchemaAsync(conn, ct);

        // Field FK = field của form có cấu hình Ui_Field_Lookup; kèm cột FK gốc (Sys_Column) + bảng/cột tên.
        var sql =
            "SELECT fi.Field_Id      AS FieldId,\n" +
            "       sc.Column_Code   AS BaseColumn,\n" +
            "       fl.Source_Name   AS SourceName,\n" +
            "       fl.Display_Column AS DisplayColumn\n" +
            "FROM   dbo.Ui_Field        fi\n" +
            "JOIN   dbo.Ui_Field_Lookup fl ON fl.Field_Id  = fi.Field_Id\n" +
            "LEFT JOIN dbo.Sys_Column   sc ON sc.Column_Id = fi.Column_Id\n" +
            "WHERE  fi.Form_Id = @FormId\n" +
            "ORDER BY sc.Column_Code";

        var rows = await conn.QueryAsync<FkLookupFieldOption>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return rows.ToList();
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Dựng tham số Dapper cho header Ui_View từ request.
    /// </summary>
    /// <param name="r">Payload upsert.</param>
    /// <param name="code">View_Code đã trim.</param>
    /// <param name="tenantId">Tenant gắn vào bản ghi mới.</param>
    /// <returns><see cref="DynamicParameters"/> đủ tham số cho INSERT/UPDATE.</returns>
    private static DynamicParameters BuildHeaderParams(ViewUpsertRequest r, string code, int tenantId)
    {
        var p = new DynamicParameters();
        p.Add("ViewCode", code);
        p.Add("ViewType", string.IsNullOrWhiteSpace(r.ViewType) ? "Grid" : r.ViewType);
        p.Add("TableId", r.TableId);
        p.Add("SourceType", string.IsNullOrWhiteSpace(r.SourceType) ? "Table" : r.SourceType);
        p.Add("SourceObject", NullIfEmpty(r.SourceObject));
        p.Add("TitleKey", NullIfEmpty(r.TitleKey));
        p.Add("EditFormId", r.EditFormId);
        p.Add("PageSize", r.PageSize <= 0 ? 20 : r.PageSize);
        p.Add("AllowPaging", r.AllowPaging);
        p.Add("VirtualScroll", r.VirtualScroll);
        p.Add("ShowFilterRow", r.ShowFilterRow);
        p.Add("ShowGroupPanel", r.ShowGroupPanel);
        p.Add("ShowSearchBox", r.ShowSearchBox);
        p.Add("ShowColumnChooser", r.ShowColumnChooser);
        p.Add("SelectionMode", string.IsNullOrWhiteSpace(r.SelectionMode) ? "none" : r.SelectionMode);
        p.Add("AllowAdd", r.AllowAdd);
        p.Add("AllowEdit", r.AllowEdit);
        p.Add("AllowDelete", r.AllowDelete);
        p.Add("AllowExport", r.AllowExport);
        p.Add("ExportFormats", NullIfEmpty(r.ExportFormats));
        p.Add("ExportFileNameKey", NullIfEmpty(r.ExportFileNameKey));
        p.Add("AllowPrint", r.AllowPrint);
        p.Add("KeyField", NullIfEmpty(r.KeyField));
        p.Add("ParentField", NullIfEmpty(r.ParentField));
        p.Add("ExpandLevel", r.ExpandLevel);
        p.Add("FilterPanelEnabled", r.FilterPanelEnabled);
        p.Add("FilterPanelPosition", string.IsNullOrWhiteSpace(r.FilterPanelPosition) ? "left" : r.FilterPanelPosition);
        p.Add("FilterCollapsible", r.FilterCollapsible);
        p.Add("AutoSearchOnLoad", r.AutoSearchOnLoad);
        p.Add("SearchLabelKey", NullIfEmpty(r.SearchLabelKey));
        p.Add("ResetLabelKey", NullIfEmpty(r.ResetLabelKey));
        p.Add("DetailViewId", r.DetailViewId);
        p.Add("DefaultFilterJson", NullIfEmpty(r.DefaultFilterJson));
        p.Add("OptionsJson", NullIfEmpty(r.OptionsJson));
        p.Add("TenantId", tenantId);
        p.Add("IsActive", r.IsActive);
        p.Add("Description", NullIfEmpty(r.Description));
        return p;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetImportKeyFieldsAsync(int viewId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || viewId <= 0) return [];
        const string sql =
            "SELECT Field_Name FROM dbo.Ui_View_Column " +
            "WHERE View_Id = @ViewId AND Is_Import_Key = 1 AND Is_Active = 1 ORDER BY Order_No";
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            var names = await conn.QueryAsync<string?>(
                new CommandDefinition(sql, new { ViewId = viewId }, cancellationToken: ct));
            return names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();
        }
        catch (SqlException)
        {
            return [];   // cột Is_Import_Key chưa migrate (db/075) → coi như không có khóa ghép
        }
    }

    /// <inheritdoc />
    public async Task SaveImportKeyFieldsAsync(
        int viewId, IReadOnlyCollection<string> keyFieldNames, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || viewId <= 0) return;
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            if (keyFieldNames.Count == 0)
            {
                // Không tick cột nào → tắt hết.
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Ui_View_Column SET Is_Import_Key = 0 WHERE View_Id = @ViewId",
                    new { ViewId = viewId }, cancellationToken: ct));
            }
            else
            {
                // Bật cột trong danh sách, tắt cột còn lại (đồng bộ nguyên khối).
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Ui_View_Column " +
                    "SET Is_Import_Key = CASE WHEN Field_Name IN @Keys THEN 1 ELSE 0 END " +
                    "WHERE View_Id = @ViewId",
                    new { ViewId = viewId, Keys = keyFieldNames }, cancellationToken: ct));
            }
        }
        catch (SqlException)
        {
            /* cột chưa migrate (db/075) → bỏ qua, không chặn lưu View */
        }
    }

    /// <summary>Ghi danh sách cột Ui_View_Column trong transaction.</summary>
    /// <param name="conn">Kết nối đang mở.</param>
    /// <param name="tx">Transaction hiện hành.</param>
    /// <param name="viewId">View_Id cha.</param>
    /// <param name="columns">Danh sách cột cần ghi.</param>
    /// <param name="ct">Token hủy.</param>
    private static async Task InsertColumnsAsync(
        SqlConnection conn, System.Data.Common.DbTransaction tx, int viewId,
        IReadOnlyList<ViewColumnRecord> columns, CancellationToken ct)
    {
        const string sql =
            "INSERT INTO dbo.Ui_View_Column (View_Id, Column_Id, Field_Name, Caption_Key, Column_Kind, Width,\n" +
            "    Min_Width, Text_Align, Display_Format, Render_Mode, Cell_Template_Key, Is_Visible, Order_No,\n" +
            "    Fixed_Position, Allow_Sort, Sort_Order, Sort_Index, Allow_Filter, Allow_Group, Group_Index,\n" +
            "    Summary_Type, Allow_Export, Export_Format, Export_Caption_Key, Style_Rule_Json, Props_Json, Is_Active)\n" +
            "VALUES (@ViewId, @ColumnId, @FieldName, @CaptionKey, @ColumnKind, @Width, @MinWidth, @TextAlign,\n" +
            "    @DisplayFormat, @RenderMode, @CellTemplateKey, @IsVisible, @OrderNo, @FixedPosition, @AllowSort,\n" +
            "    @SortOrder, @SortIndex, @AllowFilter, @AllowGroup, @GroupIndex, @SummaryType, @AllowExport,\n" +
            "    @ExportFormat, @ExportCaptionKey, @StyleRuleJson, @PropsJson, @IsActive)";

        var order = 0;
        foreach (var c in columns)
        {
            if (string.IsNullOrWhiteSpace(c.FieldName)) continue; // bỏ dòng rỗng
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ViewId = viewId,
                c.ColumnId,
                FieldName = c.FieldName.Trim(),
                CaptionKey = NullIfEmpty(c.CaptionKey),
                ColumnKind = string.IsNullOrWhiteSpace(c.ColumnKind) ? "Data" : c.ColumnKind,
                Width = NullIfEmpty(c.Width),
                c.MinWidth,
                TextAlign = NullIfEmpty(c.TextAlign),
                DisplayFormat = NullIfEmpty(c.DisplayFormat),
                RenderMode = string.IsNullOrWhiteSpace(c.RenderMode) ? "Text" : c.RenderMode,
                CellTemplateKey = NullIfEmpty(c.CellTemplateKey),
                c.IsVisible,
                OrderNo = order++,
                FixedPosition = NullIfEmpty(c.FixedPosition),
                c.AllowSort,
                SortOrder = NullIfEmpty(c.SortOrder),
                c.SortIndex,
                c.AllowFilter,
                c.AllowGroup,
                c.GroupIndex,
                SummaryType = NullIfEmpty(c.SummaryType),
                c.AllowExport,
                ExportFormat = NullIfEmpty(c.ExportFormat),
                ExportCaptionKey = NullIfEmpty(c.ExportCaptionKey),
                StyleRuleJson = NullIfEmpty(c.StyleRuleJson),
                PropsJson = NullIfEmpty(c.PropsJson),
                c.IsActive,
            }, tx, cancellationToken: ct));
        }
    }

    /// <summary>Ghi danh sách action Ui_View_Action trong transaction.</summary>
    /// <param name="conn">Kết nối đang mở.</param>
    /// <param name="tx">Transaction hiện hành.</param>
    /// <param name="viewId">View_Id cha.</param>
    /// <param name="actions">Danh sách action cần ghi.</param>
    /// <param name="ct">Token hủy.</param>
    private static async Task InsertActionsAsync(
        SqlConnection conn, System.Data.Common.DbTransaction tx, int viewId,
        IReadOnlyList<ViewActionRecord> actions, CancellationToken ct)
    {
        const string sql =
            "INSERT INTO dbo.Ui_View_Action (View_Id, Action_Code, Action_Type, Scope, Label_Key, Tooltip_Key,\n" +
            "    Confirm_Key, Icon, Export_Format, Export_Engine, Target, Require_Selection, Order_No, Props_Json, Is_Active)\n" +
            "VALUES (@ViewId, @ActionCode, @ActionType, @Scope, @LabelKey, @TooltipKey, @ConfirmKey, @Icon,\n" +
            "    @ExportFormat, @ExportEngine, @Target, @RequireSelection, @OrderNo, @PropsJson, @IsActive)";

        var order = 0;
        foreach (var a in actions)
        {
            if (string.IsNullOrWhiteSpace(a.ActionCode)) continue;
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ViewId = viewId,
                ActionCode = a.ActionCode.Trim(),
                ActionType = string.IsNullOrWhiteSpace(a.ActionType) ? "BuiltIn" : a.ActionType,
                Scope = string.IsNullOrWhiteSpace(a.Scope) ? "Toolbar" : a.Scope,
                LabelKey = NullIfEmpty(a.LabelKey),
                TooltipKey = NullIfEmpty(a.TooltipKey),
                ConfirmKey = NullIfEmpty(a.ConfirmKey),
                Icon = NullIfEmpty(a.Icon),
                ExportFormat = NullIfEmpty(a.ExportFormat),
                ExportEngine = NullIfEmpty(a.ExportEngine),
                Target = NullIfEmpty(a.Target),
                a.RequireSelection,
                OrderNo = order++,
                PropsJson = NullIfEmpty(a.PropsJson),
                a.IsActive,
            }, tx, cancellationToken: ct));
        }
    }

    /// <summary>Ghi danh sách control lọc Ui_View_Filter trong transaction.</summary>
    /// <param name="conn">Kết nối đang mở.</param>
    /// <param name="tx">Transaction hiện hành.</param>
    /// <param name="viewId">View_Id cha.</param>
    /// <param name="filters">Danh sách filter cần ghi.</param>
    /// <param name="ct">Token hủy.</param>
    private static async Task InsertFiltersAsync(
        SqlConnection conn, System.Data.Common.DbTransaction tx, int viewId,
        IReadOnlyList<ViewFilterRecord> filters, CancellationToken ct)
    {
        const string sql =
            "INSERT INTO dbo.Ui_View_Filter (View_Id, Filter_Code, Control_Type, Label_Key, Placeholder_Key,\n" +
            "    Tooltip_Key, Param_Name, Param_Type, Operator, Default_Value, Is_Required, Is_Visible, Order_No,\n" +
            "    Col_Span, Lookup_Source, Lookup_Code, Lookup_Sql, Props_Json,\n" +
            "    Depends_On, Default_To_Field, Default_Lock, Is_Active)\n" +
            "VALUES (@ViewId, @FilterCode, @ControlType, @LabelKey, @PlaceholderKey, @TooltipKey, @ParamName,\n" +
            "    @ParamType, @Operator, @DefaultValue, @IsRequired, @IsVisible, @OrderNo, @ColSpan, @LookupSource,\n" +
            "    @LookupCode, @LookupSql, @PropsJson,\n" +
            "    @DependsOn, @DefaultToField, @DefaultLock, @IsActive)";

        var order = 0;
        foreach (var f in filters)
        {
            // Bỏ dòng chưa khai báo đủ (Filter_Code + Param_Name là tối thiểu).
            if (string.IsNullOrWhiteSpace(f.FilterCode) || string.IsNullOrWhiteSpace(f.ParamName)) continue;
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ViewId = viewId,
                FilterCode = f.FilterCode.Trim(),
                ControlType = string.IsNullOrWhiteSpace(f.ControlType) ? "Text" : f.ControlType,
                LabelKey = NullIfEmpty(f.LabelKey),
                PlaceholderKey = NullIfEmpty(f.PlaceholderKey),
                TooltipKey = NullIfEmpty(f.TooltipKey),
                ParamName = f.ParamName.Trim(),
                ParamType = string.IsNullOrWhiteSpace(f.ParamType) ? "string" : f.ParamType,
                Operator = string.IsNullOrWhiteSpace(f.Operator) ? "=" : f.Operator,
                DefaultValue = NullIfEmpty(f.DefaultValue),
                f.IsRequired,
                f.IsVisible,
                OrderNo = order++,
                ColSpan = f.ColSpan < 1 ? (byte)1 : f.ColSpan,
                LookupSource = NullIfEmpty(f.LookupSource),
                LookupCode = NullIfEmpty(f.LookupCode),
                LookupSql = NullIfEmpty(f.LookupSql),
                PropsJson = NullIfEmpty(f.PropsJson),
                DependsOn = NullIfEmpty(f.DependsOn),
                DefaultToField = NullIfEmpty(f.DefaultToField),
                f.DefaultLock,
                f.IsActive,
            }, tx, cancellationToken: ct));
        }
    }

    /// <summary>
    /// Kiểm tra cụm bảng Ui_View đã tồn tại chưa; ném lỗi thân thiện nếu chưa chạy migration.
    /// </summary>
    /// <param name="conn">Kết nối SQL (sẽ tự mở khi query nếu chưa mở).</param>
    /// <param name="ct">Token hủy.</param>
    /// <remarks>Bảo vệ trường hợp migration VIEW-1 chưa chạy trên DB tenant.</remarks>
    private static async Task EnsureSchemaAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql =
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES\n" +
            "WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Ui_View'";
        var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
        if (exists == 0)
            throw new InvalidOperationException(
                "Chưa có bảng dbo.Ui_View. Cần chạy migration tạo Ui_View / Ui_View_Column / Ui_View_Action (VIEW-1) trước.");
    }

    /// <summary>Chuẩn hóa chuỗi rỗng/space về null để cột nullable lưu NULL thay vì ''.</summary>
    /// <param name="value">Chuỗi đầu vào.</param>
    /// <returns>null nếu rỗng/space, ngược lại chuỗi đã trim.</returns>
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
