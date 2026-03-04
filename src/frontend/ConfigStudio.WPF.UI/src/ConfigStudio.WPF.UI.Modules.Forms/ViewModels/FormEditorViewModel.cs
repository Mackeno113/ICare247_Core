// File    : FormEditorViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Form Editor (Screen 03) — quản lý cấu trúc form: sections, fields, toolbar.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho màn hình Form Editor (Screen 03).
/// Hiển thị TreeView sections/fields, toolbar thao tác, property panel cho node được chọn.
/// </summary>
public sealed class FormEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;

    // ── Form info ─────────────────────────────────────────────
    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private string _formCode = "";
    public string FormCode { get => _formCode; set => SetProperty(ref _formCode, value); }

    private string _formName = "";
    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }

    private int _version = 1;
    public int Version { get => _version; set => SetProperty(ref _version, value); }

    private string _platform = "web";
    public string Platform { get => _platform; set => SetProperty(ref _platform, value); }

    // ── Tree structure ────────────────────────────────────────
    /// <summary>Danh sách sections (root nodes) của form.</summary>
    public ObservableCollection<FormTreeNode> Sections { get; } = [];

    private FormTreeNode? _selectedNode;
    /// <summary>Node đang được chọn trong TreeView.</summary>
    public FormTreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                RaisePropertyChanged(nameof(IsNodeSelected));
                RaisePropertyChanged(nameof(IsFieldSelected));
                RaisePropertyChanged(nameof(IsSectionSelected));
                DeleteNodeCommand.RaiseCanExecuteChanged();
                MoveUpCommand.RaiseCanExecuteChanged();
                MoveDownCommand.RaiseCanExecuteChanged();
                OpenFieldConfigCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsNodeSelected => SelectedNode is not null;
    public bool IsFieldSelected => SelectedNode?.NodeType == FormNodeType.Field;
    public bool IsSectionSelected => SelectedNode?.NodeType == FormNodeType.Section;

    // ── State ─────────────────────────────────────────────────
    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private string _searchText = "";
    /// <summary>Text tìm kiếm để filter tree.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalSections => Sections.Count;
    public int TotalFields => Sections.Sum(s => s.Children.Count);

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddSectionCommand { get; }
    public DelegateCommand AddFieldCommand { get; }
    public DelegateCommand DeleteNodeCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }
    public DelegateCommand OpenFieldConfigCommand { get; }
    public DelegateCommand SaveFormCommand { get; }
    public DelegateCommand PublishCommand { get; }
    public DelegateCommand ViewDependenciesCommand { get; }
    public DelegateCommand BackToListCommand { get; }
    public DelegateCommand ExpandAllCommand { get; }
    public DelegateCommand CollapseAllCommand { get; }

    public FormEditorViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        AddSectionCommand = new DelegateCommand(ExecuteAddSection);
        AddFieldCommand = new DelegateCommand(ExecuteAddField);
        DeleteNodeCommand = new DelegateCommand(ExecuteDeleteNode, () => IsNodeSelected);
        MoveUpCommand = new DelegateCommand(ExecuteMoveUp, () => IsNodeSelected);
        MoveDownCommand = new DelegateCommand(ExecuteMoveDown, () => IsNodeSelected);
        OpenFieldConfigCommand = new DelegateCommand(ExecuteOpenFieldConfig, () => IsFieldSelected);
        SaveFormCommand = new DelegateCommand(ExecuteSave, () => IsDirty)
            .ObservesProperty(() => IsDirty);
        PublishCommand = new DelegateCommand(ExecutePublish);
        ViewDependenciesCommand = new DelegateCommand(ExecuteViewDependencies);
        BackToListCommand = new DelegateCommand(ExecuteBackToList);
        ExpandAllCommand = new DelegateCommand(() => SetExpandAll(true));
        CollapseAllCommand = new DelegateCommand(() => SetExpandAll(false));
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        FormId = navigationContext.Parameters.GetValue<int>("formId");

        // NOTE: Nếu formId = 0 thì tạo form mới, ngược lại load từ mock
        if (FormId == 0) FormId = 1;
        LoadMockData();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load mock data ───────────────────────────────────────

    /// <summary>
    /// Load mock data cho demo. Sau này sẽ thay bằng API call tới backend.
    /// Mock form: "Đơn Đặt Hàng" với 3 sections, 8 fields.
    /// </summary>
    private void LoadMockData()
    {
        IsLoading = true;

        // ── Form info ────────────────────────────────────────
        FormCode = "PO_ORDER";
        FormName = "Đơn Đặt Hàng";
        Version = 3;
        Platform = "web";

        // ── Section 1: Thông Tin Chung ──────────────────────
        var section1 = new FormTreeNode
        {
            Id = 1, NodeType = FormNodeType.Section,
            Code = "SEC_GENERAL", DisplayName = "Thông Tin Chung",
            SortOrder = 1
        };
        section1.Children.Add(new FormTreeNode
        {
            Id = 1, NodeType = FormNodeType.Field,
            Code = "MaDonHang", DisplayName = "Mã Đơn Hàng",
            FieldType = "text", EditorType = "TextBox",
            IsRequired = true, SortOrder = 1
        });
        section1.Children.Add(new FormTreeNode
        {
            Id = 2, NodeType = FormNodeType.Field,
            Code = "NgayDatHang", DisplayName = "Ngày Đặt Hàng",
            FieldType = "date", EditorType = "DatePicker",
            IsRequired = true, SortOrder = 2
        });
        section1.Children.Add(new FormTreeNode
        {
            Id = 3, NodeType = FormNodeType.Field,
            Code = "TrangThai", DisplayName = "Trạng Thái",
            FieldType = "text", EditorType = "ComboBox",
            IsRequired = true, SortOrder = 3
        });

        // ── Section 2: Chi Tiết ─────────────────────────────
        var section2 = new FormTreeNode
        {
            Id = 2, NodeType = FormNodeType.Section,
            Code = "SEC_DETAIL", DisplayName = "Chi Tiết",
            SortOrder = 2
        };
        section2.Children.Add(new FormTreeNode
        {
            Id = 4, NodeType = FormNodeType.Field,
            Code = "NhaCungCap", DisplayName = "Nhà Cung Cấp",
            FieldType = "number", EditorType = "LookupBox",
            IsRequired = false, SortOrder = 1
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 5, NodeType = FormNodeType.Field,
            Code = "SoLuong", DisplayName = "Số Lượng",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = true, SortOrder = 2
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 6, NodeType = FormNodeType.Field,
            Code = "DonGia", DisplayName = "Đơn Giá",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = true, SortOrder = 3
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 7, NodeType = FormNodeType.Field,
            Code = "ThanhTien", DisplayName = "Thành Tiền",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = false, SortOrder = 4
        });

        // ── Section 3: Ghi Chú ──────────────────────────────
        var section3 = new FormTreeNode
        {
            Id = 3, NodeType = FormNodeType.Section,
            Code = "SEC_NOTE", DisplayName = "Ghi Chú",
            SortOrder = 3
        };
        section3.Children.Add(new FormTreeNode
        {
            Id = 8, NodeType = FormNodeType.Field,
            Code = "LyDoTuChoi", DisplayName = "Lý Do Từ Chối",
            FieldType = "text", EditorType = "TextArea",
            IsRequired = false, SortOrder = 1
        });

        Sections.Clear();
        Sections.Add(section1);
        Sections.Add(section2);
        Sections.Add(section3);

        RaisePropertyChanged(nameof(TotalSections));
        RaisePropertyChanged(nameof(TotalFields));

        IsLoading = false;
        IsDirty = false;
    }

    // ── Filter / Search ──────────────────────────────────────

    /// <summary>
    /// Filter TreeView theo <see cref="SearchText"/>.
    /// Nếu rỗng → hiện tất cả. Nếu có text → ẩn field không match, ẩn section rỗng.
    /// </summary>
    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        foreach (var section in Sections)
        {
            if (string.IsNullOrEmpty(query))
            {
                // NOTE: Reset visibility — hiện tất cả
                section.IsActive = true;
                section.IsExpanded = true;
                foreach (var field in section.Children)
                    field.IsActive = true;
            }
            else
            {
                // NOTE: Filter field theo code hoặc display name
                bool sectionMatch = section.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                 || section.Code.Contains(query, StringComparison.OrdinalIgnoreCase);

                foreach (var field in section.Children)
                {
                    bool fieldMatch = field.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                   || field.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                                   || field.EditorType.Contains(query, StringComparison.OrdinalIgnoreCase);
                    field.IsActive = sectionMatch || fieldMatch;
                }

                section.IsActive = sectionMatch || section.Children.Any(f => f.IsActive);
                if (section.IsActive) section.IsExpanded = true;
            }
        }
    }

    // ── Tree manipulation ────────────────────────────────────

    private void ExecuteAddSection()
    {
        var newId = Sections.Count > 0 ? Sections.Max(s => s.Id) + 1 : 1;
        var section = new FormTreeNode
        {
            Id = newId,
            NodeType = FormNodeType.Section,
            Code = $"SEC_NEW_{newId}",
            DisplayName = $"Section Mới {newId}",
            SortOrder = Sections.Count + 1,
            IsExpanded = true
        };
        Sections.Add(section);
        SelectedNode = section;
        IsDirty = true;
        RaisePropertyChanged(nameof(TotalSections));
    }

    private void ExecuteAddField()
    {
        // NOTE: Thêm field vào section đang chọn, hoặc section cuối cùng
        var targetSection = SelectedNode?.NodeType == FormNodeType.Section
            ? SelectedNode
            : FindParentSection(SelectedNode);

        targetSection ??= Sections.LastOrDefault();

        if (targetSection is null)
        {
            // NOTE: Chưa có section nào → tạo section trước
            ExecuteAddSection();
            targetSection = Sections.Last();
        }

        var newId = Sections.SelectMany(s => s.Children).DefaultIfEmpty()
            .Max(f => f?.Id ?? 0) + 1;

        var field = new FormTreeNode
        {
            Id = newId,
            NodeType = FormNodeType.Field,
            Code = $"FIELD_NEW_{newId}",
            DisplayName = $"Field Mới {newId}",
            FieldType = "text",
            EditorType = "TextBox",
            SortOrder = targetSection.Children.Count + 1
        };

        targetSection.Children.Add(field);
        targetSection.IsExpanded = true;
        SelectedNode = field;
        IsDirty = true;
        RaisePropertyChanged(nameof(TotalFields));
    }

    private void ExecuteDeleteNode()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            // TODO(phase2): Confirm dialog trước khi xóa section (kèm tất cả fields)
            Sections.Remove(SelectedNode);
            RaisePropertyChanged(nameof(TotalSections));
            RaisePropertyChanged(nameof(TotalFields));
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            parent?.Children.Remove(SelectedNode);
            RaisePropertyChanged(nameof(TotalFields));
        }

        SelectedNode = null;
        IsDirty = true;
    }

    private void ExecuteMoveUp()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            MoveInCollection(Sections, SelectedNode, -1);
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            if (parent is not null)
                MoveInCollection(parent.Children, SelectedNode, -1);
        }

        ReindexSortOrders();
        IsDirty = true;
    }

    private void ExecuteMoveDown()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            MoveInCollection(Sections, SelectedNode, +1);
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            if (parent is not null)
                MoveInCollection(parent.Children, SelectedNode, +1);
        }

        ReindexSortOrders();
        IsDirty = true;
    }

    private void ExecuteOpenFieldConfig()
    {
        if (SelectedNode is null || SelectedNode.NodeType != FormNodeType.Field) return;

        var parentSection = FindParentSection(SelectedNode);
        var p = new NavigationParameters
        {
            { "fieldId", SelectedNode.Id },
            { "formId", FormId },
            { "sectionId", parentSection?.Id ?? 0 },
            { "mode", "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    // ── Navigation commands ──────────────────────────────────

    private void ExecuteSave()
    {
        // TODO(phase2): Gọi API save form metadata
        IsDirty = false;
    }

    private void ExecutePublish()
    {
        var p = new NavigationParameters { { "formId", FormId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.PublishChecklist, p);
    }

    private void ExecuteViewDependencies()
    {
        var p = new NavigationParameters { { "formId", FormId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.DependencyViewer, p);
    }

    private void ExecuteBackToList()
    {
        // TODO(phase2): Confirm nếu IsDirty trước khi navigate back
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager);
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Tìm Section cha của 1 Field node.
    /// </summary>
    private FormTreeNode? FindParentSection(FormTreeNode? fieldNode)
    {
        if (fieldNode is null) return null;
        return Sections.FirstOrDefault(s => s.Children.Contains(fieldNode));
    }

    /// <summary>
    /// Di chuyển item trong collection lên/xuống 1 vị trí.
    /// </summary>
    private static void MoveInCollection(ObservableCollection<FormTreeNode> collection, FormTreeNode item, int direction)
    {
        int currentIndex = collection.IndexOf(item);
        int newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= collection.Count) return;
        collection.Move(currentIndex, newIndex);
    }

    /// <summary>
    /// Cập nhật lại SortOrder cho tất cả sections và fields.
    /// </summary>
    private void ReindexSortOrders()
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].SortOrder = i + 1;
            for (int j = 0; j < Sections[i].Children.Count; j++)
                Sections[i].Children[j].SortOrder = j + 1;
        }
    }

    private void SetExpandAll(bool expanded)
    {
        foreach (var section in Sections)
            section.IsExpanded = expanded;
    }
}
