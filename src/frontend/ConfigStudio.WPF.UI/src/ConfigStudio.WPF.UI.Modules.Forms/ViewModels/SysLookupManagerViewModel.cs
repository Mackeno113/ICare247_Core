// File    : SysLookupManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Sys_Lookup Manager — quản lý danh mục dùng chung.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Quản lý <c>Sys_Lookup</c>: trái = danh sách Lookup_Code, phải = items của code đang chọn.
/// Hỗ trợ thêm/sửa/xóa item, thêm lookup code mới.
/// </summary>
public sealed class SysLookupManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly ISysLookupDataService? _lookupService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Left panel — Lookup Codes ─────────────────────────────
    public ObservableCollection<string> LookupCodes { get; } = [];

    private string? _selectedCode;
    public string? SelectedCode
    {
        get => _selectedCode;
        set
        {
            if (SetProperty(ref _selectedCode, value))
            {
                _ = LoadItemsAsync(value);
                AddItemCommand.RaiseCanExecuteChanged();
                DeleteCodeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _newCodeText = "";
    public string NewCodeText
    {
        get => _newCodeText;
        set
        {
            if (SetProperty(ref _newCodeText, value))
            {
                NewCodeError = "";
                AddCodeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _newCodeError = "";
    public string NewCodeError
    {
        get => _newCodeError;
        private set
        {
            if (SetProperty(ref _newCodeError, value))
                RaisePropertyChanged(nameof(HasNewCodeError));
        }
    }
    public bool HasNewCodeError => !string.IsNullOrEmpty(_newCodeError);

    // ── Right panel — Items ───────────────────────────────────
    public ObservableCollection<LookupItemEditRecord> Items { get; } = [];

    private LookupItemEditRecord? _selectedItem;
    public LookupItemEditRecord? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                EditItemCommand.RaiseCanExecuteChanged();
                DeleteItemCommand.RaiseCanExecuteChanged();

                // Load vào form edit khi chọn dòng
                if (value is not null)
                    ApplyToEditor(value);
            }
        }
    }

    // ── Editor form (thêm / sửa item) ─────────────────────────
    private int    _editLookupId;
    private string _editItemCode   = "";
    private string _editLabelVi    = "";
    private string _editLabelEn    = "";
    private int    _editSortOrder;
    private bool   _editIsActive   = true;
    private bool   _isEditorVisible;
    private string _editorError    = "";

    public string EditItemCode
    {
        get => _editItemCode;
        set { if (SetProperty(ref _editItemCode, value)) EditorError = ""; }
    }
    public string EditLabelVi
    {
        get => _editLabelVi;
        set => SetProperty(ref _editLabelVi, value);
    }
    public string EditLabelEn
    {
        get => _editLabelEn;
        set => SetProperty(ref _editLabelEn, value);
    }
    public int EditSortOrder
    {
        get => _editSortOrder;
        set => SetProperty(ref _editSortOrder, value);
    }
    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }
    public bool IsEditorVisible
    {
        get => _isEditorVisible;
        set => SetProperty(ref _isEditorVisible, value);
    }
    public string EditorError
    {
        get => _editorError;
        private set
        {
            if (SetProperty(ref _editorError, value))
                RaisePropertyChanged(nameof(HasEditorError));
        }
    }
    public bool HasEditorError => !string.IsNullOrEmpty(_editorError);

    /// <summary>true = đang edit item cũ; false = đang thêm mới.</summary>
    public bool IsEditMode => _editLookupId > 0;

    // ── Status ────────────────────────────────────────────────
    private bool   _isBusy;
    private string _statusMessage = "";
    public bool   IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaisePropertyChanged(nameof(IsNotBusy));
        }
    }
    public bool   IsNotBusy      => !_isBusy;
    public string StatusMessage  { get => _statusMessage;   private set => SetProperty(ref _statusMessage, value); }

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand                 RefreshCommand    { get; }
    public DelegateCommand                 AddCodeCommand    { get; }
    public DelegateCommand                 DeleteCodeCommand { get; }
    public DelegateCommand                 AddItemCommand    { get; }
    public DelegateCommand                 EditItemCommand   { get; }
    public DelegateCommand                 DeleteItemCommand { get; }
    public DelegateCommand                 SaveItemCommand   { get; }
    public DelegateCommand                 CancelEditCommand { get; }

    public SysLookupManagerViewModel(
        ISysLookupDataService lookupService,
        IAppConfigService appConfig)
    {
        _lookupService = lookupService;
        _appConfig     = appConfig;

        RefreshCommand    = new DelegateCommand(async () => await LoadCodesAsync());
        AddCodeCommand    = new DelegateCommand(ExecuteAddCode,    () => !string.IsNullOrWhiteSpace(NewCodeText));
        DeleteCodeCommand = new DelegateCommand(ExecuteDeleteCode, () => SelectedCode is not null);
        AddItemCommand    = new DelegateCommand(ExecuteAddItem,    () => SelectedCode is not null);
        EditItemCommand   = new DelegateCommand(ExecuteEditItem,   () => SelectedItem is not null);
        DeleteItemCommand = new DelegateCommand(async () => await ExecuteDeleteItemAsync(), () => SelectedItem is not null);
        SaveItemCommand   = new DelegateCommand(async () => await ExecuteSaveItemAsync());
        CancelEditCommand = new DelegateCommand(ExecuteCancelEdit);
    }

    // ── Load ──────────────────────────────────────────────────

    private async Task LoadCodesAsync()
    {
        if (_lookupService is null) return;

        IsBusy = true;
        StatusMessage = "Đang tải danh mục...";
        try
        {
            var codes = await _lookupService.GetAllCodesAsync(_cts.Token);
            LookupCodes.Clear();
            foreach (var c in codes) LookupCodes.Add(c);

            // Chọn lại code cũ hoặc chọn dòng đầu
            SelectedCode = LookupCodes.Contains(_selectedCode ?? "")
                ? _selectedCode
                : LookupCodes.FirstOrDefault();

            StatusMessage = $"{LookupCodes.Count} lookup code";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async Task LoadItemsAsync(string? code)
    {
        Items.Clear();
        IsEditorVisible = false;
        if (_lookupService is null || string.IsNullOrWhiteSpace(code)) return;

        IsBusy = true;
        try
        {
            var items = await _lookupService.GetItemsForEditAsync(code, _cts.Token);
            foreach (var item in items) Items.Add(item);
            StatusMessage = $"{code}: {items.Count} items";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi load items: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Code commands ─────────────────────────────────────────

    private async void ExecuteAddCode()
    {
        var code = NewCodeText.Trim().ToUpper();

        if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z][A-Z0-9_]*$"))
        {
            NewCodeError = "Chỉ dùng chữ hoa, số và _ (VD: BLOOD_TYPE)";
            return;
        }

        if (LookupCodes.Contains(code))
        {
            NewCodeError = "Code đã tồn tại";
            return;
        }

        // Không cần insert gì vào DB — code chỉ xuất hiện khi có ít nhất 1 item.
        // Thêm vào list local để user bắt đầu thêm items ngay.
        LookupCodes.Add(code);
        SelectedCode = code;
        NewCodeText  = "";
    }

    private void ExecuteDeleteCode()
    {
        // Xóa code sẽ không xóa item trong DB — chỉ deselect.
        // Để xóa hẳn, user phải xóa từng item.
        StatusMessage = "Để xóa code, hãy xóa hết tất cả items của code đó.";
    }

    // ── Item commands ─────────────────────────────────────────

    private void ExecuteAddItem()
    {
        // Reset form về trạng thái "thêm mới"
        _editLookupId  = 0;
        EditItemCode   = "";
        EditLabelVi    = "";
        EditLabelEn    = "";
        EditSortOrder  = Items.Count > 0 ? Items.Max(i => i.SortOrder) + 10 : 10;
        EditIsActive   = true;
        EditorError    = "";
        RaisePropertyChanged(nameof(IsEditMode));
        IsEditorVisible = true;
    }

    private void ExecuteEditItem()
    {
        if (SelectedItem is null) return;
        ApplyToEditor(SelectedItem);
        IsEditorVisible = true;
    }

    private void ApplyToEditor(LookupItemEditRecord item)
    {
        _editLookupId  = item.LookupId;
        EditItemCode   = item.ItemCode;
        EditLabelVi    = item.LabelVi;
        EditLabelEn    = item.LabelEn;
        EditSortOrder  = item.SortOrder;
        EditIsActive   = item.IsActive;
        EditorError    = "";
        RaisePropertyChanged(nameof(IsEditMode));
    }

    private async Task ExecuteSaveItemAsync()
    {
        if (_lookupService is null || SelectedCode is null) return;

        var itemCode = EditItemCode.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            EditorError = "Item Code không được để trống";
            return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(itemCode, @"^[A-Z][A-Z0-9_]*$"))
        {
            EditorError = "Chỉ dùng chữ hoa, số và _ (VD: NAM, NU, KXD)";
            return;
        }

        IsBusy = true;
        EditorError = "";
        try
        {
            // Validate trùng ItemCode
            var exists = await _lookupService.ItemCodeExistsAsync(
                SelectedCode, itemCode, _editLookupId, _cts.Token);
            if (exists)
            {
                EditorError = $"Item Code '{itemCode}' đã tồn tại trong {SelectedCode}";
                return;
            }

            var labelKey = $"{SelectedCode.ToLower()}.{itemCode.ToLower()}";

            var record = new LookupItemEditRecord
            {
                LookupId   = _editLookupId,
                TenantId   = _appConfig!.TenantId,
                LookupCode = SelectedCode,
                ItemCode   = itemCode,
                LabelKey   = labelKey,
                LabelVi    = EditLabelVi.Trim(),
                LabelEn    = EditLabelEn.Trim(),
                SortOrder  = EditSortOrder,
                IsActive   = EditIsActive,
            };

            if (_editLookupId == 0)
            {
                // Thêm mới
                record.LookupId = await _lookupService.AddItemAsync(record, _cts.Token);
                StatusMessage = $"Đã thêm '{itemCode}'";
            }
            else
            {
                // Cập nhật
                await _lookupService.UpdateItemAsync(record, _cts.Token);
                StatusMessage = $"Đã cập nhật '{itemCode}'";
            }

            IsEditorVisible = false;
            await LoadItemsAsync(SelectedCode);
        }
        catch (Exception ex)
        {
            EditorError = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task ExecuteDeleteItemAsync()
    {
        if (_lookupService is null || SelectedItem is null) return;

        IsBusy = true;
        try
        {
            await _lookupService.DeleteItemAsync(SelectedItem.LookupId, _cts.Token);
            StatusMessage = $"Đã xóa '{SelectedItem.ItemCode}'";
            IsEditorVisible = false;
            await LoadItemsAsync(SelectedCode);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi xóa: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void ExecuteCancelEdit()
    {
        IsEditorVisible = false;
        EditorError     = "";
    }

    // ── Navigation ────────────────────────────────────────────

    public void OnNavigatedTo(NavigationContext ctx)
    {
        _cts = new CancellationTokenSource();
        _ = LoadCodesAsync();
    }

    public bool IsNavigationTarget(NavigationContext ctx) => true;

    public void OnNavigatedFrom(NavigationContext ctx)
    {
        _cts.Cancel();
        IsEditorVisible = false;
    }
}
