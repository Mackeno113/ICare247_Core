// File    : IViewRepository.cs
// Module  : View
// Layer   : Application
// Purpose : Repository interface cho cụm Ui_View (header + cột + action) — đọc metadata hiển thị.

using ICare247.Domain.Entities.View;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Ui_View</c> + <c>Ui_View_Column</c> + <c>Ui_View_Action</c> (Config DB).
/// Mọi query resolve tenant qua <c>Ui_View.Tenant_Id</c> (global khi NULL) và localize text i18n.
/// </summary>
public interface IViewRepository
{
    /// <summary>
    /// Lấy <see cref="ViewMetadata"/> đầy đủ (header + cột + action) theo View_Code,
    /// đã resolve text i18n (Title/Caption/Label/...) theo <paramref name="langCode"/>.
    /// </summary>
    /// <param name="viewCode">Ui_View.View_Code — unique trong tenant (hoặc global).</param>
    /// <param name="tenantId">Tenant hiện tại; bản ghi global (Tenant_Id NULL) luôn match.</param>
    /// <param name="langCode">Mã ngôn ngữ resolve resource (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ViewMetadata"/> nếu tồn tại và active; <c>null</c> nếu không.</returns>
    Task<ViewMetadata?> GetByCodeAsync(
        string viewCode, int tenantId, string langCode = "vi", CancellationToken ct = default);

    /// <summary>
    /// Lấy trang dữ liệu cho View (Source_Type='Table') — SELECT các cột <c>Data</c> (Field_Name)
    /// từ bảng nguồn (Data DB), có search + paging. Cột/identifier whitelist chống SQL injection.
    /// </summary>
    /// <param name="view">Metadata View đã nạp (chứa TableId + danh sách cột).</param>
    /// <param name="search">Từ khóa LIKE trên các cột Data (null = không lọc).</param>
    /// <param name="page">Trang (1-based).</param>
    /// <param name="pageSize">Số dòng mỗi trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách dòng (dictionary cột→giá trị) + tổng số.</returns>
    Task<ViewDataResult> GetDataAsync(
        ViewMetadata view, string? search, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Thực thi lưới nâng cao (Source_Type='Sp'/'Sql') với tham số từ panel lọc trái.
    /// Chỉ bind các <c>@param</c> khai báo trong <see cref="ViewMetadata.Filters"/> (whitelist) — ép kiểu
    /// theo Param_Type, bọc %...% khi Operator='LIKE', giá trị rỗng → NULL (SP nên xử lý NULL = bỏ lọc).
    /// </summary>
    /// <param name="view">Metadata View nguồn SP/SQL (chứa danh sách filter + Source_Object).</param>
    /// <param name="filterValues">Giá trị người dùng nhập, key = <c>Filter_Code</c>. Code lạ bị bỏ qua.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Toàn bộ dòng kết quả (client phân trang) + tổng số.</returns>
    Task<ViewDataResult> GetFilteredDataAsync(
        ViewMetadata view, IReadOnlyDictionary<string, string?> filterValues, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách View (header tóm tắt) có phân trang + filter — không nạp cột/action.
    /// Ưu tiên bản tenant-specific hơn bản global khi trùng View_Code (chỉ trả 1 dòng/code).
    /// </summary>
    /// <param name="tenantId">Tenant hiện tại; bản global (Tenant_Id NULL) luôn match.</param>
    /// <param name="langCode">Mã ngôn ngữ resolve Title (mặc định "vi").</param>
    /// <param name="isActive">Lọc theo trạng thái (null = tất cả).</param>
    /// <param name="search">Từ khóa LIKE trên View_Code/Title (null = không lọc).</param>
    /// <param name="page">Trang (1-based).</param>
    /// <param name="pageSize">Số dòng mỗi trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách item + tổng số dòng khớp filter.</returns>
    Task<(IReadOnlyList<ViewListItem> Items, int TotalCount)> GetListAsync(
        int tenantId, string langCode = "vi", bool? isActive = null, string? search = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default);
}

/// <summary>DTO tóm tắt một View cho danh sách chọn (không load đầy đủ aggregate).</summary>
public sealed class ViewListItem
{
    public int ViewId { get; init; }
    public string ViewCode { get; init; } = string.Empty;
    public string ViewType { get; init; } = "Grid";
    public string TableCode { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? EditFormCode { get; init; }
    public int ColumnCount { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>Kết quả truy vấn dữ liệu cho một View (rows + tổng số).</summary>
public sealed class ViewDataResult
{
    /// <summary>Các dòng dữ liệu — mỗi dòng là map cột → giá trị.</summary>
    public IReadOnlyList<IDictionary<string, object?>> Items { get; init; } = [];

    /// <summary>Tổng số dòng khớp filter (cho paging).</summary>
    public int TotalCount { get; init; }
}
