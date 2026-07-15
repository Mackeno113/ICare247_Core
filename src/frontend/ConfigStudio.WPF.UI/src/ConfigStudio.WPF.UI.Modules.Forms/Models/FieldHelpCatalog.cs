// File    : FieldHelpCatalog.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Kho nội dung hướng dẫn chi tiết (tiếng Việt) cho từng ô cấu hình control
//           trên màn Field Config — tra theo topic key, render qua HelpAssist.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Kho tra cứu <see cref="FieldHelpTopic"/> theo key. Mỗi ô cấu hình trên các panel
/// Control Props khai <c>help:HelpAssist.Topic="key"</c> để lấy hướng dẫn tương ứng.
/// Sự kiện theo sau: HelpAssist.OnTopicChanged gọi <see cref="TryGet"/> → dựng ToolTip.
/// </summary>
public static class FieldHelpCatalog
{
    /// <summary>
    /// Tra nội dung hướng dẫn theo key. Trả false khi key chưa có trong kho
    /// (ô nhập sẽ không có tooltip thay vì ném lỗi — phòng thủ khi thêm ô mới quên khai).
    /// </summary>
    public static bool TryGet(string key, out FieldHelpTopic topic)
    {
        var found = Topics.TryGetValue(key, out var t);
        topic = t!;
        return found;
    }

    /// <summary>Toàn bộ topic — key nhóm theo tiền tố: fk. (FK Lookup), tree., popup., cb. (ComboBox), lookup. (Sys_Lookup), addNew., prop. (dynamic props).</summary>
    private static readonly IReadOnlyDictionary<string, FieldHelpTopic> Topics =
        new Dictionary<string, FieldHelpTopic>
    {
        // ══════════ FK Lookup — Nguồn dữ liệu (LookupBox / TreeLookupBox / ComboBox dynamic) ══════════

        ["fk.queryMode"] = new(
            Title:   "Chế độ truy vấn — chọn cách lấy dữ liệu",
            Purpose: "Quyết định hệ thống lấy danh sách lựa chọn từ DB bằng cách nào.",
            HowTo:
            [
                "Bảng / View — dùng cho 90% trường hợp: đọc thẳng 1 bảng danh mục hoặc View có JOIN sẵn.",
                "Function (TVF) — khi cần logic lọc phức tạp có tham số (VD: chỉ lấy phòng ban còn hiệu lực theo ngày).",
                "SQL tùy chỉnh — khi cần JOIN nhiều bảng/điều kiện đặc biệt mà View chưa có.",
            ],
            Pitfalls:
            [
                "Đổi chế độ sẽ chuyển sang nhóm ô nhập khác — cấu hình của chế độ cũ không được dùng nữa.",
                "Ưu tiên Bảng/View: chế độ này tự reload cascade theo @param trong Filter SQL; TVF/SQL phải khai reload thủ công.",
            ]),

        ["fk.valueField"] = new(
            Title:   "Cột Value — giá trị LƯU vào DB",
            Purpose: "Tên cột trong bảng nguồn mà giá trị của nó sẽ được lưu vào cột FK của form hiện tại.",
            HowTo:
            [
                "Nhập tên cột khóa (thường là cột Id / khóa chính) của BẢNG NGUỒN.",
                "Kiểu dữ liệu phải khớp cột FK của field này (int/bigint).",
                "Với TVF / SQL tùy chỉnh: nhập đúng ALIAS trong câu SELECT.",
            ],
            Example: "PhongBan_Id  ·  Id",
            Pitfalls:
            [
                "Nhầm với cột hiển thị (Ten_...) → form sẽ lưu sai giá trị vào DB.",
                "Cột phải tồn tại trong bảng nguồn — gõ sai tên là runtime lỗi khi mở form.",
            ]),

        ["fk.displayField"] = new(
            Title:   "Cột Display — text HIỂN THỊ cho user",
            Purpose: "Tên cột trong bảng nguồn dùng làm text hiển thị trong ô input và danh sách chọn.",
            HowTo:
            [
                "Nhập cột chứa tên/nhãn dễ đọc của bảng nguồn (không phải cột Id).",
                "Với TVF / SQL tùy chỉnh: nhập đúng ALIAS trong câu SELECT.",
            ],
            Example: "Ten_PhongBan  ·  Ten",
            Pitfalls:
            [
                "Để trống → ô input hiển thị rỗng dù đã chọn được giá trị.",
            ]),

        ["fk.tableName"] = new(
            Title:   "Tên bảng hoặc View nguồn",
            Purpose: "Bảng/View trong Data DB chứa danh sách lựa chọn.",
            HowTo:
            [
                "Nhập đúng tên bảng danh mục (DM_...) hoặc View (vw_...).",
                "Dùng View khi cần JOIN sẵn nhiều bảng (VD: phòng ban kèm tên chi nhánh).",
                "Với TreeLookupBox: bảng phải có cột chứa Id cha (VD: Parent_Id) để dựng cây.",
            ],
            Example: "DM_PhongBan  ·  vw_PhongBan_Full",
            Pitfalls:
            [
                "Gõ sai tên bảng → form runtime báo lỗi khi load lookup, không báo ngay lúc lưu cấu hình.",
                "Bấm nút Diễn giải để kiểm tra cấu hình trước khi lưu.",
            ]),

        ["fk.filterSql"] = new(
            Title:   "Filter SQL — điều kiện WHERE lọc dữ liệu",
            Purpose: "Điều kiện lọc thêm vào WHERE khi truy vấn bảng nguồn (không cần gõ chữ WHERE).",
            HowTo:
            [
                "Viết điều kiện SQL thuần, nối nhiều điều kiện bằng AND / OR.",
                "Tham số hệ thống tự có: @TenantId, @Today, @CurrentUser.",
                "Lọc CASCADE theo field khác trên form: dùng @TênFieldCode (VD: ChiNhanh_Id = @ChiNhanhId) — lookup tự reload khi field đó đổi.",
            ],
            Example: "Is_Active = 1 AND ChiNhanh_Id = @ChiNhanhId",
            Pitfalls:
            [
                "@param phải trùng đúng Field_Code có trên form — gõ sai tên là danh sách luôn rỗng.",
                "Không nhúng giá trị người dùng trực tiếp vào chuỗi — luôn dùng @param (chống SQL injection).",
            ]),

        ["fk.orderBy"] = new(
            Title:   "Sắp xếp (ORDER BY)",
            Purpose: "Thứ tự hiển thị các dòng trong danh sách chọn.",
            HowTo:
            [
                "Nhập tên cột + ASC (tăng) hoặc DESC (giảm); nhiều cột cách nhau dấu phẩy.",
                "Để trống = theo thứ tự mặc định của DB (không đảm bảo ổn định).",
            ],
            Example: "Ten_PhongBan ASC  ·  ThuTu ASC, Ten DESC"),

        ["fk.searchEnabled"] = new(
            Title:   "Cho phép tìm kiếm",
            Purpose: "Bật ô tìm kiếm trong popup để user gõ lọc nhanh danh sách.",
            HowTo:
            [
                "Nên BẬT khi danh mục > 20 dòng để user khỏi cuộn tìm.",
                "Tìm kiếm chạy trên cột Display.",
            ]),

        ["fk.functionName"] = new(
            Title:   "Tên Function (TVF)",
            Purpose: "Table-Valued Function trong DB trả về danh sách lựa chọn — dùng khi logic lọc phức tạp.",
            HowTo:
            [
                "Nhập đúng tên hàm đã tạo trong Data DB (thường tiền tố fn_).",
                "Hệ thống sẽ chạy: SELECT ... FROM fn_Tên(@P1, @P2).",
                "Khai đủ tham số ở mục 'Tham số hàm' bên dưới — đúng THỨ TỰ định nghĩa hàm.",
            ],
            Example: "fn_GetPhongBanHieuLuc",
            Pitfalls:
            [
                "Thứ tự tham số sai → DB báo lỗi hoặc trả kết quả sai lặng lẽ.",
                "Cột Value/Display phải khớp alias cột mà TVF trả về.",
            ]),

        ["fk.functionParams"] = new(
            Title:   "Tham số hàm (theo thứ tự)",
            Purpose: "Danh sách giá trị truyền vào TVF — mỗi dòng 1 tham số, đúng thứ tự định nghĩa hàm trong DB.",
            HowTo:
            [
                "@Tên tham số: tên khai trong TVF (không bắt buộc trùng, chỉ để đọc hiểu).",
                "Nguồn = field: lấy giá trị field khác trên form → điền Field_Code vào ô 'Field / System key'.",
                "Nguồn = system: dùng khóa hệ thống @TenantId / @Today / @CurrentUser.",
            ],
            Example: "@NgayHieuLuc ← field NgayVaoLam",
            Pitfalls:
            [
                "Thứ tự dòng = thứ tự truyền vào hàm — kéo đúng vị trí trước khi lưu.",
            ]),

        ["fk.selectSql"] = new(
            Title:   "SELECT SQL tùy chỉnh",
            Purpose: "Câu SELECT đầy đủ do bạn tự viết — dùng khi Bảng/View và TVF không đáp ứng được.",
            HowTo:
            [
                "Viết trọn câu SELECT gồm JOIN/WHERE tùy ý.",
                "Alias cột trả về PHẢI khớp với Cột Value và Cột Display đã khai ở trên.",
                "Dùng được @TenantId, @Today, @CurrentUser và @FieldCode như Filter SQL.",
            ],
            Example: "SELECT p.Id, p.Ten FROM DM_PhongBan p JOIN DM_ChiNhanh c ON ... WHERE p.Tenant_Id = @TenantId",
            Pitfalls:
            [
                "SQL mode KHÔNG tự reload cascade — cần khai 'Tự reload thủ công theo 1 field' nếu SQL tham chiếu field khác.",
                "Thiếu alias khớp Value/Display → lookup trả rỗng hoặc lỗi mapping.",
            ]),

        ["fk.codeField"] = new(
            Title:   "Cột Mã (Code_Field) — cầu nối Import",
            Purpose: "Cột chứa MÃ nghiệp vụ của bảng nguồn — dùng để đổi Mã ↔ Id khi Import Excel và xuất template.",
            HowTo:
            [
                "Nhập tên cột mã nghiệp vụ trong bảng nguồn (thường là Ma).",
                "Import Excel: user gõ Mã, hệ thống tự tra ra Id để lưu.",
                "Cũng là cột mã hiển thị khi 'EditBox hiển thị' = mã + tên.",
            ],
            Example: "Ma  ·  Ma_PhongBan",
            Pitfalls:
            [
                "Để trống → cột này không hỗ trợ Import theo Mã (template xuất Id thô, khó nhập tay).",
            ]),

        ["fk.importGlobalCode"] = new(
            Title:   "Import: resolve Mã toàn cục",
            Purpose: "Khi Import, bỏ qua Filter SQL (bỏ lọc theo cha) và tra Mã trên TOÀN bảng nguồn.",
            HowTo:
            [
                "Chỉ dành cho FK lọc cascade theo cha (VD: Chi nhánh lọc theo Ngân hàng).",
                "BẬT khi file Excel chỉ có cột Mã con mà không có cột cha đi kèm.",
            ],
            Pitfalls:
            [
                "CHỈ bật khi Mã con là DUY NHẤT trên toàn bảng — nếu trùng Mã, import từ chối cả file.",
            ]),

        // ══════════ EditBox hiển thị ══════════

        ["fk.editBoxMode"] = new(
            Title:   "Chế độ hiển thị trong ô input",
            Purpose: "Quy định ô input hiển thị gì sau khi user đã chọn 1 dòng.",
            HowTo:
            [
                "TextOnly — chỉ hiện text cột Display (mặc định, gọn nhất).",
                "CodeAndName — hiện badge mã nhỏ + tên (cần khai Cột Mã ở mục Nguồn dữ liệu FK).",
                "Custom — template hiển thị riêng (cần dev hỗ trợ).",
            ],
            Pitfalls:
            [
                "Chọn CodeAndName mà chưa khai Cột Mã → badge mã hiển thị rỗng.",
            ]),

        // ══════════ Popup grid ══════════

        ["popup.size"] = new(
            Title:   "Kích thước popup (px)",
            Purpose: "Chiều rộng / chiều cao của bảng popup khi user bấm mở danh sách chọn.",
            HowTo:
            [
                "Rộng: ước lượng theo tổng độ rộng các cột popup (mặc định 600).",
                "Cao: 400 hiển thị ~10 dòng; tăng khi danh mục dài.",
            ]),

        ["popup.columns"] = new(
            Title:   "Cột hiển thị trong popup",
            Purpose: "Khai các cột của bảng nguồn sẽ hiện trong grid popup để user đối chiếu khi chọn.",
            HowTo:
            [
                "Để TRỐNG = popup chỉ hiện 1 cột Display đơn giản (không dùng grid) — đủ cho danh mục ngắn.",
                "Tên cột DB: cột trong bảng nguồn / alias SELECT.",
                "Resource Key (i18n): khóa dịch cho tiêu đề cột — theo convention module.col.ten_cot.",
                "Rộng (px): độ rộng từng cột; dùng nút ▲▼ để sắp thứ tự hiển thị.",
            ],
            Example: "PhongBan_Code · phongban.col.ma_phong_ban · 100",
            Pitfalls:
            [
                "Tên cột không tồn tại trong nguồn → cột trống trên popup.",
                "Quên khai Resource Key → tiêu đề cột hiện key thô, không dịch được đa ngôn ngữ.",
            ]),

        ["fk.reloadTrigger"] = new(
            Title:   "Tự reload thủ công theo 1 field (nâng cao)",
            Purpose: "Ép lookup tải lại danh sách khi 1 field khác trên form đổi giá trị.",
            HowTo:
            [
                "Nhập Field_Code của field trigger (VD: ChiNhanhId).",
                "CHỈ cần khi: nguồn là TVF / SQL tùy chỉnh, hoặc muốn reload theo field KHÔNG xuất hiện trong Filter SQL.",
                "Chế độ Bảng/View đã TỰ reload theo mọi @param trong Filter SQL — để trống là đủ.",
            ],
            Pitfalls:
            [
                "Khai trùng với @param đã có trong Filter SQL là thừa — gây reload 2 lần vô ích.",
            ]),

        // ══════════ TreeLookupBox ══════════

        ["tree.parentColumn"] = new(
            Title:   "Cột cha (Parent Column) — BẮT BUỘC",
            Purpose: "Tên cột trong bảng nguồn chứa Id của dòng cha — hệ thống dựa vào đây để dựng cây cha/con.",
            HowTo:
            [
                "Nhập đúng tên cột self-reference của bảng nguồn.",
                "Dòng gốc (root) có giá trị cột này = NULL hoặc 0.",
            ],
            Example: "Parent_Id  ·  PhongBan_Cha_Id",
            Pitfalls:
            [
                "Bảng nguồn KHÔNG có cột này → cây không dựng được, popup trống.",
                "Dữ liệu vòng lặp (A cha B, B cha A) sẽ làm cây hiển thị sai — kiểm tra dữ liệu nguồn.",
            ]),

        ["tree.selectableLevel"] = new(
            Title:   "Cấp node được chọn",
            Purpose: "Giới hạn user chỉ được chọn node ở cấp nào của cây.",
            HowTo:
            [
                "all — chọn mọi node (mặc định).",
                "leaf — CHỈ node lá, không có con (VD: chỉ chọn Xã, không chọn Tỉnh/Huyện).",
                "branch — CHỈ node có con (VD: chỉ chọn nhóm, không chọn phần tử).",
            ],
            Pitfalls:
            [
                "Chọn leaf khi dữ liệu chưa đủ cấp con → user không chọn được gì.",
            ]),

        // ══════════ Thêm mới entity ══════════

        ["addNew.allow"] = new(
            Title:   "Cho phép thêm mới ngay trên control (➕)",
            Purpose: "Hiện nút 'Thêm mới' trong dropdown để user tạo nhanh bản ghi danh mục mà không rời form.",
            HowTo:
            [
                "BẬT khi user nhập liệu thường gặp giá trị chưa có trong danh mục (VD: thêm Xã mới).",
                "Sau khi bật, PHẢI chọn Form Code dialog ở ô bên dưới.",
            ]),

        ["addNew.formCode"] = new(
            Title:   "Form Code dialog thêm mới",
            Purpose: "Ui_Form sẽ mở dạng dialog khi user bấm 'Thêm mới' — form này phải bound đúng BẢNG NGUỒN của lookup.",
            HowTo:
            [
                "Chọn Form_Code có sẵn trong danh sách (form đã cấu hình trong Config Studio).",
                "Form phải insert vào đúng bảng nguồn của lookup; field code = tên cột DB.",
            ],
            Example: "DS_XaPhuong",
            Pitfalls:
            [
                "Chọn form bound bảng khác → thêm xong không thấy trong danh sách lookup.",
            ]),

        // ══════════ Sys_Lookup (RadioGroup / LookupComboBox) ══════════

        ["lookup.code"] = new(
            Title:   "Lookup Code — mã danh mục tĩnh",
            Purpose: "Mã nhóm danh mục trong bảng Sys_Lookup — nguồn options cho RadioGroup / LookupComboBox.",
            HowTo:
            [
                "Chọn mã có sẵn trong dropdown (đã seed trong Sys_Lookup) hoặc gõ mã mới.",
                "Giá trị LƯU vào DB là Item_Code của từng option (nvarchar), không phải Id.",
                "Chọn xong nhìn mục 'Xem trước options' để xác nhận đúng danh mục.",
            ],
            Example: "GENDER  ·  TRANGTHAI_PO",
            Pitfalls:
            [
                "Sys_Lookup chỉ dành cho danh mục TĨNH ngắn (giới tính, trạng thái...). Danh mục nghiệp vụ có bảng riêng → dùng LookupBox (FK).",
            ]),

        // ══════════ ComboBox / LookupComboBox — Tìm kiếm & Hiển thị ══════════

        ["cb.searchMode"] = new(
            Title:   "Chế độ tìm kiếm",
            Purpose: "Cách dropdown phản ứng khi user gõ chữ vào ô.",
            HowTo:
            [
                "None — tắt tìm kiếm (danh mục rất ngắn).",
                "AutoFilter — gõ đến đâu LỌC danh sách còn lại đến đó (khuyên dùng).",
                "AutoSearch — gõ để TÌM và highlight item khớp, danh sách giữ nguyên.",
            ]),

        ["cb.searchFilterCondition"] = new(
            Title:   "Điều kiện so khớp",
            Purpose: "Quy tắc so sánh chuỗi khi tìm kiếm/lọc.",
            HowTo:
            [
                "Contains — chứa chuỗi gõ ở bất kỳ vị trí nào (khuyên dùng cho tiếng Việt).",
                "StartsWith — bắt đầu bằng chuỗi gõ.",
                "Equals — khớp chính xác toàn bộ.",
            ]),

        ["cb.allowUserInput"] = new(
            Title:   "Cho phép nhập text tự do",
            Purpose: "User được gõ giá trị KHÔNG có trong danh sách và giá trị đó sẽ được lưu.",
            HowTo:
            [
                "Chỉ bật cho field dạng gợi ý mở (VD: chức danh tự do).",
                "Field FK / danh mục chuẩn hóa → để TẮT.",
            ],
            Pitfalls:
            [
                "Bật cho cột FK int sẽ gây lỗi lưu — giá trị gõ tay không phải Id hợp lệ.",
            ]),

        ["cb.nullTextKey"] = new(
            Title:   "Placeholder key (NullText)",
            Purpose: "Khóa i18n cho dòng chữ mờ hiển thị khi CHƯA chọn giá trị.",
            HowTo:
            [
                "Nhập resource key theo convention: module.placeholder.ten_field.",
                "Bản dịch khai trong hệ i18n — không gõ text cứng ở đây.",
            ],
            Example: "nhanvien.placeholder.phong_ban"),

        ["cb.dropDownWidthMode"] = new(
            Title:   "Chiều rộng dropdown",
            Purpose: "Cách tính độ rộng danh sách thả xuống.",
            HowTo:
            [
                "Auto — theo nội dung dài nhất.",
                "EditorWidth — bằng đúng độ rộng ô input.",
            ]),

        ["cb.clearButton"] = new(
            Title:   "Nút xóa (Clear)",
            Purpose: "Hiện nút ✕ để user xóa nhanh giá trị đã chọn.",
            HowTo:
            [
                "Bật cho field KHÔNG bắt buộc để user bỏ chọn dễ dàng.",
                "Field bắt buộc (Required) → nên tắt để tránh trạng thái trống.",
            ]),

        ["cb.groupField"] = new(
            Title:   "Group theo field",
            Purpose: "Nhóm các item trong dropdown theo giá trị của 1 cột (hiện header nhóm).",
            HowTo:
            [
                "Nhập tên cột/alias trong nguồn dữ liệu dùng làm tiêu chí nhóm.",
                "Để trống = không nhóm.",
            ],
            Example: "LoaiPhongBan"),

        ["cb.disabledField"] = new(
            Title:   "Field disable item",
            Purpose: "Cột bool trong nguồn dữ liệu — item có giá trị true sẽ hiện mờ, không chọn được.",
            HowTo:
            [
                "Nhập tên cột bool (bit) của nguồn.",
                "Dùng khi muốn HIỆN nhưng chặn chọn (VD: phòng ban ngừng hoạt động).",
            ],
            Example: "Is_Locked"),

        // ══════════ Dynamic props (TextBox / NumericBox / DatePicker / AttachmentBox ...) ══════════

        ["prop.maxLength"] = new(
            Title:   "maxLength — độ dài tối đa",
            Purpose: "Số ký tự tối đa user được nhập vào ô.",
            HowTo:
            [
                "Đặt ≤ độ dài cột DB (VD cột nvarchar(100) → maxLength 100).",
                "Mặc định 255 nếu để trống.",
            ],
            Pitfalls:
            [
                "Đặt LỚN hơn độ dài cột DB → lỗi truncate khi lưu.",
            ]),

        ["prop.isMultiline"] = new(
            Title:   "isMultiline — nhiều dòng",
            Purpose: "Cho phép xuống dòng trong ô text.",
            HowTo:
            [
                "true = ô cao nhiều dòng (kết hợp prop rows).",
                "false = 1 dòng (mặc định).",
            ]),

        ["prop.rows"] = new(
            Title:   "rows — số dòng hiển thị",
            Purpose: "Chiều cao ô text tính theo số dòng.",
            HowTo:
            [
                "Chỉ có tác dụng khi isMultiline = true (TextBox) hoặc với TextArea.",
                "Khuyến nghị ≥ 3 cho ô ghi chú/mô tả.",
            ]),

        ["prop.minValue"] = new(
            Title:   "minValue — giá trị nhỏ nhất",
            Purpose: "Chặn user nhập số nhỏ hơn ngưỡng này.",
            HowTo:
            [
                "Đặt theo nghiệp vụ (VD: số lượng ≥ 0, tuổi ≥ 18).",
                "Mặc định 0 nếu để trống.",
            ]),

        ["prop.maxValue"] = new(
            Title:   "maxValue — giá trị lớn nhất",
            Purpose: "Chặn user nhập số vượt ngưỡng này.",
            HowTo:
            [
                "Đặt theo nghiệp vụ; mặc định 999999.",
            ],
            Pitfalls:
            [
                "Quên nới maxValue cho cột tiền tệ → user không nhập được số lớn.",
            ]),

        ["prop.decimals"] = new(
            Title:   "decimals — số chữ số thập phân",
            Purpose: "Số chữ số sau dấu phẩy được nhập/hiển thị.",
            HowTo:
            [
                "0 = số nguyên; 2 = tiền tệ thông dụng.",
                "Khớp với scale của cột decimal trong DB (decimal(18,2) → decimals 2).",
            ]),

        ["prop.spinStep"] = new(
            Title:   "spinStep — bước nhảy",
            Purpose: "Giá trị cộng/trừ mỗi lần bấm nút mũi tên ▲▼ của ô số.",
            HowTo:
            [
                "Mặc định 1; đặt 1000/10000 cho ô tiền để bấm nhanh.",
            ]),

        ["prop.allowNull"] = new(
            Title:   "allowNull — cho phép để trống",
            Purpose: "Ô được phép không có giá trị (NULL) hay không.",
            HowTo:
            [
                "true = để trống được (cột DB phải NULLABLE).",
                "false = luôn phải có giá trị.",
            ],
            Pitfalls:
            [
                "allowNull=true nhưng cột DB NOT NULL → lỗi khi lưu bản ghi trống.",
            ]),

        ["prop.format"] = new(
            Title:   "format — định dạng ngày",
            Purpose: "Cách hiển thị và nhập ngày/giờ trong ô.",
            HowTo:
            [
                "dd/MM/yyyy — chỉ ngày (phổ biến nhất).",
                "dd/MM/yyyy HH:mm — ngày kèm giờ phút (cột datetime).",
                "MM/yyyy hoặc yyyy — kỳ báo cáo tháng/năm.",
            ]),

        ["prop.minDate"] = new(
            Title:   "minDate — ngày nhỏ nhất",
            Purpose: "Chặn chọn ngày trước mốc này.",
            HowTo:
            [
                "Bỏ trống = không giới hạn.",
                "Nhập ngày cố định theo định dạng yyyy-MM-dd.",
            ]),

        ["prop.maxDate"] = new(
            Title:   "maxDate — ngày lớn nhất",
            Purpose: "Chặn chọn ngày sau mốc này.",
            HowTo:
            [
                "Bỏ trống = không giới hạn.",
                "Nhập ngày cố định theo định dạng yyyy-MM-dd.",
            ]),

        ["prop.loai"] = new(
            Title:   "loai — phân loại tệp",
            Purpose: "Nhãn phân loại tệp đính kèm để lọc/nhóm khi xem lại.",
            HowTo:
            [
                "Nhập mã phân loại tự đặt (VD: HopDong, Anh, HoSo).",
                "Bỏ trống = không phân loại.",
            ]),

        ["prop.ownerTable"] = new(
            Title:   "ownerTable — bảng chủ (chế độ đa tệp)",
            Purpose: "Bảng nghiệp vụ mà các tệp đính kèm thuộc về (liên kết qua TT_TepDinhKem).",
            HowTo:
            [
                "Bỏ trống = tự suy từ bảng bound của form (đủ cho đa số trường hợp).",
                "Chỉ khai khi form lưu nhiều bảng và cần chỉ định rõ.",
                "Chế độ 1-tệp (IsVirtual tắt) không dùng prop này.",
            ]),

        ["prop.ownerIdField"] = new(
            Title:   "ownerIdField — khóa record chủ",
            Purpose: "Field trong context form dùng lấy Owner_Id cho tệp đính kèm.",
            HowTo:
            [
                "Mặc định: Id — đúng cho hầu hết form.",
                "Chỉ đổi khi khóa bản ghi nằm ở field khác.",
            ]),

        ["prop.dataSource"] = new(
            Title:   "dataSource — API endpoint",
            Purpose: "URL API trả về danh sách options cho ComboBox.",
            HowTo:
            [
                "Nhập đường dẫn tương đối của endpoint (VD: /api/phongban).",
                "API phải trả mảng JSON có field khớp valueField/displayField.",
            ],
            Example: "/api/phongban"),

        ["prop.valueField"] = new(
            Title:   "valueField — field giá trị",
            Purpose: "Tên field trong JSON API dùng làm giá trị lưu.",
            HowTo:
            [
                "Mặc định: id.",
            ]),

        ["prop.displayField"] = new(
            Title:   "displayField — field hiển thị",
            Purpose: "Tên field trong JSON API dùng làm text hiển thị.",
            HowTo:
            [
                "Mặc định: name.",
            ]),
    };
}
