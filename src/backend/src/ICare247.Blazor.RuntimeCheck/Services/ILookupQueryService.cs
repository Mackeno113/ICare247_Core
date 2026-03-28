// File    : ILookupQueryService.cs
// Module  : RuntimeCheck
// Purpose : Interface gọi POST /api/v1/lookups/query-dynamic để lấy dữ liệu dynamic lookup.

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Gọi backend để lấy rows dữ liệu cho dynamic lookup field (ComboBox / LookupBox).
/// Backend tự đọc cấu hình — client chỉ cần gửi <c>fieldId</c> + <c>contextValues</c>.
/// </summary>
public interface ILookupQueryService
{
    /// <summary>
    /// Lấy rows dữ liệu cho một field dynamic lookup.
    /// Trả danh sách rỗng nếu không có config hoặc lỗi network.
    /// </summary>
    /// <param name="fieldId">Field_Id trong Ui_Field — xác định cấu hình lookup.</param>
    /// <param name="contextValues">
    ///   Giá trị các field khác — dùng cho cascading filter.
    ///   Ví dụ: { "PhongBanId": 5 } → áp vào FilterSql WHERE PhongBan_Id = @PhongBanId.
    /// </param>
    /// <param name="ct"></param>
    Task<List<Dictionary<string, object?>>> QueryAsync(
        int fieldId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default);
}
