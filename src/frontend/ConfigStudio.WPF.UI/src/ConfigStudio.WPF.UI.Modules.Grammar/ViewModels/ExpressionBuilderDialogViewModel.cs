// File    : ExpressionBuilderDialogViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel cho Expression Builder Dialog (Screen 07) — dùng chung từ Rule/Event Editor.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;
using ConfigStudio.WPF.UI.Modules.Grammar.Services;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;

/// <summary>
/// ViewModel cho Expression Builder Dialog (Screen 07).
/// Cung cấp giao diện kéo-thả/click để xây dựng AST expression.
/// Mở từ ValidationRuleEditor và EventEditor qua IDialogService.
/// Functions/Operators load từ Gram_Function/Gram_Operator qua IGrammarDataService.
/// </summary>
public sealed class ExpressionBuilderDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly IGrammarDataService _grammarService;
    private readonly AstSerializer _serializer = new();
    private readonly AstDeserializer _deserializer = new();
    private readonly AstValidator _validator = new();
    private readonly AstNaturalLanguage _naturalLanguage = new();

    // ── IDialogAware ─────────────────────────────────────────
    public string Title => "Expression Builder";
    public DialogCloseListener RequestClose { get; set; }

    // ── AST Tree ─────────────────────────────────────────────
    private AstNodeViewModel? _rootNode;
    public AstNodeViewModel? RootNode
    {
        get => _rootNode;
        set => SetProperty(ref _rootNode, value);
    }

    /// <summary>
    /// Collection bind vào TreeView — chứa 1 phần tử (root) hoặc rỗng.
    /// </summary>
    public ObservableCollection<AstNodeViewModel> RootNodes { get; } = [];

    private AstNodeViewModel? _selectedNode;
    public AstNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
                RaisePropertyChanged(nameof(HasSelectedNode));
        }
    }

    public bool HasSelectedNode => SelectedNode is not null;

    // ── Context ──────────────────────────────────────────────
    private string _expectedReturnType = "Boolean";
    public string ExpectedReturnType
    {
        get => _expectedReturnType;
        set => SetProperty(ref _expectedReturnType, value);
    }

    private string _contextInfo = "";
    public string ContextInfo
    {
        get => _contextInfo;
        set => SetProperty(ref _contextInfo, value);
    }

    // ── Palette ──────────────────────────────────────────────
    public ObservableCollection<PaletteItem> OperatorItems { get; } = [];
    public ObservableCollection<PaletteItem> FunctionItems { get; } = [];
    public ObservableCollection<PaletteItem> FieldItems { get; } = [];

    private string _functionSearch = "";
    public string FunctionSearch
    {
        get => _functionSearch;
        set
        {
            if (SetProperty(ref _functionSearch, value))
                FilterFunctions();
        }
    }

    public ObservableCollection<PaletteItem> FilteredFunctions { get; } = [];

    // ── Preview & Validation ─────────────────────────────────
    private string _naturalLanguagePreview = "(trống)";
    public string NaturalLanguagePreview
    {
        get => _naturalLanguagePreview;
        set => SetProperty(ref _naturalLanguagePreview, value);
    }

    private AstValidationResult? _validationResult;
    public AstValidationResult? ValidationResult
    {
        get => _validationResult;
        set
        {
            if (SetProperty(ref _validationResult, value))
                RaisePropertyChanged(nameof(IsValid));
        }
    }

    public bool IsValid => ValidationResult?.IsValid ?? false;

    private string _jsonOutput = "";
    public string JsonOutput
    {
        get => _jsonOutput;
        set => SetProperty(ref _jsonOutput, value);
    }

    // ── Loading state ────────────────────────────────────────
    private bool _isLoadingPalette;
    public bool IsLoadingPalette
    {
        get => _isLoadingPalette;
        set => SetProperty(ref _isLoadingPalette, value);
    }

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand<PaletteItem> InsertOperatorCommand { get; }
    public DelegateCommand<PaletteItem> InsertFunctionCommand { get; }
    public DelegateCommand<PaletteItem> InsertFieldCommand { get; }
    public DelegateCommand DeleteSelectedNodeCommand { get; }
    public DelegateCommand WrapInBinaryCommand { get; }
    public DelegateCommand WrapInUnaryCommand { get; }
    public DelegateCommand ApplyCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ResetCommand { get; }
    public DelegateCommand CopyJsonCommand { get; }

    // ── Danh sách whitelist cho validator ───────────────────
    private List<string> _availableFunctionCodes = [];
    private List<string> _availableFieldCodes = [];
    private List<string> _availableOperatorSymbols = [];

    public ExpressionBuilderDialogViewModel(IGrammarDataService grammarService)
    {
        _grammarService = grammarService;

        InsertOperatorCommand = new DelegateCommand<PaletteItem>(ExecuteInsertOperator);
        InsertFunctionCommand = new DelegateCommand<PaletteItem>(ExecuteInsertFunction);
        InsertFieldCommand = new DelegateCommand<PaletteItem>(ExecuteInsertField);
        DeleteSelectedNodeCommand = new DelegateCommand(ExecuteDeleteSelectedNode, () => HasSelectedNode)
            .ObservesProperty(() => HasSelectedNode);
        WrapInBinaryCommand = new DelegateCommand(ExecuteWrapInBinary, () => HasSelectedNode)
            .ObservesProperty(() => HasSelectedNode);
        WrapInUnaryCommand = new DelegateCommand(ExecuteWrapInUnary, () => HasSelectedNode)
            .ObservesProperty(() => HasSelectedNode);
        ApplyCommand = new DelegateCommand(ExecuteApply, () => IsValid)
            .ObservesProperty(() => IsValid);
        CancelCommand = new DelegateCommand(ExecuteCancel);
        ResetCommand = new DelegateCommand(ExecuteReset);
        CopyJsonCommand = new DelegateCommand(ExecuteCopyJson);
    }

    // ── IDialogAware ─────────────────────────────────────────

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // ── Nhận params từ caller (Rule/Event Editor) ────────
        var json = parameters.GetValue<string?>("expressionJson");
        ExpectedReturnType = parameters.GetValue<string>("expectedReturnType") ?? "Boolean";
        ContextInfo = parameters.GetValue<string>("contextInfo") ?? "";

        // ── Load fields từ caller (form context) ─────────────
        var fields = parameters.GetValue<List<FieldInfo>>("formFields");
        if (fields is not null)
        {
            FieldItems.Clear();
            _availableFieldCodes = [];
            foreach (var f in fields)
            {
                FieldItems.Add(new PaletteItem
                {
                    ItemType = PaletteItemType.Field,
                    DisplayName = f.DisplayName,
                    Code = f.Code,
                    NetType = f.NetType,
                    Description = $"Field: {f.Code} ({f.NetType})"
                });
                _availableFieldCodes.Add(f.Code);
            }
        }

        // ── Load operators/functions từ DB (async, fallback về hardcode) ─
        _ = LoadPaletteFromDbAsync(json);
    }

    // ── Load palette từ DB ───────────────────────────────────

    /// <summary>
    /// Tải operators và functions từ Gram_Operator + Gram_Function.
    /// Fallback về hardcode nếu DB chưa cấu hình hoặc lỗi.
    /// </summary>
    private async Task LoadPaletteFromDbAsync(string? initialJson)
    {
        IsLoadingPalette = true;

        try
        {
            // ── Load operators ────────────────────────────────
            var operators = await _grammarService.GetOperatorsAsync();
            if (operators.Count > 0)
            {
                OperatorItems.Clear();
                _availableOperatorSymbols = [];
                foreach (var op in operators.Where(o => o.IsActive))
                {
                    OperatorItems.Add(new PaletteItem
                    {
                        ItemType = PaletteItemType.Operator,
                        DisplayName = op.OperatorSymbol,
                        Code = op.OperatorSymbol,
                        Description = op.Description ?? op.OperatorType
                    });
                    _availableOperatorSymbols.Add(op.OperatorSymbol);
                }
            }
            else
            {
                // Fallback nếu Gram_Operator chưa có data
                LoadFallbackOperators();
            }

            // ── Load functions ────────────────────────────────
            var functions = await _grammarService.GetFunctionsAsync();
            if (functions.Count > 0)
            {
                FunctionItems.Clear();
                _availableFunctionCodes = [];
                foreach (var fn in functions.Where(f => f.IsActive))
                {
                    FunctionItems.Add(new PaletteItem
                    {
                        ItemType = PaletteItemType.Function,
                        DisplayName = $"{fn.FunctionCode}()",
                        Code = fn.FunctionCode,
                        Description = fn.Description ?? fn.FunctionCode,
                        NetType = fn.ReturnNetType,
                        ParamCountMin = fn.ParamCountMin,
                        ParamCountMax = fn.ParamCountMax
                    });
                    _availableFunctionCodes.Add(fn.FunctionCode);
                }
            }
            else
            {
                // Fallback nếu Gram_Function chưa có data
                LoadFallbackFunctions();
            }
        }
        catch
        {
            // NOTE: DB lỗi → dùng hardcode để dialog vẫn hoạt động
            if (OperatorItems.Count == 0) LoadFallbackOperators();
            if (FunctionItems.Count == 0) LoadFallbackFunctions();
        }
        finally
        {
            IsLoadingPalette = false;
        }

        FilterFunctions();

        // ── Deserialize expression sau khi palette đã ready ──
        RootNode = _deserializer.Deserialize(initialJson);
        SyncRootNodes();
        ValidateExpression();
    }

    // ── Fallback hardcode (khi DB chưa có data) ──────────────

    private void LoadFallbackOperators()
    {
        OperatorItems.Clear();
        _availableOperatorSymbols = [];
        var ops = new[]
        {
            ("==", "Equal"), ("!=", "Not equal"), (">", "Greater than"),
            (">=", "Greater than or equal"), ("<", "Less than"), ("<=", "Less than or equal"),
            ("&&", "Logical AND"), ("||", "Logical OR"), ("!", "Logical NOT"),
            ("+", "Add"), ("-", "Subtract"), ("*", "Multiply"),
            ("/", "Divide"), ("%", "Modulo"), ("??", "Null coalesce")
        };
        foreach (var (symbol, desc) in ops)
        {
            OperatorItems.Add(new PaletteItem
            {
                ItemType = PaletteItemType.Operator,
                DisplayName = symbol, Code = symbol, Description = desc
            });
            _availableOperatorSymbols.Add(symbol);
        }
    }

    private void LoadFallbackFunctions()
    {
        FunctionItems.Clear();
        _availableFunctionCodes = [];
        var fns = new[]
        {
            ("len", "len(str)", "Đếm số ký tự", "Int32", 1, 1),
            ("trim", "trim(str)", "Xóa khoảng trắng đầu/cuối", "String", 1, 1),
            ("regex", "regex(val, pat)", "Kiểm tra regex pattern", "Boolean", 2, 2),
            ("round", "round(val, dec)", "Làm tròn số", "Decimal", 2, 2),
            ("iif", "iif(cond, t, f)", "Điều kiện if-then-else", "", 3, 3),
            ("isNull", "isNull(val)", "Kiểm tra null", "Boolean", 1, 1),
            ("toDate", "toDate(str)", "Chuyển string thành DateTime", "DateTime", 1, 1),
            ("today", "today()", "Ngày hiện tại", "DateTime", 0, 0),
            ("dateDiff", "dateDiff(d1,d2,unit)", "Khoảng cách 2 ngày", "Int32", 3, 3),
            ("concat", "concat(a, b)", "Nối chuỗi", "String", 2, 10)
        };
        foreach (var (code, display, desc, ret, min, max) in fns)
        {
            FunctionItems.Add(new PaletteItem
            {
                ItemType = PaletteItemType.Function,
                DisplayName = display, Code = code, Description = desc,
                NetType = ret, ParamCountMin = min, ParamCountMax = max
            });
            _availableFunctionCodes.Add(code);
        }
    }

    private void FilterFunctions()
    {
        FilteredFunctions.Clear();
        var query = FunctionSearch.Trim();
        foreach (var fn in FunctionItems)
        {
            if (string.IsNullOrEmpty(query)
                || fn.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                || fn.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredFunctions.Add(fn);
            }
        }
    }

    // ── Validate & Preview ───────────────────────────────────

    private void ValidateExpression()
    {
        ValidationResult = _validator.Validate(
            RootNode,
            ExpectedReturnType,
            _availableFunctionCodes,
            _availableOperatorSymbols.Count > 0 ? _availableOperatorSymbols : ["==","!=",">",">=","<","<=","&&","||","!","+","-","*","/","%","??"],
            _availableFieldCodes);

        NaturalLanguagePreview = _naturalLanguage.ToText(RootNode);
        JsonOutput = RootNode is not null ? _serializer.Serialize(RootNode) : "";

        RaisePropertyChanged(nameof(IsValid));
    }

    // ── Tree sync helper ─────────────────────────────────────

    private void SyncRootNodes()
    {
        RootNodes.Clear();
        if (RootNode is not null)
            RootNodes.Add(RootNode);
    }

    // ── Insert commands ──────────────────────────────────────

    private void ExecuteInsertOperator(PaletteItem? item)
    {
        if (item is null) return;

        if (item.Code == "!")
        {
            var unaryNode = new AstNode { Type = AstNodeType.Unary, Operator = "!" };
            var unaryVm = new AstNodeViewModel { Node = unaryNode };

            if (SelectedNode is not null)
            {
                unaryNode.Operand = SelectedNode.Node;
                unaryVm.Children.Add(SelectedNode);
                ReplaceNode(SelectedNode, unaryVm);
            }
            else
            {
                InsertAsRoot(unaryVm);
            }
        }
        else
        {
            var binaryNode = new AstNode { Type = AstNodeType.Binary, Operator = item.Code };
            var binaryVm = new AstNodeViewModel { Node = binaryNode };
            var placeholderLeft = CreatePlaceholder();
            var placeholderRight = CreatePlaceholder();

            if (SelectedNode is not null)
            {
                binaryNode.Left = SelectedNode.Node;
                binaryNode.Right = placeholderRight.Node;
                binaryVm.Children.Add(SelectedNode);
                binaryVm.Children.Add(placeholderRight);
                ReplaceNode(SelectedNode, binaryVm);
            }
            else if (RootNode is null)
            {
                binaryNode.Left = placeholderLeft.Node;
                binaryNode.Right = placeholderRight.Node;
                binaryVm.Children.Add(placeholderLeft);
                binaryVm.Children.Add(placeholderRight);
                InsertAsRoot(binaryVm);
            }
        }

        ValidateExpression();
    }

    private void ExecuteInsertFunction(PaletteItem? item)
    {
        if (item is null) return;

        var funcNode = new AstNode { Type = AstNodeType.Function, FunctionName = item.Code };
        var funcVm = new AstNodeViewModel { Node = funcNode };

        for (int i = 0; i < item.ParamCountMin; i++)
        {
            var placeholder = CreatePlaceholder();
            funcNode.Arguments.Add(placeholder.Node);
            funcVm.Children.Add(placeholder);
        }

        if (SelectedNode is not null)
            ReplaceNode(SelectedNode, funcVm);
        else
            InsertAsRoot(funcVm);

        ValidateExpression();
    }

    private void ExecuteInsertField(PaletteItem? item)
    {
        if (item is null) return;

        var identNode = new AstNode
        {
            Type = AstNodeType.Identifier,
            Name = item.Code,
            NetType = item.NetType
        };
        var identVm = new AstNodeViewModel { Node = identNode };

        if (SelectedNode is not null)
            ReplaceNode(SelectedNode, identVm);
        else
            InsertAsRoot(identVm);

        ValidateExpression();
    }

    // ── Node manipulation ────────────────────────────────────

    private void ExecuteDeleteSelectedNode()
    {
        if (SelectedNode is null) return;

        if (SelectedNode == RootNode)
        {
            RootNode = null;
            SelectedNode = null;
            SyncRootNodes();
        }
        else if (SelectedNode.Parent is not null)
        {
            var parent = SelectedNode.Parent;
            var idx = parent.Children.IndexOf(SelectedNode);
            var placeholder = CreatePlaceholder();
            placeholder.Parent = parent;
            if (idx >= 0)
                parent.Children[idx] = placeholder;
            SelectedNode = placeholder;
        }

        ValidateExpression();
    }

    private void ExecuteWrapInBinary()
    {
        if (SelectedNode is null) return;

        var binaryNode = new AstNode
        {
            Type = AstNodeType.Binary,
            Operator = "&&",
            Left = SelectedNode.Node
        };
        var placeholder = CreatePlaceholder();
        binaryNode.Right = placeholder.Node;

        var binaryVm = new AstNodeViewModel { Node = binaryNode };
        binaryVm.Children.Add(SelectedNode);
        binaryVm.Children.Add(placeholder);

        ReplaceNode(SelectedNode, binaryVm);
        ValidateExpression();
    }

    private void ExecuteWrapInUnary()
    {
        if (SelectedNode is null) return;

        var unaryNode = new AstNode
        {
            Type = AstNodeType.Unary,
            Operator = "!",
            Operand = SelectedNode.Node
        };
        var unaryVm = new AstNodeViewModel { Node = unaryNode };
        unaryVm.Children.Add(SelectedNode);

        ReplaceNode(SelectedNode, unaryVm);
        ValidateExpression();
    }

    // ── Tree helpers ─────────────────────────────────────────

    private void InsertAsRoot(AstNodeViewModel newNode)
    {
        newNode.Parent = null;
        newNode.Depth = 0;
        RootNode = newNode;
        SyncRootNodes();
    }

    private void ReplaceNode(AstNodeViewModel oldNode, AstNodeViewModel newNode)
    {
        if (oldNode == RootNode)
        {
            InsertAsRoot(newNode);
            return;
        }

        if (oldNode.Parent is null) return;

        var parent = oldNode.Parent;
        var idx = parent.Children.IndexOf(oldNode);
        if (idx >= 0)
        {
            newNode.Parent = parent;
            parent.Children[idx] = newNode;
        }

        SelectedNode = newNode;
        SyncRootNodes();
    }

    private static AstNodeViewModel CreatePlaceholder() =>
        new() { Node = new AstNode { Type = AstNodeType.Literal, Value = null, NetType = "" } };

    // ── Dialog commands ──────────────────────────────────────

    private void ExecuteApply()
    {
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("expressionJson", JsonOutput);
        result.Parameters.Add("referencedFields", ValidationResult?.ReferencedFields ?? []);
        RequestClose.Invoke(result);
    }

    private void ExecuteCancel()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private void ExecuteReset()
    {
        RootNode = null;
        SelectedNode = null;
        SyncRootNodes();
        ValidateExpression();
    }

    private void ExecuteCopyJson()
    {
        if (!string.IsNullOrEmpty(JsonOutput))
            System.Windows.Clipboard.SetText(JsonOutput);
    }
}
