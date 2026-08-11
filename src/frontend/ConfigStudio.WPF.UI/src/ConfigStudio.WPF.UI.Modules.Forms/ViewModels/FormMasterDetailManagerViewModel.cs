// File    : FormMasterDetailManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Màn "Cấu hình Master-Detail / Rail" — chọn form master → đặt bố cục (Inline/Rail)
//           + CRUD các pane chi tiết (Ui_Form_Detail). Lưới danh sách pane + panel editor.

using System.Collections.ObjectModel;
using System.Windows;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Prism.Commands;
using Prism.Dialogs;
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
    private readonly II18nDataService? _i18n;
    private readonly IDialogService? _dialogService;
    private bool _loaded;

    /// <summary>
    /// Khởi tạo VM với service master-detail (Config DB) + i18n (Sys_Resource) + dialog service.
    /// i18n/dialog tùy chọn (null trong test) — nhãn pane nhập THẲNG như editor field: key i18n TỰ SINH
    /// theo cấu trúc, người dùng gõ nhãn tiếng Việt, nút 🌐 mở dialog dịch (KHÔNG gõ key tay).
    /// </summary>
    public FormMasterDetailManagerViewModel(
        IFormMasterDetailDataService service,
        II18nDataService? i18n = null,
        IDialogService? dialogService = null)
    {
        _service = service;
        _i18n = i18n;
        _dialogService = dialogService;

        Forms = [];
        DetailForms = [];
        Panes = [];

        NewCommand = new DelegateCommand(NewPane);
        SaveCommand = new DelegateCommand(() => _ = SaveAsync());
        DeleteCommand = new DelegateCommand(() => _ = DeleteAsync());
        RefreshCommand = new DelegateCommand(() => _ = LoadFormsAsync());
        SaveLayoutCommand = new DelegateCommand(() => _ = SaveLayoutAsync());
        OpenTitleI18nCommand = new DelegateCommand(OpenTitleI18n);
        OpenGroupI18nCommand = new DelegateCommand(OpenGroupI18n);
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
        set
        {
            if (!SetProperty(ref _selectedForm, value)) return;
            RaisePropertyChanged(nameof(TitleKeyPreview));
            _ = LoadFormContextAsync();
        }
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
    /// <summary>Mã pane (unique trong form). Đổi → key i18n nhãn pane tự sinh lại (TitleKeyPreview).</summary>
    public string? EditDetailCode
    {
        get => _editDetailCode;
        set { if (SetProperty(ref _editDetailCode, value)) RaisePropertyChanged(nameof(TitleKeyPreview)); }
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
    /// <summary>Key i18n nhãn pane — TỰ SINH theo cấu trúc (không nhập tay). Chỉ đọc/hiển thị.</summary>
    public string? EditTitleKey
    {
        get => _editTitleKey;
        private set => SetProperty(ref _editTitleKey, value);
    }

    private string? _editTitleVi;
    /// <summary>
    /// Nhãn pane tiếng Việt — người dùng GÕ THẲNG (như editor field). Lưu vào Sys_Resource dưới
    /// <see cref="TitleKeyPreview"/> khi Lưu pane; các ngôn ngữ khác nhập qua nút 🌐 (dialog dịch).
    /// </summary>
    public string? EditTitleVi
    {
        get => _editTitleVi;
        set => SetProperty(ref _editTitleVi, value);
    }

    /// <summary>
    /// Key i18n nhãn pane TỰ SINH theo cấu trúc: <c>{formcode}.detail.{detailcode}.title</c>
    /// (khớp quy ước field <c>{table}.field.{code}.label</c>). Hiển thị read-only để người dùng biết
    /// key, KHÔNG cho sửa. Rỗng khi chưa đủ form master / mã pane.
    /// </summary>
    public string TitleKeyPreview => BuildDetailTitleKey();

    private string? _editIcon;
    /// <summary>Icon mục rail.</summary>
    public string? EditIcon
    {
        get => _editIcon;
        set => SetProperty(ref _editIcon, value);
    }

    private string? _editGroupKey;
    /// <summary>Nhóm rail — KEY gom nhóm (vd RELATED). Đổi → key nhãn nhóm tự sinh lại (GroupKeyPreview).</summary>
    public string? EditGroupKey
    {
        get => _editGroupKey;
        set { if (SetProperty(ref _editGroupKey, value)) RaisePropertyChanged(nameof(GroupKeyPreview)); }
    }

    private string? _editGroupVi;
    /// <summary>
    /// Nhãn nhóm tiếng Việt — GÕ THẲNG (như nhãn pane). Lưu vào Sys_Resource dưới <see cref="GroupKeyPreview"/>
    /// khi Lưu pane; ngôn ngữ khác nhập qua nút 🌐. Mọi pane cùng Group_Key chia sẻ 1 nhãn.
    /// </summary>
    public string? EditGroupVi
    {
        get => _editGroupVi;
        set => SetProperty(ref _editGroupVi, value);
    }

    /// <summary>
    /// Key i18n nhãn nhóm TỰ SINH: <c>{formcode}.railgroup.{groupkey}.title</c> (thường). Read-only.
    /// Rỗng khi chưa có form master hoặc Group_Key.
    /// </summary>
    public string GroupKeyPreview => BuildGroupTitleKey();

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
    /// <summary>Mở dialog "Dịch đa ngôn ngữ" cho nhãn pane (key tự sinh).</summary>
    public DelegateCommand OpenTitleI18nCommand { get; }
    /// <summary>Mở dialog "Dịch đa ngôn ngữ" cho nhãn NHÓM rail (key tự sinh).</summary>
    public DelegateCommand OpenGroupI18nCommand { get; }

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
        RaisePropertyChanged(nameof(TitleKeyPreview));
        // Nạp nhãn tiếng Việt đang lưu ở Sys_Resource để hiện trong ô "Tiêu đề pane" (best-effort).
        _ = LoadTitleViAsync(r.TitleKey);
        EditIcon = r.Icon;
        EditGroupKey = r.GroupKey;
        RaisePropertyChanged(nameof(GroupKeyPreview));
        _ = LoadGroupViAsync(r.GroupKey);
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
        EditTitleVi = null;
        RaisePropertyChanged(nameof(TitleKeyPreview));
        EditIcon = null;
        EditGroupKey = null;
        EditGroupVi = null;
        RaisePropertyChanged(nameof(GroupKeyPreview));
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
        if (string.IsNullOrWhiteSpace(EditDetailCode)) { ShowError("Chưa nhập Mã pane (Detail_Code)."); return; }
        try
        {
            HasLoadError = false;

            // Key i18n nhãn pane TỰ SINH theo cấu trúc (không nhập tay) — gắn vào bản ghi khi lưu.
            var titleKey = BuildDetailTitleKey();

            var record = new FormMasterDetailRecord
            {
                DetailId = _editDetailId,
                FormId = SelectedForm.FormId,
                DetailCode = EditDetailCode ?? "",
                PaneType = EditPaneType,
                DetailFormId = EditDetailForm?.FormId,
                ParentKeyColumn = EditParentKeyColumn,
                SaveMode = EditSaveMode,
                TitleKey = string.IsNullOrEmpty(titleKey) ? null : titleKey,
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
            EditTitleKey = record.TitleKey;

            // Ghi nhãn tiếng Việt vào Sys_Resource dưới key tự sinh (Hệ 1 metadata-driven):
            // có nhập → upsert (ghi đè); bỏ trống → init mặc định = Detail_Code nếu chưa có.
            if (_i18n is not null && !string.IsNullOrEmpty(titleKey))
            {
                if (!string.IsNullOrWhiteSpace(EditTitleVi))
                    await _i18n.SaveResourceAsync(titleKey, "vi", EditTitleVi.Trim());
                else
                    await _i18n.InitResourceIfMissingAsync(titleKey, "vi", EditDetailCode!.Trim());
            }

            // Nhãn NHÓM: chỉ ghi khi có Group_Key + người dùng có nhập nhãn (nhóm không bắt buộc).
            // Cùng khuôn key tự sinh; mọi pane cùng Group_Key chia sẻ 1 resource.
            var groupKey = BuildGroupTitleKey();
            if (_i18n is not null && !string.IsNullOrEmpty(groupKey) && !string.IsNullOrWhiteSpace(EditGroupVi))
                await _i18n.SaveResourceAsync(groupKey, "vi", EditGroupVi.Trim());

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

    // ── i18n nhãn pane (key tự sinh + gõ nhãn thẳng + dialog dịch) ───────────────

    /// <summary>
    /// Key i18n nhãn pane theo cấu trúc: <c>{formcode}.detail.{detailcode}.title</c> (thường, không dấu) —
    /// khớp quy ước field <c>{table}.field.{code}.label</c>. Rỗng khi thiếu form master hoặc mã pane.
    /// </summary>
    private string BuildDetailTitleKey()
    {
        var form = SelectedForm?.FormCode?.Trim().ToLowerInvariant() ?? "";
        var code = EditDetailCode?.Trim().ToLowerInvariant() ?? "";
        return string.IsNullOrEmpty(form) || string.IsNullOrEmpty(code)
            ? ""
            : $"{form}.detail.{code}.title";
    }

    /// <summary>Nạp nhãn vi đang lưu ở Sys_Resource vào ô "Tiêu đề pane" (best-effort, lỗi không chặn).</summary>
    private async Task LoadTitleViAsync(string? titleKey)
    {
        if (_i18n is null || string.IsNullOrWhiteSpace(titleKey)) { EditTitleVi = null; return; }
        try { EditTitleVi = await _i18n.ResolveKeyAsync(titleKey, "vi"); }
        catch { /* ô nhãn chỉ trợ giúp — không chặn nạp pane */ }
    }

    /// <summary>
    /// Mở dialog "Dịch đa ngôn ngữ" cho nhãn pane với key TỰ SINH. Popup tự ghi Sys_Resource mọi ngôn ngữ;
    /// callback cập nhật ô "Tiêu đề pane" (vi) theo giá trị vừa nhập.
    /// </summary>
    private void OpenTitleI18n()
    {
        if (_dialogService is null) return;
        var key = BuildDetailTitleKey();
        if (string.IsNullOrEmpty(key))
        {
            ShowError("Cần chọn form master và nhập Mã pane trước khi dịch nhãn.");
            return;
        }

        var p = new DialogParameters
        {
            { "key",          key },
            { "contextLabel", "Nhãn pane" },
            { "seedValue",    EditTitleVi ?? "" }
        };

        _dialogService.ShowDialog(ViewNames.I18nEditorDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            EditTitleVi = result.Parameters.GetValue<string>("primaryValue") ?? EditTitleVi;
            EditTitleKey = key;
            RaisePropertyChanged(nameof(TitleKeyPreview));
        });
    }

    /// <summary>
    /// Key i18n nhãn NHÓM theo cấu trúc: <c>{formcode}.railgroup.{groupkey}.title</c> (thường) —
    /// khớp SQL <c>FormRepository.GetDetailLayoutAsync</c> ghép. Rỗng khi thiếu form master / Group_Key.
    /// </summary>
    private string BuildGroupTitleKey()
    {
        var form = SelectedForm?.FormCode?.Trim().ToLowerInvariant() ?? "";
        var grp = EditGroupKey?.Trim().ToLowerInvariant() ?? "";
        return string.IsNullOrEmpty(form) || string.IsNullOrEmpty(grp)
            ? ""
            : $"{form}.railgroup.{grp}.title";
    }

    /// <summary>Nạp nhãn nhóm (vi) từ Sys_Resource vào ô "Nhãn nhóm" (best-effort).</summary>
    private async Task LoadGroupViAsync(string? groupKey)
    {
        var key = BuildGroupTitleKey();
        if (_i18n is null || string.IsNullOrEmpty(key)) { EditGroupVi = null; return; }
        try { EditGroupVi = await _i18n.ResolveKeyAsync(key, "vi"); }
        catch { /* ô nhãn nhóm chỉ trợ giúp — không chặn nạp pane */ }
    }

    /// <summary>Mở dialog "Dịch đa ngôn ngữ" cho nhãn NHÓM (key tự sinh). Yêu cầu đã có Group_Key.</summary>
    private void OpenGroupI18n()
    {
        if (_dialogService is null) return;
        var key = BuildGroupTitleKey();
        if (string.IsNullOrEmpty(key))
        {
            ShowError("Cần chọn form master và nhập Group_Key trước khi dịch nhãn nhóm.");
            return;
        }

        var p = new DialogParameters
        {
            { "key",          key },
            { "contextLabel", "Nhãn nhóm rail" },
            { "seedValue",    EditGroupVi ?? "" }
        };

        _dialogService.ShowDialog(ViewNames.I18nEditorDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            EditGroupVi = result.Parameters.GetValue<string>("primaryValue") ?? EditGroupVi;
            RaisePropertyChanged(nameof(GroupKeyPreview));
        });
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
