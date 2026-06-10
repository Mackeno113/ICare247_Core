// File    : ViewActionRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : Model một nút toolbar/row (Ui_View_Action) — editable trong lưới con.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Một hành động (nút) của View (<c>dbo.Ui_View_Action</c>). Kế thừa <see cref="BindableBase"/>
/// để chỉnh sửa inline trong GridControl.
/// </summary>
public sealed class ViewActionRecord : BindableBase
{
    public int ActionId { get; set; }

    private string _actionCode = "";
    /// <summary>add | edit | delete | export | print | refresh | column-chooser | custom.</summary>
    public string ActionCode { get => _actionCode; set => SetProperty(ref _actionCode, value); }

    private string _actionType = "BuiltIn";
    /// <summary>BuiltIn | Export | Print | Navigate | Event | Api.</summary>
    public string ActionType { get => _actionType; set => SetProperty(ref _actionType, value); }

    private string _scope = "Toolbar";
    /// <summary>Toolbar | Row | Both.</summary>
    public string Scope { get => _scope; set => SetProperty(ref _scope, value); }

    private string? _labelKey;
    public string? LabelKey { get => _labelKey; set => SetProperty(ref _labelKey, value); }

    private string? _tooltipKey;
    public string? TooltipKey { get => _tooltipKey; set => SetProperty(ref _tooltipKey, value); }

    private string? _confirmKey;
    public string? ConfirmKey { get => _confirmKey; set => SetProperty(ref _confirmKey, value); }

    private string? _icon;
    /// <summary>Unicode/tên icon — KHÔNG dịch i18n.</summary>
    public string? Icon { get => _icon; set => SetProperty(ref _icon, value); }

    private string? _exportFormat;
    /// <summary>xlsx | xls | csv | pdf | docx (khi Action_Type='Export').</summary>
    public string? ExportFormat { get => _exportFormat; set => SetProperty(ref _exportFormat, value); }

    private string? _exportEngine;
    /// <summary>Grid (client) | Server (template).</summary>
    public string? ExportEngine { get => _exportEngine; set => SetProperty(ref _exportEngine, value); }

    private string? _target;
    /// <summary>url | event_code | api endpoint | report template.</summary>
    public string? Target { get => _target; set => SetProperty(ref _target, value); }

    private bool _requireSelection;
    public bool RequireSelection { get => _requireSelection; set => SetProperty(ref _requireSelection, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private string? _propsJson;
    public string? PropsJson { get => _propsJson; set => SetProperty(ref _propsJson, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
}
