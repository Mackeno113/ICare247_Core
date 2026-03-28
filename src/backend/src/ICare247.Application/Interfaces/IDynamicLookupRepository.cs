// File    : IDynamicLookupRepository.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Repository interface truy vấn dynamic lookup data từ Ui_Field_Lookup config.
//           Thực thi trong Infrastructure bằng Dapper + safe SQL builder.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Truy vấn dữ liệu nguồn của một dynamic field lookup (table / TVF / custom_sql).
/// Kết quả trả về dạng dictionary để renderer tự map theo cột.
/// </summary>
public interface IDynamicLookupRepository
{
    /// <summary>
    /// Thực thi truy vấn dynamic lookup theo cấu hình trong <c>Ui_Field_Lookup</c>.
    /// </summary>
    /// <param name="fieldId">
    ///   Field_Id trong bảng <c>Ui_Field</c> — dùng để JOIN tìm config trong
    ///   <c>Ui_Field_Lookup</c>. Tenant được verify qua join Ui_Form → Sys_Table.
    /// </param>
    /// <param name="tenantId">Tenant_Id từ header X-Tenant-Id.</param>
    /// <param name="contextValues">
    ///   Giá trị các field khác trong form — truyền như Dapper params cho FilterSql cascading.
    ///   Ví dụ: { "PhongBanId": 5 } → @PhongBanId được thay thế trong FilterSql.
    /// </param>
    /// <param name="ct"></param>
    /// <returns>
    ///   Danh sách rows dạng <c>IDictionary&lt;string, object&gt;</c>.
    ///   Trả danh sách rỗng nếu không có config hoặc query trả 0 rows.
    /// </returns>
    Task<IReadOnlyList<IDictionary<string, object>>> QueryAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default);
}
