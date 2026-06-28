# Hướng dẫn cấu hình màn **Quản Lý View (Grid / Tree Grid)** — ConfigStudio

> Màn này cấu hình **hiển thị danh sách** (lưới Grid / cây TreeList) hoàn toàn metadata-driven:
> nguồn dữ liệu → cột → hành vi → export/print → panel lọc. **Tách khỏi form sửa** (`Ui_Form`/`Ui_Field`):
> một bảng dữ liệu → nhiều view; view trỏ `Edit_Form` để mở popup Thêm/Sửa.
>
> Bảng dưới Config DB: `Ui_View` (header) + `Ui_View_Column` (cột) + `Ui_View_Action` (nút) + `Ui_View_Filter` (panel lọc).
> Tham chiếu: spec `14_VIEW_CONFIG_SPEC.md` (ADR-015, ADR-016).

---

## ⚠️ Yêu cầu trước (1 lần) — nếu màn báo lỗi "Invalid column name"

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

---

## Bố cục màn

| Vùng | Chức năng |
|---|---|
| **Header** | Tiêu đề + ô tìm (`View_Code`/`View_Type`/`Table_Code`) + checkbox **Hiện cả View ẩn** + badge **Tenant_Id** + nút **↻** (F5) |
| **Trái** | Lưới danh sách View (Id / View_Code / Type / Bảng / Active). Chọn dòng → đổ sang editor |
| **Phải** | Editor 7 tab cấu hình (chi tiết bên dưới) |
| **Footer** | "Hiển thị {đã lọc} / {tổng} View" |

**Phím tắt:** `Ctrl+N` Tạo mới · `Ctrl+S` Lưu · `F5` Làm mới.
**Nút dưới editor:** **💾 Lưu** · **Tạo mới** · **Ẩn View** (soft-delete, set `Is_Active = 0`).

---

## Tab 1 — Cơ bản (nguồn dữ liệu)

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

---

## Tab 2 — Hành vi (lưới)

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

---

## Tab 3 — Export / Print

| Trường | Ghi chú |
|---|---|
| **Allow_Export** | Cho xuất file. |
| **Export_Formats** | Danh sách phân tách phẩy: `xlsx,csv,pdf,docx`. |
| **Export_File_Name_Key** | i18n tên file (null → dùng `View_Code`). Nút **🌐 Dịch**. |
| **Allow_Print** | Cho in. |

> **Quy tắc engine:** `xlsx/csv` xuất **client-side** qua DxGrid; `pdf/docx` xuất **server-side** theo template.
> Export **luôn lấy giá trị thuần** (bỏ qua `Render_Mode`, không xuất thẻ HTML). Nút xuất chi tiết khai ở **tab Actions**.

---

## Tab 4 — Cây (TreeList)

Chỉ dùng khi **View_Type = TreeList** (có cảnh báo vàng nhắc).

| Trường | Ghi chú |
|---|---|
| **Key_Field** | Đặt ở **tab Cơ bản** — cột khóa của node. |
| **Parent_Field** | Cột trỏ node cha (hierarchy). VD `ParentId`. |
| **Expand_Level** | Mở sẵn tới cấp mấy (0–20). |

---

## Tab 5 — Cột (`Ui_View_Column`)

Lưới sửa trực tiếp (inline). Toolbar: **+ Thêm cột** · **− Xóa cột** · **↑ ↓** đổi thứ tự ·
**🔍 Chọn cột** (lấy từ `Sys_Column` của bảng nguồn) · **🌐 Dịch caption**.

| Cột lưới | Ý nghĩa | Giá trị |
|---|---|---|
| **Field_Name** * | Tên field trên control (khớp cột data). | |
| **Caption (i18n)** | Tiêu đề cột. Nút 🌐 mỗi dòng để dịch. **Trống = fallback** label field → `Field_Name`. | |
| **Kind** | Loại cột. | `Data` \| `Selection` \| `Command` \| `TreeSpin` |
| **Render** | Cách render ô. | `Text` \| `Html` \| `Image` \| `Link` \| `Badge` \| `Boolean` \| `Template` |
| **Width / MinWidth** | Độ rộng. | |
| **Align** | Canh chỉnh. | `left` \| `center` \| `right` |
| **Ghim** | Đóng băng cột. | `none` \| `left` \| `right` |
| **Format** | Display format. | VD `n0`, `dd/MM/yyyy` |
| **Visible** | Hiện cột. | |
| **Sort / SortMặc định / SortIdx** | Cho sắp xếp; sort mặc định khi mở (`asc`/`desc`); thứ tự khi sort nhiều cột. | |
| **Filter / Group** | Cho lọc / cho nhóm. | |
| **Summary** | Dòng tổng. | `count` \| `sum` \| `avg` \| `min` \| `max` |
| **Export** | Cho xuất cột. Cột HTML trang trí / command / selection → **bỏ tick**. | |
| **Order** | Thứ tự (đọc-only, đổi bằng ↑↓). | |

---

## Tab 6 — Actions (`Ui_View_Action`)

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

---

## Tab 7 — Bộ lọc (panel lọc trái — lưới nâng cao)

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

---

## Quy trình tạo nhanh (checklist)

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
