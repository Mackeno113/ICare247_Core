// File    : MaRuleEditDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel popup Sửa/Tạo 1 quy tắc sinh mã (tách khỏi MaRuleManagerViewModel — màn
//           danh sách chỉ còn xem, mọi thao tác nhập liệu chuyển vào đây). Nhận MaRuleRecord (hoặc
//           null = tạo mới) qua Prism IDialogAware; lưu xong đóng popup và trả Rule_Id cho lưới
//           danh sách chọn lại dòng vừa lưu.

using System.Collections.ObjectModel;
using System.Text;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Editor 1 quy tắc sinh mã + các đoạn con, hiển thị dạng popup Prism.</summary>
public sealed class MaRuleEditDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly IMaRuleDataService _dataService;
    private readonly IAppConfigService _appConfig;
    private readonly IDialogService _dialogService;
    private readonly IAppLogger? _logger;

    private int? _editRuleId;
    private string _editTableCode = "";
    private string _editColumnCode = "Ma";
    private int _editStep = 1;
    private bool _editAllowManual;
    private bool _editIsActive = true;
    private string _editDescription = "";
    private bool _editIsSystem;
    private bool _editIsCustomized;
    private MaRuleSegmentRowVm? _selectedSegment;
    private bool _isBusy;
    private string _statusMessage = "";
    private bool _isStatusError;
    private string _preview = "";
    private string _scopePrefix = "";
    private string _overlapWarning = "";
    private string _title = MaRuleUiText.CreateTitle;

    public MaRuleEditDialogViewModel(
        IMaRuleDataService dataService,
        IAppConfigService appConfig,
        IDialogService dialogService,
        IAppLogger? logger = null)
    {
        _dataService = dataService;
        _appConfig = appConfig;
        _dialogService = dialogService;
        _logger = logger;

        SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
        AddSegmentCommand = new DelegateCommand(AddSegment, () => !IsBusy);
        RemoveSegmentCommand = new DelegateCommand(RemoveSegment, () => _selectedSegment is not null);
        MoveUpCommand = new DelegateCommand(() => MoveSegment(-1), () => CanMove(-1));
        MoveDownCommand = new DelegateCommand(() => MoveSegment(+1), () => CanMove(+1));

        Segments.CollectionChanged += (_, _) => RecomputePreview();
    }

    public ObservableCollection<MaRuleSegmentRowVm> Segments { get; } = [];
    public ObservableCollection<TableLookupRecord> Tables { get; } = [];
    public ObservableCollection<string> TargetColumns { get; } = [];

    public IReadOnlyList<string> SegmentTypeOptions { get; } = ["LITERAL", "DATE", "FIELD", "LOOKUP", "SEQ"];
    public IReadOnlyList<string> DateFormatOptions { get; } = ["yyyy", "yy", "yyyyMM", "yyMM", "MM", "dd"];
    public IReadOnlyList<string> PadSideOptions { get; } = ["L", "R"];
    public IReadOnlyList<string> TransformOptions { get; } = ["NONE", "UPPER", "LOWER"];

    public int? EditRuleId
    {
        get => _editRuleId;
        private set
        {
            if (!SetProperty(ref _editRuleId, value)) return;
            RaisePropertyChanged(nameof(IsNew));
            Title = IsNew ? MaRuleUiText.CreateTitle : MaRuleUiText.EditTitle;
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public string EditTableCode
    {
        get => _editTableCode;
        set
        {
            if (!SetProperty(ref _editTableCode, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
            _ = OnTargetTableChangedAsync();
        }
    }

    public string EditColumnCode
    {
        get => _editColumnCode;
        set
        {
            if (!SetProperty(ref _editColumnCode, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
            _ = RefreshOverlapWarningAsync();
        }
    }

    public int EditStep { get => _editStep; set => SetProperty(ref _editStep, value); }
    public bool EditAllowManual { get => _editAllowManual; set => SetProperty(ref _editAllowManual, value); }
    public bool EditIsActive { get => _editIsActive; set => SetProperty(ref _editIsActive, value); }
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }

    public bool EditIsSystem
    {
        get => _editIsSystem;
        private set { if (SetProperty(ref _editIsSystem, value)) DeleteCommand.RaiseCanExecuteChanged(); }
    }
    public bool EditIsCustomized { get => _editIsCustomized; private set => SetProperty(ref _editIsCustomized, value); }

    public MaRuleSegmentRowVm? SelectedSegment
    {
        get => _selectedSegment;
        set
        {
            if (!SetProperty(ref _selectedSegment, value)) return;
            RaisePropertyChanged(nameof(HasSelectedSegment));
            RemoveSegmentCommand.RaiseCanExecuteChanged();
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
        }
    }
    public bool HasSelectedSegment => _selectedSegment is not null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            AddSegmentCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (SetProperty(ref _statusMessage, value)) RaisePropertyChanged(nameof(HasStatus)); }
    }
    public bool IsStatusError { get => _isStatusError; private set => SetProperty(ref _isStatusError, value); }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public string Preview { get => _preview; private set => SetProperty(ref _preview, value); }
    public string ScopePrefix { get => _scopePrefix; private set => SetProperty(ref _scopePrefix, value); }

    public string OverlapWarning
    {
        get => _overlapWarning;
        private set { if (SetProperty(ref _overlapWarning, value)) RaisePropertyChanged(nameof(HasOverlapWarning)); }
    }
    public bool HasOverlapWarning => !string.IsNullOrWhiteSpace(OverlapWarning);

    public bool IsNew => !EditRuleId.HasValue;

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand AddSegmentCommand { get; }
    public DelegateCommand RemoveSegmentCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }

    // ── IDialogAware ─────────────────────────────────────────

    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public DialogCloseListener RequestClose { get; set; }
    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
        => _ = LoadAsync(parameters.GetValue<MaRuleRecord>("record"));

    // ── Nạp dữ liệu ───────────────────────────────────────────

    private async Task LoadAsync(MaRuleRecord? record)
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            if (!_appConfig.IsConfigured)
                await _appConfig.LoadAsync();

            var tables = await _dataService.GetTablesAsync();
            Tables.Clear();
            foreach (var t in tables) Tables.Add(t);

            if (record is not null)
                await ApplyRecordAsync(record);
            else
                ResetEditor();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleEditDialog.Load");
            SetError($"Không thể tải quy tắc sinh mã: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private async Task ApplyRecordAsync(MaRuleRecord record)
    {
        EditRuleId = record.RuleId;
        EditTableCode = record.TableCode;      // kéo theo nạp cột đích + overlap
        EditColumnCode = record.ColumnCode;
        EditStep = record.Step;
        EditAllowManual = record.AllowManual;
        EditIsActive = record.IsActive;
        EditDescription = record.Description ?? "";
        EditIsSystem = record.IsSystem;
        EditIsCustomized = record.IsCustomized;

        Segments.Clear();
        try
        {
            var segs = await _dataService.GetSegmentsAsync(record.RuleId);
            foreach (var s in segs) Segments.Add(WireSegment(new MaRuleSegmentRowVm(s)));
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleEditDialog.LoadSegments");
            SetError($"Không thể tải đoạn: {ex.Message}");
        }
        ReindexSegments();
        RecomputePreview();
        ClearStatus();
    }

    private void ResetEditor()
    {
        EditRuleId = null;
        EditTableCode = "";
        EditColumnCode = "Ma";
        EditStep = 1;
        EditAllowManual = false;
        EditIsActive = true;
        EditDescription = "";
        EditIsSystem = false;
        EditIsCustomized = false;
        Segments.Clear();
        SelectedSegment = null;
        OverlapWarning = "";
        RecomputePreview();
        ClearStatus();
    }

    private async Task OnTargetTableChangedAsync()
    {
        try
        {
            var cols = string.IsNullOrWhiteSpace(EditTableCode)
                ? []
                : await _dataService.GetColumnsAsync(EditTableCode);
            TargetColumns.Clear();
            foreach (var c in cols) TargetColumns.Add(c);
            await RefreshOverlapWarningAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleEditDialog.LoadColumns");
        }
    }

    private async Task RefreshOverlapWarningAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTableCode) || string.IsNullOrWhiteSpace(EditColumnCode))
        {
            OverlapWarning = "";
            return;
        }
        try
        {
            var count = await _dataService.CountOverlappingEventsAsync(EditTableCode, EditColumnCode);
            OverlapWarning = count > 0
                ? $"Cột {EditColumnCode} đang là đích của {count} sự kiện SET_VALUE/CLEAR_VALUE. "
                  + "Quy tắc sinh mã chỉ chạy khi ô để trống (giá trị do sự kiện đặt được ưu tiên)."
                : "";
        }
        catch { OverlapWarning = ""; }
    }

    // ── Đoạn ────────────────────────────────────────────────────────────────

    private void AddSegment()
    {
        var row = WireSegment(new MaRuleSegmentRowVm { SegmentType = "LITERAL", TextValue = "" });
        Segments.Add(row);
        ReindexSegments();
        SelectedSegment = row;
        RecomputePreview();
    }

    private void RemoveSegment()
    {
        if (_selectedSegment is null) return;
        Segments.Remove(_selectedSegment);
        SelectedSegment = null;
        ReindexSegments();
        RecomputePreview();
    }

    private bool CanMove(int delta)
    {
        if (_selectedSegment is null) return false;
        var i = Segments.IndexOf(_selectedSegment);
        var j = i + delta;
        return i >= 0 && j >= 0 && j < Segments.Count;
    }

    private void MoveSegment(int delta)
    {
        if (_selectedSegment is null) return;
        var i = Segments.IndexOf(_selectedSegment);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= Segments.Count) return;
        Segments.Move(i, j);
        ReindexSegments();
        MoveUpCommand.RaiseCanExecuteChanged();
        MoveDownCommand.RaiseCanExecuteChanged();
        RecomputePreview();
    }

    private MaRuleSegmentRowVm WireSegment(MaRuleSegmentRowVm row)
    {
        row.Changed += RecomputePreview;
        return row;
    }

    private void ReindexSegments()
    {
        for (var i = 0; i < Segments.Count; i++) Segments[i].OrderNo = i + 1;
    }

    /// <summary>Ghép preview mã mẫu + tiền tố phạm vi (phần đứng trước đoạn SEQ đầu tiên).</summary>
    private void RecomputePreview()
    {
        var full = new StringBuilder();
        var prefix = new StringBuilder();
        var seqSeen = false;
        foreach (var s in Segments)
        {
            full.Append(s.Sample);
            if (!seqSeen)
            {
                if (s.IsSeq) seqSeen = true;
                else prefix.Append(s.Sample);
            }
        }
        Preview = full.ToString();
        ScopePrefix = prefix.ToString();
        SaveCommand.RaiseCanExecuteChanged();
    }

    // ── Lưu / Xóa ─────────────────────────────────────────────────────────────

    private bool CanSave()
        => !IsBusy
           && !string.IsNullOrWhiteSpace(EditTableCode)
           && !string.IsNullOrWhiteSpace(EditColumnCode)
           && Segments.Count > 0;

    private bool CanDelete() => !IsBusy && EditRuleId.HasValue && !EditIsSystem;

    private async Task SaveAsync()
    {
        if (!CanSave()) return;
        IsBusy = true;
        ClearStatus();
        try
        {
            var request = new MaRuleUpsertRequest
            {
                RuleId = EditRuleId,
                TableCode = EditTableCode,
                ColumnCode = EditColumnCode,
                Step = EditStep,
                AllowManual = EditAllowManual,
                IsActive = EditIsActive,
                Description = EditDescription,
                Segments = Segments.Select(x => x.ToRecord()).ToList(),
            };
            var id = await _dataService.SaveRuleAsync(request);
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters.Add("savedId", id);
            RequestClose.Invoke(result);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleEditDialog.Save");
            SetError($"Không thể lưu quy tắc: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete() || !EditRuleId.HasValue) return;
        try
        {
            var parameters = new DialogParameters
            {
                { "title", MaRuleUiText.ConfirmDeleteTitle },
                { "message", $"Xóa vĩnh viễn quy tắc {EditTableCode}.{EditColumnCode}? Không thể hoàn tác." },
                { "confirmText", MaRuleUiText.ConfirmDeleteButton },
            };
            var completion = new TaskCompletionSource<ButtonResult>();
            _dialogService.ShowDialog(ViewNames.ConfirmDialog, parameters,
                result => completion.TrySetResult(result.Result));
            if (await completion.Task != ButtonResult.OK) return;

            IsBusy = true;
            await _dataService.DeleteRuleAsync(EditRuleId.Value);
            var closeResult = new DialogResult(ButtonResult.OK);
            closeResult.Parameters.Add("savedId", (int?)null!);
            RequestClose.Invoke(closeResult);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "MaRuleEditDialog.Delete");
            SetError($"Không thể xóa quy tắc: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private void SetError(string message) { StatusMessage = message; IsStatusError = true; }
    private void ClearStatus() { StatusMessage = ""; IsStatusError = false; }
}
