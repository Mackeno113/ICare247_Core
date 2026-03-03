# PROMPT 07 — Expression Builder Dialog (Screen 07)

> **Project:** ICare247.ConfigStudio.Modules.Grammar
> **Công cụ dùng chung — mở từ Rule Editor và Event Editor.**

---

## PROMPT

```
Đọc CLAUDE.md trước. Đặc biệt chú ý phần "Grammar V1 AST Node Types" và "Operators".

Implement ExpressionBuilderDialog trong ICare247.ConfigStudio.Modules.Grammar.
Đây là dialog dùng chung, mở từ ValidationRuleEditor và EventEditor.

─── FILES CẦN TẠO ───────────────────────────────────────

1. GrammarModule.cs
2. Views/ExpressionBuilderDialog.xaml + .xaml.cs    ← Prism IDialogAware
3. ViewModels/ExpressionBuilderDialogViewModel.cs
4. Models/AstNode.cs                                ← C# model cho AST node
5. Models/AstNodeViewModel.cs                       ← ViewModel bọc AstNode (cho TreeView)
6. Models/PaletteItem.cs                            ← item trong Palette (operator/function/field)
7. Services/AstSerializer.cs                        ← AstNodeViewModel → JSON string
8. Services/AstDeserializer.cs                      ← JSON string → AstNodeViewModel tree
9. Services/AstValidator.cs                         ← validate AST tree
10. Services/AstNaturalLanguage.cs                  ← AstNode → human readable string

─── DIALOG LAYOUT: 3 VÙNG ───────────────────────────────

Window (800x600, ResizeMode=CanResize):
┌─ TitleBar ────────────────────────────────────────────────────────────────────┐
│  "Expression Builder"  Context: [SoLuong | Rule: Numeric | Expected: Boolean] │
└───────────────────────────────────────────────────────────────────────────────┘
┌─ Main (Grid 3 Columns) ───────────────────────────────────────────────────────┐

COL 1: Palette (Width=200, Background=Dark)
  ┌─ Operators ─────────────────────────────────────────────────────────────────┐
  │  Header: "TOÁN TỬ"                                                          │
  │  WrapPanel of PaletteButtons:                                               │
  │    [==] [!=] [>] [>=] [<] [<=]  ← row 1: comparison                       │
  │    [&&] [||] [!]                 ← row 2: logical                          │
  │    [+]  [-]  [*]  [/]  [%]  [??] ← row 3: arithmetic                      │
  │  Hover tooltip: "Greater than or equal" + precedence level                  │
  └─────────────────────────────────────────────────────────────────────────────┘
  ┌─ Functions ─────────────────────────────────────────────────────────────────┐
  │  Header: "HÀM" + SearchBox                                                  │
  │  ListBox (từ Gram_Function, Is_Active=true):                                │
  │    len(str)                                                                  │
  │    trim(str)                                                                 │
  │    regex(val, pat)                                                           │
  │    round(val, dec)                                                           │
  │    iif(cond, t, f)                                                           │
  │    isNull(val)                                                               │
  │    dateDiff(d1,d2,unit)                                                      │
  │    ...                                                                       │
  │  Click → insert FunctionNode vào vị trí đang chọn trong tree               │
  │  Tooltip: function description + signature + example                        │
  └─────────────────────────────────────────────────────────────────────────────┘
  ┌─ Fields ────────────────────────────────────────────────────────────────────┐
  │  Header: "FIELD (Form Context)"                                             │
  │  ListBox:                                                                   │
  │    SoLuong      (Int32)                                                     │
  │    DonGia       (Decimal)                                                   │
  │    ThanhTien    (Decimal)                                                   │
  │    TrangThai    (String)                                                     │
  │    NgayDatHang  (DateTime)                                                  │
  │  Click → insert IdentifierNode vào vị trí đang chọn                        │
  └─────────────────────────────────────────────────────────────────────────────┘

COL 2 (GridSplitter) ← width=5

COL 3 (Right Panel):
  ┌─ AST Tree (Fill) ───────────────────────────────────────────────────────────┐
  │  TreeView (ItemsSource=RootNode, HierarchicalDataTemplate):                 │
  │                                                                              │
  │  Mỗi AstNodeViewModel hiển thị:                                            │
  │    - Icon theo type (Binary=⚙, Identifier=📌, Literal=📄, Function=ƒ)     │
  │    - Text mô tả (vd: "Binary: &&", "Identifier: SoLuong", "Literal: 1")   │
  │    - Nếu đang selected: highlight + hiện edit panel bên dưới tree          │
  │                                                                              │
  │  RIGHT-CLICK context menu:                                                  │
  │    - Delete node                                                            │
  │    - Wrap in Binary (chọn operator)                                         │
  │    - Wrap in Unary (!)                                                      │
  │    - Wrap in Function (chọn function)                                       │
  │    - Replace with Literal                                                   │
  │    - Replace with Identifier                                                │
  └─────────────────────────────────────────────────────────────────────────────┘
  ┌─ Node Editor (Height=120, hiện khi có node được chọn) ─────────────────────┐
  │  [Literal]:    Value: [  1  ]  NetType: [Int32  ▼]                         │
  │  [Identifier]: Field: [SoLuong  ▼]                                         │
  │  [Binary]:     Operator: [>=  ▼]                                           │
  │  [Function]:   readonly — edit qua args                                     │
  └─────────────────────────────────────────────────────────────────────────────┘
  ┌─ Preview & Validation (Height=100) ─────────────────────────────────────────┐
  │  Natural Language: SoLuong >= 1 AND SoLuong <= 9999                        │
  │  ✅ Return type: Boolean   ✅ Depth: 3/20   ✅ All functions OK            │
  │  [❌ lỗi nếu có: "Function 'xxx' không tồn tại trong whitelist"]           │
  └─────────────────────────────────────────────────────────────────────────────┘
  ┌─ JSON Output (Expander, collapsed) ─────────────────────────────────────────┐
  │  { "type": "Binary", "operator": "&&", ... }                               │
  │  [📋 Copy]                                                                  │
  └─────────────────────────────────────────────────────────────────────────────┘

┌─ Footer ──────────────────────────────────────────────────────────────────────┐
│  [✅ Apply Expression]  [↩ Cancel]  [🔄 Reset]  [📋 Copy JSON]               │
│  Chỉ enable Apply khi IsValid=true                                            │
└───────────────────────────────────────────────────────────────────────────────┘

─── AST NODE MODELS ─────────────────────────────────────

public enum AstNodeType { Literal, Identifier, Binary, Unary, Function, CustomHandler }

public class AstNode
{
    public AstNodeType Type { get; set; }
    // Literal:
    public object? Value { get; set; }
    public string NetType { get; set; } = "";        // String|Int32|Decimal|Boolean|DateTime
    // Identifier:
    public string Name { get; set; } = "";           // Column_Code
    // Binary:
    public string Operator { get; set; } = "";
    public AstNode? Left { get; set; }
    public AstNode? Right { get; set; }
    // Unary:
    public AstNode? Operand { get; set; }
    // Function:
    public string FunctionName { get; set; } = "";
    public List<AstNode> Arguments { get; set; } = new();
}

public class AstNodeViewModel : BindableBase
{
    public AstNode Node { get; set; } = null!;
    public ObservableCollection<AstNodeViewModel> Children { get; set; } = new();
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public bool IsExpanded { get; set; } = true;
    public string DisplayText { get; }              // computed: "Binary: &&", "Literal: 1 (Int32)"
    public string IconKind { get; }                 // MaterialDesign PackIconKind
    public int Depth { get; set; }
}

─── SERVICES ────────────────────────────────────────────

// AstSerializer: AstNodeViewModel tree → JSON string
public class AstSerializer
{
    public string Serialize(AstNodeViewModel root) { ... }
    // Output: đúng format Grammar V1 JSON như spec trong CLAUDE.md
}

// AstDeserializer: JSON string → AstNodeViewModel tree
public class AstDeserializer
{
    public AstNodeViewModel? Deserialize(string? json) { ... }
    // Nếu json null/empty → trả về null (blank builder)
}

// AstValidator
public class AstValidator
{
    // availableFunctions: từ Gram_Function, availableFields: từ Ui_Field của form
    public AstValidationResult Validate(
        AstNodeViewModel? root,
        string expectedReturnType,          // "Boolean" cho condition/rule, hoặc netType cho calculate
        List<string> availableFunctions,
        List<string> availableOperators,
        List<string> availableFields,
        int maxDepth = 20)
    { ... }
}

public class AstValidationResult
{
    public bool IsValid { get; set; }
    public string? ActualReturnType { get; set; }
    public int Depth { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> ReferencedFields { get; set; } = new();  // cho Sys_Dependency
}

// AstNaturalLanguage
public class AstNaturalLanguage
{
    public string ToText(AstNodeViewModel? root) { ... }
    // Binary && → "({left} VÀ {right})"
    // Binary >= → "{left} >= {right}"
    // Identifier → field name
    // Literal    → value
    // Function regex → "{arg0} khớp pattern {arg1}"
    // Function iif  → "NẾU {arg0} THÌ {arg1} KHÔNG THÌ {arg2}"
}

─── DIALOG VIEWMODEL ────────────────────────────────────

public class ExpressionBuilderDialogViewModel : BindableBase, IDialogAware
{
    // IDialogAware implementation
    public string Title => "Expression Builder";
    public event Action<IDialogResult>? RequestClose;
    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // Nhận từ caller:
        var json = parameters.GetValue<string?>("expressionJson");
        var expectedType = parameters.GetValue<string>("expectedReturnType");  // "Boolean" | NetType
        var formFields = parameters.GetValue<List<FieldInfo>>("formFields");
        var formFunctions = parameters.GetValue<List<string>>("availableFunctions");

        // Init:
        ExpectedReturnType = expectedType;
        AvailableFields = formFields;
        AvailableFunctions = formFunctions;
        RootNode = _deserializer.Deserialize(json);
        ValidateExpression();
    }

    // Properties:
    public AstNodeViewModel? RootNode { get; set; }
    public AstNodeViewModel? SelectedNode { get; set; }   // đang chọn để edit
    public string ExpectedReturnType { get; set; } = "";
    public List<FieldInfo> AvailableFields { get; set; } = new();
    public List<FunctionPaletteItem> AvailableFunctions { get; set; } = new();
    public List<string> AvailableOperators { get; } = new() { "==","!=",">",">=","<","<=","&&","||","!","+","-","*","/","%","??" };
    public string NaturalLanguagePreview { get; set; } = "";
    public AstValidationResult? ValidationResult { get; set; }
    public bool IsValid => ValidationResult?.IsValid ?? false;
    public string JsonOutput { get; set; } = "";

    // Commands:
    public DelegateCommand<PaletteItem> InsertOperatorCommand { get; }
    public DelegateCommand<PaletteItem> InsertFunctionCommand { get; }
    public DelegateCommand<PaletteItem> InsertFieldCommand { get; }
    public DelegateCommand DeleteSelectedNodeCommand { get; }
    public DelegateCommand WrapInBinaryCommand { get; }   ← mở mini-picker operator
    public DelegateCommand WrapInUnaryCommand { get; }
    public DelegateCommand ApplyCommand { get; }          ← CanExecute = IsValid
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ResetCommand { get; }
    public DelegateCommand CopyJsonCommand { get; }

    private void ValidateExpression()
    {
        ValidationResult = _validator.Validate(RootNode, ExpectedReturnType, ...);
        NaturalLanguagePreview = _naturalLanguage.ToText(RootNode);
        JsonOutput = RootNode != null ? _serializer.Serialize(RootNode) : "";
        RaisePropertyChanged(nameof(IsValid));
        RaisePropertyChanged(nameof(ValidationResult));
    }

    private void ApplyExpression()
    {
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("expressionJson", JsonOutput);
        result.Parameters.Add("referencedFields", ValidationResult?.ReferencedFields);
        RequestClose?.Invoke(result);
    }
}

─── CÁCH MỞ DIALOG TỪ RULE EDITOR ──────────────────────

// Trong ValidationRuleEditorViewModel:
private void OpenExpressionBuilder()
{
    var p = new DialogParameters
    {
        { "expressionJson", ExpressionJson },
        { "expectedReturnType", "Boolean" },
        { "formFields", AvailableFields.Select(f => new FieldInfo(f.Code, f.NetType)).ToList() },
        { "availableFunctions", _grammarFunctions }
    };
    _dialogService.ShowDialog("ExpressionBuilderDialog", p, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            ExpressionJson = result.Parameters.GetValue<string>("expressionJson");
            ValidateExpression();
        }
    });
}

─── CONSTRAINT ──────────────────────────────────────────

- Insert operator vào tree:
    Nếu SelectedNode != null → replace SelectedNode bằng BinaryNode{op, left=SelectedNode, right=placeholder}
    Nếu root == null → tạo BinaryNode mới với 2 placeholder
- Insert field (Identifier) → replace selected node hoặc thêm vào argument trống gần nhất
- Literal editing: double-click node Literal → hiện inline editor trong Node Editor panel
- TreeView selection → tự động cập nhật Node Editor panel
- Mỗi thay đổi → gọi ValidateExpression() để cập nhật preview và validation status
- Apply button chỉ enable khi IsValid=true
```
