// File    : LookupTemplateManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel màn "Mẫu Lookup" — CHỈ xem lưới danh sách + lọc/chọn dòng. Thêm/Sửa mở popup
//           LookupTemplateEditDialog (LookupTemplateEditDialogViewModel); Xóa hỏi xác nhận qua
//           ConfirmDialog (sau khi kiểm tra tham chiếu) rồi xóa thẳng từ dòng đang chọn.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Điều phối lưới danh sách mẫu lookup; nhập liệu nằm ở popup riêng.</summary>
public sealed class LookupTemplateManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly ILookupTemplateDataService _dataService;
    private readonly IAppConfigService _appConfig;
    private readonly IDialogService _dialogService;
    private readonly INavigationHistoryService? _history;
    private readonly IAppLogger? _logger;

    private LookupTemplateRecord? _selectedTemplate;
    private bool _isBusy;
    private string _statusMessage = "";
    private bool _isStatusError;

    public LookupTemplateManagerViewModel(
        ILookupTemplateDataService dataService,
        IAppConfigService appConfig,
        IDialogService dialogService,
        INavigationHistoryService? history = null,
        IAppLogger? logger = null)
    {
        _dataService = dataService;
        _appConfig = appConfig;
        _dialogService = dialogService;
        _history = history;
        _logger = logger;

        RefreshCommand = new DelegateCommand(async () => await LoadAsync(), () => !IsBusy);
        NewCommand = new DelegateCommand(() => OpenEditor(null), () => !IsBusy);
        EditCommand = new DelegateCommand(() => OpenEditor(SelectedTemplate), CanEditOrDelete);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanEditOrDelete);
    }

    public ObservableCollection<LookupTemplateRecord> Templates { get; } = [];

    public LookupTemplateRecord? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (!SetProperty(ref _selectedTemplate, value)) return;
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RefreshCommand.RaiseCanExecuteChanged();
            NewCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (!SetProperty(ref _statusMessage, value)) return;
            RaisePropertyChanged(nameof(HasStatus));
        }
    }

    public bool IsStatusError { get => _isStatusError; private set => SetProperty(ref _isStatusError, value); }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand NewCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand DeleteCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _history?.RegisterNavigation(
            new NavigationCrumb
            {
                ViewName = ViewNames.LookupTemplateManager,
                Title = LookupTemplateUiText.ScreenTitle,
                Icon = "⌕",
            },
            isHierarchical: false);
        _ = LoadAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private bool CanEditOrDelete() => !IsBusy && SelectedTemplate is not null;

    private async Task LoadAsync(int? selectId = null)
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            if (!_appConfig.IsConfigured)
                await _appConfig.LoadAsync();
            var records = await _dataService.GetTemplatesAsync();
            Templates.Clear();
            foreach (var record in records) Templates.Add(record);

            var targetId = selectId ?? SelectedTemplate?.TemplateId;
            SelectedTemplate = targetId.HasValue
                ? Templates.FirstOrDefault(x => x.TemplateId == targetId.Value)
                : null;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateManager.Load");
            SetError($"Không thể tải mẫu lookup: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Mở popup Thêm (record=null) hoặc Sửa (record=dòng đang chọn). Dùng chung cho nút
    /// "Sửa" và double-click dòng.</summary>
    public void OpenEditor(LookupTemplateRecord? record)
    {
        var parameters = new DialogParameters { { "record", record! } };
        _dialogService.ShowDialog(ViewNames.LookupTemplateEditDialog, parameters, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            var savedId = result.Parameters.GetValue<int?>("savedId");
            _ = LoadAsync(savedId);
        });
    }

    private async Task DeleteAsync()
    {
        var template = SelectedTemplate;
        if (template is null || template.IsSystem) return;

        try
        {
            var referenceCount = await _dataService.CountReferencesAsync(template.TemplateCode);
            if (referenceCount > 0)
            {
                SetError(
                    $"Không thể xóa mẫu '{template.TemplateCode}' vì đang được {referenceCount} field tham chiếu.");
                return;
            }

            var parameters = new DialogParameters
            {
                { "title", LookupTemplateUiText.ConfirmDeleteTitle },
                { "message", $"Xóa vĩnh viễn mẫu '{template.TemplateCode}'? Thao tác này không thể hoàn tác." },
                { "confirmText", LookupTemplateUiText.ConfirmDeleteButton },
            };
            var completion = new TaskCompletionSource<ButtonResult>();
            _dialogService.ShowDialog(
                ViewNames.ConfirmDialog,
                parameters,
                result => completion.TrySetResult(result.Result));
            if (await completion.Task != ButtonResult.OK) return;

            IsBusy = true;
            ClearStatus();
            await _dataService.DeleteTemplateAsync(template.TemplateId);
            SelectedTemplate = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateManager.Delete");
            SetError($"Không thể xóa mẫu lookup: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        IsStatusError = true;
    }

    private void ClearStatus()
    {
        StatusMessage = "";
        IsStatusError = false;
    }
}
