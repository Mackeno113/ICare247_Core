// File    : ViewManagerUiText.cs
// Module  : Core
// Layer   : Shared
// Purpose : Chuỗi hướng dẫn tập trung cho popup "Chi tiết cấu hình" của màn Quản Lý View
//           (tab Cột / Actions / Bộ lọc) — nội dung bám docs/huong-dan-wpf/cau-hinh-man-quan-ly-view.md.

namespace ConfigStudio.WPF.UI.Core.Constants;

/// <summary>Nhãn + hướng dẫn cho popup cấu hình nhanh một dòng Cột/Action/Filter (Ui_View).</summary>
public static class ViewManagerUiText
{
    public const string DetailDialogCancel = "Hủy";
    public const string DetailDialogApply = "Áp dụng";

    // ── Tab Cột (Ui_View_Column) ─────────────────────────────────────────────
    public const string ColumnDetailTitlePrefix = "Cấu hình cột";

    public const string ColFieldName = "Tên trường (Field_Name)";
    public const string TipColFieldName = "Tên field trên control — phải khớp tên cột dữ liệu trả về từ nguồn.";

    public const string ColCaptionKey = "Tiêu đề cột (i18n)";
    public const string TipColCaptionKey = "Key i18n tiêu đề cột. Trống = fallback sang Label field → Field_Name.";

    public const string ColKind = "Loại cột";
    public const string TipColKind = "Loại cột: Data (dữ liệu thường) | Selection (cột chọn dòng) | Command (nút thao tác) | TreeSpin (nút mở/thu cây).";

    public const string ColRenderMode = "Kiểu hiển thị";
    public const string TipColRenderMode = "Cách render ô: Text | Html | Image | Link | Badge | Boolean | Template.";

    public const string ColFkLookupFieldId = "Tra cứu FK (bảng cha)";
    public const string TipColFkLookupFieldId =
        "Chọn field khóa ngoại của form Sửa (Edit_Form) → engine tự JOIN bảng cha, cột hiện TÊN thay vì Id. "
        + "Trống = cột thường, không JOIN.";

    public const string ColWidth = "Độ rộng";
    public const string TipColWidth = "Độ rộng cột (px). Trống = engine tự co giãn theo nội dung.";

    public const string ColMinWidth = "Độ rộng tối thiểu";
    public const string TipColMinWidth = "Độ rộng tối thiểu (px) — cột không co nhỏ hơn giá trị này khi tự co giãn.";

    public const string ColTextAlign = "Canh lề";
    public const string TipColTextAlign = "Canh chỉnh nội dung ô: left | center | right.";

    public const string ColFixedPosition = "Ghim cột";
    public const string TipColFixedPosition = "Đóng băng cột khi cuộn ngang: none (không ghim) | left | right.";

    public const string ColDisplayFormat = "Định dạng hiển thị";
    public const string TipColDisplayFormat = "Display format áp cho giá trị, ví dụ 'n0' (số nguyên có phân cách), 'dd/MM/yyyy' (ngày).";

    public const string ColIsVisible = "Hiện trên lưới";
    public const string TipColIsVisible = "Hiện cột trên lưới. Tắt = ẩn hẳn khỏi client (khác với 'Export' — cột vẫn có thể xuất dù ẩn).";

    public const string ColAllowSort = "Cho phép sắp xếp";
    public const string TipColAllowSort = "Cho phép người dùng bấm header để sắp xếp theo cột này.";

    public const string ColSortOrder = "Sắp xếp mặc định";
    public const string TipColSortOrder = "Sắp xếp mặc định khi mở lưới lần đầu: asc | desc. Trống = không sắp xếp sẵn.";

    public const string ColSortIndex = "Ưu tiên sắp xếp";
    public const string TipColSortIndex = "Thứ tự ưu tiên khi sắp xếp nhiều cột cùng lúc (số nhỏ hơn ưu tiên trước). Trống = không tham gia sort mặc định nhiều cột.";

    public const string ColAllowFilter = "Cho phép lọc";
    public const string TipColAllowFilter = "Cho lọc theo cột này (dòng filter dưới header, khi Show_Filter_Row bật ở tab Hành vi).";

    public const string ColAllowGroup = "Cho phép nhóm";
    public const string TipColAllowGroup = "Cho kéo cột này vào panel nhóm (khi Show_Group_Panel bật ở tab Hành vi).";

    public const string ColSummaryType = "Tổng hợp";
    public const string TipColSummaryType = "Dòng tổng cuối lưới cho cột này: count | sum | avg | min | max. Trống = không tính tổng.";

    public const string ColAllowExport = "Cho phép xuất";
    public const string TipColAllowExport = "Cho xuất cột khi bấm nút xuất file. Cột trang trí (HTML/Command/Selection) nên bỏ tick.";

    public const string ColIsImportKey = "Khóa trùng khi nhập";
    public const string TipColIsImportKey = "Tick các cột làm khóa kiểm trùng khi import (upsert theo khóa này). Tick nhiều cột = khóa ghép.";

    public const string ColOrderNo = "Thứ tự";
    public const string TipColOrderNo = "Thứ tự hiển thị cột trái→phải. Đổi bằng nút ↑ ↓ trên toolbar, không gõ tay ở đây.";

    // ── Tab Actions (Ui_View_Action) ─────────────────────────────────────────
    public const string ActionDetailTitlePrefix = "Cấu hình action";

    public const string ActActionCode = "Action_Code";
    public const string TipActActionCode = "Mã hành động, ví dụ add/edit/delete/export/print/refresh hoặc mã tùy biến riêng.";

    public const string ActActionType = "Type";
    public const string TipActActionType = "Loại hành động: BuiltIn (có sẵn engine xử lý) | Export | Print | Navigate (điều hướng URL) | Event (bắn event code) | Api (gọi endpoint).";

    public const string ActScope = "Scope";
    public const string TipActScope = "Vị trí hiện nút: Toolbar (trên đầu lưới) | Row (trên từng dòng) | Both.";

    public const string ActLabelKey = "Label (i18n)";
    public const string TipActLabelKey = "Key i18n nhãn nút hiển thị cho người dùng.";

    public const string ActIcon = "Icon";
    public const string TipActIcon = "Tên/ký tự unicode icon hiển thị trên nút — literal kỹ thuật, KHÔNG dịch i18n.";

    public const string ActExportFormat = "Export_Format";
    public const string TipActExportFormat = "Định dạng file khi Type = Export: xlsx | xls | csv | pdf | docx.";

    public const string ActExportEngine = "Engine";
    public const string TipActExportEngine = "Cơ chế xuất: Grid (xuất client-side từ DxGrid, chỉ xlsx/csv) | Server (xuất theo mẫu tài liệu, cho pdf/docx).";

    public const string ActTarget = "Target";
    public const string TipActTarget = "Giá trị đích tùy Type: url (Navigate) | event_code (Event) | endpoint (Api) | mã bộ mẫu tài liệu (Export/Print engine=Server).";

    public const string ActRequireSelection = "Req_Sel";
    public const string TipActRequireSelection = "Bắt buộc người dùng chọn ít nhất 1 dòng trước khi nút này chạy được.";

    // ── Tab Bộ lọc (Ui_View_Filter) ───────────────────────────────────────────
    public const string FilterDetailTitlePrefix = "Cấu hình bộ lọc";

    public const string FilFilterCode = "Filter_Code";
    public const string TipFilFilterCode = "Định danh kỹ thuật control, duy nhất trong View — client gửi giá trị lọc kèm theo code này.";

    public const string FilControlType = "Control";
    public const string TipFilControlType = "Loại editor hiển thị trên panel lọc: Text | Number | Date | Combo | MultiSelect | Checkbox | Radio.";

    public const string FilLabelKey = "Label (i18n)";
    public const string TipFilLabelKey = "Key i18n nhãn control — bắt buộc phải có khi lưu.";

    public const string FilParamName = "Param_Name";
    public const string TipFilParamName =
        "Tên tham số SP/SQL nhận giá trị lọc, ví dụ @TuNgay, @MaBN. Luôn được whitelist + truyền parameterized "
        + "(không nối chuỗi trực tiếp vào câu lệnh).";

    public const string FilParamType = "Type";
    public const string TipFilParamType = "Kiểu ép giá trị trước khi truyền tham số: string | int | decimal | date | bool.";

    public const string FilOperator = "Op";
    public const string TipFilOperator =
        "Toán tử so sánh dùng ở SP/SQL: = | LIKE (engine tự bọc %...%) | >= | <= | IN. "
        + "Khoảng giá trị (từ–đến) tách 2 dòng riêng: 1 dòng Op >= và 1 dòng Op <=.";

    public const string FilDefaultValue = "Mặc định";
    public const string TipFilDefaultValue = "Giá trị khởi tạo sẵn khi mở panel lọc — literal, KHÔNG phải key i18n.";

    public const string FilIsRequired = "Bắt buộc";
    public const string TipFilIsRequired = "Phải nhập control này mới cho bấm Tìm — thiếu sẽ chặn, báo lỗi và focus vào ô thiếu.";

    public const string FilIsVisible = "Hiện";
    public const string TipFilIsVisible = "Hiện control này trên panel lọc.";

    public const string FilColSpan = "ColSpan";
    public const string TipFilColSpan = "Số cột chiếm trên panel lọc (panel chia lưới 4 cột).";

    public const string FilLookupCode = "LookupCode";
    public const string TipFilLookupCode = "Mã danh sách tĩnh (Sys_Lookup.Lookup_Code) khi Control = Combo/MultiSelect/Radio lấy options cố định.";

    public const string FilOrderNo = "Order";
    public const string TipFilOrderNo = "Thứ tự hiển thị control trên panel lọc. Đổi bằng nút ↑ ↓ trên toolbar, không gõ tay ở đây.";

    /// <summary>Chữ hiển thị cho lựa chọn "không chọn gì" trong combo popup chi tiết (map về null khi Commit).</summary>
    public const string ComboNone = "(mặc định)";

    // ── Nhóm nghiệp vụ trong popup Chi tiết cấu hình (hierarchy — icare247-admin-ui §5.1) ───
    public const string SectionColSource = "Nguồn dữ liệu";
    public const string SectionColDisplay = "Kích thước & hiển thị";
    public const string SectionColSortFilter = "Sắp xếp, lọc & nhóm";
    public const string SectionColSummaryExport = "Tổng hợp & xuất";

    public const string SectionActIdentity = "Định danh & vị trí";
    public const string SectionActTarget = "Đích hành động";
    public const string SectionActCondition = "Điều kiện chạy";

    public const string SectionFilIdentity = "Định danh & control";
    public const string SectionFilCondition = "Điều kiện lọc";
    public const string SectionFilLayout = "Bố cục & nguồn dữ liệu";
}
