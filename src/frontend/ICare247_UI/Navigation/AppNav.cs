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
/// <param name="Route">Route đích tường minh (vd "/view/Tree_TC_CongTy") → màn engine-driven.
///   null = dùng placeholder ScreenView "/m/{module}/{screen}". (ORG-CFG-4)</param>
public sealed record NavScreen(string Key, string Title, string? Permission = null, string? Route = null);

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
    /// <summary>Khóa nhãn nhóm, vd "nav.group.operations". Đoạn khóa luôn được ASCII-hóa (xem <see cref="Slug"/>).</summary>
    public static string Group(string groupKey) => $"nav.group.{Slug(groupKey)}";

    /// <summary>Khóa tiêu đề phân hệ, vd "nav.module.organization". Đoạn khóa luôn được ASCII-hóa.</summary>
    public static string Module(string moduleKey) => $"nav.module.{Slug(moduleKey)}";

    /// <summary>Khóa tiêu đề màn con, vd "nav.screen.organization.company". Đoạn khóa luôn được ASCII-hóa.</summary>
    public static string Screen(string moduleKey, string screenKey) => $"nav.screen.{Slug(moduleKey)}.{Slug(screenKey)}";

    /// <summary>Khóa mục đơn cấp gốc, vd "nav.dashboard". ASCII-hóa.</summary>
    public static string Root(string ma) => $"nav.{Slug(ma)}";

    /// <summary>
    /// Chuẩn hóa 1 đoạn khóa i18n về ASCII-slug: bỏ dấu tiếng Việt, đ→d, thường hóa, ký tự lạ → '-'.
    /// Dùng CHUNG cho NavMenu (lúc dịch) và Menu Builder (lúc hiển thị khóa) để 2 nơi luôn khớp.
    /// "Hệ thống" → "he-thong"; "Grid_DM_QuocGia" → "grid-dm-quocgia".
    /// </summary>
    public static string Slug(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        // Bỏ prefix kỹ thuật nếu lỡ truyền cả Ma (group./view./form.).
        foreach (var p in new[] { "group.", "view.", "form." })
            if (s.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { s = s[p.Length..]; break; }

        var norm = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(norm.Length);
        foreach (var ch in norm)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue; // bỏ dấu kết hợp
            var c = char.ToLowerInvariant(ch);
            if (c is 'đ') sb.Append('d');
            else if (c is >= 'a' and <= 'z' or >= '0' and <= '9') sb.Append(c);
            else sb.Append('-');
        }
        var slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
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
            // Công ty: engine-driven — TreeList đọc vw_TC_CongTy (Ui_View "Tree_TC_CongTy"). (ORG-CFG-4)
            new("company",       "Công ty", Route: "/view/Tree_TC_CongTy"),
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
        // Danh mục nền tảng (engine-driven Grid). Route → /view/Grid_* (Ui_View tự cấu hình trong ConfigStudio).
        // Thứ tự cấu hình theo phụ thuộc: Quốc gia → Tỉnh/TP → Phường/Xã (cascade lookup).
        new("catalog", "Danh mục", "system", "list", new List<NavScreen>
        {
            new("quoc-gia",      "Quốc gia",        Route: "/view/Grid_DM_QuocGia"),
            new("tinh-thanh",    "Tỉnh / Thành phố", Route: "/view/Grid_DM_TinhThanhPho"),
            new("phuong-xa",     "Phường / Xã",     Route: "/view/Grid_DM_PhuongXa"),
            new("don-vi-tinh",   "Đơn vị tính",     Route: "/view/Grid_DM_DonViTinh"),
            new("ngan-hang",     "Ngân hàng",       Route: "/view/Grid_DM_NganHang"),
            new("cap-cong-ty",   "Cấp công ty",     Route: "/view/Grid_TC_CapCongTy"),
            new("cap-phong-ban", "Cấp phòng ban",   Route: "/view/Grid_TC_CapPhongBan"),
        }),
        new("administration", "Quản trị hệ thống", "system", "sliders", new List<NavScreen>
        {
            new("users",       "Người dùng"),
            new("roles",       "Vai trò"),
            new("permissions", "Phân quyền"),
            new("config-sync", "Đồng bộ cấu hình"),
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
