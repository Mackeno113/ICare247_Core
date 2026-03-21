// File    : DependencyViewerViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel cho Dependency Graph Viewer (Screen 08) — hiển thị quan hệ Field/Rule/Event.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;

/// <summary>
/// ViewModel cho Dependency Graph Viewer (Screen 08).
/// Hiển thị đồ thị phụ thuộc giữa Field, Rule, Event trong form.
/// Hỗ trợ auto-layout, filter, và deep-link đến editor tương ứng.
/// Load dữ liệu thật từ DB qua IFormDetailDataService, IRuleDataService, IEventDataService.
/// Khi chọn Field node → phân tích impact qua IImpactPreviewService.
/// </summary>
public sealed class DependencyViewerViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFormDetailDataService _formDetailService;
    private readonly IRuleDataService _ruleService;
    private readonly IEventDataService _eventService;
    private readonly IImpactPreviewService _impactService;

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
                // Tính toán impact khi chọn node
                _ = LoadNodeImpactAsync(value);
            }
        }
    }

    public bool HasSelectedNode => SelectedNode is not null;

    // ── Impact analysis ──────────────────────────────────────
    private ObservableCollection<ImpactItem> _selectedNodeImpact = [];
    public ObservableCollection<ImpactItem> SelectedNodeImpact
    {
        get => _selectedNodeImpact;
        private set => SetProperty(ref _selectedNodeImpact, value);
    }

    private bool _isLoadingImpact;
    public bool IsLoadingImpact
    {
        get => _isLoadingImpact;
        set => SetProperty(ref _isLoadingImpact, value);
    }

    // ── Loading state ────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _loadError = "";
    public string LoadError
    {
        get => _loadError;
        set
        {
            SetProperty(ref _loadError, value);
            RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => !string.IsNullOrEmpty(_loadError);

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
    private int _tenantId = 1; // NOTE: Admin tool dùng tenant mặc định 1
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

    public DependencyViewerViewModel(
        IRegionManager regionManager,
        IFormDetailDataService formDetailService,
        IRuleDataService ruleService,
        IEventDataService eventService,
        IImpactPreviewService impactService)
    {
        _regionManager = regionManager;
        _formDetailService = formDetailService;
        _ruleService = ruleService;
        _eventService = eventService;
        _impactService = impactService;

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
        FormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "";

        _ = LoadRealGraphAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load dữ liệu thật từ DB ──────────────────────────────

    /// <summary>
    /// Tải đồ thị phụ thuộc từ DB: Field, Rule, Event nodes + edges từ Val_Rule_Field và Evt_Definition.
    /// </summary>
    private async Task LoadRealGraphAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        LoadError = "";
        _allNodes.Clear();
        _allEdges.Clear();

        try
        {
            // ── Load fields của form ──────────────────────────────
            var fields = await _formDetailService.GetFieldsByFormAsync(_formId, _tenantId, ct);

            foreach (var f in fields)
            {
                _allNodes.Add(new DependencyNode
                {
                    Id = $"Field_{f.FieldId}",
                    NodeType = "Field",
                    Label = f.ColumnCode,
                    SubLabel = f.EditorType
                });
            }

            // ── Load rules summary để tạo Rule nodes ─────────────
            var ruleSummaries = await _formDetailService.GetRulesSummaryByFormAsync(_formId, _tenantId, ct);
            // Dùng dictionary để tránh duplicate Rule nodes
            var ruleNodeIds = new HashSet<int>();

            foreach (var r in ruleSummaries)
            {
                if (ruleNodeIds.Contains(r.RuleId)) continue;
                ruleNodeIds.Add(r.RuleId);
                _allNodes.Add(new DependencyNode
                {
                    Id = $"Rule_{r.RuleId}",
                    NodeType = "Rule",
                    Label = r.RuleTypeCode,
                    SubLabel = r.ErrorKey
                });
            }

            // ── Load events summary để tạo Event nodes ────────────
            var eventSummaries = await _formDetailService.GetEventsSummaryByFormAsync(_formId, _tenantId, ct);

            foreach (var e in eventSummaries)
            {
                _allNodes.Add(new DependencyNode
                {
                    Id = $"Event_{e.EventId}",
                    NodeType = "Event",
                    Label = e.TriggerCode,
                    SubLabel = e.FieldTarget
                });
            }

            // ── Build edges: Field → Rule (Val_Rule_Field) ────────
            foreach (var field in fields)
            {
                var fieldRules = await _ruleService.GetRulesByFieldAsync(field.FieldId, ct);
                foreach (var rule in fieldRules)
                {
                    // Thêm Rule node nếu chưa có (rule không nằm trong summary — edge case)
                    if (!ruleNodeIds.Contains(rule.RuleId))
                    {
                        ruleNodeIds.Add(rule.RuleId);
                        _allNodes.Add(new DependencyNode
                        {
                            Id = $"Rule_{rule.RuleId}",
                            NodeType = "Rule",
                            Label = rule.RuleTypeCode,
                            SubLabel = rule.ErrorKey
                        });
                    }

                    _allEdges.Add(new DependencyEdge
                    {
                        SourceId = $"Field_{field.FieldId}",
                        TargetId = $"Rule_{rule.RuleId}",
                        Label = "validates"
                    });
                }

                // ── Build edges: Field → Event (Evt_Definition.Field_Id) ─
                var fieldEvents = await _eventService.GetEventsByFieldAsync(field.FieldId, ct);
                foreach (var evt in fieldEvents)
                {
                    _allEdges.Add(new DependencyEdge
                    {
                        SourceId = $"Field_{field.FieldId}",
                        TargetId = $"Event_{evt.EventId}",
                        Label = "triggers"
                    });
                }
            }

            // ── Build edges: Event → Field target (FieldTarget) ───
            // Tìm Field node theo ColumnCode từ EventSummaryRecord.FieldTarget
            var fieldNodeByCode = _allNodes
                .Where(n => n.NodeType == "Field")
                .ToDictionary(n => n.Label, StringComparer.OrdinalIgnoreCase);

            foreach (var e in eventSummaries)
            {
                if (string.IsNullOrWhiteSpace(e.FieldTarget)) continue;
                if (!fieldNodeByCode.TryGetValue(e.FieldTarget, out var targetFieldNode)) continue;

                _allEdges.Add(new DependencyEdge
                {
                    SourceId = $"Event_{e.EventId}",
                    TargetId = targetFieldNode.Id,
                    Label = "sets/hides"
                });
            }

            // ── Populate filter ───────────────────────────────────
            AvailableFields.Clear();
            AvailableFields.Add("All");
            foreach (var n in _allNodes.Where(n => n.NodeType == "Field"))
                AvailableFields.Add(n.Label);

            ApplyFilter();
            ExecuteAutoLayout();
            DetectCircularDependencies();
        }
        catch (Exception ex)
        {
            LoadError = $"Không thể tải đồ thị: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Impact analysis khi chọn node ────────────────────────

    /// <summary>
    /// Khi chọn Field node → gọi IImpactPreviewService để phân tích ảnh hưởng.
    /// Khi chọn Rule/Event node → hiển thị các Field kết nối từ graph edges.
    /// </summary>
    private async Task LoadNodeImpactAsync(DependencyNode? node, CancellationToken ct = default)
    {
        SelectedNodeImpact.Clear();

        if (node is null) return;

        IsLoadingImpact = true;

        try
        {
            if (node.NodeType == "Field")
            {
                // Gọi ImpactPreviewService để phân tích expression-based impact
                var analysis = await _impactService.AnalyzeFieldImpactAsync(
                    node.Label, _formId, _tenantId, ct);

                // Gộp kết quả từ ImpactPreviewService
                foreach (var item in analysis.AffectedRules)
                    SelectedNodeImpact.Add(item);
                foreach (var item in analysis.AffectedEvents)
                    SelectedNodeImpact.Add(item);
                foreach (var item in analysis.AffectedFields)
                    SelectedNodeImpact.Add(item);

                // Nếu không có kết quả từ service, fallback dùng graph edges
                if (SelectedNodeImpact.Count == 0)
                    PopulateImpactFromEdges(node);

                HasCircularDependencies = HasCircularDependencies || analysis.HasCircularDependency;
            }
            else
            {
                // Rule/Event node → lấy connected Fields từ graph edges
                PopulateImpactFromEdges(node);
            }
        }
        catch
        {
            // Fallback: dùng edges nếu service lỗi
            PopulateImpactFromEdges(node);
        }
        finally
        {
            IsLoadingImpact = false;
        }
    }

    /// <summary>
    /// Lấy danh sách impact items từ các edges kết nối với node được chọn.
    /// </summary>
    private void PopulateImpactFromEdges(DependencyNode node)
    {
        var nodeMap = _allNodes.ToDictionary(n => n.Id);

        // Outgoing edges (node → target)
        foreach (var edge in _allEdges.Where(e => e.SourceId == node.Id))
        {
            if (!nodeMap.TryGetValue(edge.TargetId, out var target)) continue;
            SelectedNodeImpact.Add(new ImpactItem(
                target.Id,
                target.Label,
                target.NodeType.ToLower(),
                edge.Label));
        }

        // Incoming edges (source → node)
        foreach (var edge in _allEdges.Where(e => e.TargetId == node.Id))
        {
            if (!nodeMap.TryGetValue(edge.SourceId, out var source)) continue;
            SelectedNodeImpact.Add(new ImpactItem(
                source.Id,
                source.Label,
                source.NodeType.ToLower(),
                $"← {edge.Label}"));
        }
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
    /// Phát hiện circular dependency qua DFS trên toàn bộ _allEdges.
    /// </summary>
    private void DetectCircularDependencies()
    {
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var edge in _allEdges)
        {
            if (!adjacency.ContainsKey(edge.SourceId))
                adjacency[edge.SourceId] = [];
            adjacency[edge.SourceId].Add(edge.TargetId);
        }

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();
        var circularNodes = new HashSet<string>();

        foreach (var node in _allNodes)
            DfsDetect(node.Id, adjacency, visited, inStack, circularNodes);

        // Đánh dấu các nodes nằm trong cycle
        foreach (var n in _allNodes)
            n.HasWarning = circularNodes.Contains(n.Id);

        foreach (var e in _allEdges)
            e.IsCircular = circularNodes.Contains(e.SourceId) && circularNodes.Contains(e.TargetId);

        CircularDependencyCount = circularNodes.Count;
        HasCircularDependencies = circularNodes.Count > 0;
    }

    private static bool DfsDetect(
        string nodeId,
        Dictionary<string, List<string>> adj,
        HashSet<string> visited,
        HashSet<string> inStack,
        HashSet<string> circularNodes)
    {
        if (inStack.Contains(nodeId))
        {
            circularNodes.Add(nodeId);
            return true;
        }

        if (visited.Contains(nodeId)) return false;

        visited.Add(nodeId);
        inStack.Add(nodeId);

        if (adj.TryGetValue(nodeId, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (DfsDetect(neighbor, adj, visited, inStack, circularNodes))
                    circularNodes.Add(nodeId);
            }
        }

        inStack.Remove(nodeId);
        return false;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteRegenerate()
    {
        _ = LoadRealGraphAsync();
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
