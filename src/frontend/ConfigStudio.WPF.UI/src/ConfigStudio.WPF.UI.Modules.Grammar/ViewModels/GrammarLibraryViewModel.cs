// File    : GrammarLibraryViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Grammar Library (Screen 09) — whitelist functions/operators.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;

/// <summary>
/// ViewModel cho màn hình Grammar Library (Screen 09).
/// 2 tab: Functions và Operators, mỗi tab có DataGrid + search/filter.
/// </summary>
public sealed class GrammarLibraryViewModel : ViewModelBase, INavigationAware
{
    // ── Functions ─────────────────────────────────────────────
    public ObservableCollection<FunctionDto> Functions { get; } = [];
    public ICollectionView FunctionsView { get; }

    private FunctionDto? _selectedFunction;
    public FunctionDto? SelectedFunction
    {
        get => _selectedFunction;
        set
        {
            if (SetProperty(ref _selectedFunction, value))
                DeleteFunctionCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Operators ─────────────────────────────────────────────
    public ObservableCollection<OperatorDto> Operators { get; } = [];
    public ICollectionView OperatorsView { get; }

    private OperatorDto? _selectedOperator;
    public OperatorDto? SelectedOperator
    {
        get => _selectedOperator;
        set
        {
            if (SetProperty(ref _selectedOperator, value))
                DeleteOperatorCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Tab ───────────────────────────────────────────────────
    private int _selectedTabIndex;
    public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }

    // ── Filter ────────────────────────────────────────────────
    private string _functionSearch = "";
    public string FunctionSearch
    {
        get => _functionSearch;
        set
        {
            if (SetProperty(ref _functionSearch, value))
                FunctionsView.Refresh();
        }
    }

    private string _operatorSearch = "";
    public string OperatorSearch
    {
        get => _operatorSearch;
        set
        {
            if (SetProperty(ref _operatorSearch, value))
                OperatorsView.Refresh();
        }
    }

    public List<string> FunctionCategories { get; } = ["Tất cả", "String", "Math", "Date", "Logic", "Conversion"];

    private string _functionCategoryFilter = "Tất cả";
    public string FunctionCategoryFilter
    {
        get => _functionCategoryFilter;
        set
        {
            if (SetProperty(ref _functionCategoryFilter, value))
                FunctionsView.Refresh();
        }
    }

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddFunctionCommand { get; }
    public DelegateCommand DeleteFunctionCommand { get; }
    public DelegateCommand AddOperatorCommand { get; }
    public DelegateCommand DeleteOperatorCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    public GrammarLibraryViewModel()
    {
        FunctionsView = CollectionViewSource.GetDefaultView(Functions);
        FunctionsView.Filter = FilterFunctions;

        OperatorsView = CollectionViewSource.GetDefaultView(Operators);
        OperatorsView.Filter = FilterOperators;

        AddFunctionCommand = new DelegateCommand(ExecuteAddFunction);
        DeleteFunctionCommand = new DelegateCommand(ExecuteDeleteFunction, () => SelectedFunction is not null);
        AddOperatorCommand = new DelegateCommand(ExecuteAddOperator);
        DeleteOperatorCommand = new DelegateCommand(ExecuteDeleteOperator, () => SelectedOperator is not null);
        RefreshCommand = new DelegateCommand(LoadMockData);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext) => LoadMockData();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load mock data ───────────────────────────────────────

    private void LoadMockData()
    {
        Functions.Clear();
        Functions.Add(new FunctionDto { FunctionId = 1, FunctionName = "len", Category = "String", ParamCount = 1, ReturnType = "Number", Description = "Trả về độ dài chuỗi", Example = "len(MaDonHang)" });
        Functions.Add(new FunctionDto { FunctionId = 2, FunctionName = "trim", Category = "String", ParamCount = 1, ReturnType = "String", Description = "Xóa khoảng trắng đầu/cuối", Example = "trim(TenKhach)" });
        Functions.Add(new FunctionDto { FunctionId = 3, FunctionName = "upper", Category = "String", ParamCount = 1, ReturnType = "String", Description = "Chuyển thành chữ hoa", Example = "upper(MaDonHang)" });
        Functions.Add(new FunctionDto { FunctionId = 4, FunctionName = "lower", Category = "String", ParamCount = 1, ReturnType = "String", Description = "Chuyển thành chữ thường", Example = "lower(Email)" });
        Functions.Add(new FunctionDto { FunctionId = 5, FunctionName = "concat", Category = "String", ParamCount = -1, ReturnType = "String", Description = "Nối chuỗi", Example = "concat(Ho, \" \", Ten)" });
        Functions.Add(new FunctionDto { FunctionId = 6, FunctionName = "round", Category = "Math", ParamCount = 2, ReturnType = "Number", Description = "Làm tròn số", Example = "round(ThanhTien, 2)" });
        Functions.Add(new FunctionDto { FunctionId = 7, FunctionName = "abs", Category = "Math", ParamCount = 1, ReturnType = "Number", Description = "Giá trị tuyệt đối", Example = "abs(ChenhLech)" });
        Functions.Add(new FunctionDto { FunctionId = 8, FunctionName = "min", Category = "Math", ParamCount = -1, ReturnType = "Number", Description = "Giá trị nhỏ nhất", Example = "min(A, B, C)" });
        Functions.Add(new FunctionDto { FunctionId = 9, FunctionName = "max", Category = "Math", ParamCount = -1, ReturnType = "Number", Description = "Giá trị lớn nhất", Example = "max(A, B, C)" });
        Functions.Add(new FunctionDto { FunctionId = 10, FunctionName = "today", Category = "Date", ParamCount = 0, ReturnType = "DateTime", Description = "Ngày hiện tại", Example = "today()" });
        Functions.Add(new FunctionDto { FunctionId = 11, FunctionName = "toDate", Category = "Date", ParamCount = 1, ReturnType = "DateTime", Description = "Chuyển chuỗi thành ngày", Example = "toDate(NgayStr)" });
        Functions.Add(new FunctionDto { FunctionId = 12, FunctionName = "dateDiff", Category = "Date", ParamCount = 3, ReturnType = "Number", Description = "Số ngày giữa 2 ngày", Example = "dateDiff(\"day\", Start, End)" });
        Functions.Add(new FunctionDto { FunctionId = 13, FunctionName = "iif", Category = "Logic", ParamCount = 3, ReturnType = "Object", Description = "Nếu-thì-không thì", Example = "iif(SoLuong > 0, \"OK\", \"NG\")" });
        Functions.Add(new FunctionDto { FunctionId = 14, FunctionName = "isNull", Category = "Logic", ParamCount = 1, ReturnType = "Boolean", Description = "Kiểm tra null", Example = "isNull(NhaCungCap)" });
        Functions.Add(new FunctionDto { FunctionId = 15, FunctionName = "coalesce", Category = "Logic", ParamCount = -1, ReturnType = "Object", Description = "Giá trị đầu tiên khác null", Example = "coalesce(A, B, 0)" });
        Functions.Add(new FunctionDto { FunctionId = 16, FunctionName = "toNumber", Category = "Conversion", ParamCount = 1, ReturnType = "Number", Description = "Chuyển sang số", Example = "toNumber(SoLuongStr)" });
        Functions.Add(new FunctionDto { FunctionId = 17, FunctionName = "toString", Category = "Conversion", ParamCount = 1, ReturnType = "String", Description = "Chuyển sang chuỗi", Example = "toString(SoLuong)" });

        Operators.Clear();
        Operators.Add(new OperatorDto { OperatorId = 1, Symbol = "+", OperatorName = "Addition", Category = "Arithmetic", Precedence = 10, Description = "Cộng" });
        Operators.Add(new OperatorDto { OperatorId = 2, Symbol = "-", OperatorName = "Subtraction", Category = "Arithmetic", Precedence = 10, Description = "Trừ" });
        Operators.Add(new OperatorDto { OperatorId = 3, Symbol = "*", OperatorName = "Multiplication", Category = "Arithmetic", Precedence = 20, Description = "Nhân" });
        Operators.Add(new OperatorDto { OperatorId = 4, Symbol = "/", OperatorName = "Division", Category = "Arithmetic", Precedence = 20, Description = "Chia" });
        Operators.Add(new OperatorDto { OperatorId = 5, Symbol = "%", OperatorName = "Modulo", Category = "Arithmetic", Precedence = 20, Description = "Chia lấy dư" });
        Operators.Add(new OperatorDto { OperatorId = 6, Symbol = "==", OperatorName = "Equal", Category = "Comparison", Precedence = 5, Description = "Bằng" });
        Operators.Add(new OperatorDto { OperatorId = 7, Symbol = "!=", OperatorName = "NotEqual", Category = "Comparison", Precedence = 5, Description = "Khác" });
        Operators.Add(new OperatorDto { OperatorId = 8, Symbol = ">", OperatorName = "GreaterThan", Category = "Comparison", Precedence = 6, Description = "Lớn hơn" });
        Operators.Add(new OperatorDto { OperatorId = 9, Symbol = "<", OperatorName = "LessThan", Category = "Comparison", Precedence = 6, Description = "Nhỏ hơn" });
        Operators.Add(new OperatorDto { OperatorId = 10, Symbol = ">=", OperatorName = "GreaterOrEqual", Category = "Comparison", Precedence = 6, Description = "Lớn hơn hoặc bằng" });
        Operators.Add(new OperatorDto { OperatorId = 11, Symbol = "<=", OperatorName = "LessOrEqual", Category = "Comparison", Precedence = 6, Description = "Nhỏ hơn hoặc bằng" });
        Operators.Add(new OperatorDto { OperatorId = 12, Symbol = "&&", OperatorName = "And", Category = "Logical", Precedence = 3, Description = "VÀ logic" });
        Operators.Add(new OperatorDto { OperatorId = 13, Symbol = "||", OperatorName = "Or", Category = "Logical", Precedence = 2, Description = "HOẶC logic" });
        Operators.Add(new OperatorDto { OperatorId = 14, Symbol = "!", OperatorName = "Not", Category = "Logical", Precedence = 30, Description = "PHỦ ĐỊNH logic" });
    }

    // ── Filter ───────────────────────────────────────────────

    private bool FilterFunctions(object obj)
    {
        if (obj is not FunctionDto fn) return false;

        if (FunctionCategoryFilter != "Tất cả" && fn.Category != FunctionCategoryFilter) return false;

        if (!string.IsNullOrWhiteSpace(FunctionSearch))
        {
            var q = FunctionSearch.Trim();
            return fn.FunctionName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || fn.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private bool FilterOperators(object obj)
    {
        if (obj is not OperatorDto op) return false;

        if (!string.IsNullOrWhiteSpace(OperatorSearch))
        {
            var q = OperatorSearch.Trim();
            return op.Symbol.Contains(q, StringComparison.OrdinalIgnoreCase)
                || op.OperatorName.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteAddFunction()
    {
        var newId = Functions.Count > 0 ? Functions.Max(f => f.FunctionId) + 1 : 1;
        Functions.Add(new FunctionDto
        {
            FunctionId = newId, FunctionName = $"newFunc{newId}",
            Category = "String", ParamCount = 1, ReturnType = "String",
            Description = "Function mới", Example = ""
        });
    }

    private void ExecuteDeleteFunction()
    {
        if (SelectedFunction is null) return;
        Functions.Remove(SelectedFunction);
        SelectedFunction = null;
    }

    private void ExecuteAddOperator()
    {
        var newId = Operators.Count > 0 ? Operators.Max(o => o.OperatorId) + 1 : 1;
        Operators.Add(new OperatorDto
        {
            OperatorId = newId, Symbol = "??",
            OperatorName = "NewOperator", Category = "Logical",
            Precedence = 1, Description = "Operator mới"
        });
    }

    private void ExecuteDeleteOperator()
    {
        if (SelectedOperator is null) return;
        Operators.Remove(SelectedOperator);
        SelectedOperator = null;
    }
}
