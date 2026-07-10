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

    /// <summary>
    /// Truy vấn dữ liệu cho TreeLookupBox — trả về flat list có thêm cột ParentId.
    /// Client tự build hierarchy từ danh sách phẳng này.
    /// </summary>
    /// <param name="fieldId">Field_Id trong Ui_Field.</param>
    /// <param name="tenantId">Tenant_Id từ header.</param>
    /// <param name="contextValues">Giá trị các field khác — dùng cho FilterSql cascading.</param>
    /// <param name="ct"></param>
    /// <returns>
    /// Danh sách rows; mỗi row đảm bảo có key <c>__parentId</c> (nullable object)
    /// ánh xạ từ cột <c>Parent_Column</c> trong config.
    /// </returns>
    Task<IReadOnlyList<IDictionary<string, object>>> QueryTreeAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default);

    /// <summary>
    /// Insert một bản ghi mới vào bảng nguồn của field lookup (dùng cho "thêm mới" trên LookupBox).
    /// Chỉ áp dụng khi <c>Query_Mode = 'table'</c>. Mỗi cặp key/value trong <paramref name="values"/>
    /// được map thẳng thành cột → tham số Dapper (key phải là identifier hợp lệ).
    /// </summary>
    /// <param name="fieldId">Field_Id của LookupBox — xác định bảng nguồn + Value/Display column.</param>
    /// <param name="tenantId">Tenant hiện tại — chỉ dùng dựng cache key, KHÔNG lọc SQL (ADR-035).</param>
    /// <param name="values">Cặp Cột↔Giá trị từ dialog thêm mới (key = tên cột DB).</param>
    /// <param name="userId">Người thao tác (claim sub) — engine bơm vào <c>CreatedBy</c> nếu bảng đích có cột.</param>
    /// <param name="ct"></param>
    /// <returns>
    /// Dictionary gồm <c>value</c> (khóa vừa insert, từ OUTPUT INSERTED) và <c>display</c>
    /// (giá trị cột Display nếu có trong <paramref name="values"/>). Null nếu insert thất bại.
    /// </returns>
    Task<IDictionary<string, object?>?> InsertAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> values,
        long? userId = null,
        CancellationToken ct = default);
}
