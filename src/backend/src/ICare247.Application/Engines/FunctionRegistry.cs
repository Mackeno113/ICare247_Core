// File    : FunctionRegistry.cs
// Module  : Engines
// Layer   : Application
// Purpose : Registry quản lý whitelist functions cho AST evaluation.

using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// Delegate cho một grammar function.
/// Nhận danh sách arguments đã evaluate, trả về kết quả.
/// </summary>
public delegate object? GrammarFunction(object?[] args, EvaluationContext context);

/// <summary>
/// Registry quản lý whitelist functions.
/// Tất cả function phải đăng ký trước khi dùng — không có eval/dynamic.
/// </summary>
public sealed class FunctionRegistry
{
    private readonly Dictionary<string, FunctionEntry> _functions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Đăng ký một function vào whitelist.
    /// </summary>
    /// <param name="name">Tên function (case-insensitive).</param>
    /// <param name="minParams">Số tham số tối thiểu.</param>
    /// <param name="maxParams">Số tham số tối đa (-1 = variadic).</param>
    /// <param name="func">Delegate thực thi.</param>
    public void Register(string name, int minParams, int maxParams, GrammarFunction func)
    {
        _functions[name] = new FunctionEntry(name, minParams, maxParams, func);
    }

    /// <summary>
    /// Kiểm tra function có trong whitelist không.
    /// </summary>
    public bool Contains(string name) => _functions.ContainsKey(name);

    /// <summary>
    /// Lấy function entry. Throw nếu không tồn tại.
    /// </summary>
    public FunctionEntry Get(string name)
    {
        if (!_functions.TryGetValue(name, out var entry))
            throw new AstEvalException($"Function '{name}' không có trong whitelist.");
        return entry;
    }

    /// <summary>
    /// Validate số lượng arguments cho function.
    /// </summary>
    public void ValidateArgCount(string name, int argCount)
    {
        var entry = Get(name);
        if (argCount < entry.MinParams)
            throw new AstEvalException($"Function '{name}' cần ít nhất {entry.MinParams} tham số, nhận {argCount}.");
        if (entry.MaxParams >= 0 && argCount > entry.MaxParams)
            throw new AstEvalException($"Function '{name}' nhận tối đa {entry.MaxParams} tham số, nhận {argCount}.");
    }
}

/// <summary>
/// Entry mô tả một function trong registry.
/// </summary>
public sealed record FunctionEntry(
    string Name,
    int MinParams,
    int MaxParams,
    GrammarFunction Func);

/// <summary>
/// Exception khi evaluate AST thất bại.
/// </summary>
public sealed class AstEvalException : Exception
{
    public AstEvalException(string message) : base(message) { }
    public AstEvalException(string message, Exception inner) : base(message, inner) { }
}
