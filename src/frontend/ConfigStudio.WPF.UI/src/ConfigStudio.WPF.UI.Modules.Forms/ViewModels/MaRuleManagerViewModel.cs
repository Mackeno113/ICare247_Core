// File    : MaRuleManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel màn "Quy tắc sinh mã" — CHỈ xem lưới danh sách + lọc/chọn dòng. Thêm/Sửa
//           mở popup MaRuleEditDialog (MaRuleEditDialogViewModel); Xóa hỏi xác nhận qua
//           ConfirmDialog rồi xóa thẳng từ dòng đang chọn (không cần nạp editor).

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

/// <summary>Điều phối lưới danh sách quy tắc sinh mã; nhập liệu nằm ở popup riêng.</summary>
public sealed class MaRuleManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly IMaRuleDataService _dataService;
    private readonly IAppConfigService _appConfig;
    private readonly IDialogService _dialogService;
    private readonly IAppLogger? _logger;

    private MaRuleRecord? _selectedRule;
    private bool _isBusy;
    private string _statusMessage = "";
    private bool _isStatusError;

    public MaRuleManagerViewModel(
        IMaRuleDataService dataService,
        IAppConfigService appConfig,
        IDialogService dialogService,
        IAppLogger? logger = null)
    {
        _dataService = dataService;
        _appConfig = appConfig;
        _dialogService = dialogService;
        _logger = logger;

        RefreshCommand = new DelegateCommand(async () => await LoadAsync(), () => !IsBusy);
        NewCommand = new DelegateCommand(() => OpenEditor(null), () => !IsBusy);
        EditCommand = new DelegateCommand(() => OpenEditor(SelectedRule), CanEditOrDelete);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanEditOrDelete);
    }

    public ObservableCollection<MaRuleRecord> Rules { get; } = [];

    public MaRuleRecord? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (!SetProperty(ref _selectedRule, value)) return;
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
        private set { if (SetProperty(ref _statusMessage, value)) RaisePropertyChanged(nameof(HasStatus)); }
    }
    public bool IsStatusError { get => _isStatusError; private set => SetProperty(ref _isStatusError, value); }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand NewCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand DeleteCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private bool CanEditOrDelete() => !IsBusy && SelectedRule is not null;

    private async Task LoadAsync(int? selectId = null)
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            if (!_appConfig.IsConfigured)
                await _appConfig.LoadAsync();

            var rules = await _dataService.GetRulesAsync();
            await ApplyPreviewCodesAsync(rules);
            Rules.Clear();
            foreach (var r in rules) Rules.Add(r);

            var targetId = selectId ?? SelectedRule?.RuleId;
            SelectedRule = targetId.HasValue
                ? Rules.FirstOrDefault(x => x.RuleId == targetId.Value)
                : null;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleManager.Load");
            SetError($"Không thể tải quy tắc sinh mã: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Gán <see cref="MaRuleRecord.PreviewCode"/> cho từng quy tắc — 1 query lấy hết đoạn
    /// (tránh N+1 GetSegmentsAsync theo từng dòng), lỗi thì bỏ qua (không chặn hiển thị lưới).</summary>
    private async Task ApplyPreviewCodesAsync(IReadOnlyList<MaRuleRecord> rules)
    {
        if (rules.Count == 0) return;
        try
        {
            var segments = await _dataService.GetAllSegmentsAsync();
            var byRule = segments.GroupBy(s => s.RuleId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var r in rules)
                if (byRule.TryGetValue(r.RuleId, out var segs))
                    r.PreviewCode = MaRulePreviewCalculator.BuildPreview(segs);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleManager.LoadPreviewCodes");
        }
    }

    /// <summary>Mở popup Thêm (record=null) hoặc Sửa (record=dòng đang chọn). Dùng chung cho nút
    /// "Sửa" và double-click dòng.</summary>
    public void OpenEditor(MaRuleRecord? record)
    {
        var parameters = new DialogParameters { { "record", record! } };
        _dialogService.ShowDialog(ViewNames.MaRuleEditDialog, parameters, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            var savedId = result.Parameters.GetValue<int?>("savedId");
            _ = LoadAsync(savedId);
        });
    }

    private async Task DeleteAsync()
    {
        var rule = SelectedRule;
        if (rule is null || rule.IsSystem) return;
        try
        {
            var parameters = new DialogParameters
            {
                { "title", MaRuleUiText.ConfirmDeleteTitle },
                { "message", $"Xóa vĩnh viễn quy tắc {rule.TableCode}.{rule.ColumnCode}? Không thể hoàn tác." },
                { "confirmText", MaRuleUiText.ConfirmDeleteButton },
            };
            var completion = new TaskCompletionSource<ButtonResult>();
            _dialogService.ShowDialog(ViewNames.ConfirmDialog, parameters,
                result => completion.TrySetResult(result.Result));
            if (await completion.Task != ButtonResult.OK) return;

            IsBusy = true;
            ClearStatus();
            await _dataService.DeleteRuleAsync(rule.RuleId);
            SelectedRule = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleManager.Delete");
            SetError($"Không thể xóa quy tắc: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private void SetError(string message) { StatusMessage = message; IsStatusError = true; }
    private void ClearStatus() { StatusMessage = ""; IsStatusError = false; }
}
