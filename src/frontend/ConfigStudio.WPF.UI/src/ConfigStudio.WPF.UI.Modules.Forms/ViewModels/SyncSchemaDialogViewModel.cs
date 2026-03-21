// File    : SyncSchemaDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho dialog Đồng bộ Schema — hiện diff giữa Target DB và form hiện tại,
//           cho user quyết định thêm cột mới / xóa field mồ côi / bỏ qua.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Dialog 3-tab: "Có thể thêm" / "Cảnh báo" / "Type Mismatch".
/// Nhận SchemaDiffResult từ FormEditorViewModel.
/// Trả về: OK + columnsToAdd (IReadOnlyList&lt;ColumnSchemaDto&gt;) + fieldsToRemove (IReadOnlyList&lt;FormTreeNode&gt;).
/// </summary>
public sealed class SyncSchemaDialogViewModel : ViewModelBase, IDialogAware
{
    // ── IDialogAware ─────────────────────────────────────────
    public string Title => "Đồng bộ cấu trúc cột DB";
    public DialogCloseListener RequestClose { get; set; }

    // ── Tab "Có thể thêm" ────────────────────────────────────
    /// <summary>Cột có trong Target DB nhưng chưa có field (cột ShouldSkip đã bị lọc).</summary>
    public ObservableCollection<AutoGenerateColumnItem> ColumnsToAdd { get; } = [];

    // ── Tab "Cảnh báo" ───────────────────────────────────────
    /// <summary>Field đang tham chiếu cột không còn tồn tại trong Target DB.</summary>
    public ObservableCollection<OrphanedFieldItem> OrphanedFields { get; } = [];

    // ── Tab "Type Mismatch" ───────────────────────────────────
    /// <summary>Field có EditorType không còn khớp với DataType hiện tại.</summary>
    public ObservableCollection<TypeMismatchItem> TypeMismatches { get; } = [];

    // ── Section đích (cho tab "Có thể thêm") ─────────────────
    public ObservableCollection<SectionOptionItem> AvailableSections { get; } = [];

    private SectionOptionItem? _selectedSection;
    public SectionOptionItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
                ApplyCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Active tab ────────────────────────────────────────────
    private int _activeTab;
    public int ActiveTab
    {
        get => _activeTab;
        set => SetProperty(ref _activeTab, value);
    }

    // ── Thống kê ──────────────────────────────────────────────
    public int AddCount     => ColumnsToAdd.Count(i => i.IsSelected);
    public int RemoveCount  => OrphanedFields.Count(i => i.IsMarkedForRemoval);
    public int MismatchCount => TypeMismatches.Count;

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand SelectAllAddCommand    { get; }
    public DelegateCommand DeselectAllAddCommand  { get; }
    public DelegateCommand ApplyCommand           { get; }
    public DelegateCommand CloseCommand           { get; }

    public SyncSchemaDialogViewModel()
    {
        SelectAllAddCommand   = new DelegateCommand(() => SetAllAddSelected(true));
        DeselectAllAddCommand = new DelegateCommand(() => SetAllAddSelected(false));
        ApplyCommand = new DelegateCommand(ExecuteApply, CanApply);
        CloseCommand = new DelegateCommand(ExecuteClose);
    }

    // ── IDialogAware ─────────────────────────────────────────

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        var diff = parameters.GetValue<SchemaDiffResult>("diffResult") ?? SchemaDiffResult.Empty;

        // ── Tab 1: Cột cần thêm ───────────────────────────────
        ColumnsToAdd.Clear();
        foreach (var col in diff.ColumnsToAdd)
        {
            var item = new AutoGenerateColumnItem(col);
            // NOTE: IsSelected mặc định = true (muốn thêm hết)
            item.PropertyChanged += (_, _) =>
            {
                RaisePropertyChanged(nameof(AddCount));
                ApplyCommand.RaiseCanExecuteChanged();
            };
            ColumnsToAdd.Add(item);
        }

        // ── Tab 2: Field mồ côi ───────────────────────────────
        OrphanedFields.Clear();
        foreach (var field in diff.OrphanedFields)
        {
            var item = new OrphanedFieldItem(field);
            item.PropertyChanged += (_, _) =>
            {
                RaisePropertyChanged(nameof(RemoveCount));
                ApplyCommand.RaiseCanExecuteChanged();
            };
            OrphanedFields.Add(item);
        }

        // ── Tab 3: Type mismatch ──────────────────────────────
        TypeMismatches.Clear();
        foreach (var m in diff.TypeMismatches)
            TypeMismatches.Add(m);

        // ── Sections ─────────────────────────────────────────
        var sections = parameters.GetValue<IReadOnlyList<SectionOptionItem>>("sections") ?? [];
        AvailableSections.Clear();
        foreach (var s in sections)
            AvailableSections.Add(s);
        SelectedSection = AvailableSections.FirstOrDefault();

        RaisePropertyChanged(nameof(AddCount));
        RaisePropertyChanged(nameof(RemoveCount));
        RaisePropertyChanged(nameof(MismatchCount));

        // Tự động mở tab có issue (ưu tiên: Cảnh báo > Thêm > Mismatch)
        ActiveTab = OrphanedFields.Count > 0 ? 1
                  : ColumnsToAdd.Count > 0 ? 0
                  : 2;
    }

    // ── Helpers ──────────────────────────────────────────────

    private void SetAllAddSelected(bool value)
    {
        foreach (var item in ColumnsToAdd)
            item.IsSelected = value;
    }

    private bool CanApply()
        => (AddCount > 0 && SelectedSection is not null) || RemoveCount > 0;

    private void ExecuteApply()
    {
        var columnsToAdd = ColumnsToAdd
            .Where(i => i.IsSelected)
            .Select(i => i.Column)
            .ToList();

        var fieldsToRemove = OrphanedFields
            .Where(i => i.IsMarkedForRemoval)
            .Select(i => i.Field)
            .ToList();

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("columnsToAdd",     (IReadOnlyList<ColumnSchemaDto>)columnsToAdd);
        result.Parameters.Add("fieldsToRemove",   (IReadOnlyList<FormTreeNode>)fieldsToRemove);
        result.Parameters.Add("targetSectionCode", SelectedSection?.Code ?? "");
        RequestClose.Invoke(result);
    }

    private void ExecuteClose()
        => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
}
