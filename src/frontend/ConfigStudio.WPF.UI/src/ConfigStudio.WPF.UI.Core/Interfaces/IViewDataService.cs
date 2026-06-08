// File    : IViewDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn cụm bảng Ui_View / Ui_View_Column / Ui_View_Action (Config DB).

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Truy vấn / ghi cấu hình View hiển thị danh sách (Grid/TreeList) trên SQL Server qua Dapper.
/// Mọi method truyền tenantId để cô lập multi-tenant.
/// </summary>
public interface IViewDataService
{
    /// <summary>
    /// Lấy danh sách header <c>Ui_View</c> theo tenant cho lưới quản lý.
    /// </summary>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="includeInactive">true = lấy cả View đã ẩn (Is_Active = 0).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách <see cref="ViewRecord"/> (chỉ header, kèm Table_Code join).</returns>
    Task<IReadOnlyList<ViewRecord>> GetViewsAsync(
        int tenantId,
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>
    /// Nạp chi tiết đầy đủ một View: header + cột + action.
    /// </summary>
    /// <param name="viewId">Khóa View_Id.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns><see cref="ViewDetailRecord"/> hoặc null nếu không tồn tại.</returns>
    Task<ViewDetailRecord?> GetViewDetailAsync(int viewId, CancellationToken ct = default);

    /// <summary>
    /// Tạo mới hoặc cập nhật một View kèm toàn bộ cột + action trong cùng transaction.
    /// </summary>
    /// <param name="request">Payload header + cột + action.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>View_Id sau khi ghi (mới hoặc đang sửa).</returns>
    /// <remarks>
    /// Side-effect: xóa toàn bộ cột + action cũ của View rồi ghi lại theo request
    /// (đồng bộ nguyên khối); commit/rollback theo transaction.
    /// </remarks>
    Task<int> SaveViewAsync(ViewUpsertRequest request, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Ẩn (soft-delete) một View — set Is_Active = 0.
    /// </summary>
    /// <param name="viewId">Khóa View_Id.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Token hủy.</param>
    /// <remarks>Side-effect: View biến mất khỏi lưới khi không bật "hiện inactive".</remarks>
    Task DeactivateViewAsync(int viewId, int tenantId, CancellationToken ct = default);
}
