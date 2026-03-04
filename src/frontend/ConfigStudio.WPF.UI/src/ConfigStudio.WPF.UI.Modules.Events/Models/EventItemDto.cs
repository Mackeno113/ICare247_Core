// File    : EventItemDto.cs
// Module  : Events
// Layer   : Presentation
// Purpose : DTO hiển thị 1 event trong DataGrid.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Events.Models;

/// <summary>
/// DTO đại diện cho 1 event gắn với field.
/// </summary>
public class EventItemDto : BindableBase
{
    public int EventId { get; set; }
    public int FieldId { get; set; }
    public string FieldCode { get; set; } = "";

    private string _triggerCode = "";
    /// <summary>Loại trigger: OnChange, OnBlur, OnLoad, OnSubmit, ...</summary>
    public string TriggerCode { get => _triggerCode; set => SetProperty(ref _triggerCode, value); }

    private string _conditionPreview = "";
    /// <summary>Mô tả condition expression (human-readable).</summary>
    public string ConditionPreview { get => _conditionPreview; set => SetProperty(ref _conditionPreview, value); }

    private string _conditionJson = "";
    public string ConditionJson { get => _conditionJson; set => SetProperty(ref _conditionJson, value); }

    private int _actionsCount;
    /// <summary>Số lượng actions gắn với event này.</summary>
    public int ActionsCount { get => _actionsCount; set => SetProperty(ref _actionsCount, value); }

    private string _actionsPreview = "";
    /// <summary>Mô tả ngắn danh sách actions.</summary>
    public string ActionsPreview { get => _actionsPreview; set => SetProperty(ref _actionsPreview, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }
}
