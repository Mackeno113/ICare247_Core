// File    : IFieldDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn chi tiết field + Sys_Column cho FieldConfigView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD field metadata + lookup columns từ Sys_Column + Ui_Field_Lookup.
/// </summary>
public interface IFieldDataService
{
    Task<FieldConfigRecord?> GetFieldDetailAsync(int fieldId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ColumnInfoRecord>> GetColumnsByTableAsync(int tableId, CancellationToken ct = default);
    Task<int> GetTableIdByFormAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Lấy cấu hình FK lookup từ Ui_Field_Lookup. Null nếu field không phải dynamic.
    /// </summary>
    Task<FieldLookupConfigRecord?> GetFieldLookupConfigAsync(int fieldId, CancellationToken ct = default);

    /// <summary>
    /// Lưu field (INSERT/UPDATE Ui_Field) + lookup config (INSERT/UPDATE/DELETE Ui_Field_Lookup)
    /// trong cùng transaction. lookupConfig = null → xóa row Ui_Field_Lookup nếu tồn tại.
    /// INSERT (FieldId=0) → trả về Field_Id mới. UPDATE → trả về FieldId truyền vào.
    /// </summary>
    Task<int> SaveFieldAsync(FieldConfigRecord field, int tenantId,
        FieldLookupConfigRecord? lookupConfig = null, CancellationToken ct = default);

    /// <summary>
    /// Đảm bảo cột tồn tại trong Sys_Column (INSERT nếu chưa có), trả về Column_Id.
    /// </summary>
    Task<int> EnsureColumnExistsAsync(int tableId, ColumnSchemaDto col, CancellationToken ct = default);

    /// <summary>
    /// Xóa field khỏi Ui_Field + Ui_Field_Lookup trong cùng transaction.
    /// Dùng khi đồng bộ schema — field mồ côi (cột đã bị xóa khỏi DB thật).
    /// </summary>
    Task DeleteFieldAsync(int fieldId, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật Order_No hàng loạt cho danh sách field trong cùng section.
    /// items = [(fieldId, newOrder)] — thứ tự bắt đầu từ 1, bước nhảy +2 (1,3,5...).
    /// </summary>
    Task UpdateFieldOrderAsync(IReadOnlyList<(int FieldId, int OrderNo)> items,
        CancellationToken ct = default);
}
