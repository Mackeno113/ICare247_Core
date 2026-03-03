// File    : ValidationResult.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Kết quả evaluate của một rule validation đơn lẻ.

namespace ICare247.Domain.Engine.Models;

/// <summary>
/// Kết quả evaluate của một rule trong danh sách validation rules.
/// </summary>
/// <param name="RuleId">ID của rule trong bảng Sys_Rule.</param>
/// <param name="Severity">Mức độ: 'error' | 'warning' | 'info'.</param>
/// <param name="Message">Thông báo lỗi đã format (đã thay placeholder nếu có).</param>
public sealed record ValidationResult(
    int RuleId,
    string Severity,
    string Message);
