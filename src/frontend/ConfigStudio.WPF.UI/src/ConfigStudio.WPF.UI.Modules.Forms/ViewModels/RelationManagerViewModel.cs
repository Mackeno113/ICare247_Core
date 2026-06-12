// File    : RelationManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Màn "Quản lý quan hệ" (Sys_Relation) — CRUD registry quan hệ master-detail
//           + soft-check FK. Master grid danh sách + panel editor bên phải.

using System.Collections.ObjectModel;
using System.Windows;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel màn quản lý <c>Sys_Relation</c>. Nạp danh sách quan hệ + bảng/cột để
/// chọn, cho phép thêm/sửa/ẩn từng quan hệ.
/// </summary>
public sealed class RelationManagerViewModel : BindableBase, INavigationAware
{
    private readonly IRelationDataService _service;
    private readonly IAppConfigService _config;
    private bool _loaded;

    /// <summary>Khởi tạo VM với service truy vấn quan hệ + cấu hình DB.</summary>
    /// <param name="service">Service Sys_Relation.</param>
    /// <param name="config">Cấu hình DB hiện hành (ConnectionString + Tenant_Id).</param>
    public RelationManagerViewModel(IRelationDataService service, IAppConfigService config)
    {
        _service = service;
        _config = config;

        Relations = [];
        Tables = [];
        MasterColumns = [];
        DetailColumns = [];

        NewCommand = new DelegateCommand(NewRelation);
        SaveCommand = new DelegateCommand(() => _ = SaveAsync());
        DeleteCommand = new DelegateCommand(() => _ = DeleteAsync());
        RefreshCommand = new DelegateCommand(() => _ = LoadAsync());
    }

    // ── Dữ liệu lưới + dropdown ─────────────────────────────────

    /// <summary>Danh sách quan hệ hiển thị trên lưới.</summary>
    public ObservableCollection<RelationRecord> Relations { get; }

    /// <summary>Danh sách bảng để chọn master/detail.</summary>
    public ObservableCollection<TableLookupRecord> Tables { get; }

    /// <summary>Cột của bảng cha (cho Master_Key_Column / Display / Value).</summary>
    public ObservableCollection<string> MasterColumns { get; }

    /// <summary>Cột của bảng con (cho Detail_FK_Column).</summary>
    public ObservableCollection<string> DetailColumns { get; }

    /// <summary>Tùy chọn loại quan hệ.</summary>
    public string[] RelationTypeOptions { get; } = ["OneToMany", "OneToOne"];

    /// <summary>Tùy chọn hành vi khi xóa master.</summary>
    public string[] OnDeleteOptions { get; } = ["Restrict", "Cascade", "SetNull", "NoAction"];

    private RelationRecord? _selectedRelation;
    /// <summary>Quan hệ đang chọn trên lưới — set sẽ nạp vào editor.</summary>
    public RelationRecord? SelectedRelation
    {
        get => _selectedRelation;
        set
        {
            if (SetProperty(ref _selectedRelation, value) && value is not null)
                LoadIntoEditor(value);
        }
    }

    private bool _showInactive;
    /// <summary>true = hiện cả quan hệ đã ẩn.</summary>
    public bool ShowInactive
    {
        get => _showInactive;
        set { if (SetProperty(ref _showInactive, value)) _ = LoadAsync(); }
    }

    // ── Trạng thái lỗi ──────────────────────────────────────────

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

    // ── Trường editor ───────────────────────────────────────────

    private int _editRelationId;
    private string? _editRelationCode;
    /// <summary>Mã quan hệ (tùy chọn, unique).</summary>
    public string? EditRelationCode
    {
        get => _editRelationCode;
        set => SetProperty(ref _editRelationCode, value);
    }

    private TableLookupRecord? _editMasterTable;
    /// <summary>Bảng cha đang chọn — đổi sẽ nạp lại cột master.</summary>
    public TableLookupRecord? EditMasterTable
    {
        get => _editMasterTable;
        set { if (SetProperty(ref _editMasterTable, value)) _ = LoadColumnsAsync(value?.TableId ?? 0, isMaster: true); }
    }

    private string _editMasterKeyColumn = "Id";
    /// <summary>Cột khóa ở bảng cha (mặc định 'Id').</summary>
    public string EditMasterKeyColumn
    {
        get => _editMasterKeyColumn;
        set => SetProperty(ref _editMasterKeyColumn, value);
    }

    private TableLookupRecord? _editDetailTable;
    /// <summary>Bảng con đang chọn — đổi sẽ nạp lại cột detail.</summary>
    public TableLookupRecord? EditDetailTable
    {
        get => _editDetailTable;
        set { if (SetProperty(ref _editDetailTable, value)) _ = LoadColumnsAsync(value?.TableId ?? 0, isMaster: false); }
    }

    private string? _editDetailFkColumn;
    /// <summary>Cột FK vật lý ở bảng con trỏ về master.</summary>
    public string? EditDetailFkColumn
    {
        get => _editDetailFkColumn;
        set => SetProperty(ref _editDetailFkColumn, value);
    }

    private string _editRelationType = "OneToMany";
    /// <summary>Loại quan hệ.</summary>
    public string EditRelationType
    {
        get => _editRelationType;
        set => SetProperty(ref _editRelationType, value);
    }

    private string _editOnDelete = "Restrict";
    /// <summary>Hành vi khi xóa master.</summary>
    public string EditOnDelete
    {
        get => _editOnDelete;
        set => SetProperty(ref _editOnDelete, value);
    }

    private string? _editDisplayColumn;
    /// <summary>Cột hiển thị (của bảng cha).</summary>
    public string? EditDisplayColumn
    {
        get => _editDisplayColumn;
        set => SetProperty(ref _editDisplayColumn, value);
    }

    private string? _editValueColumn;
    /// <summary>Cột giá trị (của bảng cha).</summary>
    public string? EditValueColumn
    {
        get => _editValueColumn;
        set => SetProperty(ref _editValueColumn, value);
    }

    private bool _editIsActive = true;
    /// <summary>Quan hệ đang dùng hay đã ẩn.</summary>
    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    private string _editorTitle = "Quan hệ mới";
    /// <summary>Tiêu đề panel editor.</summary>
    public string EditorTitle
    {
        get => _editorTitle;
        set => SetProperty(ref _editorTitle, value);
    }

    // ── Commands ────────────────────────────────────────────────

    /// <summary>Tạo quan hệ mới (xóa trắng editor).</summary>
    public DelegateCommand NewCommand { get; }
    /// <summary>Lưu quan hệ đang soạn.</summary>
    public DelegateCommand SaveCommand { get; }
    /// <summary>Ẩn quan hệ đang chọn.</summary>
    public DelegateCommand DeleteCommand { get; }
    /// <summary>Tải lại danh sách.</summary>
    public DelegateCommand RefreshCommand { get; }

    // ── Logic ───────────────────────────────────────────────────

    /// <summary>
    /// Nạp danh sách bảng + quan hệ từ DB. Sự kiện theo sau: lưới và dropdown được điền,
    /// banner lỗi ẩn nếu thành công.
    /// </summary>
    private async Task LoadAsync()
    {
        try
        {
            HasLoadError = false;
            var tenantId = _config.TenantId;

            var tables = await _service.GetTablesAsync(tenantId);
            Tables.Clear();
            foreach (var t in tables) Tables.Add(t);

            var relations = await _service.GetRelationsAsync(tenantId, ShowInactive);
            Relations.Clear();
            foreach (var r in relations) Relations.Add(r);

            _loaded = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>
    /// Nạp danh sách cột của một bảng vào MasterColumns/DetailColumns.
    /// </summary>
    /// <param name="tableId">Table_Id cần lấy cột (0 = bỏ qua).</param>
    /// <param name="isMaster">true = đổ vào MasterColumns; false = DetailColumns.</param>
    private async Task LoadColumnsAsync(int tableId, bool isMaster)
    {
        var target = isMaster ? MasterColumns : DetailColumns;
        target.Clear();
        if (tableId <= 0) return;
        try
        {
            var cols = await _service.GetColumnsAsync(tableId);
            foreach (var c in cols) target.Add(c);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>Nạp một quan hệ vào các trường editor. Sự kiện theo sau: dropdown cột tải lại.</summary>
    /// <param name="r">Quan hệ được chọn.</param>
    private void LoadIntoEditor(RelationRecord r)
    {
        _editRelationId = r.RelationId;
        EditRelationCode = r.RelationCode;
        EditMasterTable = Tables.FirstOrDefault(t => t.TableId == r.MasterTableId);
        EditMasterKeyColumn = string.IsNullOrWhiteSpace(r.MasterKeyColumn) ? "Id" : r.MasterKeyColumn;
        EditDetailTable = Tables.FirstOrDefault(t => t.TableId == r.DetailTableId);
        EditDetailFkColumn = r.DetailFkColumn;
        EditRelationType = r.RelationType;
        EditOnDelete = r.OnDelete;
        EditDisplayColumn = r.DisplayColumn;
        EditValueColumn = r.ValueColumn;
        EditIsActive = r.IsActive;
        EditorTitle = $"Sửa quan hệ #{r.RelationId}";
    }

    /// <summary>Xóa trắng editor để soạn quan hệ mới.</summary>
    private void NewRelation()
    {
        _selectedRelation = null;
        RaisePropertyChanged(nameof(SelectedRelation));
        _editRelationId = 0;
        EditRelationCode = null;
        EditMasterTable = null;
        EditMasterKeyColumn = "Id";
        EditDetailTable = null;
        EditDetailFkColumn = null;
        EditRelationType = "OneToMany";
        EditOnDelete = "Restrict";
        EditDisplayColumn = null;
        EditValueColumn = null;
        EditIsActive = true;
        EditorTitle = "Quan hệ mới";
    }

    /// <summary>Lưu quan hệ. Sự kiện theo sau: ghi DB rồi tải lại lưới.</summary>
    private async Task SaveAsync()
    {
        try
        {
            HasLoadError = false;
            var record = new RelationRecord
            {
                RelationId = _editRelationId,
                RelationCode = EditRelationCode,
                MasterTableId = EditMasterTable?.TableId ?? 0,
                MasterKeyColumn = EditMasterKeyColumn,
                DetailTableId = EditDetailTable?.TableId ?? 0,
                DetailFkColumn = EditDetailFkColumn,
                RelationType = EditRelationType,
                OnDelete = EditOnDelete,
                DisplayColumn = EditDisplayColumn,
                ValueColumn = EditValueColumn,
                IsActive = EditIsActive,
            };

            var id = await _service.SaveRelationAsync(record);
            _editRelationId = id;
            EditorTitle = $"Sửa quan hệ #{id}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>Ẩn quan hệ đang chọn (có xác nhận). Sự kiện theo sau: tải lại lưới.</summary>
    private async Task DeleteAsync()
    {
        if (_editRelationId <= 0)
        {
            ShowError("Chưa chọn quan hệ để ẩn.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Ẩn quan hệ #{_editRelationId}? (soft-delete, có thể bật lại bằng 'Hiện đã ẩn')",
            "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            HasLoadError = false;
            await _service.DeactivateRelationAsync(_editRelationId);
            NewRelation();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>Hiện banner lỗi đỏ.</summary>
    /// <param name="message">Nội dung lỗi.</param>
    private void ShowError(string message)
    {
        LoadErrorMessage = message;
        HasLoadError = true;
    }

    // ── INavigationAware ────────────────────────────────────────

    /// <summary>Nạp dữ liệu lần đầu khi điều hướng tới màn.</summary>
    /// <param name="navigationContext">Ngữ cảnh điều hướng Prism.</param>
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (!_loaded) _ = LoadAsync();
    }

    /// <summary>Cho phép tái dùng instance khi điều hướng lại.</summary>
    /// <param name="navigationContext">Ngữ cảnh điều hướng Prism.</param>
    /// <returns>Luôn true.</returns>
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    /// <summary>Không xử lý khi rời màn.</summary>
    /// <param name="navigationContext">Ngữ cảnh điều hướng Prism.</param>
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
