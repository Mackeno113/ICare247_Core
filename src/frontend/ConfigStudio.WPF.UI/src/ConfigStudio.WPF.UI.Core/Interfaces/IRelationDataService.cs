// File    : IRelationDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn / ghi bảng Sys_Relation (registry quan hệ) trên Config DB.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Truy vấn / ghi cấu hình quan hệ <c>Sys_Relation</c> qua Dapper trên Config DB.
/// Phục vụ soft-check FK khi xóa + dựng master-detail 1:N.
/// </summary>
public interface IRelationDataService
{
    /// <summary>
    /// Lấy danh sách quan hệ (kèm Table_Code master/detail đã join) cho lưới quản lý.
    /// </summary>
    /// <param name="tenantId">Tenant hiện hành (lọc bảng theo tenant + global).</param>
    /// <param name="includeInactive">true = lấy cả quan hệ đã ẩn (Is_Active = 0).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách <see cref="RelationRecord"/>.</returns>
    Task<IReadOnlyList<RelationRecord>> GetRelationsAsync(
        int tenantId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách bảng <c>Sys_Table</c> để chọn master/detail trong editor.
    /// </summary>
    /// <param name="tenantId">Tenant hiện hành.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách <see cref="TableLookupRecord"/>.</returns>
    Task<IReadOnlyList<TableLookupRecord>> GetTablesAsync(int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách Column_Code của một bảng (để chọn cột khóa/FK/hiển thị).
    /// </summary>
    /// <param name="tableId">Table_Id cần lấy cột.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách tên cột (Column_Code).</returns>
    Task<IReadOnlyList<string>> GetColumnsAsync(int tableId, CancellationToken ct = default);

    /// <summary>
    /// Tạo mới hoặc cập nhật một quan hệ.
    /// </summary>
    /// <param name="r">Bản ghi quan hệ (RelationId = 0 → insert).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Relation_Id sau khi ghi.</returns>
    /// <remarks>Side-effect: ghi 1 dòng Sys_Relation; ném lỗi nếu trùng Relation_Code.</remarks>
    Task<int> SaveRelationAsync(RelationRecord r, CancellationToken ct = default);

    /// <summary>
    /// Ẩn (soft-delete) một quan hệ — set Is_Active = 0.
    /// </summary>
    /// <param name="relationId">Relation_Id cần ẩn.</param>
    /// <param name="ct">Token hủy.</param>
    /// <remarks>Side-effect: quan hệ biến mất khỏi lưới khi không bật "hiện inactive".</remarks>
    Task DeactivateRelationAsync(int relationId, CancellationToken ct = default);
}
