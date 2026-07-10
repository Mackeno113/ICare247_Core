// File    : IViewRepository.cs
// Module  : View
// Layer   : Application
// Purpose : Repository interface cho cụm Ui_View (header + cột + action) — đọc metadata hiển thị.

using ICare247.Domain.Entities.View;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Ui_View</c> + <c>Ui_View_Column</c> + <c>Ui_View_Action</c> (Config DB).
/// Cô lập tenant ở tầng connection (1 Config DB = 1 tenant, ADR-035); localize text i18n.
/// </summary>
public interface IViewRepository
{
    /// <summary>
    /// Lấy <see cref="ViewMetadata"/> đầy đủ (header + cột + action) theo View_Code,
    /// đã resolve text i18n (Title/Caption/Label/...) theo <paramref name="langCode"/>.
    /// </summary>
    /// <param name="viewCode">Ui_View.View_Code — unique trong Config DB.</param>
    /// <param name="tenantId">Chỉ dùng dựng cache key (Redis L2 dùng chung), KHÔNG lọc SQL.</param>
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
    /// Nạp options cho 1 control lọc Combo/MultiSelect/Radio (cascade — ADR-030). Nguồn:
    /// <c>static</c> → Sys_Lookup theo Lookup_Code; <c>dynamic</c> → chạy Lookup_Sql (Data DB) bind giá trị
    /// filter CHA (whitelist theo <c>Depends_On</c>) + token ngữ cảnh (<c>Sys_Context_Param</c>, spec 19).
    /// </summary>
    /// <param name="view">Metadata View chứa filter.</param>
    /// <param name="filterCode">Filter_Code của control cần nạp options.</param>
    /// <param name="parentValues">Giá trị filter cha hiện tại (key = Filter_Code) — chỉ cha hợp lệ được bind.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách (value, display); rỗng nếu cha chưa đủ giá trị hoặc không có nguồn.</returns>
    Task<IReadOnlyList<FilterOption>> GetFilterOptionsAsync(
        ViewMetadata view, string filterCode, IReadOnlyDictionary<string, string?> parentValues,
        string langCode = "vi", CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách View (header tóm tắt) có phân trang + filter — không nạp cột/action.
    /// </summary>
    /// <param name="tenantId">Chỉ dùng dựng cache key (Redis L2 dùng chung), KHÔNG lọc SQL.</param>
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

/// <summary>Một option cho control lọc Combo/MultiSelect/Radio (cascade — ADR-030).</summary>
public sealed class FilterOption
{
    /// <summary>Giá trị gửi lên khi chọn (Filter value → @param).</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Nhãn hiển thị.</summary>
    public string Display { get; init; } = string.Empty;
}

/// <summary>Kết quả truy vấn dữ liệu cho một View (rows + tổng số).</summary>
public sealed class ViewDataResult
{
    /// <summary>Các dòng dữ liệu — mỗi dòng là map cột → giá trị.</summary>
    public IReadOnlyList<IDictionary<string, object?>> Items { get; init; } = [];

    /// <summary>Tổng số dòng khớp filter (cho paging).</summary>
    public int TotalCount { get; init; }
}
