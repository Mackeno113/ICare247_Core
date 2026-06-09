// File    : ViewRepository.cs
// Module  : View
// Layer   : Infrastructure
// Purpose : Dapper implementation của IViewRepository — đọc Ui_View + cột + action (Config DB),
//           resolve text i18n (Title/Caption/Label/...) theo langCode.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.View;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Ui_View</c> + <c>Ui_View_Column</c> + <c>Ui_View_Action</c>.
/// Ưu tiên bản tenant-specific hơn bản global (Tenant_Id NULL) khi trùng View_Code.
/// </summary>
public sealed class ViewRepository : IViewRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ViewRepository> _logger;

    public ViewRepository(IDbConnectionFactory db, ILogger<ViewRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ViewMetadata?> GetByCodeAsync(
        string viewCode, int tenantId, string langCode = "vi", CancellationToken ct = default)
    {
        // ── Header: join Sys_Table (Table_Code), Ui_Form (Edit_Form_Code), Sys_Resource (Title/FileName) ──
        const string sqlHeader = """
            SELECT TOP 1
                   v.View_Id              AS ViewId,
                   v.View_Code            AS ViewCode,
                   v.View_Type            AS ViewType,
                   v.Table_Id             AS TableId,
                   t.Table_Code           AS TableCode,
                   v.Source_Type          AS SourceType,
                   v.Source_Object        AS SourceObject,
                   v.Title_Key            AS TitleKey,
                   rt.Resource_Value      AS Title,
                   v.Edit_Form_Id         AS EditFormId,
                   ef.Form_Code           AS EditFormCode,
                   v.Page_Size            AS PageSize,
                   v.Allow_Paging         AS AllowPaging,
                   v.Virtual_Scroll       AS VirtualScroll,
                   v.Show_Filter_Row      AS ShowFilterRow,
                   v.Show_Group_Panel     AS ShowGroupPanel,
                   v.Show_Search_Box      AS ShowSearchBox,
                   v.Show_Column_Chooser  AS ShowColumnChooser,
                   v.Selection_Mode       AS SelectionMode,
                   v.Allow_Add            AS AllowAdd,
                   v.Allow_Edit           AS AllowEdit,
                   v.Allow_Delete         AS AllowDelete,
                   v.Allow_Export         AS AllowExport,
                   v.Export_Formats       AS ExportFormats,
                   v.Export_File_Name_Key AS ExportFileNameKey,
                   rf.Resource_Value      AS ExportFileName,
                   v.Allow_Print          AS AllowPrint,
                   v.Key_Field            AS KeyField,
                   v.Parent_Field         AS ParentField,
                   v.Expand_Level         AS ExpandLevel,
                   v.Tenant_Id            AS TenantId,
                   v.Version,
                   v.Is_Active            AS IsActive,
                   v.Description
            FROM   dbo.Ui_View v
            JOIN   dbo.Sys_Table t  ON t.Table_Id  = v.Table_Id
            LEFT JOIN dbo.Ui_Form ef ON ef.Form_Id = v.Edit_Form_Id
            LEFT JOIN dbo.Sys_Resource rt ON rt.Resource_Key = v.Title_Key
                                         AND rt.Lang_Code     = @LangCode
            LEFT JOIN dbo.Sys_Resource rf ON rf.Resource_Key = v.Export_File_Name_Key
                                         AND rf.Lang_Code     = @LangCode
            WHERE  v.View_Code = @ViewCode
              AND  (v.Tenant_Id = @TenantId OR v.Tenant_Id IS NULL)
              AND  v.Is_Active  = 1
            ORDER BY v.Tenant_Id DESC   -- tenant-specific (non-NULL) ưu tiên; NULL xếp cuối khi DESC
            """;

        const string sqlColumns = """
            SELECT c.View_Column_Id    AS ViewColumnId,
                   c.Column_Id         AS ColumnId,
                   c.Field_Name        AS FieldName,
                   c.Caption_Key       AS CaptionKey,
                   rc.Resource_Value   AS Caption,
                   c.Column_Kind       AS ColumnKind,
                   c.Width,
                   c.Min_Width         AS MinWidth,
                   c.Text_Align        AS TextAlign,
                   c.Display_Format    AS DisplayFormat,
                   c.Render_Mode       AS RenderMode,
                   c.Cell_Template_Key AS CellTemplateKey,
                   c.Is_Visible        AS IsVisible,
                   c.Order_No          AS OrderNo,
                   c.Fixed_Position    AS FixedPosition,
                   c.Allow_Sort        AS AllowSort,
                   c.Sort_Order        AS SortOrder,
                   c.Sort_Index        AS SortIndex,
                   c.Allow_Filter      AS AllowFilter,
                   c.Allow_Group       AS AllowGroup,
                   c.Group_Index       AS GroupIndex,
                   c.Summary_Type      AS SummaryType,
                   c.Allow_Export      AS AllowExport,
                   c.Export_Format     AS ExportFormat,
                   c.Export_Caption_Key AS ExportCaptionKey,
                   rec.Resource_Value  AS ExportCaption,
                   c.Style_Rule_Json   AS StyleRuleJson
            FROM   dbo.Ui_View_Column c
            LEFT JOIN dbo.Sys_Resource rc  ON rc.Resource_Key  = c.Caption_Key
                                          AND rc.Lang_Code      = @LangCode
            LEFT JOIN dbo.Sys_Resource rec ON rec.Resource_Key = c.Export_Caption_Key
                                          AND rec.Lang_Code     = @LangCode
            WHERE  c.View_Id = @ViewId
              AND  c.Is_Active = 1
            ORDER BY c.Order_No
            """;

        const string sqlActions = """
            SELECT a.Action_Id         AS ActionId,
                   a.Action_Code       AS ActionCode,
                   a.Action_Type       AS ActionType,
                   a.Scope,
                   a.Label_Key         AS LabelKey,
                   rl.Resource_Value   AS Label,
                   a.Tooltip_Key       AS TooltipKey,
                   rtt.Resource_Value  AS Tooltip,
                   a.Confirm_Key       AS ConfirmKey,
                   rcf.Resource_Value  AS Confirm,
                   a.Icon,
                   a.Export_Format     AS ExportFormat,
                   a.Export_Engine     AS ExportEngine,
                   a.Target,
                   a.Require_Selection AS RequireSelection,
                   a.Order_No          AS OrderNo
            FROM   dbo.Ui_View_Action a
            LEFT JOIN dbo.Sys_Resource rl  ON rl.Resource_Key  = a.Label_Key
                                          AND rl.Lang_Code      = @LangCode
            LEFT JOIN dbo.Sys_Resource rtt ON rtt.Resource_Key = a.Tooltip_Key
                                          AND rtt.Lang_Code     = @LangCode
            LEFT JOIN dbo.Sys_Resource rcf ON rcf.Resource_Key = a.Confirm_Key
                                          AND rcf.Lang_Code     = @LangCode
            WHERE  a.View_Id = @ViewId
              AND  a.Is_Active = 1
            ORDER BY a.Order_No
            """;

        using var conn = _db.CreateConnection();

        var header = await conn.QueryFirstOrDefaultAsync<ViewMetadata>(
            new CommandDefinition(sqlHeader,
                new { ViewCode = viewCode, TenantId = tenantId, LangCode = langCode },
                cancellationToken: ct));

        if (header is null)
        {
            _logger.LogWarning(
                "View không tồn tại — ViewCode={ViewCode}, TenantId={TenantId}", viewCode, tenantId);
            return null;
        }

        var byView = new { ViewId = header.ViewId, LangCode = langCode };

        var columns = (await conn.QueryAsync<ViewColumn>(
            new CommandDefinition(sqlColumns, byView, cancellationToken: ct))).AsList();

        var actions = (await conn.QueryAsync<ViewAction>(
            new CommandDefinition(sqlActions, byView, cancellationToken: ct))).AsList();

        // ── Ráp aggregate (init props → tạo bản đầy đủ kèm cột + action) ──
        return new ViewMetadata
        {
            ViewId = header.ViewId,
            ViewCode = header.ViewCode,
            ViewType = header.ViewType,
            TableId = header.TableId,
            TableCode = header.TableCode,
            SourceType = header.SourceType,
            SourceObject = header.SourceObject,
            TitleKey = header.TitleKey,
            Title = header.Title,
            EditFormId = header.EditFormId,
            EditFormCode = header.EditFormCode,
            PageSize = header.PageSize,
            AllowPaging = header.AllowPaging,
            VirtualScroll = header.VirtualScroll,
            ShowFilterRow = header.ShowFilterRow,
            ShowGroupPanel = header.ShowGroupPanel,
            ShowSearchBox = header.ShowSearchBox,
            ShowColumnChooser = header.ShowColumnChooser,
            SelectionMode = header.SelectionMode,
            AllowAdd = header.AllowAdd,
            AllowEdit = header.AllowEdit,
            AllowDelete = header.AllowDelete,
            AllowExport = header.AllowExport,
            ExportFormats = header.ExportFormats,
            ExportFileNameKey = header.ExportFileNameKey,
            ExportFileName = header.ExportFileName,
            AllowPrint = header.AllowPrint,
            KeyField = header.KeyField,
            ParentField = header.ParentField,
            ExpandLevel = header.ExpandLevel,
            TenantId = header.TenantId,
            Version = header.Version,
            IsActive = header.IsActive,
            Description = header.Description,
            Columns = columns,
            Actions = actions
        };
    }
}
