# PROMPT 08+09+10 — Dependency Viewer, Grammar Library, i18n Manager

> **Chạy sau các prompt core đã xong (01-07).**

---

## PROMPT 08 — Dependency Graph Viewer

```
Đọc CLAUDE.md trước.

Implement DependencyViewerView trong ICare247.ConfigStudio.Modules.Grammar
(hoặc tạo module riêng ICare247.ConfigStudio.Modules.Analysis).

─── FILES CẦN TẠO ───────────────────────────────────────

1. Views/DependencyViewerView.xaml + .xaml.cs
2. ViewModels/DependencyViewerViewModel.cs
3. Models/DependencyNode.cs
4. Models/DependencyEdge.cs
5. Controls/DependencyGraphControl.xaml    ← UserControl render graph bằng Canvas

─── LAYOUT ──────────────────────────────────────────────

DockPanel:
┌── Toolbar (Top, 52px) ──────────────────────────────────────────────────────┐
│  Form: [PURCHASE_ORDER ▼]  Filter Field: [All ▼]                            │
│  Show: [✓ Rules] [✓ Events]  [Auto-layout] [🔄 Regenerate from Expressions] │
└─────────────────────────────────────────────────────────────────────────────┘
┌── Graph Canvas (Fill) ──────────────────────────────────────────────────────┐
│  ScrollViewer + Canvas (zoom/pan support):                                  │
│                                                                              │
│  DependencyGraphControl:                                                    │
│    - Nodes = DependencyNode (Field, Rule, Event)                            │
│    - Edges = DependencyEdge (Source → Target, labeled "triggers")           │
│    - Node colors:                                                           │
│        Field  → Indigo box                                                  │
│        Rule   → Teal box                                                    │
│        Event  → Amber box                                                   │
│    - Edge: Arrow line với label                                             │
│    - Circular dep warning: Edge màu đỏ + icon ⚠️                           │
│    - Click node → hiện detail popup (field info / rule info / event info)  │
└─────────────────────────────────────────────────────────────────────────────┘
┌── Detail Panel (Bottom, 120px) ────────────────────────────────────────────┐
│  Khi chọn node: hiện info + nút [Open Editor →]                            │
│  Khi chọn edge: hiện "Source → Target" detail                              │
│  Warnings: "⚠️ 2 circular dependencies detected" (nếu có)                  │
└─────────────────────────────────────────────────────────────────────────────┘

─── MODELS ──────────────────────────────────────────────

public class DependencyNode
{
    public string Id { get; set; } = "";            // "Field_42", "Rule_18"
    public string NodeType { get; set; } = "";       // Field|Rule|Event
    public string Label { get; set; } = "";          // Column_Code / Rule type / Trigger
    public string SubLabel { get; set; } = "";       // Net_Type / Error_Key / Field_Code
    public double X { get; set; }                   // Canvas position
    public double Y { get; set; }
    public bool HasWarning { get; set; }            // circular dep
}

public class DependencyEdge
{
    public string SourceId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Label { get; set; } = "";         // "triggers", "references"
    public bool IsCircular { get; set; }
}

─── VIEWMODEL ───────────────────────────────────────────

Properties:
  - Nodes: ObservableCollection<DependencyNode>
  - Edges: ObservableCollection<DependencyEdge>
  - SelectedNode: DependencyNode?
  - HasCircularDependencies: bool
  - CircularDependencyCount: int

Commands:
  - LoadGraphCommand: DelegateCommand
  - RegenerateCommand: DelegateCommand
      → Parse tất cả Expression_Json và Condition_Expr trong form
      → Extract Identifier nodes → build Sys_Dependency records
      → Confirm "Regenerate sẽ xóa và tạo lại toàn bộ dependency. Tiếp tục?"
  - AutoLayoutCommand: DelegateCommand    ← sắp xếp nodes tự động (simple left-to-right)
  - OpenNodeEditorCommand: DelegateCommand<DependencyNode>

─── GRAPH RENDERING ─────────────────────────────────────

Dùng Canvas đơn giản với:
  - ItemsControl với Canvas.Left/Top binding từ Node.X, Node.Y
  - Border + TextBlock cho mỗi node
  - Line elements cho edges (tính điểm đầu/cuối từ node positions)
  - Arrow head: Polygon nhỏ ở đầu Line
  - Không dùng thư viện graph bên ngoài

Auto-layout thuật toán đơn giản:
  - Column 1: Field nodes (x=50)
  - Column 2: Rule nodes (x=300)
  - Column 3: Event nodes (x=550)
  - Y: phân đều theo số lượng nodes mỗi cột
```

---

## PROMPT 09 — Grammar Library (Screen 09)

```
Đọc CLAUDE.md trước.

Implement GrammarLibraryView trong ICare247.ConfigStudio.Modules.Grammar.

─── FILES CẦN TẠO ───────────────────────────────────────

1. Views/GrammarLibraryView.xaml + .xaml.cs
2. ViewModels/GrammarLibraryViewModel.cs
3. Models/GramFunctionDto.cs
4. Models/GramOperatorDto.cs

─── LAYOUT: TABCONTROL 2 TABS ───────────────────────────

TAB 1: Functions
  ┌─ Toolbar ───────────────────────────────────────────────────────────────┐
  │  [+ Add Function]  Search: [____🔍]  Filter: [All ▼] [System ▼]        │
  └─────────────────────────────────────────────────────────────────────────┘
  Grid (2 columns):
  COL 1 (260px): ListBox functions
    - Each item: [Function_Code]  ReturnType badge  Params count
    - Selected: highlight
    - Is_System=true: lock icon, cannot delete
  COL 2 (Fill): Detail panel
    ┌─ Function Detail ──────────────────────────────────────────────────────┐
    │  Code:        [dateDiff          ]  (readonly nếu Is_System)           │
    │  Description: [Khoảng cách 2 ngày]                                     │
    │  Return Type: [Int32  ▼]                                               │
    │  Min Params:  [3]    Max Params:  [3]                                  │
    │  Is System:   [✓ (readonly)]   Is Active:  [✓]                        │
    │                                                                         │
    │  Parameters:                                                            │
    │    DataGrid:  Index │ Name       │ Net_Type    │ Is_Optional            │
    │               0     │ startDate  │ DateTime    │ ○                     │
    │               1     │ endDate    │ DateTime    │ ○                     │
    │               2     │ unit       │ String      │ ○                     │
    │    [+ Add Param]  [🗑 Remove]  (chỉ cho non-system function)           │
    │                                                                         │
    │  Example:                                                               │
    │    [dateDiff(NgayBatDau, NgayKetThuc, "days")        ]                │
    │                                                                         │
    │  [💾 Save]  [🗑 Delete] (disabled nếu Is_System)                      │
    └────────────────────────────────────────────────────────────────────────┘

TAB 2: Operators
  DataGrid (simple, mostly readonly):
    Symbol │ Type            │ Precedence │ Description       │ Active
    ──────────────────────────────────────────────────────────────────
    ==     │ Comparison      │ 3          │ Bằng nhau         │  ✓
    !=     │ Comparison      │ 3          │ Khác nhau         │  ✓
    >      │ Comparison      │ 3          │ Lớn hơn           │  ✓
    &&     │ Logical         │ 2          │ Và (AND)          │  ✓
    ||     │ Logical         │ 1          │ Hoặc (OR)         │  ✓
    ...
  Chỉ cho edit Description và Is_Active.
  Operator Symbol không thể thêm mới từ UI (chỉ qua DB trực tiếp).

─── MODELS ──────────────────────────────────────────────

public class GramFunctionDto
{
    public int FunctionId { get; set; }
    public string FunctionCode { get; set; } = "";
    public string? Description { get; set; }
    public string ReturnNetType { get; set; } = "";
    public int ParamCountMin { get; set; }
    public int ParamCountMax { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public List<FunctionParamDto> Params { get; set; } = new();
}

public class FunctionParamDto
{
    public int ParamId { get; set; }
    public int ParamIndex { get; set; }
    public string ParamName { get; set; } = "";
    public string NetType { get; set; } = "";
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}
```

---

## PROMPT 10 — i18n Manager + Publish Checklist (Screen 10 & 11)

```
Đọc CLAUDE.md trước.

Implement I18nManagerView và PublishChecklistView.

─── I18N MANAGER ────────────────────────────────────────

FILES:
  Views/I18nManagerView.xaml + .xaml.cs
  ViewModels/I18nManagerViewModel.cs
  Models/ResourceKeyDto.cs

LAYOUT:
  Toolbar: [+ Add Key] | Lang tabs: [VI] [EN] [JA]... | Search + Filter
  DataGrid:
    Resource_Key (300px)  │  VI  (Fill)  │  EN  (Fill)  │  Status
    ───────────────────────────────────────────────────────────────
    err.soluong.range     │  Số lượng... │  Qty must... │  ✅ Complete
    lbl.soluong           │  Số Lượng    │  Quantity    │  ✅ Complete
    err.phone.invalid     │  Không hợp lệ│              │  ⚠️ Missing EN

  Inline edit: double-click cell → TextBox edit trực tiếp trong cell
  Missing translation: row highlight vàng + badge "Missing X lang"
  Filter: [All] [Missing] [Errors only]

  Bulk Import: [📥 Import Excel] → import từ file .xlsx (Key/VI/EN columns)
  Export: [📤 Export Excel]

ViewModelProperties:
  - ResourceKeys: ObservableCollection<ResourceKeyDto>
  - Languages: List<string>    // từ Sys_Language
  - SelectedKey: ResourceKeyDto?
  - SearchText: string
  - FilterMode: string         // All|Missing|Complete
  - MissingCount: int          // hiển thị badge trên tab

─── PUBLISH CHECKLIST ───────────────────────────────────

FILES:
  Views/PublishChecklistView.xaml + .xaml.cs
  ViewModels/PublishChecklistViewModel.cs
  Models/ChecklistItem.cs

LAYOUT:
  Header: "Publish Checklist — PURCHASE_ORDER"
  [▶ Run All Checks]                              [🚀 Publish] (disabled until AllPassed)
  ────────────────────────────────────────────────────────────────────────────────────
  ItemsControl (ChecklistItems):
    Each item: [✅/❌/⏳ icon]  Description                  [Jump To → button nếu fail]

  Checklist items:
    ✅ Tất cả field có Label_Key hợp lệ
    ✅ Tất cả Expression_Json parse thành công
    ✅ Tất cả function trong expression có trong Gram_Function whitelist
    ✅ Tất cả operator trong expression có trong Gram_Operator whitelist
    ✅ Return type của rule expression = Boolean
    ✅ Return type của calculate = compatible với target field
    ✅ Không có circular dependency
    ✅ Tất cả AST depth ≤ 20
    ❌ Field SoLuong: Error_Key 'err.soluong.range' thiếu bản dịch EN → [Jump to i18n]
    ✅ Tất cả CallAPI URL có format hợp lệ
    ✅ Sys_Dependency đầy đủ cho cross-field references

  Summary: "1 issue cần sửa trước khi Publish"

  Footer:
    [🔬 Simulate Runtime]   [🚀 Publish]   [← Back to Form Editor]

ChecklistItem model:
  public class ChecklistItem
  {
      public string Description { get; set; } = "";
      public CheckStatus Status { get; set; }          // Pending|Running|Passed|Failed|Warning
      public string? Detail { get; set; }              // chi tiết lỗi
      public string? JumpToView { get; set; }          // ViewNames để navigate
      public NavigationParameters? JumpToParams { get; set; }
  }

PublishChecklistViewModel Commands:
  - RunAllChecksCommand: DelegateCommand
      → set tất cả status = Running
      → chạy từng check (async)
      → cập nhật status từng item
  - PublishCommand: DelegateCommand
      → CanExecute: AllPassed=true
      → Confirm "Publish form PURCHASE_ORDER? Cache sẽ được invalidate."
      → Gọi publish service → navigate về FormManager
  - JumpToCommand: DelegateCommand<ChecklistItem>
      → navigate sang view tương ứng để fix lỗi
```
