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

    /// <summary>
    /// Đọc cờ làm mờ log của cột (Sys_Column.Is_Log_Masked/Log_Mask_Mode) theo Column_Id.
    /// Phòng thủ: cột chưa migrate (db/071) → trả (false, null), KHÔNG ném. Column_Id ≤ 0 → (false, null).
    /// </summary>
    Task<(bool IsMasked, string? MaskMode)> GetColumnMaskingAsync(int columnId, CancellationToken ct = default);

    /// <summary>
    /// Ghi cờ làm mờ log vào Sys_Column theo Column_Id (thuộc tính cấp cột, dùng chung mọi form/view).
    /// Phòng thủ: cột chưa migrate hoặc Column_Id ≤ 0 → bỏ qua, KHÔNG ném.
    /// </summary>
    Task SaveColumnMaskingAsync(int columnId, bool isMasked, string? maskMode, CancellationToken ct = default);
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
    /// <paramref name="shiftOnInsert"/> = true (chỉ áp cho INSERT): trước khi chèn, đẩy Order_No
    /// của mọi field cùng section có Order_No ≥ vị trí chèn lên +1 để nhường chỗ (chèn giữa danh
    /// sách); false = nối cuối như cũ.
    /// </summary>
    Task<int> SaveFieldAsync(FieldConfigRecord field, int tenantId,
        FieldLookupConfigRecord? lookupConfig = null, bool shiftOnInsert = false,
        CancellationToken ct = default);

    /// <summary>
    /// Đánh dấu field ĐÃ được cấu hình (Ui_Field.Is_Configured = 1). Chỉ gọi khi user bấm
    /// "Lưu Field" ở màn cấu hình chi tiết — KHÔNG gọi từ auto-generate/bulk/sync.
    /// </summary>
    Task MarkFieldConfiguredAsync(int fieldId, CancellationToken ct = default);

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

    /// <summary>
    /// Chuyển field sang section khác (cập nhật Ui_Field.Section_Id).
    /// Dùng khi di chuyển field xuyên section trong Form Editor.
    /// Thứ tự (Order_No) được cập nhật riêng qua <see cref="UpdateFieldOrderAsync"/>.
    /// </summary>
    Task MoveFieldToSectionAsync(int fieldId, int sectionId, CancellationToken ct = default);
}
