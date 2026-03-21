// File    : ISchemaInspectorService.cs
// Module  : Interfaces
// Layer   : Core
// Purpose : Đọc cấu trúc cột từ Target DB (INFORMATION_SCHEMA) để auto-generate fields.
//           Hoạt động với connection string của Target DB — KHÔNG dùng Config DB.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Dịch vụ đọc metadata cấu trúc bảng từ Target DB thông qua INFORMATION_SCHEMA.
/// Dùng để hỗ trợ tính năng Auto-generate Fields và Sync Schema.
/// </summary>
public interface ISchemaInspectorService
{
    /// <summary>
    /// Lấy danh sách tên bảng trong Target DB, sắp xếp theo schema rồi tên bảng.
    /// Dùng để user chọn bảng khi cấu hình form.
    /// </summary>
    /// <param name="connectionString">Connection string tới Target DB.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách "schema.tableName", ví dụ "dbo.PurchaseOrder".</returns>
    Task<IReadOnlyList<string>> GetTableNamesAsync(
        string connectionString,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách cột của một bảng trong Target DB.
    /// Kết quả sắp xếp theo ORDINAL_POSITION.
    /// </summary>
    /// <param name="connectionString">Connection string tới Target DB.</param>
    /// <param name="schemaName">Schema SQL (thường là "dbo").</param>
    /// <param name="tableName">Tên bảng (không có schema prefix).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Danh sách ColumnSchemaDto đã được map NetType và DefaultEditorType.
    /// Trả list rỗng nếu bảng không tồn tại.
    /// </returns>
    Task<IReadOnlyList<ColumnSchemaDto>> GetColumnsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken ct = default);
}
