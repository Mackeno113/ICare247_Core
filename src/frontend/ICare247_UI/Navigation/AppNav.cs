// File    : AppNav.cs
// Module  : ICare247_UI (host)
// Layer   : Frontend (UI)
// Purpose : Danh mục điều hướng tĩnh — nguồn dữ liệu cho NavMenu + trang placeholder.
//           Khai báo cây phân hệ (module) và các màn con để "hình dung" cấu trúc app
//           khi chưa code nghiệp vụ. Không chứa dữ liệu/logic — chỉ tiêu đề + route key.
//           Các tiêu đề ở đây là FALLBACK tiếng Việt, được render qua Loc.L(key, title)
//           trong NavMenu → KHÔNG hardcode. Đánh dấu để scanner bỏ qua: i18n:skip-hardcode

namespace ICare247_UI.Navigation;

/// <summary>Một màn hình con trong 1 phân hệ.</summary>
/// <param name="Key">Khóa route (slug), ví dụ "company". Cũng dùng để suy key i18n.</param>
/// <param name="Title">Tiêu đề tiếng Việt — base/fallback khi chưa dịch.</param>
/// <param name="Permission">Khóa quyền yêu cầu để thấy màn (null = ai cũng thấy). #4</param>
public sealed record NavScreen(string Key, string Title, string? Permission = null);

/// <summary>Một phân hệ (module / bounded context) gồm nhiều màn con.</summary>
/// <param name="Key">Khóa route, ví dụ "organization". Cũng dùng để suy key i18n.</param>
/// <param name="Title">Tiêu đề tiếng Việt — base/fallback khi chưa dịch.</param>
/// <param name="Group">Khóa nhóm hiển thị (vd "operations") — gom phân hệ thành cụm có nhãn.</param>
/// <param name="Icon">Tên icon (kiểu Lucide, vd "building") cho component dùng chung Icon.</param>
/// <param name="Screens">Danh sách màn con.</param>
/// <param name="Permission">Khóa quyền yêu cầu để thấy phân hệ (null = ai cũng thấy). #4</param>
public sealed record NavModule(string Key, string Title, string Group, string Icon, IReadOnlyList<NavScreen> Screens, string? Permission = null);

/// <summary>Một nhóm hiển thị trên sidebar (overline caption) gom nhiều phân hệ.</summary>
/// <param name="Key">Khóa nhóm, vd "operations". Cũng dùng để suy key i18n.</param>
/// <param name="Title">Nhãn nhóm tiếng Việt — base/fallback khi chưa dịch.</param>
public sealed record NavGroup(string Key, string Title);

/// <summary>Tiện ích suy khóa i18n từ cấu trúc (key luôn có trước, dịch sau).</summary>
public static class NavKeys
{
    /// <summary>Khóa nhãn nhóm, vd "nav.group.operations".</summary>
    public static string Group(string groupKey) => $"nav.group.{groupKey}";

    /// <summary>Khóa tiêu đề phân hệ, vd "nav.module.organization".</summary>
    public static string Module(string moduleKey) => $"nav.module.{moduleKey}";

    /// <summary>Khóa tiêu đề màn con, vd "nav.screen.organization.company".</summary>
    public static string Screen(string moduleKey, string screenKey) => $"nav.screen.{moduleKey}.{screenKey}";
}

/// <summary>
/// Cây điều hướng toàn app. Sửa ở đây là đổi cả menu lẫn các trang placeholder —
/// một nguồn sự thật duy nhất cho phần "hình dung cấu trúc".
/// </summary>
public static class AppNav
{
    /// <summary>
    /// Nhóm hiển thị theo thứ tự trên sidebar (overline caption). Phân hệ gom theo
    /// <see cref="NavModule.Group"/>. Tạo nhịp đọc mà không cần thêm màu.
    /// </summary>
    public static readonly IReadOnlyList<NavGroup> Groups = new List<NavGroup>
    {
        new("operations", "Vận hành"),
        new("business",   "Kinh doanh"),
        new("system",     "Hệ thống"),
    };

    /// <summary>Toàn bộ phân hệ theo thứ tự hiển thị trên sidebar.</summary>
    public static readonly IReadOnlyList<NavModule> Modules = new List<NavModule>
    {
        new("organization", "Tổ chức", "operations", "building", new List<NavScreen>
        {
            new("company",       "Công ty"),
            new("department",    "Phòng ban"),
            new("position",      "Vị trí công việc"),
            new("title",         "Chức danh"),
            new("headcount",     "Định biên nhân sự"),
            new("position-plan", "Hoạch định vị trí"),
            new("hr-cost",       "Chi phí nhân sự"),
        }),
        new("hr", "Nhân sự", "operations", "users", new List<NavScreen>
        {
            new("employee",  "Hồ sơ nhân viên"),
            new("contract",  "Hợp đồng lao động"),
            new("process",   "Quá trình công tác"),
            new("transfer",  "Điều chuyển"),
            new("reward",    "Khen thưởng – Kỷ luật"),
        }),
        new("payroll", "Chấm công – Lương", "operations", "clock", new List<NavScreen>
        {
            new("timesheet", "Chấm công"),
            new("period",    "Kỳ lương"),
            new("payslip",   "Bảng lương"),
            new("config",    "Thiết lập lương"),
        }),
        new("trade", "Bán hàng & Kho", "business", "package", new List<NavScreen>
        {
            new("product",   "Danh mục hàng hóa"),
            new("purchase",  "Mua hàng"),
            new("sales",     "Bán hàng"),
            new("stock-in",  "Nhập kho"),
            new("stock-out", "Xuất kho"),
            new("stock",     "Tồn kho"),
        }),
        new("finance", "Công nợ", "business", "credit-card", new List<NavScreen>
        {
            new("receivable", "Công nợ phải thu"),
            new("payable",    "Công nợ phải trả"),
        }),
        new("reporting", "Báo cáo", "business", "bar-chart", new List<NavScreen>
        {
            new("inventory", "Báo cáo tồn kho"),
            new("debt",      "Báo cáo công nợ"),
            new("pnl",       "Kết quả kinh doanh"),
            new("hr",        "Báo cáo nhân sự"),
            new("payroll",   "Báo cáo công lương"),
        }),
        new("administration", "Quản trị hệ thống", "system", "sliders", new List<NavScreen>
        {
            new("users",       "Người dùng"),
            new("roles",       "Vai trò"),
            new("permissions", "Phân quyền"),
            new("settings",    "Cấu hình hệ thống"),
        }),
    };

    /// <summary>
    /// Tìm phân hệ theo khóa route. Sự kiện theo sau: trả null nếu không có khóa khớp.
    /// </summary>
    public static NavModule? FindModule(string? key)
        => Modules.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Tìm tiêu đề màn con theo khóa. Sự kiện theo sau: trả chính khóa nếu không tìm thấy.
    /// </summary>
    public static string ScreenTitle(NavModule module, string? screenKey)
        => module.Screens.FirstOrDefault(s => string.Equals(s.Key, screenKey, StringComparison.OrdinalIgnoreCase))?.Title
           ?? screenKey
           ?? string.Empty;
}
