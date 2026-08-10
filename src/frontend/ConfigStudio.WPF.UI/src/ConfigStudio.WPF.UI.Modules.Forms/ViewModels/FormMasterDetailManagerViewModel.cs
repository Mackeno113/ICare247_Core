// File    : FormMasterDetailManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Màn "Cấu hình Master-Detail / Rail" — chọn form master → đặt bố cục (Inline/Rail)
//           + CRUD các pane chi tiết (Ui_Form_Detail). Lưới danh sách pane + panel editor.

using System.Collections.ObjectModel;
using System.Windows;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel màn cấu hình master-detail. Chọn 1 form master → đặt <c>Detail_Layout</c> và
/// quản lý danh sách pane (<c>Ui_Form_Detail</c>). Config qua WPF (không SQL) — runtime dựng rail từ đây.
/// </summary>
public sealed class FormMasterDetailManagerViewModel : BindableBase, INavigationAware
{
    private readonly IFormMasterDetailDataService _service;
    private bool _loaded;

    /// <summary>Khởi tạo VM với service master-detail (Config DB).</summary>
    public FormMasterDetailManagerViewModel(IFormMasterDetailDataService service)
    {
        _service = service;

        Forms = [];
        DetailForms = [];
        Panes = [];

        NewCommand = new DelegateCommand(NewPane);
        SaveCommand = new DelegateCommand(() => _ = SaveAsync());
        DeleteCommand = new DelegateCommand(() => _ = DeleteAsync());
        RefreshCommand = new DelegateCommand(() => _ = LoadFormsAsync());
        SaveLayoutCommand = new DelegateCommand(() => _ = SaveLayoutAsync());
    }

    // ── Nguồn dữ liệu ───────────────────────────────────────────

    /// <summary>Danh sách form để chọn form master.</summary>
    public ObservableCollection<FormLookupItem> Forms { get; }

    /// <summary>Danh sách form để chọn form con của pane Grid (= Forms, tách biến để bind riêng).</summary>
    public ObservableCollection<FormLookupItem> DetailForms { get; }

    /// <summary>Danh sách pane của form master đang chọn.</summary>
    public ObservableCollection<FormMasterDetailRecord> Panes { get; }

    /// <summary>Tùy chọn kiểu bố cục chi tiết của form.</summary>
    public string[] LayoutOptions { get; } = ["Inline", "Rail"];

    /// <summary>Tùy chọn kiểu pane.</summary>
    public string[] PaneTypeOptions { get; } = ["Grid", "Timeline"];

    /// <summary>Tùy chọn chế độ lưu lưới con.</summary>
    public string[] SaveModeOptions { get; } = ["Immediate", "WithMaster"];

    /// <summary>Tùy chọn chế độ nhập lưới.</summary>
    public string[] EditModeOptions { get; } = ["EntryPanel", "CellInline", "RowPopup"];

    // ── Chọn form master ────────────────────────────────────────

    private FormLookupItem? _selectedForm;
    /// <summary>Form master đang chọn — đổi sẽ nạp bố cục + danh sách pane.</summary>
    public FormLookupItem? SelectedForm
    {
        get => _selectedForm;
        set { if (SetProperty(ref _selectedForm, value)) _ = LoadFormContextAsync(); }
    }

    private string _detailLayout = "Inline";
    /// <summary>Bố cục chi tiết của form master (Inline | Rail).</summary>
    public string DetailLayout
    {
        get => _detailLayout;
        set => SetProperty(ref _detailLayout, value);
    }

    private bool _showInactive;
    /// <summary>true = hiện cả pane đã ẩn.</summary>
    public bool ShowInactive
    {
        get => _showInactive;
        set { if (SetProperty(ref _showInactive, value)) _ = LoadPanesAsync(); }
    }

    // ── Banner lỗi ──────────────────────────────────────────────

    private bool _hasLoadError;
    /// <summary>Có lỗi nạp/ghi không (để hiện banner đỏ).</summary>
    public bool HasLoadError
    {
        get => _hasLoadError;
        set => SetProperty(ref _hasLoadError, value);
    }

    private string _loadErrorMessage = "";
    /// <summary>Nội dung lỗi hiển thị trên banner.</summary>
    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        set => SetProperty(ref _loadErrorMessage, value);
    }

    // ── Chọn pane trên lưới ─────────────────────────────────────

    private FormMasterDetailRecord? _selectedPane;
    /// <summary>Pane đang chọn — set sẽ nạp vào editor.</summary>
    public FormMasterDetailRecord? SelectedPane
    {
        get => _selectedPane;
        set
        {
            if (SetProperty(ref _selectedPane, value) && value is not null)
                LoadIntoEditor(value);
        }
    }

    // ── Trường editor ───────────────────────────────────────────

    private int _editDetailId;

    private string? _editDetailCode;
    /// <summary>Mã pane (unique trong form).</summary>
    public string? EditDetailCode
    {
        get => _editDetailCode;
        set => SetProperty(ref _editDetailCode, value);
    }

    private string _editPaneType = "Grid";
    /// <summary>Kiểu pane — 'Timeline' thì không cần form con/khóa cha.</summary>
    public string EditPaneType
    {
        get => _editPaneType;
        set { if (SetProperty(ref _editPaneType, value)) RaisePropertyChanged(nameof(IsGridPane)); }
    }

    /// <summary>true khi pane là lưới CRUD (hiện các ô form con / khóa cha / save mode).</summary>
    public bool IsGridPane => EditPaneType != "Timeline";

    private FormLookupItem? _editDetailForm;
    /// <summary>Form con định nghĩa cột lưới (pane Grid).</summary>
    public FormLookupItem? EditDetailForm
    {
        get => _editDetailForm;
        set => SetProperty(ref _editDetailForm, value);
    }

    private string? _editParentKeyColumn;
    /// <summary>Cột FK bảng con trỏ về master (vd 'NhanVien_Id').</summary>
    public string? EditParentKeyColumn
    {
        get => _editParentKeyColumn;
        set => SetProperty(ref _editParentKeyColumn, value);
    }

    private string _editSaveMode = "Immediate";
    /// <summary>Chế độ lưu lưới con.</summary>
    public string EditSaveMode
    {
        get => _editSaveMode;
        set => SetProperty(ref _editSaveMode, value);
    }

    private string? _editTitleKey;
    /// <summary>Key i18n nhãn pane.</summary>
    public string? EditTitleKey
    {
        get => _editTitleKey;
        set => SetProperty(ref _editTitleKey, value);
    }

    private string? _editIcon;
    /// <summary>Icon mục rail.</summary>
    public string? EditIcon
    {
        get => _editIcon;
        set => SetProperty(ref _editIcon, value);
    }

    private string? _editGroupKey;
    /// <summary>Nhóm rail (vd RELATED / HISTORY).</summary>
    public string? EditGroupKey
    {
        get => _editGroupKey;
        set => SetProperty(ref _editGroupKey, value);
    }

    private string _editEditMode = "EntryPanel";
    /// <summary>Chế độ nhập lưới.</summary>
    public string EditEditMode
    {
        get => _editEditMode;
        set => SetProperty(ref _editEditMode, value);
    }

    private bool _editAllowAdd = true;
    /// <summary>Cho phép thêm dòng.</summary>
    public bool EditAllowAdd
    {
        get => _editAllowAdd;
        set => SetProperty(ref _editAllowAdd, value);
    }

    private bool _editAllowDelete = true;
    /// <summary>Cho phép xóa dòng.</summary>
    public bool EditAllowDelete
    {
        get => _editAllowDelete;
        set => SetProperty(ref _editAllowDelete, value);
    }

    private bool _editAllowReorder;
    /// <summary>Cho phép kéo sắp thứ tự dòng.</summary>
    public bool EditAllowReorder
    {
        get => _editAllowReorder;
        set => SetProperty(ref _editAllowReorder, value);
    }

    private int _editMinRows;
    /// <summary>Số dòng tối thiểu.</summary>
    public int EditMinRows
    {
        get => _editMinRows;
        set => SetProperty(ref _editMinRows, value);
    }

    private int _editOrderNo;
    /// <summary>Thứ tự pane trên rail.</summary>
    public int EditOrderNo
    {
        get => _editOrderNo;
        set => SetProperty(ref _editOrderNo, value);
    }

    private bool _editIsActive = true;
    /// <summary>Pane đang dùng hay đã ẩn.</summary>
    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    private string _editorTitle = "Pane mới";
    /// <summary>Tiêu đề panel editor.</summary>
    public string EditorTitle
    {
        get => _editorTitle;
        set => SetProperty(ref _editorTitle, value);
    }

    // ── Commands ────────────────────────────────────────────────

    /// <summary>Tạo pane mới (xóa trắng editor).</summary>
    public DelegateCommand NewCommand { get; }
    /// <summary>Lưu pane đang soạn.</summary>
    public DelegateCommand SaveCommand { get; }
    /// <summary>Ẩn pane đang chọn.</summary>
    public DelegateCommand DeleteCommand { get; }
    /// <summary>Tải lại danh sách form + pane.</summary>
    public DelegateCommand RefreshCommand { get; }
    /// <summary>Lưu bố cục Detail_Layout của form master.</summary>
    public DelegateCommand SaveLayoutCommand { get; }

    // ── Logic ───────────────────────────────────────────────────

    /// <summary>Nạp danh sách form vào 2 dropdown. Sự kiện theo sau: dropdown được điền.</summary>
    private async Task LoadFormsAsync()
    {
        try
        {
            HasLoadError = false;
            var forms = await _service.GetFormsAsync();
            Forms.Clear();
            DetailForms.Clear();
            foreach (var f in forms) { Forms.Add(f); DetailForms.Add(f); }
            _loaded = true;

            if (SelectedForm is not null)
                await LoadFormContextAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Nạp bố cục + danh sách pane cho form master đang chọn.</summary>
    private async Task LoadFormContextAsync()
    {
        if (SelectedForm is null) return;
        try
        {
            HasLoadError = false;
            DetailLayout = await _service.GetDetailLayoutAsync(SelectedForm.FormId);
            await LoadPanesAsync();
            NewPane();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Nạp danh sách pane của form master đang chọn.</summary>
    private async Task LoadPanesAsync()
    {
        Panes.Clear();
        if (SelectedForm is null) return;
        try
        {
            var panes = await _service.GetPanesAsync(SelectedForm.FormId, ShowInactive);
            foreach (var p in panes) Panes.Add(p);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Nạp một pane vào các trường editor.</summary>
    private void LoadIntoEditor(FormMasterDetailRecord r)
    {
        _editDetailId = r.DetailId;
        EditDetailCode = r.DetailCode;
        EditPaneType = r.PaneType;
        EditDetailForm = DetailForms.FirstOrDefault(f => f.FormId == r.DetailFormId);
        EditParentKeyColumn = r.ParentKeyColumn;
        EditSaveMode = r.SaveMode;
        EditTitleKey = r.TitleKey;
        EditIcon = r.Icon;
        EditGroupKey = r.GroupKey;
        EditEditMode = r.EditMode;
        EditAllowAdd = r.AllowAdd;
        EditAllowDelete = r.AllowDelete;
        EditAllowReorder = r.AllowReorder;
        EditMinRows = r.MinRows;
        EditOrderNo = r.OrderNo;
        EditIsActive = r.IsActive;
        EditorTitle = $"Sửa pane #{r.DetailId}";
    }

    /// <summary>Xóa trắng editor để soạn pane mới. Order_No gợi ý = max hiện có + 1.</summary>
    private void NewPane()
    {
        _selectedPane = null;
        RaisePropertyChanged(nameof(SelectedPane));
        _editDetailId = 0;
        EditDetailCode = null;
        EditPaneType = "Grid";
        EditDetailForm = null;
        EditParentKeyColumn = null;
        EditSaveMode = "Immediate";
        EditTitleKey = null;
        EditIcon = null;
        EditGroupKey = null;
        EditEditMode = "EntryPanel";
        EditAllowAdd = true;
        EditAllowDelete = true;
        EditAllowReorder = false;
        EditMinRows = 0;
        EditOrderNo = Panes.Count == 0 ? 1 : Panes.Max(p => p.OrderNo) + 1;
        EditIsActive = true;
        EditorTitle = "Pane mới";
    }

    /// <summary>Lưu pane. Sự kiện theo sau: ghi DB rồi tải lại lưới.</summary>
    private async Task SaveAsync()
    {
        if (SelectedForm is null) { ShowError("Chưa chọn form master."); return; }
        try
        {
            HasLoadError = false;
            var record = new FormMasterDetailRecord
            {
                DetailId = _editDetailId,
                FormId = SelectedForm.FormId,
                DetailCode = EditDetailCode ?? "",
                PaneType = EditPaneType,
                DetailFormId = EditDetailForm?.FormId,
                ParentKeyColumn = EditParentKeyColumn,
                SaveMode = EditSaveMode,
                TitleKey = EditTitleKey,
                Icon = EditIcon,
                GroupKey = EditGroupKey,
                EditMode = EditEditMode,
                AllowAdd = EditAllowAdd,
                AllowDelete = EditAllowDelete,
                AllowReorder = EditAllowReorder,
                MinRows = EditMinRows,
                OrderNo = EditOrderNo,
                IsActive = EditIsActive,
            };

            var id = await _service.SavePaneAsync(record);
            _editDetailId = id;
            EditorTitle = $"Sửa pane #{id}";
            await LoadPanesAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Lưu bố cục Detail_Layout của form master.</summary>
    private async Task SaveLayoutAsync()
    {
        if (SelectedForm is null) { ShowError("Chưa chọn form master."); return; }
        try
        {
            HasLoadError = false;
            await _service.SaveDetailLayoutAsync(SelectedForm.FormId, DetailLayout);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Ẩn pane đang chọn (có xác nhận). Sự kiện theo sau: tải lại lưới.</summary>
    private async Task DeleteAsync()
    {
        if (_editDetailId <= 0) { ShowError("Chưa chọn pane để ẩn."); return; }

        var confirm = MessageBox.Show(
            $"Ẩn pane #{_editDetailId}? (soft-delete, bật lại bằng 'Hiện đã ẩn')",
            "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            HasLoadError = false;
            await _service.DeactivatePaneAsync(_editDetailId);
            NewPane();
            await LoadPanesAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    /// <summary>Hiện banner lỗi đỏ.</summary>
    private void ShowError(string message)
    {
        LoadErrorMessage = message;
        HasLoadError = true;
    }

    // ── INavigationAware ────────────────────────────────────────

    /// <summary>Nạp danh sách form lần đầu khi điều hướng tới màn.</summary>
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (!_loaded) _ = LoadFormsAsync();
    }

    /// <summary>Cho phép tái dùng instance khi điều hướng lại.</summary>
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    /// <summary>Không xử lý khi rời màn.</summary>
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
