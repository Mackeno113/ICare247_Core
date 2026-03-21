// File    : IFieldDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn chi tiết field + Sys_Column cho FieldConfigView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD field metadata + lookup columns từ Sys_Column.
/// </summary>
public interface IFieldDataService
{
    Task<FieldConfigRecord?> GetFieldDetailAsync(int fieldId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ColumnInfoRecord>> GetColumnsByTableAsync(int tableId, CancellationToken ct = default);
    Task<int> GetTableIdByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    /// <summary>
    /// Lưu field: INSERT (FieldId=0) → trả về Field_Id mới, UPDATE → trả về FieldId truyền vào.
    /// </summary>
    Task<int> SaveFieldAsync(FieldConfigRecord field, int tenantId, CancellationToken ct = default);
}
