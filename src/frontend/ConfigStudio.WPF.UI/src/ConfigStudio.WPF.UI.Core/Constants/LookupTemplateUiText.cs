// File    : LookupTemplateUiText.cs
// Module  : Core
// Layer   : Shared
// Purpose : Chuỗi giao diện tập trung cho màn quản lý mẫu lookup.

namespace ConfigStudio.WPF.UI.Core.Constants;

/// <summary>Chuỗi giao diện của module Mẫu Lookup, tránh rải literal trong View/ViewModel.</summary>
public static class LookupTemplateUiText
{
    public const string ScreenTitle = "Mẫu Lookup";
    public const string ScreenSubtitle = "Quản lý mẫu nguồn dữ liệu dùng lại cho LookupBox và TreeLookupBox.";
    public const string New = "+ Tạo mới";
    public const string Refresh = "↻";
    public const string Save = "Lưu";
    public const string Delete = "Xóa";
    public const string Cancel = "Hủy";
    public const string CreateTitle = "Tạo mẫu lookup";
    public const string EditTitle = "Chỉnh sửa mẫu lookup";
    public const string ListTitle = "Danh sách mẫu";
    public const string BasicSection = "Thông tin cơ bản";
    public const string SourceSection = "Nguồn truy vấn";
    public const string ColumnSection = "Ánh xạ cột";
    public const string AdvancedSection = "Lọc, cây và tham số";
    public const string SyncSection = "Trạng thái đồng bộ";
    public const string EditorHelp = "Mẫu gom nguồn truy vấn, ánh xạ cột và tham số để nhiều field dùng chung. Các trường có dấu * là bắt buộc.";
    public const string TemplateCode = "Template_Code *";
    public const string Name = "Tên *";
    public const string Description = "Mô tả";
    public const string QueryMode = "Query_Mode *";
    public const string SourceName = "Source_Name *";
    public const string ValueColumn = "Value_Column *";
    public const string DisplayColumn = "Display_Column *";
    public const string CodeField = "Code_Field";
    public const string FilterSql = "Filter_Sql";
    public const string OrderBy = "Order_By";
    public const string PopupColumnsJson = "Popup_Columns_Json";
    public const string ParentColumn = "Parent_Column";
    public const string CanonicalParams = "Canonical_Params";
    public const string Active = "Đang hoạt động";
    public const string System = "Mẫu hệ thống";
    public const string Customized = "Đã tùy biến";
    public const string SyncedAt = "Đồng bộ lúc";
    public const string SourceVer = "Phiên bản nguồn";
    public const string CodeHelp = "Khóa nghiệp vụ duy nhất; chỉ được nhập khi tạo mới để không làm gãy field đang tham chiếu.";
    public const string SourceHelpTable = "table/view: nhập tên bảng hoặc view.";
    public const string SourceHelpTvf = "tvf: nhập tên hàm trả về bảng.";
    public const string SourceHelpSql = "custom_sql: nhập câu SELECT đầy đủ; có thể xuống dòng.";
    public const string FilterHelp = "Điều kiện phải parameterized bằng @CanonicalParam hoặc @token; không nối giá trị trực tiếp.";
    public const string ParentHelp = "Cột tự tham chiếu cha để dựng cây; custom_sql phải SELECT ra cột này.";
    public const string CanonicalHelp = "JSON array, ví dụ [{\"name\":\"TinhId\",\"type\":\"bigint\",\"required\":true,\"moTa\":\"Field Tỉnh/Thành trên form\"}].";
    public const string PopupHelp = "JSON array mô tả các cột hiển thị trong popup lookup.";
    public const string ConfirmDeleteTitle = "Xác nhận xóa mẫu lookup";
    public const string ConfirmDeleteButton = "Xóa mẫu";
}
