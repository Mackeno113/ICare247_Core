// File    : ISysLookupDataService.cs
// Module  : Core
// Layer   : Interfaces
// Purpose : Contract CRUD Sys_Lookup từ WPF ConfigStudio.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Quản lý danh mục dùng chung từ <c>Sys_Lookup</c> (đọc + ghi).
/// Label resolve theo ngôn ngữ từ <c>Sys_Resource</c>.
/// </summary>
public interface ISysLookupDataService
{
    // ── Đọc ──────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách items của một lookup code (chỉ Is_Active = 1).
    /// </summary>
    Task<IReadOnlyList<LookupItemRecord>> GetByCodeAsync(
        string lookupCode, string langCode = "vi",
        CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả lookup codes hiện có (kể cả inactive) — dùng cho dropdown.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllCodesAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ items của một code (kể cả inactive) để hiển thị trong Manager.
    /// </summary>
    Task<IReadOnlyList<LookupItemEditRecord>> GetItemsForEditAsync(
        string lookupCode, CancellationToken ct = default);

    // ── Ghi ──────────────────────────────────────────────────

    /// <summary>
    /// Thêm item mới vào lookup code. Trả về Lookup_Id mới.
    /// </summary>
    Task<int> AddItemAsync(LookupItemEditRecord item, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật item (Item_Code, Label_Key, Sort_Order, Is_Active).
    /// Cập nhật Sys_Resource vi/en tương ứng.
    /// </summary>
    Task UpdateItemAsync(LookupItemEditRecord item, CancellationToken ct = default);

    /// <summary>
    /// Xóa item khỏi Sys_Lookup theo Lookup_Id.
    /// </summary>
    Task DeleteItemAsync(int lookupId, CancellationToken ct = default);

    /// <summary>
    /// Thêm lookup code mới (chưa có item). Trả về true nếu thành công.
    /// </summary>
    Task<bool> AddLookupCodeAsync(string lookupCode, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra Item_Code đã tồn tại trong lookup code chưa (để validate khi thêm).
    /// excludeLookupId dùng khi edit để loại trừ chính nó.
    /// </summary>
    Task<bool> ItemCodeExistsAsync(
        string lookupCode, string itemCode,
        int excludeLookupId = 0,
        CancellationToken ct = default);
}
