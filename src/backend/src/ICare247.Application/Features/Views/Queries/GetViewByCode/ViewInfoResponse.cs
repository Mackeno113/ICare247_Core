// File    : ViewInfoResponse.cs
// Module  : Views
// Layer   : Application
// Purpose : Response DTO cho endpoint /views/{code}/info — mirror ViewMetadata nhưng LOẠI Lookup_Sql.
//           Lý do: ViewFilter.LookupSql là SQL admin tự viết (tên bảng quyền, cột, token ngữ cảnh)
//           → KHÔNG được lộ ra client. Cache (IConfigCache) vẫn giữ entity đầy đủ cho filter-options;
//           ta chỉ chặn ở tầng serialize-ra-client bằng DTO riêng, KHÔNG đụng entity cache (tránh
//           mất Lookup_Sql sau round-trip cache L2 nếu lỡ gắn [JsonIgnore] lên entity).

using ICare247.Domain.Entities.View;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Metadata View trả cho client (Grid/TreeList) — mirror <see cref="ViewMetadata"/> trừ trường nhạy cảm.
/// Cột/Action giữ nguyên entity (chỉ là cấu hình hiển thị, không chứa SQL); filter dùng
/// <see cref="ViewFilterInfo"/> đã loại <c>Lookup_Sql</c>.
/// </summary>
public sealed class ViewInfoResponse
{
    public int ViewId { get; init; }
    public string ViewCode { get; init; } = string.Empty;
    public string ViewType { get; init; } = "Grid";
    public int TableId { get; init; }
    public string TableCode { get; init; } = string.Empty;
    public string SourceType { get; init; } = "Table";
    public string? SourceObject { get; init; }
    public string? TitleKey { get; init; }
    public string? Title { get; init; }
    public int? EditFormId { get; init; }
    public string? EditFormCode { get; init; }

    // ── Hành vi lưới ──────────────────────────────────────────
    public int PageSize { get; init; } = 20;
    public bool AllowPaging { get; init; } = true;
    public bool VirtualScroll { get; init; }
    public bool ShowFilterRow { get; init; } = true;
    public bool ShowGroupPanel { get; init; }
    public bool ShowSearchBox { get; init; } = true;
    public bool ShowColumnChooser { get; init; }
    public string SelectionMode { get; init; } = "none";
    public bool AllowAdd { get; init; } = true;
    public bool AllowEdit { get; init; } = true;
    public bool AllowDelete { get; init; } = true;

    // ── Export / Print ────────────────────────────────────────
    public bool AllowExport { get; init; } = true;
    public string? ExportFormats { get; init; }
    public string? ExportFileNameKey { get; init; }
    public string? ExportFileName { get; init; }
    public bool AllowPrint { get; init; }

    // ── TreeList ──────────────────────────────────────────────
    public string? KeyField { get; init; }
    public string? ParentField { get; init; }
    public int? ExpandLevel { get; init; }
    public bool AllowReorder { get; init; }

    // ── Panel lọc trái ────────────────────────────────────────
    public bool FilterPanelEnabled { get; init; }
    public string FilterPanelPosition { get; init; } = "left";
    public bool FilterCollapsible { get; init; } = true;
    public bool AutoSearchOnLoad { get; init; }
    public string? SearchLabelKey { get; init; }
    public string? SearchLabel { get; init; }
    public string? ResetLabelKey { get; init; }
    public string? ResetLabel { get; init; }

    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public string? Description { get; init; }

    /// <summary>Cột hiển thị (Ui_View_Column) — chỉ cấu hình render, không chứa SQL.</summary>
    public IReadOnlyList<ViewColumn> Columns { get; init; } = [];

    /// <summary>Nút toolbar/row (Ui_View_Action).</summary>
    public IReadOnlyList<ViewAction> Actions { get; init; } = [];

    /// <summary>Control lọc trái — đã loại <c>Lookup_Sql</c> (không lộ SQL ra client).</summary>
    public IReadOnlyList<ViewFilterInfo> Filters { get; init; } = [];

    // ── Cờ dẫn xuất (giữ y hệt ViewMetadata để JSON client không đổi) ──
    /// <summary>Cờ View dạng cây.</summary>
    public bool IsTreeList =>
        string.Equals(ViewType, "TreeList", StringComparison.OrdinalIgnoreCase);

    /// <summary>Nguồn là Stored Procedure hoặc SQL tùy chỉnh.</summary>
    public bool IsQuerySource =>
        string.Equals(SourceType, "Sp", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SourceType, "Sql", StringComparison.OrdinalIgnoreCase);

    /// <summary>Panel lọc trái thực sự hiển thị: bật + nguồn query + có ≥1 control.</summary>
    public bool HasFilterPanel => FilterPanelEnabled && IsQuerySource && Filters.Count > 0;

    /// <summary>
    /// Map từ entity <see cref="ViewMetadata"/> sang response — sao chép nguyên trường hiển thị,
    /// LOẠI <c>Lookup_Sql</c> khỏi từng filter. Sự kiện theo sau: kết quả được serialize trả client.
    /// </summary>
    /// <param name="view">Entity metadata lấy từ cache (giữ nguyên, không chỉnh sửa).</param>
    public static ViewInfoResponse FromEntity(ViewMetadata view) => new()
    {
        ViewId = view.ViewId,
        ViewCode = view.ViewCode,
        ViewType = view.ViewType,
        TableId = view.TableId,
        TableCode = view.TableCode,
        SourceType = view.SourceType,
        SourceObject = view.SourceObject,
        TitleKey = view.TitleKey,
        Title = view.Title,
        EditFormId = view.EditFormId,
        EditFormCode = view.EditFormCode,
        PageSize = view.PageSize,
        AllowPaging = view.AllowPaging,
        VirtualScroll = view.VirtualScroll,
        ShowFilterRow = view.ShowFilterRow,
        ShowGroupPanel = view.ShowGroupPanel,
        ShowSearchBox = view.ShowSearchBox,
        ShowColumnChooser = view.ShowColumnChooser,
        SelectionMode = view.SelectionMode,
        AllowAdd = view.AllowAdd,
        AllowEdit = view.AllowEdit,
        AllowDelete = view.AllowDelete,
        AllowExport = view.AllowExport,
        ExportFormats = view.ExportFormats,
        ExportFileNameKey = view.ExportFileNameKey,
        ExportFileName = view.ExportFileName,
        AllowPrint = view.AllowPrint,
        KeyField = view.KeyField,
        ParentField = view.ParentField,
        ExpandLevel = view.ExpandLevel,
        AllowReorder = view.AllowReorder,
        FilterPanelEnabled = view.FilterPanelEnabled,
        FilterPanelPosition = view.FilterPanelPosition,
        FilterCollapsible = view.FilterCollapsible,
        AutoSearchOnLoad = view.AutoSearchOnLoad,
        SearchLabelKey = view.SearchLabelKey,
        SearchLabel = view.SearchLabel,
        ResetLabelKey = view.ResetLabelKey,
        ResetLabel = view.ResetLabel,
        Version = view.Version,
        IsActive = view.IsActive,
        Description = view.Description,
        Columns = view.Columns,
        Actions = view.Actions,
        Filters = [.. view.Filters.Select(ViewFilterInfo.FromEntity)],
    };
}

/// <summary>
/// Control lọc trả cho client — mirror <see cref="ViewFilter"/> nhưng KHÔNG có <c>Lookup_Sql</c>.
/// Client không cần SQL (options nạp qua endpoint /filter-options); server tự bind whitelist tham số.
/// </summary>
public sealed class ViewFilterInfo
{
    public int FilterId { get; init; }
    public string FilterCode { get; init; } = string.Empty;
    public string ControlType { get; init; } = "Text";
    public string? LabelKey { get; init; }
    public string? Label { get; init; }
    public string? PlaceholderKey { get; init; }
    public string? Placeholder { get; init; }
    public string? TooltipKey { get; init; }
    public string? Tooltip { get; init; }
    public string ParamName { get; init; } = string.Empty;
    public string ParamType { get; init; } = "string";
    public string Operator { get; init; } = "=";
    public string? DefaultValue { get; init; }
    public bool IsRequired { get; init; }
    public bool IsVisible { get; init; } = true;
    public int OrderNo { get; init; }
    public byte ColSpan { get; init; } = 1;
    public string? LookupSource { get; init; }
    public string? LookupCode { get; init; }
    // CHÚ Ý: KHÔNG có LookupSql — đây là điểm khác biệt cốt lõi so với entity (chống lộ SQL).
    public string? PropsJson { get; init; }
    public string? DependsOn { get; init; }
    public string? DefaultToField { get; init; }
    public bool DefaultLock { get; init; }

    /// <summary>Các <c>Filter_Code</c> cha tách từ <see cref="DependsOn"/> (CSV) — rỗng nếu độc lập.</summary>
    public IReadOnlyList<string> ParentFilterCodes =>
        string.IsNullOrWhiteSpace(DependsOn)
            ? []
            : DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Map từ entity <see cref="ViewFilter"/> — bỏ qua <c>Lookup_Sql</c> (không sao chép).</summary>
    public static ViewFilterInfo FromEntity(ViewFilter f) => new()
    {
        FilterId = f.FilterId,
        FilterCode = f.FilterCode,
        ControlType = f.ControlType,
        LabelKey = f.LabelKey,
        Label = f.Label,
        PlaceholderKey = f.PlaceholderKey,
        Placeholder = f.Placeholder,
        TooltipKey = f.TooltipKey,
        Tooltip = f.Tooltip,
        ParamName = f.ParamName,
        ParamType = f.ParamType,
        Operator = f.Operator,
        DefaultValue = f.DefaultValue,
        IsRequired = f.IsRequired,
        IsVisible = f.IsVisible,
        OrderNo = f.OrderNo,
        ColSpan = f.ColSpan,
        LookupSource = f.LookupSource,
        LookupCode = f.LookupCode,
        PropsJson = f.PropsJson,
        DependsOn = f.DependsOn,
        DefaultToField = f.DefaultToField,
        DefaultLock = f.DefaultLock,
    };
}
