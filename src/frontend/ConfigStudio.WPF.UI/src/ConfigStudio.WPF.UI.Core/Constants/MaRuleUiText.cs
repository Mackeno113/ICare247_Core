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
    public const string Refresh = "Làm mới";
    public const string Save = "Lưu";
    public const string Edit = "Sửa";
    public const string Delete = "Xóa";
    public const string Cancel = "Hủy";
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
    public const string MoveUp = "Lên";
    public const string MoveDown = "Xuống";

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

    public const string PreviewCodeColumn = "Mã dự kiến";
    public const string TipPreviewCodeColumn =
        "Mã mẫu ghép từ các đoạn — chỉ để xem trước. Đoạn FIELD/LOOKUP hiện placeholder vì ConfigStudio "
        + "không đọc giá trị thật ở Data DB; mã thật chỉ chốt khi ứng dụng thật sự lưu bản ghi.";

    public const string ConfirmDeleteTitle = "Xác nhận xóa quy tắc";
    public const string ConfirmDeleteButton = "Xóa quy tắc";

    // Placeholder trong preview cho đoạn phụ thuộc dữ liệu (không đọc Data DB từ ConfigStudio)
    public const string SamplePlaceholderChar = "X";

    // ── Tooltip hướng dẫn nhập liệu (hover để xem) ──────────────────────────
    public const string TipNew = "Tạo quy tắc sinh mã mới cho một cặp (Bảng, Cột mã).";
    public const string TipRefresh = "Tải lại danh sách quy tắc từ Config DB.";
    public const string TipEdit = "Mở quy tắc đang chọn để sửa các đoạn ghép mã.";
    public const string TipDeleteList = "Xóa vĩnh viễn quy tắc đang chọn (có hỏi xác nhận trước khi xóa). Quy tắc hệ thống không xóa được.";

    public const string TipTargetTable = "Bảng dữ liệu đích sẽ áp dụng sinh mã tự động, ví dụ TC_PhongBan.";
    public const string TipTargetColumn = "Cột trong bảng đích được gán mã sinh ra — thường là 'Ma'.";
    public const string TipStep = "Số tăng thêm mỗi lần cấp số thứ tự (đoạn SEQ). Thường để 1.";
    public const string TipAllowManual = "Bật: cho người dùng gõ đè mã dự kiến trên form Thêm mới. Tắt: mã luôn do hệ thống cấp, không sửa được.";
    public const string TipIsActive = "Tắt để tạm ngưng quy tắc mà không cần xóa hẳn.";
    public const string TipDescription = "Ghi chú nội bộ cho người cấu hình sau này — không hiển thị cho người dùng cuối.";

    public const string TipAddSegment = "Thêm 1 đoạn mới vào cuối danh sách ghép mã.";
    public const string TipRemoveSegment = "Xóa đoạn đang chọn khỏi quy tắc.";
    public const string TipMoveUp = "Đẩy đoạn đang chọn lên 1 bậc — thứ tự đoạn quyết định thứ tự ghép trong mã.";
    public const string TipMoveDown = "Đẩy đoạn đang chọn xuống 1 bậc — thứ tự đoạn quyết định thứ tự ghép trong mã.";

    public const string TipSegmentType =
        "LITERAL = chữ cố định · DATE = ngày tháng · FIELD = lấy giá trị field khác · LOOKUP = tra sang bảng khác "
        + "· SEQ = số thứ tự tự tăng. Bắt buộc đúng 1 đoạn SEQ trong toàn quy tắc.";
    public const string TipLiteralText = "Chuỗi ký tự cố định sẽ ghép nguyên vào mã, ví dụ 'CT-'.";
    public const string TipDateFormat = "Định dạng ngày hệ thống chèn vào mã (giờ địa phương): yyyy=năm 4 số, yy=năm 2 số, MM=tháng, dd=ngày, yyyyMM=năm+tháng.";
    public const string TipFieldCode = "Mã cột trong bảng đích — giá trị của field này (trong bản ghi đang lưu) sẽ được ghép vào mã.";
    public const string TipLookupTable = "Bảng sẽ tra để lấy giá trị ghép vào mã, ví dụ TC_CongTy.";
    public const string TipLookupKeyCol = "Cột khóa dùng để so khớp trong bảng tra — thường là 'Id'.";
    public const string TipLookupValCol = "Cột lấy giá trị sau khi tra được — thường là 'Ma'.";
    public const string TipSubstringStart = "Bỏ qua N ký tự đầu của giá trị trước khi ghép (để trống = lấy từ đầu).";
    public const string TipWidth =
        "Ép đoạn về đúng số ký tự này (đệm thêm nếu thiếu, cắt bớt nếu dư). Đoạn đứng TRƯỚC đoạn SEQ bắt buộc "
        + "đặt > 0 (trừ LITERAL) để hệ thống dò đúng phần số.";
    public const string TipPadChar = "Ký tự dùng để đệm cho đủ Độ rộng, ví dụ '0'.";
    public const string TipPadSide = "L = đệm bên trái (0007), R = đệm bên phải (7000).";
    public const string TipTextTransform = "Chuyển chữ HOA/thường cho đoạn này trước khi ghép vào mã.";

    public const string TipSave = "Lưu quy tắc — bắt buộc có đúng 1 đoạn SEQ trong danh sách đoạn.";
    public const string TipDeleteDialog = "Xóa vĩnh viễn quy tắc này (có hỏi xác nhận trước khi xóa).";
    public const string TipCancel = "Đóng lại, không lưu thay đổi.";

    // ── Hướng dẫn chi tiết (Expander trong popup) — nội dung bám spec 32 §3 ──
    public const string DetailGuideToggle = "📖 Hướng dẫn chi tiết cách ghép mã";
    public const string DetailGuideIntro = "Mã hoàn chỉnh = ghép các ĐOẠN theo đúng thứ tự trong lưới bên dưới. Mỗi đoạn có 1 trong 5 loại:";
    public const string DetailGuideLiteral = "LITERAL — chữ cố định. Ví dụ 'CT-' → luôn ra đúng 'CT-'.";
    public const string DetailGuideDate = "DATE — ngày giờ hiện tại theo định dạng bạn chọn. Ví dụ 'yyyy' → '2026'.";
    public const string DetailGuideField = "FIELD — lấy thẳng giá trị 1 cột khác trong bản ghi đang lưu.";
    public const string DetailGuideLookup = "LOOKUP — lấy giá trị 1 field khác làm khóa, tra sang bảng khác lấy về 1 cột hiển thị. Ví dụ: CongTy_Id → tra TC_CongTy.Ma → ra 'CT01'.";
    public const string DetailGuideSeq = "SEQ — số thứ tự tự tăng, hệ thống tự cấp khi lưu. BẮT BUỘC đúng 1 đoạn SEQ trong quy tắc.";
    public const string DetailGuideRule1 =
        "Ràng buộc 1: quy tắc phải có đúng 1 đoạn SEQ — không có thì mọi bản ghi ra cùng 1 mã (trùng ngay dòng "
        + "thứ hai); có 2 đoạn thì hệ thống không biết đoạn nào mang số.";
    public const string DetailGuideRule2 =
        "Ràng buộc 2: mọi đoạn đứng TRƯỚC đoạn SEQ phải có độ dài xác định — hoặc là LITERAL, hoặc đặt 'Độ rộng "
        + "cố định' > 0. Nếu không, hệ thống không biết mã cũ dài bao nhiêu để cắt ra đúng phần số khi tính số tiếp theo.";
    public const string DetailGuideExampleTitle = "Ví dụ: mã phòng ban dạng CT01-PHONG-007";
    public const string DetailGuideExampleBody =
        "1) LOOKUP: CongTy_Id → TC_CongTy.Ma → 'CT01'\n"
        + "2) LITERAL: '-'\n"
        + "3) LOOKUP: Cap_Id → DM_CapPhongBan.Ma → 'PHONG'\n"
        + "4) LITERAL: '-'\n"
        + "5) SEQ: Độ rộng=3, đệm '0' → '007'";
    public const string DetailGuideScope =
        "Phạm vi đánh số luôn TỰ SUY RA từ các đoạn đứng trước SEQ — không có ô cấu hình riêng. Ở ví dụ trên, số "
        + "007 đếm RIÊNG cho từng cặp (công ty, cấp) vì phần đứng trước SEQ là 'CT01-PHONG-'; đổi công ty/cấp là "
        + "tự đánh số lại từ đầu, không cần khai gì thêm. Muốn mỗi công ty đánh số riêng thì mã BẮT BUỘC phải "
        + "chứa mã công ty (đoạn LOOKUP/FIELD/LITERAL công ty phải đứng trước SEQ).";
}
