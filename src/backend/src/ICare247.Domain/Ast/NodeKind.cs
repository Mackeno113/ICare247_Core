// File    : NodeKind.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Enum phân loại các loại node trong AST expression tree.

namespace ICare247.Domain.Ast;

/// <summary>
/// Phân loại node trong AST.
/// AstCompiler dùng để switch-dispatch mà không cần reflection hay type-check.
/// </summary>
public enum NodeKind
{
    /// <summary>Giá trị literal: số, string, bool, null.</summary>
    Literal,

    /// <summary>Tham chiếu đến field trong EvaluationContext theo tên.</summary>
    Identifier,

    /// <summary>Phép toán 2 vế: số học, so sánh, logic.</summary>
    Binary,

    /// <summary>Phép toán 1 vế: phủ định logic (!), đảo dấu (-).</summary>
    Unary,

    /// <summary>Gọi hàm từ whitelist (len, iif, toDate, ...).</summary>
    FunctionCall,

    /// <summary>Truy cập property qua dot notation (obj.Property).</summary>
    MemberAccess,
}
