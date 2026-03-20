// File    : EventEditorViewModel.cs
// Module  : Events
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Event Editor (Screen 06) — quản lý events và actions của field.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Events.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Events.ViewModels;

/// <summary>
/// ViewModel cho màn hình Event Editor (Screen 06).
/// Hiển thị DataGrid events, danh sách actions, mở Expression Builder cho condition.
/// Khi DB đã cấu hình → load/save dữ liệu thật qua IEventDataService.
/// Khi chưa cấu hình → fallback mock data.
/// </summary>
public sealed class EventEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IDialogService _dialogService;
    private readonly IEventDataService? _eventService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Navigation params ─────────────────────────────────────
    private int _fieldId;
    public int FieldId { get => _fieldId; set => SetProperty(ref _fieldId, value); }

    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private string _fieldCode = "";
    public string FieldCode { get => _fieldCode; set => SetProperty(ref _fieldCode, value); }

    // ── Events list ──────────────────────────────────────────
    public ObservableCollection<EventItemDto> Events { get; } = [];

    private EventItemDto? _selectedEvent;
    public EventItemDto? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetProperty(ref _selectedEvent, value))
            {
                _ = LoadActionsForEventAsync();
                RaisePropertyChanged(nameof(IsEventSelected));
                DeleteEventCommand.RaiseCanExecuteChanged();
                EditConditionCommand.RaiseCanExecuteChanged();
                AddActionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsEventSelected => SelectedEvent is not null;

    // ── Actions list (cho event đang chọn) ───────────────────
    public ObservableCollection<ActionItemDto> Actions { get; } = [];

    private ActionItemDto? _selectedAction;
    public ActionItemDto? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (SetProperty(ref _selectedAction, value))
            {
                DeleteActionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Trigger types ─────────────────────────────────────────
    public List<string> TriggerOptions { get; } = ["OnChange", "OnBlur", "OnFocus", "OnLoad", "OnSubmit"];

    public List<string> ActionTypeOptions { get; } =
        ["SetValue", "SetVisible", "SetReadOnly", "SetRequired", "Recalculate", "ShowMessage", "Navigate"];

    // ── State ─────────────────────────────────────────────────
    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddEventCommand { get; }
    public DelegateCommand DeleteEventCommand { get; }
    public DelegateCommand EditConditionCommand { get; }
    public DelegateCommand AddActionCommand { get; }
    public DelegateCommand DeleteActionCommand { get; }
    public DelegateCommand SaveAllCommand { get; }
    public DelegateCommand BackCommand { get; }

    public EventEditorViewModel(
        IRegionManager regionManager,
        IDialogService dialogService,
        IEventDataService? eventService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager = regionManager;
        _dialogService = dialogService;
        _eventService = eventService;
        _appConfig = appConfig;

        AddEventCommand = new DelegateCommand(ExecuteAddEvent);
        DeleteEventCommand = new DelegateCommand(async () => await ExecuteDeleteEventAsync(), () => IsEventSelected);
        EditConditionCommand = new DelegateCommand(ExecuteEditCondition, () => IsEventSelected);
        AddActionCommand = new DelegateCommand(ExecuteAddAction, () => IsEventSelected);
        DeleteActionCommand = new DelegateCommand(ExecuteDeleteAction, () => SelectedAction is not null);
        SaveAllCommand = new DelegateCommand(async () => await ExecuteSaveAllAsync(), () => IsDirty)
            .ObservesProperty(() => IsDirty);
        BackCommand = new DelegateCommand(ExecuteBack);
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        FieldId = navigationContext.Parameters.GetValue<int>("fieldId");
        FormId = navigationContext.Parameters.GetValue<int>("formId");

        if (FieldId == 0) FieldId = 5;
        if (FormId == 0) FormId = 1;

        await LoadDataAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data (DB hoặc mock) ─────────────────────────────

    private async Task LoadDataAsync()
    {
        if (_eventService is not null && _appConfig is { IsConfigured: true })
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            LoadMockData();
        }
    }

    /// <summary>
    /// Load events từ DB qua IEventDataService.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct = _cts.Token;

            // Load trigger types từ DB (nếu có) → cập nhật TriggerOptions
            var triggerTypes = await _eventService!.GetTriggerTypesAsync(ct);
            if (triggerTypes.Count > 0)
            {
                TriggerOptions.Clear();
                foreach (var t in triggerTypes)
                    TriggerOptions.Add(t);
            }

            // Load action types từ DB (nếu có) → cập nhật ActionTypeOptions
            var actionTypes = await _eventService.GetActionTypesAsync(ct);
            if (actionTypes.Count > 0)
            {
                ActionTypeOptions.Clear();
                foreach (var a in actionTypes)
                    ActionTypeOptions.Add(a.ActionCode);
            }

            // Load events cho field
            var events = await _eventService.GetEventsByFieldAsync(FieldId, ct);
            Events.Clear();
            foreach (var e in events)
            {
                Events.Add(new EventItemDto
                {
                    EventId = e.EventId,
                    FieldId = FieldId,
                    FieldCode = FieldCode,
                    TriggerCode = e.TriggerCode,
                    OrderNo = e.OrderNo,
                    ConditionPreview = e.ConditionExpr ?? "(luôn thực thi)",
                    ConditionJson = e.ConditionExpr ?? "{}",
                    ActionsCount = e.ActionsCount,
                    IsActive = e.IsActive
                });
            }

            IsDirty = false;
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            LoadMockData();
        }
    }

    // ── Load mock data ───────────────────────────────────────

    private void LoadMockData()
    {
        FieldCode = "SoLuong";

        Events.Clear();
        Events.Add(new EventItemDto
        {
            EventId = 1, FieldId = FieldId, FieldCode = FieldCode,
            TriggerCode = "OnChange", OrderNo = 1,
            ConditionPreview = "TrangThai == \"TuChoi\"",
            ConditionJson = "{\"type\":\"Binary\",\"op\":\"==\"}",
            ActionsCount = 3,
            ActionsPreview = "SetVisible, SetRequired, Recalculate",
            IsActive = true
        });
        Events.Add(new EventItemDto
        {
            EventId = 2, FieldId = FieldId, FieldCode = FieldCode,
            TriggerCode = "OnBlur", OrderNo = 2,
            ConditionPreview = "SoLuong > 0",
            ConditionJson = "{\"type\":\"Binary\",\"op\":\">\"}",
            ActionsCount = 1,
            ActionsPreview = "Recalculate",
            IsActive = true
        });
        Events.Add(new EventItemDto
        {
            EventId = 3, FieldId = FieldId, FieldCode = FieldCode,
            TriggerCode = "OnLoad", OrderNo = 3,
            ConditionPreview = "(luôn thực thi)",
            ConditionJson = "{}",
            ActionsCount = 2,
            ActionsPreview = "SetValue, SetReadOnly",
            IsActive = true
        });

        IsDirty = false;
    }

    /// <summary>
    /// Load danh sách actions khi chọn event. Dùng DB nếu có, fallback mock.
    /// </summary>
    private async Task LoadActionsForEventAsync()
    {
        Actions.Clear();
        if (SelectedEvent is null) return;

        if (_eventService is not null && _appConfig is { IsConfigured: true })
        {
            try
            {
                var actions = await _eventService.GetActionsByEventAsync(SelectedEvent.EventId, _cts.Token);
                foreach (var a in actions)
                {
                    Actions.Add(new ActionItemDto
                    {
                        ActionId = a.ActionId,
                        EventId = a.EventId,
                        ActionType = a.ActionCode,
                        ParamJson = a.ParamJson ?? "{}",
                        OrderNo = a.OrderNo
                    });
                }
            }
            catch (OperationCanceledException) { /* Navigation away */ }
            catch
            {
                LoadMockActionsForEvent();
            }
        }
        else
        {
            LoadMockActionsForEvent();
        }
    }

    /// <summary>
    /// Mock actions theo EventId.
    /// </summary>
    private void LoadMockActionsForEvent()
    {
        if (SelectedEvent is null) return;

        switch (SelectedEvent.EventId)
        {
            case 1:
                Actions.Add(new ActionItemDto { ActionId = 1, EventId = 1, ActionType = "SetVisible", TargetField = "LyDoTuChoi", ParamJson = "{\"visible\": true}", OrderNo = 1 });
                Actions.Add(new ActionItemDto { ActionId = 2, EventId = 1, ActionType = "SetRequired", TargetField = "LyDoTuChoi", ParamJson = "{\"required\": true}", OrderNo = 2 });
                Actions.Add(new ActionItemDto { ActionId = 3, EventId = 1, ActionType = "Recalculate", TargetField = "ThanhTien", ParamJson = "{}", OrderNo = 3 });
                break;
            case 2:
                Actions.Add(new ActionItemDto { ActionId = 4, EventId = 2, ActionType = "Recalculate", TargetField = "ThanhTien", ParamJson = "{\"formula\": \"SoLuong * DonGia\"}", OrderNo = 1 });
                break;
            case 3:
                Actions.Add(new ActionItemDto { ActionId = 5, EventId = 3, ActionType = "SetValue", TargetField = "NgayDatHang", ParamJson = "{\"value\": \"today()\"}", OrderNo = 1 });
                Actions.Add(new ActionItemDto { ActionId = 6, EventId = 3, ActionType = "SetReadOnly", TargetField = "MaDonHang", ParamJson = "{\"readonly\": true}", OrderNo = 2 });
                break;
        }
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteAddEvent()
    {
        var newId = Events.Count > 0 ? Events.Max(e => e.EventId) + 1 : 1;
        var evt = new EventItemDto
        {
            EventId = newId, FieldId = FieldId, FieldCode = FieldCode,
            TriggerCode = "OnChange", OrderNo = Events.Count + 1,
            ConditionPreview = "(luôn thực thi)", ConditionJson = "{}",
            ActionsCount = 0, ActionsPreview = "", IsActive = true
        };
        Events.Add(evt);
        SelectedEvent = evt;
        IsDirty = true;
    }

    private async Task ExecuteDeleteEventAsync()
    {
        if (SelectedEvent is null) return;

        // Xóa từ DB nếu có
        if (_eventService is not null && _appConfig is { IsConfigured: true } && SelectedEvent.EventId > 0)
        {
            await _eventService.DeleteEventAsync(SelectedEvent.EventId, _cts.Token);
        }

        Events.Remove(SelectedEvent);
        ReindexEvents();
        SelectedEvent = null;
        IsDirty = true;
    }

    private void ExecuteEditCondition()
    {
        if (SelectedEvent is null) return;

        var p = new DialogParameters
        {
            { "expressionJson", SelectedEvent.ConditionJson },
            { "fieldCode", FieldCode }
        };

        _dialogService.ShowDialog(ViewNames.ExpressionBuilderDialog, p, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var newJson = result.Parameters.GetValue<string>("expressionJson");
                if (!string.IsNullOrEmpty(newJson))
                {
                    SelectedEvent.ConditionJson = newJson;
                    SelectedEvent.ConditionPreview = result.Parameters.GetValue<string>("naturalText") ?? newJson;
                    IsDirty = true;
                }
            }
        });
    }

    private void ExecuteAddAction()
    {
        if (SelectedEvent is null) return;
        var newId = Actions.Count > 0 ? Actions.Max(a => a.ActionId) + 1 : 1;
        var action = new ActionItemDto
        {
            ActionId = newId, EventId = SelectedEvent.EventId,
            ActionType = "SetValue", TargetField = "",
            ParamJson = "{}", OrderNo = Actions.Count + 1
        };
        Actions.Add(action);
        SelectedEvent.ActionsCount = Actions.Count;
        IsDirty = true;
    }

    private void ExecuteDeleteAction()
    {
        if (SelectedAction is null || SelectedEvent is null) return;
        Actions.Remove(SelectedAction);
        SelectedEvent.ActionsCount = Actions.Count;
        ReindexActions();
        IsDirty = true;
    }

    /// <summary>
    /// Save tất cả events + actions qua IEventDataService (nếu DB configured).
    /// </summary>
    private async Task ExecuteSaveAllAsync()
    {
        if (_eventService is not null && _appConfig is { IsConfigured: true })
        {
            var ct = _cts.Token;
            foreach (var evt in Events)
            {
                var eventRecord = new EventItemRecord
                {
                    EventId = evt.EventId,
                    FormId = FormId,
                    FieldId = FieldId,
                    TriggerCode = evt.TriggerCode,
                    ConditionExpr = evt.ConditionJson,
                    OrderNo = evt.OrderNo,
                    IsActive = evt.IsActive
                };
                var savedId = await _eventService.SaveEventAsync(eventRecord, ct);

                // Save actions nếu event đang được chọn
                if (evt == SelectedEvent)
                {
                    foreach (var action in Actions)
                    {
                        var actionRecord = new ActionItemRecord
                        {
                            ActionId = action.ActionId,
                            EventId = savedId,
                            ActionCode = action.ActionType,
                            ParamJson = action.ParamJson,
                            OrderNo = action.OrderNo
                        };
                        await _eventService.SaveActionAsync(actionRecord, ct);
                    }
                }
            }
        }
        IsDirty = false;
    }

    private void ExecuteBack()
    {
        var p = new NavigationParameters
        {
            { "fieldId", FieldId },
            { "formId", FormId },
            { "mode", "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    // ── Helpers ──────────────────────────────────────────────

    private void ReindexEvents()
    {
        for (int i = 0; i < Events.Count; i++)
            Events[i].OrderNo = i + 1;
    }

    private void ReindexActions()
    {
        for (int i = 0; i < Actions.Count; i++)
            Actions[i].OrderNo = i + 1;
    }
}
