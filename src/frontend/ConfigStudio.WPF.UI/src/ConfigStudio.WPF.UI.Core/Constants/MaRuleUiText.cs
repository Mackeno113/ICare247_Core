// File    : MaRuleUiText.cs
// Module  : Core
// Layer   : Shared
// Purpose : Chuỗi giao diện tập trung cho màn "Quy tắc sinh mã" (MA-6), tránh rải literal.

namespace ConfigStudio.WPF.UI.Core.Constants;

/// <summary>Chuỗi giao diện của màn Quy tắc sinh mã.</summary>
public static class MaRuleUiText
{
    public const string ScreenTitle = "Quy tắc sinh mã";
    public const string ScreenSubtitle = "Cấu hình tự sinh mã (cột Ma) theo các đoạn ghép: chữ cố định, ngày, field, tra bảng, số thứ tự.";
    public const string New = "+ Tạo mới";
    public const string Refresh = "↻";
    public const string Save = "Lưu";
    public const string Delete = "Xóa";
    public const string ListTitle = "Danh sách quy tắc";
    public const string CreateTitle = "Tạo quy tắc sinh mã";
    public const string EditTitle = "Chỉnh sửa quy tắc sinh mã";

    public const string EditorHelp =
        "Mã được ghép từ các ĐOẠN theo thứ tự. Phải có đúng 1 đoạn SEQ (số thứ tự); mọi đoạn đứng TRƯỚC SEQ "
        + "phải có độ dài xác định (LITERAL, hoặc đặt 'Độ rộng' > 0) để hệ thống cắt được phần số khi dò mã lớn nhất.";

    public const string BasicSection = "Đích áp dụng";
    public const string TargetTable = "Bảng *";
    public const string TargetColumn = "Cột mã *";
    public const string Step = "Bước nhảy";
    public const string AllowManual = "Cho gõ tay đè";
    public const string Active = "Đang hoạt động";
    public const string Description = "Mô tả";

    public const string SegmentSection = "Các đoạn ghép mã";
    public const string AddSegment = "+ Thêm đoạn";
    public const string RemoveSegment = "Xóa đoạn";
    public const string MoveUp = "▲";
    public const string MoveDown = "▼";

    // Cột lưới đoạn
    public const string ColOrder = "#";
    public const string ColType = "Loại";
    public const string ColContent = "Nội dung";
    public const string ColWidth = "Độ rộng";
    public const string ColSample = "Mẫu";

    // Panel thuộc tính đoạn
    public const string SegPropTitle = "Thuộc tính đoạn";
    public const string SegPropNoSelection = "Chọn một đoạn ở lưới để sửa thuộc tính.";
    public const string LiteralText = "Chữ cố định";
    public const string DateFormat = "Định dạng ngày";
    public const string SourceField = "Field nguồn";
    public const string LookupTable = "Bảng tra";
    public const string LookupKeyCol = "Cột khóa (=)";
    public const string LookupValCol = "Cột lấy giá trị";
    public const string SubstringStart = "Cắt từ ký tự";
    public const string Width = "Độ rộng cố định";
    public const string PadChar = "Ký tự đệm";
    public const string PadSide = "Đệm phía";
    public const string TextTransform = "Biến đổi chữ";

    public const string PreviewLabel = "Xem trước:";
    public const string ScopeLabel = "Phạm vi đánh số:";
    public const string ScopeHelp =
        "= phần đứng trước đoạn SEQ. Mã cùng phần này dùng chung một dãy số; đổi phần này (vd sang năm/công ty khác) "
        + "là tự đánh số lại từ đầu.";

    public const string IndexAdvisory =
        "Lưu ý: bảng đích PHẢI có index thường trên cột mã — filtered unique (WHERE IsDeleted=0) KHÔNG đủ vì engine "
        + "dò cả bản ghi đã xóa mềm. Hãy tạo index này ở Data DB trước khi bật quy tắc chạy thật.";

    public const string System = "Quy tắc hệ thống";
    public const string Customized = "Đã tùy biến";

    public const string ConfirmDeleteTitle = "Xác nhận xóa quy tắc";
    public const string ConfirmDeleteButton = "Xóa quy tắc";

    // Placeholder trong preview cho đoạn phụ thuộc dữ liệu (không đọc Data DB từ ConfigStudio)
    public const string SamplePlaceholderChar = "X";
}
