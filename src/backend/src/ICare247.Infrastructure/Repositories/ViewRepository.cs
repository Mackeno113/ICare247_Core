// File    : ViewRepository.cs
// Module  : View
// Layer   : Infrastructure
// Purpose : Dapper implementation của IViewRepository — đọc Ui_View + cột + action (Config DB),
//           resolve text i18n (Title/Caption/Label/...) theo langCode.

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.View;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Ui_View</c> + <c>Ui_View_Column</c> + <c>Ui_View_Action</c>.
/// Ưu tiên bản tenant-specific hơn bản global (Tenant_Id NULL) khi trùng View_Code.
/// </summary>
/// <remarks>
/// Metadata đọc ở Config DB; dữ liệu (<see cref="GetDataAsync"/>) đọc ở Data DB. An toàn injection:
/// mọi identifier (schema/table/column) validate qua <see cref="SafeIdentifierRegex"/>, giá trị qua Dapper params.
/// </remarks>
public sealed partial class ViewRepository : IViewRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ViewRepository> _logger;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public ViewRepository(
        IDbConnectionFactory db, IDataDbConnectionFactory dataDb, ILogger<ViewRepository> logger)
    {
        _db = db;
        _dataDb = dataDb;
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
                   v.Filter_Panel_Enabled  AS FilterPanelEnabled,
                   v.Filter_Panel_Position AS FilterPanelPosition,
                   v.Filter_Collapsible    AS FilterCollapsible,
                   v.Auto_Search_On_Load   AS AutoSearchOnLoad,
                   v.Search_Label_Key      AS SearchLabelKey,
                   rsl.Resource_Value      AS SearchLabel,
                   v.Reset_Label_Key       AS ResetLabelKey,
                   rrl.Resource_Value      AS ResetLabel,
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
            LEFT JOIN dbo.Sys_Resource rsl ON rsl.Resource_Key = v.Search_Label_Key
                                          AND rsl.Lang_Code     = @LangCode
            LEFT JOIN dbo.Sys_Resource rrl ON rrl.Resource_Key = v.Reset_Label_Key
                                          AND rrl.Lang_Code     = @LangCode
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

        const string sqlFilters = """
            SELECT f.Filter_Id        AS FilterId,
                   f.Filter_Code      AS FilterCode,
                   f.Control_Type     AS ControlType,
                   f.Label_Key        AS LabelKey,
                   rfl.Resource_Value AS Label,
                   f.Placeholder_Key  AS PlaceholderKey,
                   rfp.Resource_Value AS Placeholder,
                   f.Tooltip_Key      AS TooltipKey,
                   rft.Resource_Value AS Tooltip,
                   f.Param_Name       AS ParamName,
                   f.Param_Type       AS ParamType,
                   f.Operator,
                   f.Default_Value    AS DefaultValue,
                   f.Is_Required      AS IsRequired,
                   f.Is_Visible       AS IsVisible,
                   f.Order_No         AS OrderNo,
                   f.Col_Span         AS ColSpan,
                   f.Lookup_Source    AS LookupSource,
                   f.Lookup_Code      AS LookupCode,
                   f.Lookup_Sql       AS LookupSql,
                   f.Props_Json       AS PropsJson
            FROM   dbo.Ui_View_Filter f
            LEFT JOIN dbo.Sys_Resource rfl ON rfl.Resource_Key = f.Label_Key
                                          AND rfl.Lang_Code     = @LangCode
            LEFT JOIN dbo.Sys_Resource rfp ON rfp.Resource_Key = f.Placeholder_Key
                                          AND rfp.Lang_Code     = @LangCode
            LEFT JOIN dbo.Sys_Resource rft ON rft.Resource_Key = f.Tooltip_Key
                                          AND rft.Lang_Code     = @LangCode
            WHERE  f.View_Id = @ViewId
              AND  f.Is_Active = 1
            ORDER BY f.Order_No
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

        var filters = (await conn.QueryAsync<ViewFilter>(
            new CommandDefinition(sqlFilters, byView, cancellationToken: ct))).AsList();

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
            FilterPanelEnabled = header.FilterPanelEnabled,
            FilterPanelPosition = header.FilterPanelPosition,
            FilterCollapsible = header.FilterCollapsible,
            AutoSearchOnLoad = header.AutoSearchOnLoad,
            SearchLabelKey = header.SearchLabelKey,
            SearchLabel = header.SearchLabel,
            ResetLabelKey = header.ResetLabelKey,
            ResetLabel = header.ResetLabel,
            TenantId = header.TenantId,
            Version = header.Version,
            IsActive = header.IsActive,
            Description = header.Description,
            Columns = columns,
            Actions = actions,
            Filters = filters
        };
    }

    /// <inheritdoc />
    public async Task<ViewDataResult> GetDataAsync(
        ViewMetadata view, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        // Đọc trực tiếp chỉ áp cho Table/View (SELECT giống nhau). Sp/Sql đi qua GetFilteredDataAsync (panel lọc).
        if (!IsDirectReadSource(view.SourceType))
            throw new NotSupportedException(
                $"View '{view.ViewCode}': Source_Type='{view.SourceType}' chưa hỗ trợ đọc trực tiếp " +
                $"(chỉ 'Table'/'View'; nguồn 'Sp'/'Sql' phải qua panel lọc).");

        // ── Nguồn FROM: bảng vật lý (Sys_Table) hoặc SQL View (Source_Object) ──
        var table = await ResolveFromTargetAsync(view, ct);

        // ── Cột Data (Field_Name) — whitelist ─────────────────────────────
        var dataCols = view.Columns
            .Where(c => string.Equals(c.ColumnKind, "Data", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.FieldName)
            .Where(n => !string.IsNullOrWhiteSpace(n) && SafeIdentifierRegex().IsMatch(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (dataCols.Count == 0)
            return new ViewDataResult();

        var keyValid = !string.IsNullOrWhiteSpace(view.KeyField) && SafeIdentifierRegex().IsMatch(view.KeyField);

        // Order: Key_Field nếu hợp lệ, ngược lại cột Data đầu tiên (OFFSET cần ORDER BY).
        var orderCol = keyValid ? view.KeyField! : dataCols[0];

        // SELECT: cột Data + thêm Key_Field (PK) nếu chưa có → client có khóa để Sửa/Xóa theo dòng.
        var selectFields = new List<string>(dataCols);
        if (keyValid && !selectFields.Contains(view.KeyField!, StringComparer.OrdinalIgnoreCase))
            selectFields.Insert(0, view.KeyField!);
        var selectCols = string.Join(", ", selectFields.Select(Bracket));

        var dp = new DynamicParameters();
        var whereSql = "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            // CAST sang NVARCHAR để LIKE hoạt động trên mọi kiểu cột (không cần biết NetType).
            whereSql = " WHERE (" + string.Join(" OR ",
                dataCols.Select(c => $"CAST({Bracket(c)} AS NVARCHAR(4000)) LIKE @Search")) + ")";
            dp.Add("Search", $"%{search.Trim()}%");
        }

        dp.Add("Skip", Math.Max(0, (page - 1) * pageSize));
        dp.Add("Take", pageSize < 1 ? 50 : pageSize);

        var listSql =
            $"SELECT {selectCols} FROM {table}{whereSql} " +
            $"ORDER BY {Bracket(orderCol)} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        var countSql = $"SELECT COUNT(*) FROM {table}{whereSql}";

        using var data = _dataDb.CreateConnection();
        var rows = await data.QueryAsync(new CommandDefinition(listSql, dp, cancellationToken: ct));
        var total = await data.ExecuteScalarAsync<int>(new CommandDefinition(countSql, dp, cancellationToken: ct));

        return new ViewDataResult
        {
            Items = rows.Select(r => (IDictionary<string, object?>)
                            ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => (object?)v.Value))
                        .ToList(),
            TotalCount = total
        };
    }

    /// <summary>Nguồn cho phép đọc trực tiếp bằng SELECT (Table hoặc SQL View). Sp/Sql thì không.</summary>
    private static bool IsDirectReadSource(string? sourceType) =>
        string.Equals(sourceType, "Table", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, "View", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Phân giải đối tượng FROM cho SELECT trực tiếp: <c>Source_Type='View'</c> kèm <c>Source_Object</c>
    /// → đọc thẳng view đó (cho phép 'schema.object'); ngược lại lấy bảng vật lý từ <c>Sys_Table</c> theo Table_Id.
    /// Mọi identifier whitelist qua <see cref="SafeIdentifierRegex"/> (chống injection).
    /// </summary>
    private async Task<string> ResolveFromTargetAsync(ViewMetadata view, CancellationToken ct)
    {
        // 'View' + có Source_Object → FROM chính view đó (vd vw_DM_TinhThanhPho — JOIN sẵn tên cha).
        if (string.Equals(view.SourceType, "View", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(view.SourceObject))
            return ParseQualifiedName(view.SourceObject!, view.ViewCode);

        // Mặc định: bảng/view vật lý từ Sys_Table (Config DB).
        TableRow? tbl;
        using (var cfg = _db.CreateConnection())
        {
            tbl = await cfg.QueryFirstOrDefaultAsync<TableRow>(new CommandDefinition(
                "SELECT Schema_Name AS SchemaName, Table_Code AS TableName FROM dbo.Sys_Table WHERE Table_Id = @TableId",
                new { view.TableId }, cancellationToken: ct));
        }
        if (tbl is null)
            throw new InvalidOperationException(
                $"View '{view.ViewCode}': bảng nguồn Table_Id={view.TableId} không tồn tại.");

        var schema = SafeIdentifierRegex().IsMatch(tbl.SchemaName ?? "") ? tbl.SchemaName! : "dbo";
        if (string.IsNullOrWhiteSpace(tbl.TableName) || !SafeIdentifierRegex().IsMatch(tbl.TableName))
            throw new InvalidOperationException($"View '{view.ViewCode}': tên bảng không hợp lệ.");
        return $"{Bracket(schema)}.{Bracket(tbl.TableName)}";
    }

    /// <summary>Tách 'schema.object' hoặc 'object' → '[schema].[object]' (mặc định schema dbo); mỗi phần whitelist.</summary>
    private static string ParseQualifiedName(string name, string viewCode)
    {
        var parts = name.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || parts.Any(p => !SafeIdentifierRegex().IsMatch(p)))
            throw new InvalidOperationException($"View '{viewCode}': tên nguồn '{name}' không hợp lệ.");
        return parts.Length == 2
            ? $"{Bracket(parts[0])}.{Bracket(parts[1])}"
            : $"[dbo].{Bracket(parts[0])}";
    }

    /// <inheritdoc />
    public async Task<ViewDataResult> GetFilteredDataAsync(
        ViewMetadata view, IReadOnlyDictionary<string, string?> filterValues, CancellationToken ct = default)
    {
        if (!view.IsQuerySource)
            throw new NotSupportedException(
                $"View '{view.ViewCode}': GetFilteredDataAsync chỉ dùng cho Source_Type='Sp'/'Sql' " +
                $"(hiện '{view.SourceType}').");
        if (string.IsNullOrWhiteSpace(view.SourceObject))
            throw new InvalidOperationException(
                $"View '{view.ViewCode}': Source_Object rỗng — chưa khai báo SP/SQL.");

        var isSp = string.Equals(view.SourceType, "Sp", StringComparison.OrdinalIgnoreCase);

        // ── Bind tham số: CHỈ từ view.Filters (whitelist). Code lạ trong filterValues bị bỏ qua. ──
        var dp = new DynamicParameters();
        foreach (var f in view.Filters)
        {
            if (string.IsNullOrWhiteSpace(f.ParamName))
                continue;

            // Giá trị người dùng (theo Filter_Code) → fallback Default_Value khi không gửi.
            filterValues.TryGetValue(f.FilterCode, out var raw);
            if (string.IsNullOrWhiteSpace(raw))
                raw = f.DefaultValue;

            // Required mà rỗng → chặn (client cũng chặn + focus, đây là hàng rào server).
            if (f.IsRequired && string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException(
                    $"Thiếu tham số bắt buộc '{f.FilterCode}' ({f.ParamName}).", f.FilterCode);

            var value = ConvertFilterValue(raw, f.ParamType, f.Operator, f.FilterCode);
            // Luôn add (NULL khi rỗng) → SP/SQL kiểu `WHERE (@x IS NULL OR col=@x)` bỏ lọc đúng.
            dp.Add(NormalizeParamName(f.ParamName), value);
        }

        using var data = _dataDb.CreateConnection();

        var rows = isSp
            ? await data.QueryAsync(new CommandDefinition(
                ValidateSpName(view.SourceObject!, view.ViewCode), dp,
                commandType: System.Data.CommandType.StoredProcedure, cancellationToken: ct))
            : await data.QueryAsync(new CommandDefinition(
                view.SourceObject!, dp, commandType: System.Data.CommandType.Text, cancellationToken: ct));

        var items = rows
            .Select(r => (IDictionary<string, object?>)
                ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => (object?)v.Value))
            .ToList();

        // SP/SQL trả nguyên tập đã lọc → client phân trang. TotalCount = số dòng trả về.
        return new ViewDataResult { Items = items, TotalCount = items.Count };
    }

    /// <summary>Bỏ tiền tố '@' để Dapper tự thêm (tránh '@@'); whitelist ký tự an toàn.</summary>
    private static string NormalizeParamName(string name)
    {
        var n = name.TrimStart('@');
        if (!SafeIdentifierRegex().IsMatch(n))
            throw new InvalidOperationException($"Tên tham số không hợp lệ: '{name}'.");
        return n;
    }

    /// <summary>Validate tên SP ('schema.proc' hoặc 'proc') — mỗi phần là identifier an toàn.</summary>
    private static string ValidateSpName(string spName, string viewCode)
    {
        var parts = spName.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || parts.Any(p => !SafeIdentifierRegex().IsMatch(p)))
            throw new InvalidOperationException(
                $"View '{viewCode}': tên Stored Procedure '{spName}' không hợp lệ.");
        return string.Join('.', parts.Select(Bracket));
    }

    /// <summary>Ép giá trị thô (string) sang kiểu tham số; bọc %...% khi LIKE; tách mảng khi IN.</summary>
    private static object? ConvertFilterValue(string? raw, string paramType, string op, string filterCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        raw = raw.Trim();

        if (string.Equals(op, "IN", StringComparison.OrdinalIgnoreCase))
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Select(s => ConvertScalar(s, paramType, filterCode))
                      .ToArray();

        var val = ConvertScalar(raw, paramType, filterCode);
        if (string.Equals(op, "LIKE", StringComparison.OrdinalIgnoreCase) && val is string s)
            return $"%{s}%";
        return val;
    }

    /// <summary>Ép 1 giá trị scalar theo Param_Type; ném <see cref="ArgumentException"/> nếu sai định dạng.</summary>
    private static object ConvertScalar(string raw, string paramType, string filterCode) =>
        paramType.ToLowerInvariant() switch
        {
            "int" => int.TryParse(raw, out var i)
                ? i : throw new ArgumentException($"'{filterCode}': '{raw}' không phải số nguyên.", filterCode),
            "decimal" => decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var d)
                ? d : throw new ArgumentException($"'{filterCode}': '{raw}' không phải số.", filterCode),
            "date" => DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var dt)
                ? dt : throw new ArgumentException($"'{filterCode}': '{raw}' không phải ngày.", filterCode),
            "bool" => raw is "1" or "true" or "True" ? true
                    : raw is "0" or "false" or "False" ? false
                    : throw new ArgumentException($"'{filterCode}': '{raw}' không phải bool.", filterCode),
            _ => raw   // string
        };

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ViewListItem> Items, int TotalCount)> GetListAsync(
        int tenantId, string langCode = "vi", bool? isActive = null, string? search = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        // ROW_NUMBER khử trùng View_Code: giữ bản tenant-specific (Tenant_Id non-NULL) trước bản global.
        const string sqlList = """
            WITH Ranked AS (
                SELECT v.View_Id       AS ViewId,
                       v.View_Code     AS ViewCode,
                       v.View_Type     AS ViewType,
                       t.Table_Code    AS TableCode,
                       rt.Resource_Value AS Title,
                       ef.Form_Code    AS EditFormCode,
                       v.Version,
                       v.Is_Active     AS IsActive,
                       (SELECT COUNT(*) FROM dbo.Ui_View_Column c
                         WHERE c.View_Id = v.View_Id AND c.Is_Active = 1) AS ColumnCount,
                       ROW_NUMBER() OVER (PARTITION BY v.View_Code
                                          ORDER BY CASE WHEN v.Tenant_Id IS NULL THEN 1 ELSE 0 END) AS Rn
                FROM   dbo.Ui_View v
                JOIN   dbo.Sys_Table t  ON t.Table_Id = v.Table_Id
                LEFT JOIN dbo.Ui_Form ef ON ef.Form_Id = v.Edit_Form_Id
                LEFT JOIN dbo.Sys_Resource rt ON rt.Resource_Key = v.Title_Key
                                             AND rt.Lang_Code     = @LangCode
                WHERE  (v.Tenant_Id = @TenantId OR v.Tenant_Id IS NULL)
                  AND  (@IsActive IS NULL OR v.Is_Active = @IsActive)
                  AND  (@Search IS NULL OR v.View_Code LIKE @Search OR rt.Resource_Value LIKE @Search)
            )
            SELECT ViewId, ViewCode, ViewType, TableCode, Title, EditFormCode,
                   ColumnCount, Version, IsActive
            FROM   Ranked
            WHERE  Rn = 1
            ORDER BY ViewCode
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        const string sqlCount = """
            SELECT COUNT(DISTINCT v.View_Code)
            FROM   dbo.Ui_View v
            LEFT JOIN dbo.Sys_Resource rt ON rt.Resource_Key = v.Title_Key
                                         AND rt.Lang_Code     = @LangCode
            WHERE  (v.Tenant_Id = @TenantId OR v.Tenant_Id IS NULL)
              AND  (@IsActive IS NULL OR v.Is_Active = @IsActive)
              AND  (@Search IS NULL OR v.View_Code LIKE @Search OR rt.Resource_Value LIKE @Search)
            """;

        var prm = new
        {
            TenantId = tenantId,
            LangCode = langCode,
            IsActive = isActive,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            Skip = Math.Max(0, (page - 1) * pageSize),
            Take = pageSize < 1 ? 50 : pageSize
        };

        using var conn = _db.CreateConnection();
        var items = (await conn.QueryAsync<ViewListItem>(
            new CommandDefinition(sqlList, prm, cancellationToken: ct))).AsList();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, prm, cancellationToken: ct));

        return (items, total);
    }

    /// <summary>Bọc identifier bằng [] (đã whitelist regex trước đó).</summary>
    private static string Bracket(string ident) => "[" + ident.Replace("]", "]]") + "]";

    /// <summary>Row tra cứu schema + tên bảng vật lý từ Sys_Table.</summary>
    private sealed class TableRow
    {
        public string? SchemaName { get; init; }
        public string? TableName { get; init; }
    }
}
