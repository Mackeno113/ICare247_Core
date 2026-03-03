# PROMPT 04 — Field Config (Screen 04)

> **Project:** ICare247.ConfigStudio.Modules.Forms
> **Chạy sau Prompt 03.**

---

## PROMPT

```
Đọc CLAUDE.md trước.

Implement FieldConfigView trong ICare247.ConfigStudio.Modules.Forms.
Đây là panel cấu hình chi tiết 1 field — mở từ FormEditor khi click [⚙] trên field.

─── FILES CẦN TẠO ───────────────────────────────────────

1. Views/FieldConfigView.xaml + .xaml.cs
2. ViewModels/FieldConfigViewModel.cs
3. Models/ColumnInfoDto.cs            ← thông tin column từ Sys_Column
4. Models/ControlPropDefinition.cs    ← định nghĩa 1 prop trong Control_Props_Json schema
5. Models/ControlPropValue.cs         ← giá trị thực tế của prop

─── LAYOUT: TABCONTROL 4 TABS ───────────────────────────

DockPanel:
┌── Header (Top, 52px) ────────────────────────────────────────────────────────┐
│  ← Back to Form Editor  │  Field: [SoLuong]  [Section: Chi Tiết]            │
│  Breadcrumb badge: Form > Section > Field                                    │
└──────────────────────────────────────────────────────────────────────────────┘
┌── TabControl (Fill) ─────────────────────────────────────────────────────────┐
│  [📋 Basic]  [⚙ Control Props]  [✅ Validation Rules]  [⚡ Events]           │
│  ─────────────────────────────────────────────────────────────────────────── │

TAB 1: Basic
  ┌─ Card "Thông Tin Cơ Bản" ────────────────────────────────────────────────┐
  │  Column (DB):    [SoLuong (Int32, NOT NULL)       ▼]  [🔍 Browse]        │
  │  Net Type:       Int32  (readonly, từ column đã chọn)                    │
  │  Editor Type:    [NumericBox                       ▼]                    │
  │                  → Maps to control: "DevExpress DXNumericEdit"            │
  │  Order No:       [  1  ]                                                  │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌─ Card "Display" ─────────────────────────────────────────────────────────┐
  │  Label Key:      [lbl.soluong              ]  Preview: "Số Lượng"        │
  │  Placeholder Key:[ph.soluong               ]  Preview: "Nhập số lượng"   │
  │  Tooltip Key:    [tip.soluong              ]  Preview: ""                 │
  │  [🌐 Manage i18n Keys →]                                                 │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌─ Card "Behavior" ────────────────────────────────────────────────────────┐
  │  Is Visible:   [✓ Toggle]    Is ReadOnly:  [ Toggle]                     │
  │  Required:     [✓ Toggle]    ← shortcut tạo Required rule                │
  └─────────────────────────────────────────────────────────────────────────┘

TAB 2: Control Props
  Header mô tả: "Cấu hình thuộc tính cho Editor Type: NumericBox"
  ┌─ Dynamic Form (ItemsControl bind ControlProps) ──────────────────────────┐
  │  Per ControlPropDefinition: hiển thị label + input phù hợp với PropType: │
  │    - String  → TextBox                                                    │
  │    - Number  → NumericUpDown                                              │
  │    - Boolean → ToggleSwitch                                               │
  │    - Enum    → ComboBox với options từ AllowedValues                      │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌─ Expander "Raw JSON Preview" (collapsed mặc định) ──────────────────────┐
  │  TextBox readonly: { "minValue": 1, "maxValue": 99999, "decimals": 0 }   │
  └─────────────────────────────────────────────────────────────────────────┘

TAB 3: Validation Rules
  Toolbar: [+ Add Rule] [↑↓ Reorder]
  DataGrid (rules gắn vào field này):
    Order │ Rule Type   │ Expression Preview          │ Error Key       │ Active │ ⚙
    ──────────────────────────────────────────────────────────────────────────────
    1     │ Required    │ (built-in)                  │ err.fld.req     │  ✓    │ [⚙]
    2     │ Numeric     │ SoLuong >= 1 && SoLuong <= 9999 │ err.sl.range │ ✓   │ [⚙]
  Buttons per row:
    [⚙] → navigate sang ValidationRuleEditor: { "ruleId", "fieldId" }
    [🗑] → delete rule_field mapping
  [+ Add Rule] → navigate sang ValidationRuleEditor: { "fieldId", "mode": "new" }

TAB 4: Events
  DataGrid (events liên quan đến field này):
    Event ID │ Trigger     │ Condition Preview    │ Actions Count │ Active │ ⚙
    ─────────────────────────────────────────────────────────────────────────
    18       │ OnChange    │ TrangThai == "TuChoi" │  3 actions    │  ✓   │ [⚙]
  [+ Add Event] → navigate sang EventEditor: { "fieldId", "mode": "new" }

┌── Footer (Bottom, 52px) ─────────────────────────────────────────────────────┐
│  [💾 Save Field]   [↩ Cancel]   [Go to Validation Rules →]   [Go to Events →]│
└──────────────────────────────────────────────────────────────────────────────┘

─── MODELS ──────────────────────────────────────────────

public class ColumnInfoDto
{
    public int ColumnId { get; set; }
    public string ColumnCode { get; set; } = "";
    public string DataType { get; set; } = "";         // SQL type
    public string NetType { get; set; } = "";          // .NET type
    public bool IsNullable { get; set; }
    public string DisplayName => $"{ColumnCode} ({NetType}{(IsNullable ? ", NULL" : ", NOT NULL")})";
}

public class ControlPropDefinition
{
    public string PropName { get; set; } = "";
    public string PropType { get; set; } = "";          // String | Number | Boolean | Enum
    public string Label { get; set; } = "";
    public object? DefaultValue { get; set; }
    public List<string>? AllowedValues { get; set; }    // cho Enum type
    public string? Description { get; set; }
}

public class ControlPropValue : BindableBase
{
    public ControlPropDefinition Definition { get; set; } = null!;
    private object? _value;
    public object? Value { get => _value; set => SetProperty(ref _value, value); }
}

─── VIEWMODEL ───────────────────────────────────────────

Properties:
  - FieldId: int
  - FormId: int
  - SectionId: int
  - ColumnCode: string
  - AvailableColumns: ObservableCollection<ColumnInfoDto>     ← từ Sys_Column
  - SelectedColumn: ColumnInfoDto?
  - AvailableEditorTypes: List<string>                        ← từ Ui_Control_Map
  - SelectedEditorType: string (OnPropertyChanged → LoadControlPropSchema())
  - ControlProps: ObservableCollection<ControlPropValue>      ← dynamic
  - LabelKey, PlaceholderKey, TooltipKey: string
  - LabelPreview, PlaceholderPreview: string                  ← resolved i18n
  - OrderNo: int
  - IsVisible: bool
  - IsReadOnly: bool
  - IsRequired: bool                                          ← shortcut
  - LinkedRules: ObservableCollection<RuleSummaryDto>
  - LinkedEvents: ObservableCollection<EventSummaryDto>
  - ControlPropsJson: string                                  ← computed từ ControlProps
  - IsDirty: bool

Commands:
  - LoadFieldCommand: DelegateCommand        ← gọi trong OnNavigatedTo
  - SaveFieldCommand: DelegateCommand
  - CancelCommand: DelegateCommand
  - BrowseColumnCommand: DelegateCommand     ← mở popup chọn column từ Sys_Column
  - ManageI18nCommand: DelegateCommand       ← navigate sang I18nManager
  - AddRuleCommand: DelegateCommand
  - OpenRuleCommand: DelegateCommand<RuleSummaryDto>
  - AddEventCommand: DelegateCommand
  - OpenEventCommand: DelegateCommand<EventSummaryDto>

─── EDITOR TYPE → CONTROL PROP SCHEMA (mock) ───────────

LoadControlPropSchema() dựa trên SelectedEditorType:

"NumericBox":
  PropName=minValue,   PropType=Number,  Default=0,       Label="Giá trị tối thiểu"
  PropName=maxValue,   PropType=Number,  Default=999999,  Label="Giá trị tối đa"
  PropName=decimals,   PropType=Number,  Default=0,       Label="Số chữ số thập phân"
  PropName=spinStep,   PropType=Number,  Default=1,       Label="Bước nhảy"
  PropName=allowNull,  PropType=Boolean, Default=false,   Label="Cho phép rỗng"

"TextBox":
  PropName=maxLength,  PropType=Number,  Default=255,     Label="Độ dài tối đa"
  PropName=isMultiline,PropType=Boolean, Default=false,   Label="Nhiều dòng"
  PropName=rows,       PropType=Number,  Default=1,       Label="Số dòng (khi multiline)"

"ComboBox":
  PropName=dataSource, PropType=String,  Default="",      Label="API endpoint datasource"
  PropName=valueField, PropType=String,  Default="id",    Label="Field giá trị"
  PropName=displayField,PropType=String, Default="name",  Label="Field hiển thị"
  PropName=allowNull,  PropType=Boolean, Default=true,    Label="Cho phép rỗng"

"DatePicker":
  PropName=format,     PropType=Enum,    Default="dd/MM/yyyy",
    AllowedValues=["dd/MM/yyyy","MM/yyyy","yyyy"],        Label="Định dạng ngày"
  PropName=minDate,    PropType=String,  Default="",      Label="Ngày tối thiểu"
  PropName=maxDate,    PropType=String,  Default="",      Label="Ngày tối đa"

─── CONSTRAINTS ─────────────────────────────────────────

- Khi SelectedEditorType thay đổi → reload ControlProps từ schema mới
  → giữ lại giá trị cũ nếu PropName trùng, reset về Default nếu mới
- ControlPropsJson tự động update khi bất kỳ ControlPropValue.Value thay đổi
  → dùng PropertyChanged event trên từng item
- Tab 3 (Rules) và Tab 4 (Events) chỉ load khi tab đó được active (lazy load)
- IsRequired toggle: tự động tạo/xóa Required rule trong LinkedRules (chưa save DB)
```