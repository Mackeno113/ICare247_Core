// File    : ActionItemDto.cs
// Module  : Events
// Layer   : Presentation
// Purpose : DTO hiển thị 1 action trong danh sách actions của event.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Events.Models;

/// <summary>
/// DTO đại diện cho 1 action thuộc event.
/// </summary>
public class ActionItemDto : BindableBase
{
    public int ActionId { get; set; }
    public int EventId { get; set; }

    private string _actionType = "";
    /// <summary>Loại action: SetValue, SetVisible, SetReadOnly, Recalculate, ShowMessage, ...</summary>
    public string ActionType { get => _actionType; set => SetProperty(ref _actionType, value); }

    private string _targetField = "";
    /// <summary>Field mục tiêu của action.</summary>
    public string TargetField { get => _targetField; set => SetProperty(ref _targetField, value); }

    private string _paramJson = "";
    /// <summary>JSON chứa tham số của action.</summary>
    public string ParamJson { get => _paramJson; set => SetProperty(ref _paramJson, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    /// <summary>Mô tả ngắn action.</summary>
    public string DisplayText => $"{ActionType} → {TargetField}";
}
