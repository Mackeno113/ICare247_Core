# PROMPT 06 — Event Editor (Screen 06)

> **Project:** ICare247.ConfigStudio.Modules.Events
> **Chạy sau Prompt 05.**

---

## PROMPT

```
Đọc CLAUDE.md trước. Đặc biệt chú ý phần "6 Action Types cho Event".

Implement EventEditorView trong ICare247.ConfigStudio.Modules.Events.

─── FILES CẦN TẠO ───────────────────────────────────────

1. EventsModule.cs
2. Views/EventEditorView.xaml + .xaml.cs
3. ViewModels/EventEditorViewModel.cs
4. Models/EventDefinitionDto.cs
5. Models/EventActionDto.cs
6. Views/ActionConfigPanel.xaml       ← UserControl: hiển thị config panel cho từng action
7. ViewModels/ActionItems/ShowHideFieldActionViewModel.cs
8. ViewModels/ActionItems/SetReadOnlyActionViewModel.cs
9. ViewModels/ActionItems/SetValueActionViewModel.cs
10. ViewModels/ActionItems/CalculateActionViewModel.cs
11. ViewModels/ActionItems/CallApiActionViewModel.cs

─── LAYOUT: 3 BƯỚC ──────────────────────────────────────

DockPanel:
┌── Header (Top, 52px) ────────────────────────────────────────────────────────┐
│  ← Back  │  Event #18  │  Form: PURCHASE_ORDER  │  Status: [● Active]       │
└──────────────────────────────────────────────────────────────────────────────┘

┌── Content (Fill) — 3 Grid Rows với dividers ─────────────────────────────────┐

STEP 1: TRIGGER (Card, Height=Auto)
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ 🔵 Bước 1: Trigger — Khi nào event này được kích hoạt?                 │
  │  Scope:    ● Field-level  ○ Form-level                                  │
  │  Field:    [TrangThai                    ▼]                             │
  │  Trigger:  [OnChange                     ▼]  OnChange/OnLoad/OnBlur/   │
  │                                               OnFocus/OnSubmit          │
  │  Order No: [  1  ]   (thứ tự nếu field có nhiều event cùng trigger)    │
  └─────────────────────────────────────────────────────────────────────────┘

STEP 2: CONDITION (Card, Height=Auto)
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ 🟡 Bước 2: Condition — Điều kiện để thực thi Actions                   │
  │  Bỏ trống = luôn thực thi khi trigger xảy ra                           │
  │                                                                          │
  │  Preview: [TrangThai == "TuChoi"                    ]  (readonly)      │
  │  Status:  ✅ Valid (return type: Boolean)                               │
  │  [🌳 Edit Condition]  [✕ Clear]                                        │
  └─────────────────────────────────────────────────────────────────────────┘

STEP 3: ACTION CHAIN (Card, Fill)
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ 🟢 Bước 3: Actions — Chuỗi hành động thực thi theo thứ tự              │
  │  [+ Add Action ▼]  (dropdown: ShowField|HideField|SetReadOnly|          │
  │                               SetValue|Calculate|CallAPI)               │
  │  ───────────────────────────────────────────────────────────────────── │
  │  ItemsControl (Actions):                                                │
  │                                                                          │
  │  ┌─ Action #1 ─────────────────────────────────── [↑][↓] [🗑] ────────┐│
  │  │  [ShowField ▼]  ← ComboBox chọn action type                        ││
  │  │  ─────────────────────────────────────────────────────────────────  ││
  │  │  ActionConfigPanel (DataTemplate theo ActionType):                  ││
  │  │    Target Field: [LyDoTuChoi               ▼]                      ││
  │  └───────────────────────────────────────────────────────────────────┘│
  │                                                                          │
  │  ┌─ Action #2 ─────────────────────────────────── [↑][↓] [🗑] ────────┐│
  │  │  [SetReadOnly ▼]                                                    ││
  │  │  Field: [MaDonHang ▼]   ReadOnly: [● True  ○ False]                ││
  │  └───────────────────────────────────────────────────────────────────┘│
  │                                                                          │
  │  ┌─ Action #3 ─────────────────────────────────── [↑][↓] [🗑] ────────┐│
  │  │  [Calculate ▼]                                                      ││
  │  │  Target Field: [ThanhTien ▼]                                        ││
  │  │  Expression:   [SoLuong * DonGia          ]  (readonly preview)    ││
  │  │  ✅ Valid  [🌳 Edit Expression]                                      ││
  │  └───────────────────────────────────────────────────────────────────┘│
  └─────────────────────────────────────────────────────────────────────────┘

┌── Footer (52px) ─────────────────────────────────────────────────────────────┐
│  [💾 Validate & Save]  [↩ Cancel]  [▶ Simulate Event]  [📋 Duplicate]       │
└──────────────────────────────────────────────────────────────────────────────┘

─── MODELS ──────────────────────────────────────────────

public class EventDefinitionDto
{
    public int EventId { get; set; }
    public int FormId { get; set; }
    public int? FieldId { get; set; }
    public string FieldCode { get; set; } = "";
    public string TriggerCode { get; set; } = "";         // OnChange|OnLoad|OnBlur|OnFocus|OnSubmit
    public string? ConditionExpr { get; set; }
    public int OrderNo { get; set; }
    public bool IsActive { get; set; } = true;
    public List<EventActionDto> Actions { get; set; } = new();
}

public class EventActionDto : BindableBase
{
    public int ActionId { get; set; }
    public int EventId { get; set; }
    private string _actionCode = "";
    public string ActionCode { get => _actionCode; set => SetProperty(ref _actionCode, value); }
    public string? ActionParamJson { get; set; }         // JSON theo từng ActionType
    public int OrderNo { get; set; }

    // Runtime helper (không lưu DB):
    public string ActionPreview => BuildPreview();        // "ShowField: LyDoTuChoi"
    private string BuildPreview() { ... }
}

─── ACTION CONFIG VIEW MODELS ───────────────────────────

// ShowField / HideField (dùng chung ViewModel)
public class ShowHideFieldActionViewModel : BindableBase
{
    public string FieldCode { get; set; }          // Column_Code của field target
    public List<string> AvailableFields { get; set; }  // tất cả field trong form
    public string ActionParamJson => $"{{\"fieldCode\":\"{FieldCode}\"}}";
    public void LoadFromJson(string? json) { ... }
}

// SetReadOnly
public class SetReadOnlyActionViewModel : BindableBase
{
    public string FieldCode { get; set; }
    public bool IsReadOnly { get; set; }
    public string ActionParamJson => $"{{\"fieldCode\":\"{FieldCode}\",\"readOnly\":{IsReadOnly.ToString().ToLower()}}}";
}

// SetValue — chỉ Literal hoặc Identifier (không phức tạp)
public class SetValueActionViewModel : BindableBase
{
    public string TargetFieldCode { get; set; }
    public string ValueMode { get; set; } = "Literal";  // "Literal" | "Field"
    public string LiteralValue { get; set; } = "";
    public string LiteralNetType { get; set; } = "String";
    public string SourceFieldCode { get; set; } = "";   // khi ValueMode=Field
    public string ActionParamJson { get; }               // serialize tùy ValueMode
    // Validate: ValueMode=Literal → chỉ Literal node
    //           ValueMode=Field   → chỉ Identifier node
}

// Calculate — full AST expression
public class CalculateActionViewModel : BindableBase
{
    public string TargetFieldCode { get; set; }
    public string ExpressionJson { get; set; } = "";
    public string ExpressionPreview { get; set; } = "";  // natural language
    public bool IsExpressionValid { get; set; }
    public DelegateCommand OpenExpressionBuilderCommand { get; }
    public string ActionParamJson => $"{{\"targetField\":\"{TargetFieldCode}\",\"expression\":{ExpressionJson}}}";
}

// CallAPI — phức tạp nhất
public class CallApiActionViewModel : BindableBase
{
    public string Url { get; set; } = "";              // hỗ trợ {FieldCode} placeholder
    public string Method { get; set; } = "GET";        // GET|POST|PUT
    public int TimeoutMs { get; set; } = 5000;
    public string? BodyExprJson { get; set; }          // chỉ cho POST/PUT
    public ObservableCollection<ResponseMappingItem> Mappings { get; set; } = new();
    // UI: DataGrid với 2 cột: FieldCode (ComboBox) | ResponsePath (TextBox)
    public string ActionParamJson { get; }             // serialize toàn bộ

    // Validate URL placeholders: extract {xxx} → check tồn tại trong form fields
    public bool HasInvalidPlaceholders { get; }
    public List<string> InvalidPlaceholders { get; }
}

public class ResponseMappingItem : BindableBase
{
    public string FieldCode { get; set; } = "";        // target field
    public string ResponsePath { get; set; } = "";     // dot notation path
}

─── EVENT EDITOR VIEWMODEL ──────────────────────────────

Properties:
  - Event: EventDefinitionDto
  - TriggerOptions: List<string>           // OnChange, OnLoad, OnBlur, OnFocus, OnSubmit
  - FieldOptions: List<FieldSelectItem>    // tất cả field trong form
  - Actions: ObservableCollection<EventActionDto>
  - ConditionPreview: string
  - IsConditionValid: bool
  - IsDirty: bool

Commands:
  - LoadEventCommand: DelegateCommand
  - AddActionCommand: DelegateCommand<string>   ← param = ActionCode
  - RemoveActionCommand: DelegateCommand<EventActionDto>
  - MoveActionUpCommand: DelegateCommand<EventActionDto>
  - MoveActionDownCommand: DelegateCommand<EventActionDto>
  - EditConditionCommand: DelegateCommand        ← mở ExpressionBuilderDialog
  - ClearConditionCommand: DelegateCommand
  - SaveEventCommand: DelegateCommand
  - SimulateEventCommand: DelegateCommand        ← mở SimulateEventDialog
  - DuplicateEventCommand: DelegateCommand

─── ACTION CONFIG PANEL (UserControl) ───────────────────

ActionConfigPanel.xaml nhận DataContext = EventActionDto.
Dùng DataTemplateSelector để chọn DataTemplate theo ActionCode:
  "ShowField" | "HideField" → ShowHideFieldTemplate
  "SetReadOnly"             → SetReadOnlyTemplate
  "SetValue"                → SetValueTemplate
  "Calculate"               → CalculateTemplate
  "CallAPI"                 → CallApiTemplate

Mỗi template bind vào ViewModel tương ứng được tạo khi chọn ActionCode.

─── SIMULATE EVENT DIALOG ───────────────────────────────

SimulateEventDialog (Window hoặc MaterialDesign Dialog):
  - Form nhỏ để nhập giá trị cho các field liên quan đến event
  - Nút [▶ Run Simulation]
  - Kết quả hiển thị dạng UI Delta:
    { type: "visibility", fieldCode: "LyDoTuChoi", visible: true }
    { type: "readonly",   fieldCode: "MaDonHang",  readOnly: true }
    { type: "value",      fieldCode: "ThanhTien",  value: 1500000 }
  - Chỉ simulate local (không gọi backend), dùng mock evaluator

─── CONSTRAINTS ─────────────────────────────────────────

- Action order tự cập nhật khi add/remove/move (renumber 1,2,3...)
- Khi ActionCode thay đổi trong 1 action → reset ActionParamJson, tạo ViewModel mới
- Khi Scope = "Form-level": ẩn Field picker, TriggerOptions = chỉ "OnSubmit", "OnLoad"
- CallAPI: URL field có autocomplete cho {FieldCode} placeholders
- Calculate: nút OpenExpressionBuilder pass TargetField.NetType làm expectedReturnType
```
