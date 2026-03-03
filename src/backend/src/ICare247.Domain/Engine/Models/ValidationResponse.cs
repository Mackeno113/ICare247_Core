// File    : ValidationResponse.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Tổng hợp kết quả validate một field — trả về từ IValidationEngine.

namespace ICare247.Domain.Engine.Models;

/// <summary>
/// Kết quả validate của một field, tổng hợp từ toàn bộ rules áp dụng.
/// </summary>
/// <param name="FieldCode">Field được validate (Ui_Field.Field_Code).</param>
/// <param name="IsValid">
/// True khi không có rule nào fail với severity 'error'.
/// Warning/info không ảnh hưởng IsValid.
/// </param>
/// <param name="Results">
/// Danh sách kết quả của các rule đã fail (bỏ qua rule pass để giảm payload).
/// </param>
public sealed record ValidationResponse(
    string FieldCode,
    bool IsValid,
    IReadOnlyList<ValidationResult> Results)
{
    /// <summary>
    /// Tạo response "hợp lệ" không có lỗi — dùng khi field không có rule nào fail.
    /// </summary>
    public static ValidationResponse Valid(string fieldCode) =>
        new(fieldCode, IsValid: true, Results: []);
}
