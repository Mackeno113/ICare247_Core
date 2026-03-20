// File    : IGrammarDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface đọc/ghi Gram_Function + Gram_Operator cho GrammarLibraryView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD grammar functions + operators (global, không có Tenant_Id).
/// </summary>
public interface IGrammarDataService
{
    Task<IReadOnlyList<FunctionRecord>> GetFunctionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OperatorRecord>> GetOperatorsAsync(CancellationToken ct = default);
    Task SaveFunctionAsync(FunctionRecord func, CancellationToken ct = default);
    Task SaveOperatorAsync(OperatorRecord op, CancellationToken ct = default);
    Task DeleteFunctionAsync(int functionId, CancellationToken ct = default);
    Task DeleteOperatorAsync(string operatorSymbol, CancellationToken ct = default);
}
