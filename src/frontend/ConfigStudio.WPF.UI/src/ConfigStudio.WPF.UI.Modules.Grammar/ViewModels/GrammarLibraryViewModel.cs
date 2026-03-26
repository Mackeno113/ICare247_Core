// File    : GrammarLibraryViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Grammar Library (Screen 09) — whitelist functions/operators.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;

/// <summary>
/// ViewModel cho màn hình Grammar Library (Screen 09).
/// 2 tab: Functions và Operators, mỗi tab có DataGrid + search/filter.
/// Khi DB đã cấu hình → load dữ liệu thật qua IGrammarDataService.
/// Khi chưa cấu hình → hiển thị danh sách rỗng.
/// </summary>
public sealed class GrammarLibraryViewModel : ViewModelBase, INavigationAware
{
    private readonly IGrammarDataService? _grammarService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

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

    public GrammarLibraryViewModel(IGrammarDataService? grammarService = null, IAppConfigService? appConfig = null)
    {
        _grammarService = grammarService;
        _appConfig = appConfig;

        FunctionsView = CollectionViewSource.GetDefaultView(Functions);
        FunctionsView.Filter = FilterFunctions;

        OperatorsView = CollectionViewSource.GetDefaultView(Operators);
        OperatorsView.Filter = FilterOperators;

        AddFunctionCommand = new DelegateCommand(ExecuteAddFunction);
        DeleteFunctionCommand = new DelegateCommand(ExecuteDeleteFunction, () => SelectedFunction is not null);
        AddOperatorCommand = new DelegateCommand(ExecuteAddOperator);
        DeleteOperatorCommand = new DelegateCommand(ExecuteDeleteOperator, () => SelectedOperator is not null);
        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext) => await LoadDataAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data ────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        if (_grammarService is not null && _appConfig is { IsConfigured: true })
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            // Chưa cấu hình DB → danh sách rỗng
            Functions.Clear();
            Operators.Clear();
        }
    }

    /// <summary>
    /// Đọc Gram_Function + Gram_Operator từ DB, map sang UI DTO.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct = _cts.Token;
            var functions = await _grammarService!.GetFunctionsAsync(ct);
            var operators = await _grammarService.GetOperatorsAsync(ct);

            Functions.Clear();
            foreach (var f in functions)
            {
                Functions.Add(new FunctionDto
                {
                    FunctionId = f.FunctionId,
                    FunctionName = f.FunctionCode,
                    Category = MapNetTypeToCategory(f.ReturnNetType),
                    ParamCount = f.ParamCountMax == 99 ? -1 : f.ParamCountMin,
                    ReturnType = f.ReturnNetType,
                    Description = f.Description ?? "",
                    IsActive = f.IsActive
                });
            }

            Operators.Clear();
            foreach (var o in operators)
            {
                Operators.Add(new OperatorDto
                {
                    OperatorId = 0,
                    Symbol = o.OperatorSymbol,
                    OperatorName = o.OperatorSymbol,
                    Category = o.OperatorType,
                    Precedence = o.Precedence,
                    Description = o.Description ?? "",
                    IsActive = o.IsActive
                });
            }
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            Functions.Clear();
            Operators.Clear();
        }
    }

    /// <summary>
    /// Ánh xạ ReturnNetType sang Category hiển thị (heuristic).
    /// DB không có Category column — suy từ ReturnNetType.
    /// </summary>
    private static string MapNetTypeToCategory(string netType) => netType switch
    {
        "String" => "String",
        "Number" or "Int32" or "Decimal" or "Double" => "Math",
        "DateTime" => "Date",
        "Boolean" => "Logic",
        _ => "Conversion"
    };

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
