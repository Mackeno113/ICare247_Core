# PROMPT 05 — Validation Rule Editor (Screen 05)

> **Project:** ICare247.ConfigStudio.Modules.Rules
> **Màn hình quan trọng nhất — cấu hình rule cho control.**

---

## PROMPT

```
Đọc CLAUDE.md trước. Đặc biệt chú ý phần "6 Rule Types" và "Grammar V1 AST Node Types".

Implement ValidationRuleEditorView trong ICare247.ConfigStudio.Modules.Rules.

─── FILES CẦN TẠO ───────────────────────────────────────

1. RulesModule.cs
2. Views/ValidationRuleEditorView.xaml + .xaml.cs
3. ViewModels/ValidationRuleEditorViewModel.cs
4. Models/RuleTypeOption.cs
5. Models/ValidationRuleDto.cs
6. ViewModels/QuickConfig/NumericQuickConfigViewModel.cs
7. ViewModels/QuickConfig/RegexQuickConfigViewModel.cs
8. ViewModels/QuickConfig/CompareQuickConfigViewModel.cs
9. ViewModels/QuickConfig/CustomHandlerQuickConfigViewModel.cs

─── LAYOUT ──────────────────────────────────────────────

DockPanel:
┌── Header (Top, 52px) ────────────────────────────────────────────────────────┐
│  ← Back  │  Rule #42  │  Field: SoLuong  │  Status: [● Active]              │
└──────────────────────────────────────────────────────────────────────────────┘
┌── Content (Fill) — TabControl 3 Tabs ────────────────────────────────────────┐

TAB 1: "⚙ Rule Setup"
  ┌─ Card "Rule Type" ───────────────────────────────────────────────────────┐
  │  ○ Required  ● Numeric  ○ Regex  ○ Compare  ○ Conditional  ○ Custom     │
  │  Mô tả nhỏ dưới selection: "Kiểm tra giá trị số nằm trong khoảng cho phép"│
  └─────────────────────────────────────────────────────────────────────────┘

  ┌─ Card "Quick Config" (ẩn/hiện theo Rule Type) ──────────────────────────┐
  │  [NUMERIC]: ─────────────────────────────────────────────────────────── │
  │    Chế độ:  ● Cả 2  ○ Chỉ Min  ○ Chỉ Max                              │
  │    Min Value: [    1    ]   Max Value: [  9999  ]                        │
  │    [⚡ Auto-generate Expression →]                                       │
  │                                                                          │
  │  [REGEX]: ─────────────────────────────────────────────────────────────│
  │    Pattern:     [^(0|\+84)[0-9]{9}$                        ]           │
  │    Mô tả:       [Số điện thoại VN 10 số, bắt đầu 0/+84    ]           │
  │    Ví dụ mẫu:   [0912345678                                ]           │
  │    [Test Pattern]  kết quả test hiển thị inline                         │
  │    Patterns phổ biến: [SĐT VN] [Email] [Mã số thuế] [Số nguyên dương] │
  │    [⚡ Auto-generate Expression →]                                       │
  │                                                                          │
  │  [COMPARE]: ───────────────────────────────────────────────────────────│
  │    Field hiện tại: SoLuong (Int32)                                      │
  │    Toán tử: [>=  ▼]  (==, !=, >, >=, <, <=)                           │
  │    So sánh với: ● Field khác  ○ Giá trị cố định                        │
  │      → Field: [NgayBatDau     ▼]  (chỉ hiện field cùng/compatible type)│
  │      → Value: [0              ]   netType: [Int32  ▼]                  │
  │    ⚠️ Note: So sánh cross-field sẽ tạo Sys_Dependency tự động           │
  │    [⚡ Auto-generate Expression →]                                       │
  │                                                                          │
  │  [CUSTOM]: ────────────────────────────────────────────────────────────│
  │    Handler Code: [PhoneVN     ▼]  (load từ registered handlers)        │
  │    Config JSON:  [{                                                      │
  │                    "allowInternational": true                           │
  │                  }                                  ]  (TextBox)       │
  │    [⚡ Auto-generate Expression →]                                       │
  │                                                                          │
  │  [REQUIRED]: ──────────────────────────────────────────────────────────│
  │    Không cần config thêm.                                               │
  │    Expression_Json = NULL (engine tự xử lý null/empty check)           │
  │                                                                          │
  │  [CONDITIONAL]: ───────────────────────────────────────────────────────│
  │    → Chuyển sang Tab 2 để viết expression trực tiếp                    │
  │    Note: Conditional type yêu cầu Condition_Expr bắt buộc              │
  └─────────────────────────────────────────────────────────────────────────┘

TAB 2: "🌳 Expression"
  ┌─ Card "Expression (Grammar V1)" ────────────────────────────────────────┐
  │  Preview (ngôn ngữ tự nhiên):                                           │
  │  [SoLuong >= 1 AND SoLuong <= 9999                     ] (readonly)    │
  │                                                                          │
  │  [🌳 Open Full Expression Builder]  ← mở ExpressionBuilderDialog       │
  │                                                                          │
  │  Validation Status:                                                      │
  │    ✅ Return type: Boolean          ✅ AST depth: 3/20                  │
  │    ✅ All functions whitelisted     ✅ All identifiers found             │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌─ Expander "Condition Expression" (khi nào rule này chạy) ───────────────┐
  │  Bỏ trống = luôn chạy                                                   │
  │  Preview: [(empty — rule runs unconditionally)            ] (readonly)  │
  │  [🌳 Edit Condition]  [✕ Clear Condition]                               │
  │  Condition Validation: [chưa có condition]                              │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌─ Expander "JSON AST Raw" (collapsed mặc định) ──────────────────────────┐
  │  TextBox readonly: { "type": "Binary", "operator": "&&", ... }          │
  │  [📋 Copy JSON]                                                         │
  └─────────────────────────────────────────────────────────────────────────┘

TAB 3: "💬 Error Message"
  ┌─ Card "Error Key & Preview" ────────────────────────────────────────────┐
  │  Error Key:  [err.soluong.range                              ]          │
  │  Format tip: err.[fieldname].[ruletype]                                 │
  │                                                                          │
  │  Bản dịch:                                                              │
  │    🇻🇳 VI:  [Số lượng phải từ 1 đến 9999             ] ← editable     │
  │    🇬🇧 EN:  [Quantity must be between 1 and 9999      ] ← editable     │
  │  [💾 Save i18n]  [🌐 Open i18n Manager]                                │
  │                                                                          │
  │  ⚠️ Hỗ trợ placeholder: {0}=Min, {1}=Max (tùy rule type)               │
  └─────────────────────────────────────────────────────────────────────────┘

┌── Dependency Panel (Bottom, 60px, chỉ hiện khi có cross-field ref) ──────────┐
│  ⓘ Rule này tham chiếu field: [NgayBatDau] → Sys_Dependency sẽ được tạo     │
│  khi Save. Engine sẽ re-validate rule này khi NgayBatDau thay đổi.          │
└──────────────────────────────────────────────────────────────────────────────┘
┌── Footer (52px) ─────────────────────────────────────────────────────────────┐
│  [💾 Validate & Save]   [↩ Cancel]   [📋 Duplicate Rule]   [🗑 Delete]      │
└──────────────────────────────────────────────────────────────────────────────┘

─── MODEL ───────────────────────────────────────────────

public class ValidationRuleDto
{
    public int RuleId { get; set; }
    public string RuleTypeCode { get; set; } = "";     // Required|Numeric|Regex|Compare|Conditional|Custom
    public string ErrorKey { get; set; } = "";
    public string? ExpressionJson { get; set; }        // Grammar V1 JSON AST
    public string? ConditionExpr { get; set; }         // Grammar V1 JSON AST
    public bool IsActive { get; set; } = true;
    // Navigation
    public int FieldId { get; set; }
    public int OrderNo { get; set; }
}

─── VIEWMODEL ───────────────────────────────────────────

Properties:
  - Rule: ValidationRuleDto
  - RuleTypeOptions: List<RuleTypeOption>
  - SelectedRuleType: RuleTypeOption
    OnPropertyChanged:
      → cập nhật mô tả rule type
      → ẩn/hiện QuickConfig panel tương ứng
      → reset ExpressionJson nếu đổi type

  // Quick Config ViewModels (chỉ 1 active tại 1 thời điểm):
  - NumericConfig: NumericQuickConfigViewModel
  - RegexConfig: RegexQuickConfigViewModel
  - CompareConfig: CompareQuickConfigViewModel
  - CustomConfig: CustomHandlerQuickConfigViewModel

  - ExpressionJson: string?
  - ExpressionPreview: string                          ← natural language từ AST
  - IsExpressionValid: bool
  - ExpressionValidationErrors: ObservableCollection<string>

  - ConditionExpr: string?
  - ConditionPreview: string
  - IsConditionValid: bool

  - ErrorKey: string
  - ViTranslation: string
  - EnTranslation: string

  - HasCrossFieldReference: bool                       ← true khi expression có Identifier != field hiện tại
  - ReferencedFields: List<string>                     ← extract từ AST

  - FieldId: int
  - FieldCode: string
  - FormId: int
  - AvailableFields: List<string>                      ← tất cả fields trong form (cho Compare)

Commands:
  - LoadRuleCommand: DelegateCommand
  - AutoGenerateExpressionCommand: DelegateCommand
      → dựa trên SelectedRuleType + QuickConfig values:
        Numeric  → build Binary && AST từ min/max
        Regex    → build Function "regex" AST
        Compare  → build Binary operator AST
        Custom   → build CustomHandler JSON
  - OpenExpressionBuilderCommand: DelegateCommand
      → mở ExpressionBuilderDialog với ExpressionJson hiện tại
      → nhận về JSON mới sau khi dialog confirm
  - OpenConditionBuilderCommand: DelegateCommand
  - ClearConditionCommand: DelegateCommand
  - ValidateExpressionCommand: DelegateCommand        ← parse + validate AST
  - SaveRuleCommand: DelegateCommand
      → validate expression trước khi save
      → nếu HasCrossFieldReference → tạo Sys_Dependency records
  - CancelCommand: DelegateCommand
  - DuplicateRuleCommand: DelegateCommand
  - DeleteRuleCommand: DelegateCommand

─── AUTO-GENERATE LOGIC (trong ViewModel) ───────────────

// Numeric: SoLuong >= 1 && SoLuong <= 9999
private string GenerateNumericExpression()
{
    var field = FieldCode;  // "SoLuong"
    if (NumericConfig.Mode == "Both")
        return JsonSerializer.Serialize(new {
            type = "Binary", @operator = "&&",
            left = new { type="Binary", @operator=">=",
                left=new{type="Identifier",name=field},
                right=new{type="Literal",value=NumericConfig.MinValue,netType="Int32"}},
            right = new { type="Binary", @operator="<=",
                left=new{type="Identifier",name=field},
                right=new{type="Literal",value=NumericConfig.MaxValue,netType="Int32"}}
        });
    // ... handle MinOnly / MaxOnly cases
}

// Regex: regex(SoDienThoai, "^(0|\+84)[0-9]{9}$")
private string GenerateRegexExpression() { ... }

// Compare: NgayKetThuc >= NgayBatDau
private string GenerateCompareExpression() { ... }

─── EXPRESSION PREVIEW (natural language) ───────────────

Implement ExpressionToNaturalLanguage(string json) → string:
  Binary &&  → "A VÀ B"
  Binary ||  → "A HOẶC B"
  Binary >=  → "A >= B"
  Binary ==  → "A == B"
  Identifier → field name (bôi đậm nếu có thể)
  Literal    → giá trị thực
  Function regex(X, Y) → "X khớp pattern Y"

─── CONSTRAINT ──────────────────────────────────────────

- Nút Save bị disabled nếu:
  IsExpressionValid=false (trừ Required type)
  ErrorKey rỗng
- Khi SelectedRuleType = "Required": tab Expression ẩn, hiện message giải thích
- Khi SelectedRuleType = "Conditional": auto focus sang Tab 2
- RegexConfig có nút [Test Pattern]: chạy Regex.IsMatch với mẫu thử → hiện kết quả
```
