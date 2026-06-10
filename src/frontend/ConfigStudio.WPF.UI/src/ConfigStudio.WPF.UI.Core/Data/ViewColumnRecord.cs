// File    : ViewColumnRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : Model một dòng cấu hình cột Ui_View_Column — editable trong lưới con.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Một cột của View (<c>dbo.Ui_View_Column</c>). Kế thừa <see cref="BindableBase"/> để
/// chỉnh sửa inline trong GridControl phản ánh ngay ra UI.
/// </summary>
public sealed class ViewColumnRecord : BindableBase
{
    public int ViewColumnId { get; set; }
    public int? ColumnId { get; set; }

    private string _fieldName = "";
    /// <summary>FieldName gắn vào control (bắt buộc).</summary>
    public string FieldName { get => _fieldName; set => SetProperty(ref _fieldName, value); }

    private string? _captionKey;
    /// <summary>Key i18n tiêu đề cột (null = fallback Label_Key field → Field_Name).</summary>
    public string? CaptionKey { get => _captionKey; set => SetProperty(ref _captionKey, value); }

    private string _columnKind = "Data";
    /// <summary>Data | Selection | Command | TreeSpin.</summary>
    public string ColumnKind { get => _columnKind; set => SetProperty(ref _columnKind, value); }

    private string? _width;
    public string? Width { get => _width; set => SetProperty(ref _width, value); }

    private int? _minWidth;
    public int? MinWidth { get => _minWidth; set => SetProperty(ref _minWidth, value); }

    private string? _textAlign;
    /// <summary>left | center | right.</summary>
    public string? TextAlign { get => _textAlign; set => SetProperty(ref _textAlign, value); }

    private string? _displayFormat;
    public string? DisplayFormat { get => _displayFormat; set => SetProperty(ref _displayFormat, value); }

    private string _renderMode = "Text";
    /// <summary>Text | Html | Image | Link | Badge | Boolean | Template.</summary>
    public string RenderMode { get => _renderMode; set => SetProperty(ref _renderMode, value); }

    private string? _cellTemplateKey;
    public string? CellTemplateKey { get => _cellTemplateKey; set => SetProperty(ref _cellTemplateKey, value); }

    private bool _isVisible = true;
    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private string? _fixedPosition;
    /// <summary>none | left | right (frozen).</summary>
    public string? FixedPosition { get => _fixedPosition; set => SetProperty(ref _fixedPosition, value); }

    private bool _allowSort = true;
    public bool AllowSort { get => _allowSort; set => SetProperty(ref _allowSort, value); }

    private string? _sortOrder;
    /// <summary>asc | desc.</summary>
    public string? SortOrder { get => _sortOrder; set => SetProperty(ref _sortOrder, value); }

    private int? _sortIndex;
    public int? SortIndex { get => _sortIndex; set => SetProperty(ref _sortIndex, value); }

    private bool _allowFilter = true;
    public bool AllowFilter { get => _allowFilter; set => SetProperty(ref _allowFilter, value); }

    private bool _allowGroup;
    public bool AllowGroup { get => _allowGroup; set => SetProperty(ref _allowGroup, value); }

    private int? _groupIndex;
    public int? GroupIndex { get => _groupIndex; set => SetProperty(ref _groupIndex, value); }

    private string? _summaryType;
    /// <summary>count | sum | avg | min | max.</summary>
    public string? SummaryType { get => _summaryType; set => SetProperty(ref _summaryType, value); }

    private bool _allowExport = true;
    public bool AllowExport { get => _allowExport; set => SetProperty(ref _allowExport, value); }

    private string? _exportFormat;
    public string? ExportFormat { get => _exportFormat; set => SetProperty(ref _exportFormat, value); }

    private string? _exportCaptionKey;
    public string? ExportCaptionKey { get => _exportCaptionKey; set => SetProperty(ref _exportCaptionKey, value); }

    private string? _styleRuleJson;
    public string? StyleRuleJson { get => _styleRuleJson; set => SetProperty(ref _styleRuleJson, value); }

    private string? _propsJson;
    public string? PropsJson { get => _propsJson; set => SetProperty(ref _propsJson, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
}
