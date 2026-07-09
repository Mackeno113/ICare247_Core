# Hướng dẫn **gắn mẫu tài liệu vào màn hình** — xuất Word/PDF từ lưới (ConfigStudio)

> **Đối tượng:** người triển khai (deployer) cấu hình hệ thống trong ConfigStudio (WPF).
> **Phạm vi:** từ lúc có stored proc dữ liệu → soạn bộ mẫu → **gắn nút "Xuất tài liệu" vào một màn lưới** → người dùng bấm để tải file.
> **Liên quan:**
> - Đặc tả kỹ thuật đầy đủ (engine, bảng, bảo mật) → [28_DOC_TEMPLATE_SPEC.md](../spec/28_DOC_TEMPLATE_SPEC.md)
> - Cấu hình lưới / nút hành động (Ui_View_Action) → [14_VIEW_CONFIG_SPEC.md](../spec/14_VIEW_CONFIG_SPEC.md)
> - Soạn bộ mẫu (RichEdit + kéo biến) → màn **"📄 Mẫu tài liệu"** trong ConfigStudio

---

## 0. Bức tranh tổng thể — "gắn mẫu vào màn hình" là gì

Một **bộ mẫu tài liệu** (VD *Hợp đồng lao động*) được soạn 1 lần, rồi **gắn** vào một hoặc nhiều **màn lưới** (danh sách nhân viên, danh sách hợp đồng…). Trên lưới xuất hiện **nút "Xuất tài liệu"**; người dùng **chọn 1 dòng** rồi bấm nút → hệ thống bơm dữ liệu của **chính dòng đó** vào mẫu và tải file `.docx`/`.pdf` về.

Điểm mấu chốt — **không có bảng "gắn màn hình" riêng**. Việc gắn dùng lại đúng cơ chế nút hành động của lưới: **`Ui_View_Action`**. Một dòng action với `Target = mã bộ mẫu` chính là "file template này thuộc màn này".

```
Bộ mẫu (Doc_Template.Ma = 'HOP_DONG_LD')
        ▲  gắn qua Ui_View_Action.Target
        │
Màn lưới "Danh sách nhân viên" (Ui_View)  ──► nút [📄 Xuất hợp đồng]
                                                   │ người dùng chọn 1 dòng → bấm
                                                   ▼
                          POST /api/v1/doc-templates/by-code/HOP_DONG_LD/render
                          body = toàn bộ cột dòng đang chọn  ◄── "thông tin được truyền"
                                                   ▼
                          proc master nhận @NhanVien_Id ← key:Id  → file .docx/.pdf
```

**"Thông tin nào được truyền?"** = **toàn bộ các cột của dòng đang chọn** trên lưới. Bảng `Doc_Template_Param` quyết định cột nào ánh xạ vào tham số proc nào (VD `@NhanVien_Id` lấy từ cột `Id`). Các cột thừa được bỏ qua.

---

## 1. Chuẩn bị (làm 1 lần cho mỗi tenant)

1. **Chạy migration** `db/077_create_doc_template.sql` trên Config DB của tenant (tạo 4 bảng `Doc_Template*`).
2. **Có stored proc dữ liệu** trên Data DB tenant:
   - **Proc master** — trả **đúng 1 dòng** (VD `sp_DocNhanVien_Master @NhanVien_Id, @Tenant_Id`), mỗi cột = 1 biến đơn.
   - (tuỳ chọn) **Proc detail** — trả **N dòng** cho bảng lặp (VD danh sách phụ cấp).
   - Proc chỉ `SELECT`, tham số hoá 100%, không DML/DDL.
3. **Đăng ký proc vào whitelist** `Doc_Proc_Registry` (`Proc_Name`, `Loai='master'|'detail'`, `Is_Active=1`, `Tenant_Id`). **Proc không đăng ký sẽ bị từ chối render.**

> ⏳ Hiện `Doc_Proc_Registry` / `Doc_Template_Param` chưa có màn quản lý riêng → khai bằng SQL (INSERT set `CreatedBy` tường minh). Màn quản trị các bảng này là việc pha sau.

---

## 2. Soạn bộ mẫu (màn "📄 Mẫu tài liệu")

1. Mở ConfigStudio → menu **"📄 Mẫu tài liệu"**.
2. Nhập tên proc master vào ô **Stored proc** → **Nạp biến** (hiện danh sách cột proc trả ra).
3. **Tạo bộ mẫu**: nhập **Mã** (VD `HOP_DONG_LD`) + **Tên** → *Tạo bộ mẫu*. Mã này chính là thứ sẽ gắn vào màn ở bước 4.
4. Chọn đích **Master (A4 dọc)** → soạn nội dung trong RichEdit, **kéo/chèn biến** (MERGEFIELD) từ panel → **Lưu**.
5. (tuỳ chọn) **Thêm mảnh detail** (A4 ngang) với proc detail → soạn bảng lặp → Lưu.
6. **Ánh xạ tham số** trong `Doc_Template_Param` (mỗi proc cần biết lấy giá trị tham số từ đâu):

   | Param_Name | Nguon | Nguon_Key | Ý nghĩa |
   |---|---|---|---|
   | `@NhanVien_Id` | `key` | `Id` | Lấy cột **`Id`** của dòng đang chọn trên lưới |
   | `@Tenant_Id` | `context` | `Tenant_Id` | Tự lấy từ phiên đăng nhập |
   | `@LoaiHopDong` | `const` | `CHINH_THUC` | Hằng số |

   - `Detail_Id = NULL` → tham số cho **proc master**; điền `Detail_Id` → cho proc **detail** tương ứng.
   - **`Nguon_Key` phải trùng tên một cột trên lưới** (khi `Nguon='key'`). Đây là cầu nối "dòng đang chọn → tham số proc".

---

## 3. Gắn mẫu vào màn lưới (màn "Quản lý View" → tab **Actions**)

1. Mở ConfigStudio → **Quản lý View** → chọn **View** của màn cần gắn (VD lưới *Danh sách nhân viên*) → **Sửa**.
2. Sang tab **Actions** → **+ Thêm action**. Điền:

   | Cột | Giá trị | Ghi chú |
   |---|---|---|
   | **Action_Code** | `export-hop-dong` | mã tuỳ ý, duy nhất trong view |
   | **Type** | `Export` (hoặc `Print`) | |
   | **Scope** | `Toolbar` (hoặc `Both`) | nút trên thanh công cụ lưới |
   | **Export_Format** | `docx` hoặc `pdf` | |
   | **Engine** | `Server` | **bắt buộc** — docx/pdf render theo mẫu ở server |
   | **Target** | *(dùng combo bên dưới)* | = **mã bộ mẫu** |
   | **Req_Sel** | ✔ | buộc chọn 1 dòng |
   | **Label (i18n)** | 🌐 đặt nhãn "Xuất hợp đồng" | |
   | **Icon** | 📄 | tuỳ chọn |

3. **Điền Target nhanh**: chọn dòng action vừa tạo → dùng combo **"Bộ mẫu (Xuất tài liệu):"** ở thanh trên → chọn bộ mẫu. Combo sẽ **tự điền `Target = mã bộ mẫu`** và đặt `Engine = Server`.
4. **Lưu** View.

> Muốn nút xuất hiện **trên từng dòng** thay vì toolbar: đặt `Scope='Row'`. (Nút-theo-dòng đang ở mức toolbar-first; xem §6.)

---

## 4. Người dùng chạy (runtime web)

1. Mở màn lưới → **tick chọn 1 dòng**.
2. Bấm nút **"Xuất hợp đồng"** trên toolbar.
3. Trình duyệt tải về file `.docx`/`.pdf` đã bơm dữ liệu của dòng đó.

- Chưa chọn dòng → báo *"Hãy chọn (tick) 1 dòng trước khi xuất tài liệu."*
- Proc chưa đăng ký / mẫu chưa soạn master → báo lỗi rõ ràng (RFC 7807) từ server.

---

## 5. Xử lý sự cố nhanh

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| Nút báo *"chưa gắn bộ mẫu (Target)"* | Action `Engine=Server` nhưng `Target` rỗng → chọn lại bộ mẫu ở combo |
| Lỗi *"Stored proc … chưa đăng ký"* | Thiếu dòng trong `Doc_Proc_Registry` (hoặc `Is_Active=0`, sai `Tenant_Id`) |
| Lỗi *"chưa soạn fragment master"* | Bộ mẫu chưa Lưu nội dung master |
| File ra nhưng thiếu giá trị | Sai ánh xạ `Doc_Template_Param` — `Nguon_Key` không trùng tên cột lưới, hoặc tên biến ≠ tên cột proc |
| Watermark trên file | Đang dùng bản DevExpress trial → mua license Universal khi lên prod |

---

## 6. Giới hạn hiện tại (pha sau)

- **Chỉ màn lưới/danh sách** (`Ui_View_Action`). Nút xuất **trên form chi tiết** (đang mở 1 bản ghi) chưa có — cần cơ chế `Ui_Form_Action` (chưa làm).
- **Xuất 1 bản ghi/lần**. In hàng loạt (nhiều dòng → 1 file / nhiều file) hoãn quyết sau GĐ1 (Spec 28 §13-D).
- Màn quản trị `Doc_Proc_Registry` / `Doc_Template_Param` chưa có (đang khai bằng SQL).
