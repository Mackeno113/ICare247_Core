// File    : AstValidator.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Validate AST tree — kiểm tra depth, function whitelist, operator, return type.

using ConfigStudio.WPF.UI.Modules.Grammar.Models;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Services;

/// <summary>
/// Validate <see cref="AstNodeViewModel"/> tree.
/// Kiểm tra: max depth, function/operator whitelist, referenced fields tồn tại, return type.
/// </summary>
public sealed class AstValidator
{
    /// <summary>
    /// Validate AST tree và trả về <see cref="AstValidationResult"/>.
    /// </summary>
    /// <param name="root">Root node cần validate.</param>
    /// <param name="expectedReturnType">"Boolean" cho condition/rule, hoặc NetType cho calculate.</param>
    /// <param name="availableFunctions">Danh sách function cho phép (từ Gram_Function).</param>
    /// <param name="availableOperators">Danh sách operator cho phép (từ Gram_Operator).</param>
    /// <param name="availableFields">Danh sách field cho phép (Column_Code từ Ui_Field).</param>
    /// <param name="maxDepth">Giới hạn độ sâu tối đa (mặc định 20).</param>
    public AstValidationResult Validate(
        AstNodeViewModel? root,
        string expectedReturnType,
        List<string> availableFunctions,
        List<string> availableOperators,
        List<string> availableFields,
        int maxDepth = 20)
    {
        var result = new AstValidationResult();

        if (root is null)
        {
            result.Errors.Add("Expression trống — chưa có node nào.");
            return result;
        }

        // ── 1. Validate từng node đệ quy ────────────────────
        ValidateNode(root, availableFunctions, availableOperators, availableFields, maxDepth, result);

        // ── 2. Tính depth ────────────────────────────────────
        result.Depth = CalculateDepth(root);
        if (result.Depth > maxDepth)
            result.Errors.Add($"Depth ({result.Depth}) vượt quá giới hạn {maxDepth}.");

        // ── 3. Infer return type ─────────────────────────────
        result.ActualReturnType = InferReturnType(root);

        // ── 4. Check expected return type ────────────────────
        if (!string.IsNullOrEmpty(expectedReturnType)
            && result.ActualReturnType is not null
            && !string.Equals(result.ActualReturnType, expectedReturnType, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add($"Return type '{result.ActualReturnType}' không khớp expected '{expectedReturnType}'.");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static void ValidateNode(
        AstNodeViewModel nodeVm,
        List<string> functions,
        List<string> operators,
        List<string> fields,
        int maxDepth,
        AstValidationResult result)
    {
        var node = nodeVm.Node;

        switch (node.Type)
        {
            case AstNodeType.Identifier:
                if (!fields.Contains(node.Name, StringComparer.OrdinalIgnoreCase))
                    result.Errors.Add($"Field '{node.Name}' không tồn tại trong form context.");
                if (!result.ReferencedFields.Contains(node.Name))
                    result.ReferencedFields.Add(node.Name);
                break;

            case AstNodeType.Binary:
                if (!operators.Contains(node.Operator))
                    result.Errors.Add($"Operator '{node.Operator}' không nằm trong whitelist.");
                break;

            case AstNodeType.Unary:
                if (!operators.Contains(node.Operator))
                    result.Errors.Add($"Unary operator '{node.Operator}' không nằm trong whitelist.");
                break;

            case AstNodeType.Function:
                if (!functions.Contains(node.FunctionName, StringComparer.OrdinalIgnoreCase))
                    result.Errors.Add($"Function '{node.FunctionName}' không tồn tại trong whitelist.");
                break;

            case AstNodeType.Literal:
                // NOTE: Literal luôn hợp lệ nếu có value
                break;
        }

        // ── Validate children đệ quy ────────────────────────
        foreach (var child in nodeVm.Children)
            ValidateNode(child, functions, operators, fields, maxDepth, result);
    }

    private static int CalculateDepth(AstNodeViewModel node)
    {
        if (node.Children.Count == 0) return 1;
        return 1 + node.Children.Max(CalculateDepth);
    }

    /// <summary>
    /// Infer return type đơn giản dựa trên root node type.
    /// TODO(phase2): Type inference đầy đủ dựa trên operator overloading và function signatures.
    /// </summary>
    private static string? InferReturnType(AstNodeViewModel nodeVm)
    {
        var node = nodeVm.Node;
        return node.Type switch
        {
            AstNodeType.Literal => node.NetType,
            AstNodeType.Identifier => node.NetType,
            AstNodeType.Binary when IsComparisonOperator(node.Operator) => "Boolean",
            AstNodeType.Binary when IsLogicalOperator(node.Operator) => "Boolean",
            AstNodeType.Binary when IsArithmeticOperator(node.Operator) => "Decimal",
            AstNodeType.Unary when node.Operator == "!" => "Boolean",
            AstNodeType.Function => null, // TODO(phase2): Infer từ Gram_Function.Return_Net_Type
            _ => null
        };
    }

    private static bool IsComparisonOperator(string op) =>
        op is "==" or "!=" or ">" or ">=" or "<" or "<=";

    private static bool IsLogicalOperator(string op) =>
        op is "&&" or "||";

    private static bool IsArithmeticOperator(string op) =>
        op is "+" or "-" or "*" or "/" or "%" or "??";
}
