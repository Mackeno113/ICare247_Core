// File    : DependencyViewerViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel cho Dependency Graph Viewer (Screen 08) — hiển thị quan hệ Field/Rule/Event.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;

/// <summary>
/// ViewModel cho Dependency Graph Viewer (Screen 08).
/// Hiển thị đồ thị phụ thuộc giữa Field, Rule, Event trong form.
/// Hỗ trợ auto-layout, filter, và deep-link đến editor tương ứng.
/// </summary>
public sealed class DependencyViewerViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;

    // ── Graph data ───────────────────────────────────────────
    public ObservableCollection<DependencyNode> Nodes { get; } = [];
    public ObservableCollection<DependencyEdge> Edges { get; } = [];

    private DependencyNode? _selectedNode;
    public DependencyNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            // NOTE: Bỏ select node cũ trước
            if (_selectedNode is not null) _selectedNode.IsSelected = false;
            if (SetProperty(ref _selectedNode, value))
            {
                if (value is not null) value.IsSelected = true;
                RaisePropertyChanged(nameof(HasSelectedNode));
            }
        }
    }

    public bool HasSelectedNode => SelectedNode is not null;

    // ── Filter ───────────────────────────────────────────────
    private bool _showRules = true;
    public bool ShowRules
    {
        get => _showRules;
        set { if (SetProperty(ref _showRules, value)) ApplyFilter(); }
    }

    private bool _showEvents = true;
    public bool ShowEvents
    {
        get => _showEvents;
        set { if (SetProperty(ref _showEvents, value)) ApplyFilter(); }
    }

    private string _filterField = "All";
    public string FilterField
    {
        get => _filterField;
        set { if (SetProperty(ref _filterField, value)) ApplyFilter(); }
    }

    public List<string> AvailableFields { get; } = ["All"];

    // ── Stats ────────────────────────────────────────────────
    private bool _hasCircularDependencies;
    public bool HasCircularDependencies
    {
        get => _hasCircularDependencies;
        set => SetProperty(ref _hasCircularDependencies, value);
    }

    private int _circularDependencyCount;
    public int CircularDependencyCount
    {
        get => _circularDependencyCount;
        set => SetProperty(ref _circularDependencyCount, value);
    }

    // ── Form context ─────────────────────────────────────────
    private int _formId;
    private string _formCode = "";
    public string FormCode { get => _formCode; set => SetProperty(ref _formCode, value); }

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand AutoLayoutCommand { get; }
    public DelegateCommand RegenerateCommand { get; }
    public DelegateCommand<DependencyNode> SelectNodeCommand { get; }
    public DelegateCommand OpenNodeEditorCommand { get; }
    public DelegateCommand BackCommand { get; }

    // ── Tất cả nodes/edges gốc (trước filter) ───────────────
    private readonly List<DependencyNode> _allNodes = [];
    private readonly List<DependencyEdge> _allEdges = [];

    public DependencyViewerViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        AutoLayoutCommand = new DelegateCommand(ExecuteAutoLayout);
        RegenerateCommand = new DelegateCommand(ExecuteRegenerate);
        SelectNodeCommand = new DelegateCommand<DependencyNode>(node => SelectedNode = node);
        OpenNodeEditorCommand = new DelegateCommand(ExecuteOpenNodeEditor, () => HasSelectedNode)
            .ObservesProperty(() => HasSelectedNode);
        BackCommand = new DelegateCommand(ExecuteBack);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _formId = navigationContext.Parameters.GetValue<int>("formId");
        FormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "PURCHASE_ORDER";

        LoadMockGraph();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Mock data ────────────────────────────────────────────

    /// <summary>
    /// Load mock dependency graph cho demo. Sau này sẽ load từ Sys_Dependency qua API.
    /// </summary>
    private void LoadMockGraph()
    {
        _allNodes.Clear();
        _allEdges.Clear();

        // ── Field nodes ──────────────────────────────────────
        _allNodes.Add(new DependencyNode { Id = "Field_1", NodeType = "Field", Label = "MaDonHang", SubLabel = "String" });
        _allNodes.Add(new DependencyNode { Id = "Field_2", NodeType = "Field", Label = "NgayDatHang", SubLabel = "DateTime" });
        _allNodes.Add(new DependencyNode { Id = "Field_3", NodeType = "Field", Label = "TrangThai", SubLabel = "String" });
        _allNodes.Add(new DependencyNode { Id = "Field_5", NodeType = "Field", Label = "SoLuong", SubLabel = "Int32" });
        _allNodes.Add(new DependencyNode { Id = "Field_6", NodeType = "Field", Label = "DonGia", SubLabel = "Decimal" });
        _allNodes.Add(new DependencyNode { Id = "Field_7", NodeType = "Field", Label = "ThanhTien", SubLabel = "Decimal" });
        _allNodes.Add(new DependencyNode { Id = "Field_8", NodeType = "Field", Label = "LyDoTuChoi", SubLabel = "String" });

        // ── Rule nodes ───────────────────────────────────────
        _allNodes.Add(new DependencyNode { Id = "Rule_1", NodeType = "Rule", Label = "Required", SubLabel = "err.fld.req" });
        _allNodes.Add(new DependencyNode { Id = "Rule_2", NodeType = "Rule", Label = "Numeric", SubLabel = "err.sl.range" });
        _allNodes.Add(new DependencyNode { Id = "Rule_3", NodeType = "Rule", Label = "Numeric", SubLabel = "err.dg.range" });

        // ── Event nodes ──────────────────────────────────────
        _allNodes.Add(new DependencyNode { Id = "Event_1", NodeType = "Event", Label = "OnChange", SubLabel = "TrangThai" });
        _allNodes.Add(new DependencyNode { Id = "Event_2", NodeType = "Event", Label = "OnChange", SubLabel = "SoLuong/DonGia" });

        // ── Edges ────────────────────────────────────────────
        _allEdges.Add(new DependencyEdge { SourceId = "Field_5", TargetId = "Rule_1", Label = "validates" });
        _allEdges.Add(new DependencyEdge { SourceId = "Field_5", TargetId = "Rule_2", Label = "validates" });
        _allEdges.Add(new DependencyEdge { SourceId = "Field_6", TargetId = "Rule_3", Label = "validates" });
        _allEdges.Add(new DependencyEdge { SourceId = "Field_3", TargetId = "Event_1", Label = "triggers" });
        _allEdges.Add(new DependencyEdge { SourceId = "Event_1", TargetId = "Field_8", Label = "shows/hides" });
        _allEdges.Add(new DependencyEdge { SourceId = "Field_5", TargetId = "Event_2", Label = "triggers" });
        _allEdges.Add(new DependencyEdge { SourceId = "Field_6", TargetId = "Event_2", Label = "triggers" });
        _allEdges.Add(new DependencyEdge { SourceId = "Event_2", TargetId = "Field_7", Label = "calculates" });

        // ── Populate filter ──────────────────────────────────
        AvailableFields.Clear();
        AvailableFields.Add("All");
        foreach (var n in _allNodes.Where(n => n.NodeType == "Field"))
            AvailableFields.Add(n.Label);

        ApplyFilter();
        ExecuteAutoLayout();
        DetectCircularDependencies();
    }

    // ── Filter ───────────────────────────────────────────────

    private void ApplyFilter()
    {
        Nodes.Clear();
        Edges.Clear();

        var visibleNodeTypes = new HashSet<string> { "Field" };
        if (ShowRules) visibleNodeTypes.Add("Rule");
        if (ShowEvents) visibleNodeTypes.Add("Event");

        foreach (var node in _allNodes)
        {
            if (!visibleNodeTypes.Contains(node.NodeType)) continue;
            if (FilterField != "All" && node.NodeType == "Field" && node.Label != FilterField) continue;
            Nodes.Add(node);
        }

        var visibleIds = Nodes.Select(n => n.Id).ToHashSet();
        foreach (var edge in _allEdges)
        {
            if (visibleIds.Contains(edge.SourceId) && visibleIds.Contains(edge.TargetId))
                Edges.Add(edge);
        }

        RecalculateEdgePositions();
    }

    // ── Auto-layout ──────────────────────────────────────────

    /// <summary>
    /// Thuật toán layout đơn giản: 3 cột (Field, Rule, Event), phân đều Y.
    /// </summary>
    private void ExecuteAutoLayout()
    {
        const double colField = 50;
        const double colRule = 280;
        const double colEvent = 510;
        const double startY = 30;
        const double spacingY = 80;

        LayoutColumn(Nodes.Where(n => n.NodeType == "Field"), colField, startY, spacingY);
        LayoutColumn(Nodes.Where(n => n.NodeType == "Rule"), colRule, startY, spacingY);
        LayoutColumn(Nodes.Where(n => n.NodeType == "Event"), colEvent, startY, spacingY);

        RecalculateEdgePositions();

        static void LayoutColumn(IEnumerable<DependencyNode> nodes, double x, double startY, double spacing)
        {
            int i = 0;
            foreach (var node in nodes)
            {
                node.X = x;
                node.Y = startY + i * spacing;
                i++;
            }
        }
    }

    /// <summary>
    /// Tính lại vị trí đầu/cuối của edges dựa trên node positions.
    /// </summary>
    private void RecalculateEdgePositions()
    {
        var nodeMap = Nodes.ToDictionary(n => n.Id);

        foreach (var edge in Edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceId, out var src)) continue;
            if (!nodeMap.TryGetValue(edge.TargetId, out var tgt)) continue;

            // NOTE: Arrow từ cạnh phải source → cạnh trái target
            edge.X1 = src.X + src.Width;
            edge.Y1 = src.Y + src.Height / 2;
            edge.X2 = tgt.X;
            edge.Y2 = tgt.Y + tgt.Height / 2;
        }
    }

    // ── Circular dependency detection ────────────────────────

    /// <summary>
    /// Phát hiện circular dependency đơn giản qua DFS.
    /// TODO(phase2): Thuật toán chính xác hơn cho multi-path cycles.
    /// </summary>
    private void DetectCircularDependencies()
    {
        // NOTE: Mock — trong demo không có circular dep
        HasCircularDependencies = false;
        CircularDependencyCount = 0;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteRegenerate()
    {
        // TODO(phase2): Parse tất cả Expression_Json → extract references → rebuild Sys_Dependency
        LoadMockGraph();
    }

    private void ExecuteOpenNodeEditor()
    {
        if (SelectedNode is null) return;

        var p = new NavigationParameters { { "formId", _formId } };

        switch (SelectedNode.NodeType)
        {
            case "Field":
                p.Add("fieldId", 0);
                p.Add("mode", "edit");
                _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
                break;
            case "Rule":
                p.Add("ruleId", 0);
                _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
                break;
            case "Event":
                p.Add("eventId", 0);
                _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
                break;
        }
    }

    private void ExecuteBack()
    {
        var p = new NavigationParameters { { "formId", _formId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }
}
