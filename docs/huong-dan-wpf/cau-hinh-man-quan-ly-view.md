# Hướng dẫn cấu hình màn **Quản Lý View (Grid / Tree Grid)** — ConfigStudio

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh tên trường kỹ thuật của
> từng tab, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Hướng dẫn cách dùng màn **"Quản Lý View"** — nơi cấu hình mọi **màn danh
> sách** (lưới phẳng hoặc lưới cây) trong hệ thống: chọn nguồn dữ liệu, chọn cột hiển thị, bật/tắt các
> nút Thêm/Sửa/Xóa/Xuất file... **không cần viết code**. Đây là màn được dùng lại ở
> [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md), [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md)
> và [cau-hinh-man-phong-ban.md](cau-hinh-man-phong-ban.md) — bài này giải thích **đầy đủ từng ô** của
> màn đó, dùng cho khi bạn cần tra cứu kỹ hơn ngoài phần tối thiểu đã nêu ở các bài trên.
>
> Ví dụ xuyên suốt cả bài: dựng lưới danh sách **"Khách hàng"** (`Grid_KhachHang`).

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Lưới (Grid)** | Danh sách hiển thị phẳng dạng bảng — mỗi dòng 1 bản ghi, không có quan hệ cha-con. |
| **Lưới cây (TreeList)** | Danh sách hiển thị dạng cây cha–con lồng nhau (xem thêm bài Công ty/Phòng ban). |
| **Cột (Column)** | 1 cột dữ liệu hiển thị trên lưới — ứng với 1 field trên Form hoặc 1 cột trong bảng dữ liệu. |
| **Form Thêm/Sửa (Edit_Form)** | Form popup sẽ mở ra khi người dùng bấm nút Thêm mới, hoặc double-click 1 dòng trên lưới để sửa. |
| **Xuất file (Export)** | Cho phép tải dữ liệu đang xem ra file Excel/CSV/PDF/Word. |
| **Bộ lọc nâng cao (panel lọc)** | Panel lọc riêng bên trái màn hình, chỉ dùng cho lưới lấy dữ liệu từ 1 thủ tục/câu lệnh SQL riêng (không phải bảng thường) — tính năng nâng cao, thường cần IT/lập trình viên hỗ trợ khai báo. |
| **Bảng dữ liệu / Form / ConfigStudio / Đồng bộ cấu hình** | Xem lại [bảng thuật ngữ ở bài Danh mục](cau-hinh-man-danh-muc.md#vài-thuật-ngữ-cần-biết-trước-khi-đọc) nếu chưa rõ. |

---

## Phần A — Làm theo từng bước

### Bước 1 — Chọn nguồn dữ liệu (tab Cơ bản)

**Mục đích:** cho hệ thống biết lưới này hiển thị kiểu gì (phẳng hay cây), lấy dữ liệu từ bảng nào, và
bấm vào 1 dòng thì mở form nào để sửa.

**Làm gì:**
1. Bấm **Ctrl+N** (hoặc nút **Tạo mới**) để tạo 1 View mới.
2. Ở tab **Cơ bản**, **chọn `View_Type` TRƯỚC**: **Grid** (lưới phẳng bình thường) hoặc **TreeList**
   (lưới cây — dùng cho dữ liệu có quan hệ cha-con như Công ty, Phòng ban).
3. Ô **View_Code**: gõ phần tên riêng, ví dụ gõ `KhachHang` → hệ thống tự ghép thành **`Grid_KhachHang`**
   (xem dòng "→ View_Code:" để biết trước kết quả). **Lưu ý:** mã này phải khớp đúng với đường dẫn
   (route) khai trong menu, nếu không màn hình sẽ không mở được — nếu chưa rõ phần menu, hỏi người phụ
   trách cấu hình menu.
4. Ô **Bảng nguồn (Table)**: chọn bảng đã đăng ký trước đó ở màn Sys Table, ví dụ `DM_KhachHang`.
5. Ô **Title_Key**: bấm **🌐 Dịch**, đặt tiêu đề màn hiển thị cho người dùng, ví dụ "Danh sách khách
   hàng".
6. Ô **Form Thêm/Sửa (Edit_Form)**: chọn Form đã tạo trước đó nếu muốn cho phép Thêm/Sửa ngay từ lưới
   này. **Để trống = lưới chỉ đọc**, không cho sửa.
7. Ô **Key_Field**: gõ tên cột khóa chính của bảng nguồn, thường là `Id`.

**Bạn sẽ thấy gì:** 1 dòng View mới với mã `Grid_KhachHang` xuất hiện trong danh sách bên trái màn
hình.

**Lỗi thường gặp:** đổi `View_Type` **sau khi** đã gõ `View_Code` → mã không tự cập nhật đúng tiền tố,
phải xóa và gõ lại từ đầu (luôn chọn `View_Type` **trước** khi gõ mã).

---

### Bước 2 — Chọn cột hiển thị (tab Cột)

**Mục đích:** quyết định lưới hiện những cột nào, tiêu đề mỗi cột là gì, canh trái/phải, độ rộng ra
sao.

**Làm gì:**
1. Sang tab **Cột**.
2. Bấm **🔍 Chọn cột** → chọn các cột cần hiện từ danh sách cột có sẵn của bảng nguồn.
3. Với mỗi cột: bấm **🌐** để dịch tiêu đề (Caption) hiển thị cho người dùng cuối; chỉnh Width/Align
   nếu cần.
4. Sắp xếp lại thứ tự cột bằng nút **↑ ↓** (hoặc kéo-thả).

**Bạn sẽ thấy gì:** lưới hiện đúng các cột đã chọn, đúng tiêu đề tiếng Việt, đúng thứ tự mong muốn.

**Lỗi thường gặp:** cột hiện tên kỹ thuật (vd `MaKH`) thay vì tiêu đề tiếng Việt → quên bấm **🌐** dịch
Caption cho cột đó (dấu **✓ xanh** cạnh nút 🌐 nghĩa là đã có bản dịch; không có dấu ✓ = chưa dịch).

---

### Bước 3 — Bật các nút cần thiết (tab Hành vi + Export/Print)

**Mục đích:** quyết định người dùng có được thêm/sửa/xóa dữ liệu từ lưới không, có được xuất
Excel/PDF không.

**Làm gì:**
1. Sang tab **Hành vi**: tick **Allow_Add / Allow_Edit / Allow_Delete** nếu muốn cho phép (chỉ có tác
   dụng khi đã gắn **Edit_Form** ở Bước 1). Chọn **Selection_Mode** = `multiple` nếu cần chọn nhiều
   dòng để xóa hàng loạt.
2. (Tuỳ chọn) Sang tab **Export/Print**: tick **Allow_Export**, chọn định dạng ở **Export_Formats**
   (ví dụ `xlsx,csv,pdf`).

**Bạn sẽ thấy gì:** nút Thêm/nút xóa dòng xuất hiện trên màn thật; nếu bật Export, nút xuất file xuất
hiện trên thanh công cụ.

**Lỗi thường gặp:** tick **Allow_Add** nhưng không thấy nút Thêm trên màn thật → chưa chọn **Form
Thêm/Sửa (Edit_Form)** ở tab Cơ bản (Bước 1).

---

### Bước 4 — Cấu hình cho lưới dạng cây (chỉ áp dụng nếu chọn `View_Type = TreeList`)

**Mục đích:** khai báo cột nào là "cột cha" để lưới biết vẽ đúng cấu trúc cây mẹ-con.

**Làm gì:** sang tab **Cây** → chọn **Parent_Field** = cột lưu id của bản ghi cha (ví dụ `ParentId`)
→ đặt **Expand_Level** = số cấp muốn tự mở sẵn khi vào màn.

**Bạn sẽ thấy gì:** lưới hiện đúng dạng cây (thụt lề theo cấp) thay vì phẳng.

**Lỗi thường gặp:** quên chọn **Parent_Field** → lưới TreeList vẫn hiện phẳng như lưới Grid bình
thường, không có cây.

---

### Bước 5 — Lưu và đưa ra sử dụng

**Mục đích:** ghi lại cấu hình và cho màn xuất hiện trong menu để nhân viên dùng được.

**Làm gì:**
1. Bấm **💾 Lưu** (hoặc **Ctrl+S**).
2. Báo người phụ trách cấu hình menu thêm đường dẫn `/view/{View_Code vừa tạo}` vào menu.
3. Chạy **đồng bộ cấu hình** (giống các bài khác — vào ứng dụng web, **Quản trị › Đồng bộ cấu hình** →
   **Xem trước** → **Áp dụng từ master**) để đưa cấu hình xuống môi trường thật.

**Bạn sẽ thấy gì:** màn danh sách xuất hiện đúng trong menu, bấm vào ra đúng lưới vừa cấu hình.

**Lỗi thường gặp:** bấm vào menu báo lỗi "trang không tồn tại" → đường dẫn khai trong menu không khớp
chính xác `View_Code` (phải khớp cả chữ hoa/thường).

---

> **Cần thêm ô lọc kiểu "từ ngày – đến ngày", "mã khách hàng"... cho 1 màn lấy dữ liệu từ thủ tục/SQL
> riêng?** Đây là tính năng nâng cao (panel lọc trái, tab **Bộ lọc**) — nên nhờ IT/lập trình viên hỗ
> trợ vì cần khai đúng tên tham số SQL. Chi tiết đầy đủ xem [Tab 7 — Bộ lọc](#tab-7--bộ-lọc-panel-lọc-trái--lưới-nâng-cao)
> trong Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật
> của từng tab.
>
> Màn này cấu hình **hiển thị danh sách** (lưới Grid / cây TreeList) hoàn toàn metadata-driven:
> nguồn dữ liệu → cột → hành vi → export/print → panel lọc. **Tách khỏi form sửa** (`Ui_Form`/`Ui_Field`):
> một bảng dữ liệu → nhiều view; view trỏ `Edit_Form` để mở popup Thêm/Sửa.
>
> Bảng dưới Config DB: `Ui_View` (header) + `Ui_View_Column` (cột) + `Ui_View_Action` (nút) + `Ui_View_Filter` (panel lọc).
> Tham chiếu: spec `14_VIEW_CONFIG_SPEC.md` (ADR-015, ADR-016).

### ⚠️ Yêu cầu trước (1 lần) — nếu màn báo lỗi "Invalid column name"

Màn đọc các cột panel lọc (`Filter_Panel_Enabled`, `Filter_Panel_Position`, `Filter_Collapsible`,
`Auto_Search_On_Load`, `Search_Label_Key`, `Reset_Label_Key`) trên `Ui_View`. Nếu báo
**"Không thể tải danh sách View: Invalid column name ..."** nghĩa là DB chưa chạy migration:

```
db/034_create_ui_view_filter.sql   →  chạy trên DB ICare247_Config (idempotent, chạy lại an toàn)
```

Migration này tạo bảng `Ui_View_Filter`, thêm 6 cột cờ panel lọc vào `Ui_View`, và seed resource i18n
(nút Tìm/Đặt lại, thông báo thiếu tham số). Chạy xong → bấm **↻ Làm mới (F5)**.

> **Điều kiện chung:** bảng nguồn đã đăng ký ở **Forms › Sys Table**; (tuỳ) đã có `Ui_Form` Thêm/Sửa nếu
> muốn cho phép thêm/sửa từ lưới. Xem `cau-hinh-man-danh-muc.md` cho luồng đăng ký bảng + form.

### Bố cục màn

| Vùng | Chức năng |
|---|---|
| **Header** | Tiêu đề + ô tìm (`View_Code`/`View_Type`/`Table_Code`) + checkbox **Hiện cả View ẩn** + badge **Tenant_Id** + nút **↻** (F5) |
| **Trái** | Lưới danh sách View (Id / View_Code / Type / Bảng / Active). Chọn dòng → đổ sang editor |
| **Phải** | Editor 7 tab cấu hình (chi tiết bên dưới) |
| **Footer** | "Hiển thị {đã lọc} / {tổng} View" |

**Phím tắt:** `Ctrl+N` Tạo mới · `Ctrl+S` Lưu · `F5` Làm mới.
**Nút dưới editor:** **💾 Lưu** · **Tạo mới** · **Ẩn View** (soft-delete, set `Is_Active = 0`).

### Tab 1 — Cơ bản (nguồn dữ liệu)

Thứ tự nhập có chủ đích (① → ②):

| # | Trường | Bắt buộc | Ghi chú |
|---|---|:---:|---|
| ① | **View_Type** | ✓ | `Grid` \| `TreeList` \| `Cards`. **Chọn trước** vì nó quyết định **tiền tố** của `View_Code`. |
| ② | **View_Code** | ✓ | Tự ghép `{View_Type}_` + phần bạn nhập. VD chọn Grid + gõ `KhachHang` → **`Grid_KhachHang`**. PHẢI khớp route `/view/{code}` khai trong menu. Dòng `→ View_Code:` xem trước kết quả. |
| ③ | **Bảng nguồn (Table)** | ✓ | Chọn từ `Sys_Table` đã đăng ký. Là bảng **base** — dùng làm scope i18n + truy data. |
| | **Source_Type** | | `Table` (mặc định) \| `View` \| `Sp` \| `Api`. Khác `Table` → bắt buộc điền **Source_Object**. |
| | **Source_Object** | | Tên view / SP / câu SQL / endpoint khi `Source_Type ≠ Table`. Để trống khi = `Table`. |
| | **Title_Key** | | i18n tiêu đề màn. Nút **🌐 Dịch** tạo/sửa bản dịch (tự sinh key theo convention nếu trống). VD `khach_hang.view.grid.title`. |
| | **Form Thêm/Sửa (Edit_Form)** | | Trỏ `Ui_Form` để mở popup Thêm/Sửa. **Để trống = lưới chỉ đọc.** |
| | **Key_Field** | | Cột khóa chính bảng nguồn — cần cho **Sửa/Xóa theo dòng** và bắt buộc cho **TreeList**. VD `Id`. |
| | **Is_Active** | | View đang dùng. |
| | **Description** | | Mô tả nội bộ (không hiển thị cho người dùng cuối). |

> **i18n vs literal:** chỉ các `*_Key` là khóa dịch (`Sys_Resource`); `View_Code`, `View_Type`,
> `Source_*`, `Key_Field` là literal kỹ thuật — KHÔNG dịch.

### Tab 2 — Hành vi (lưới)

| Trường | Mặc định | Ý nghĩa |
|---|:---:|---|
| **Page_Size** | 20 | Số dòng/trang (1–1000). |
| **Selection_Mode** | none | `none` \| `single` \| `multiple` (thêm cột chọn). |
| Allow_Paging | ✓ | Bật phân trang. |
| Virtual_Scroll | ✗ | Cuộn ảo (data lớn). |
| Show_Filter_Row | ✓ | Dòng lọc ngay dưới header cột. |
| Show_Group_Panel | ✗ | Panel kéo-thả nhóm. |
| Show_Search_Box | ✓ | Ô tìm toàn lưới. |
| Show_Column_Chooser | ✗ | Cho người dùng bật/tắt cột. |
| **Allow_Add / Allow_Edit / Allow_Delete** | ✓ | Quyền CRUD trên lưới (chỉ hiệu lực khi đã gắn **Edit_Form**). |

### Tab 3 — Export / Print

| Trường | Ghi chú |
|---|---|
| **Allow_Export** | Cho xuất file. |
| **Export_Formats** | Danh sách phân tách phẩy: `xlsx,csv,pdf,docx`. |
| **Export_File_Name_Key** | i18n tên file (null → dùng `View_Code`). Nút **🌐 Dịch**. |
| **Allow_Print** | Cho in. |

> **Quy tắc engine:** `xlsx/csv` xuất **client-side** qua DxGrid; `pdf/docx` xuất **server-side** theo template.
> Export **luôn lấy giá trị thuần** (bỏ qua `Render_Mode`, không xuất thẻ HTML). Nút xuất chi tiết khai ở **tab Actions**.

### Tab 4 — Cây (TreeList)

Chỉ dùng khi **View_Type = TreeList** (có cảnh báo vàng nhắc).

| Trường | Ghi chú |
|---|---|
| **Key_Field** | Đặt ở **tab Cơ bản** — cột khóa của node. |
| **Parent_Field** | Cột trỏ node cha (hierarchy). VD `ParentId`. |
| **Expand_Level** | Mở sẵn tới cấp mấy (0–20). |

### Tab 5 — Cột (`Ui_View_Column`)

Lưới sửa trực tiếp (inline). Toolbar: **+ Thêm cột** · **− Xóa cột** · **↑ ↓** đổi thứ tự ·
**🔍 Chọn cột** (lấy từ `Sys_Column` của bảng nguồn) · **🌐 Dịch caption**.

| Cột lưới | Ý nghĩa | Giá trị |
|---|---|---|
| **Field_Name** * | Tên field trên control (khớp cột data). | |
| **Caption (i18n)** | Tiêu đề cột. Nút 🌐 mỗi dòng để dịch. **Trống = fallback** label field → `Field_Name`. | |
| **Kind** | Loại cột. | `Data` \| `Selection` \| `Command` \| `TreeSpin` |
| **Render** | Cách render ô. | `Text` \| `Html` \| `Image` \| `Link` \| `Badge` \| `Boolean` \| `Template` |
| **FK lookup (cha)** | Ô thân thiện cấu hình FK auto-JOIN: chọn `Field_Id` của LookupBox FK bên **form sửa** → engine tự JOIN bảng cha để hiện **TÊN** thay vì Id trên lưới. Ghi vào `Props_Json.fkLookup.fieldId` (giữ nguyên khóa khác trong Props_Json); trống/0 = gỡ cấu hình. | |
| **Width / MinWidth** | Độ rộng / độ rộng tối thiểu. | |
| **Align** | Canh chỉnh. | `left` \| `center` \| `right` |
| **Ghim** | Đóng băng cột (frozen). | `none` \| `left` \| `right` |
| **Format** | Display format. | VD `n0`, `dd/MM/yyyy` |
| **Visible** | Hiện cột. | |
| **Sort / SortMặc định / SortIdx** | Cho sắp xếp; sort mặc định khi mở (`asc`/`desc`); thứ tự ưu tiên khi sort nhiều cột. | |
| **Filter / Group** | Cho lọc / cho nhóm theo cột này. | |
| **Summary** | Dòng tổng. | `count` \| `sum` \| `avg` \| `min` \| `max` |
| **Export** | Cho xuất cột. Cột HTML trang trí / command / selection → **bỏ tick**. | |
| **Khóa trùng (import)** | Tick = cột này là 1 phần **khóa ghép** kiểm trùng khi import dữ liệu (`Is_Import_Key`). Tick nhiều cột = khóa ghép nhiều field. | |
| **Order** | Thứ tự cột trên lưới (đọc-only, đổi bằng ↑↓ hoặc kéo-thả). | |

> **Nút ⓘ đầu mỗi dòng** (cột Cột / Actions / Bộ lọc): mở popup **"Chi tiết cấu hình"** — sửa được
> trực tiếp từng field theo nhóm nghiệp vụ (không phải chỉ xem), kèm gợi ý bước tiếp theo tự cập nhật
> theo giá trị đang nhập. Bấm Lưu trong popup ghi thẳng vào dòng đang chọn trên lưới.
>
> **Dấu ✓ xanh cạnh nút 🌐** (cột Caption/Label): key đã có bản dịch (ngôn ngữ mặc định) trong
> Sys_Resource. Không có dấu ✓ = key rỗng hoặc chưa dịch — bấm 🌐 để dịch.

### Tab 6 — Actions (`Ui_View_Action`)

Nút toolbar / nút trên dòng. Toolbar: **+ Thêm action** · **− Xóa action** · **🌐 Dịch nhãn**.

| Cột lưới | Ý nghĩa | Giá trị |
|---|---|---|
| **Action_Code** * | Mã hành động. | `add`/`edit`/`delete`/`export`/`print`/`refresh`/`<custom>` |
| **Type** | Loại. | `BuiltIn` \| `Export` \| `Print` \| `Navigate` \| `Event` \| `Api` |
| **Scope** | Vị trí nút. | `Toolbar` \| `Row` \| `Both` |
| **Label (i18n)** | Nhãn nút (🌐 dịch theo dòng). | |
| **Icon** | Tên/unicode icon (literal, không dịch). | |
| **Export_Format** | Khi Type=Export. | `xlsx`/`xls`/`csv`/`pdf`/`docx` |
| **Engine** | Cơ chế xuất. | `Grid` (client) \| `Server` (template) |
| **Target** | url / event_code / endpoint / template tuỳ Type. | |
| **Req_Sel** | Bắt buộc chọn dòng mới chạy. | |

### Tab 7 — Bộ lọc (panel lọc trái — lưới nâng cao)

> **Chỉ hiển thị runtime khi:** `Filter_Panel_Enabled = 1` **VÀ** `Source_Type ∈ {Sp, Sql}` (tab Cơ bản)
> **VÀ** có ≥1 control. Nguồn `Table` → dùng filter row trong cột, **không** có panel này.

**Cờ panel:**

| Trường | Mặc định | Ghi chú |
|---|:---:|---|
| **Filter_Panel_Enabled** | ✗ | Bật panel lọc trái. |
| **Filter_Collapsible** | ✓ | Cho thu gọn panel. |
| **Auto_Search_On_Load** | ✗ | Tự Tìm khi mở. **Mặc định chờ bấm Tìm** — tránh chạy SP nặng ngay. |
| **Vị trí panel** | left | `left` \| `top`. |
| **Nhãn nút Tìm / Đặt lại** | | i18n; trống → dùng `common.filter.search` / `common.filter.reset`. |

**Danh sách control lọc** — toolbar: **+ Thêm filter** · **− Xóa filter** · **↑ ↓** · **🌐 Dịch nhãn**.
**Mỗi dòng = 1 control = 1 tham số** truyền vào SP/SQL.

| Cột lưới | Ý nghĩa | Giá trị |
|---|---|---|
| **Filter_Code** * | Định danh kỹ thuật (unique/View); client gửi value theo code này. | |
| **Control** | Loại editor. | `Text` \| `Number` \| `Date` \| `Combo` \| `MultiSelect` \| `Checkbox` \| `Radio` |
| **Label (i18n)** | Nhãn control (🌐 dịch theo dòng). | |
| **Param_Name** * | Tham số SP/SQL (whitelist). | VD `@TuNgay`, `@MaBN` |
| **Type** | Kiểu ép. | `string` \| `int` \| `decimal` \| `date` \| `bool` |
| **Op** | Toán tử. | `=` \| `LIKE` \| `>=` \| `<=` \| `IN` |
| **Mặc định** | Giá trị khởi tạo (literal, không i18n). | |
| **Bắt buộc** | Phải nhập mới cho Tìm → thiếu sẽ chặn + báo `"{0} là bắt buộc"` + focus ô lỗi. | |
| **Hiện / ColSpan** | Hiển thị / chiếm mấy cột (panel grid 4-col). | |
| **LookupCode** | Combo tĩnh: `Sys_Lookup.Lookup_Code`. | |
| **Order** | Thứ tự (đổi bằng ↑↓). | |

> **Khoảng giá trị (từ–đến):** tách **2 dòng** — vd `tu_ngay` (Op `>=`) + `den_ngay` (Op `<=`),
> mỗi dòng nhãn + Bắt buộc riêng để báo lỗi và focus đúng ô.
>
> **An toàn:** tham số luôn parameterized (Dapper, whitelist từ `Ui_View_Filter`). SP nên dùng pattern
> `WHERE (@x IS NULL OR col = @x)` để bỏ qua tham số rỗng. `LIKE` được engine bọc `%...%`.

### Quy trình tạo nhanh (checklist)

1. **Tạo mới** → tab **Cơ bản**: chọn `View_Type` → gõ hậu tố `View_Code` → chọn **Bảng nguồn** →
   (nếu cần) gắn **Edit_Form** + **Key_Field** → **💾 Lưu**.
2. Tab **Cột**: **🔍 Chọn cột** từ `Sys_Column` → tinh chỉnh Render/Width/Align/Format → **🌐 Dịch caption** →
   sắp thứ tự ↑↓.
3. Tab **Hành vi** / **Export-Print**: bật cờ cần thiết.
4. (TreeList) tab **Cây**: điền `Parent_Field` + `Expand_Level`.
5. (Nguồn SP/SQL) tab **Bộ lọc**: bật `Filter_Panel_Enabled` → thêm các control + tham số.
6. Tab **Actions**: khai nút export/print/custom nếu cần.
7. **💾 Lưu** → khai route `/view/{View_Code}` vào menu → đồng bộ xuống tenant.

> **i18n nhắc lại:** mọi text người dùng thấy là `*_Key` → dịch qua nút **🌐**. Đừng gõ chữ tiếng Việt
> thẳng vào ô `_Key`; gõ key rồi dịch, hoặc để trống cho nút 🌐 tự sinh key theo convention.
