# Hướng dẫn **gắn mẫu tài liệu vào màn hình** — xuất Word/PDF từ lưới (ConfigStudio)

> **Đối tượng:** người triển khai (deployer) cấu hình hệ thống trong ConfigStudio (WPF) — **không cần
> biết lập trình**, chỉ cần phối hợp với IT kỹ thuật ở phần chuẩn bị dữ liệu. Nếu bạn là lập trình
> viên/AI cần tra cứu nhanh, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
> **Phạm vi:** từ lúc có stored proc dữ liệu → soạn bộ mẫu → **gắn nút "Xuất tài liệu" vào một màn lưới** → người dùng bấm để tải file.
> **Liên quan:**
> - Đặc tả kỹ thuật đầy đủ (engine, bảng, bảo mật) → [28_DOC_TEMPLATE_SPEC.md](../spec/28_DOC_TEMPLATE_SPEC.md)
> - Cấu hình lưới / nút hành động (Ui_View_Action) → [14_VIEW_CONFIG_SPEC.md](../spec/14_VIEW_CONFIG_SPEC.md)
> - Soạn bộ mẫu (RichEdit + kéo biến) → màn **"📄 Mẫu tài liệu"** trong ConfigStudio
>
> Ví dụ xuyên suốt cả bài: gắn mẫu **Hợp đồng lao động** vào màn danh sách **Nhân viên**, để người
> dùng chọn 1 nhân viên rồi bấm nút để tải file hợp đồng `.docx` đã điền sẵn thông tin người đó.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Bộ mẫu tài liệu** | File Word/PDF mẫu có sẵn các "chỗ trống" — hệ thống tự điền dữ liệu thật vào khi xuất. |
| **Biến (kéo biến)** | 1 "chỗ trống" trong bộ mẫu, ứng với 1 cột dữ liệu — VD kéo biến "Họ tên" vào đúng chỗ cần in tên. |
| **Stored proc / Nguồn dữ liệu** | Nơi lấy dữ liệu thật để điền vào mẫu — do IT kỹ thuật chuẩn bị sẵn, bạn chỉ cần biết **tên** của nó để gõ vào ô Stored proc. |
| **Action (nút hành động)** | 1 nút bấm gắn thêm vào màn danh sách, VD nút "Xuất hợp đồng" trên thanh công cụ lưới. |
| **Target** | Ô nối 1 nút hành động với đúng 1 bộ mẫu — chọn qua combo, không cần gõ tay. |

---

## Bức tranh tổng thể — "gắn mẫu vào màn hình" là gì

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

## Phần A — Làm theo từng bước

### Bước 0 — Kiểm tra phần chuẩn bị (thường IT kỹ thuật đã làm sẵn)

**Mục đích:** đảm bảo đã có "nguồn dữ liệu" để bơm vào mẫu trước khi bắt đầu soạn mẫu — nếu thiếu,
soạn xong mẫu cũng không xuất được file.

**Làm gì:** hỏi/xác nhận với IT kỹ thuật 3 điều sau đã có cho tenant của bạn:
1. Migration tạo bảng quản lý mẫu tài liệu đã chạy trên Config DB.
2. Đã có **stored proc dữ liệu** lấy đúng thông tin cần in (VD thông tin 1 nhân viên).
3. Proc đó đã được **đăng ký vào whitelist** — proc chưa đăng ký sẽ bị hệ thống từ chối khi xuất file.

**Bạn sẽ thấy gì:** khi mở màn **"📄 Mẫu tài liệu"** và gõ tên proc vào ô **Stored proc** rồi bấm
**Nạp biến**, hệ thống liệt kê ra đúng danh sách cột proc trả về — nghĩa là phần chuẩn bị đã xong.

**Lỗi thường gặp:** bấm **Nạp biến** không ra gì, hoặc sau này xuất file báo lỗi *"Stored proc …
chưa đăng ký"* → báo lại IT kỹ thuật kiểm tra whitelist (mục 5 ở Phần B).

---

### Bước 1 — Soạn bộ mẫu tài liệu

**Mục đích:** dựng ra 1 bộ mẫu (VD Hợp đồng lao động) — soạn nội dung Word 1 lần, dùng lại cho mọi
nhân viên.

**Làm gì:**
1. Mở ConfigStudio → menu **"📄 Mẫu tài liệu"**.
2. Nhập tên proc master vào ô **Stored proc** → bấm **Nạp biến** (hiện danh sách cột proc trả ra).
3. Nhập **Mã** (VD `HOP_DONG_LD`) + **Tên** → bấm **Tạo bộ mẫu**. Mã này chính là thứ sẽ gắn vào màn
   ở Bước 2.
4. Chọn đích **Master (A4 dọc)** → soạn nội dung trong khung soạn thảo, **kéo/chèn biến** từ panel
   bên cạnh vào đúng vị trí cần điền dữ liệu → bấm **Lưu**.
5. (Tùy chọn) muốn có thêm bảng lặp (VD danh sách phụ cấp) → **Thêm mảnh detail** (A4 ngang) với 1
   proc detail riêng → soạn bảng lặp → **Lưu**.
6. **Ánh xạ tham số** — cho hệ thống biết mỗi tham số của proc lấy giá trị từ đâu:

   | Param_Name | Nguồn | Nguồn_Key | Ý nghĩa |
   |---|---|---|---|
   | `@NhanVien_Id` | `key` | `Id` | Lấy cột **`Id`** của dòng đang chọn trên lưới |
   | `@Tenant_Id` | `context` | `Tenant_Id` | Tự lấy từ phiên đăng nhập |
   | `@LoaiHopDong` | `const` | `CHINH_THUC` | Hằng số cố định |

**Bạn sẽ thấy gì:** bộ mẫu `HOP_DONG_LD` xuất hiện trong danh sách bộ mẫu — nhưng **chưa gắn vào màn
hình nào**, việc đó làm ở Bước 2.

**Lỗi thường gặp:** file xuất ra thiếu giá trị ở 1 số chỗ → sai ánh xạ tham số (`Nguồn_Key` không
trùng tên cột trên lưới, hoặc tên biến khác tên cột proc trả ra).

---

### Bước 2 — Gắn mẫu vào màn danh sách

**Mục đích:** đưa nút **"Xuất tài liệu"** lên màn danh sách (VD *Danh sách nhân viên*), nối đúng
tới bộ mẫu đã soạn ở Bước 1.

**Làm gì:**
1. Mở ConfigStudio → **Quản lý View** → chọn View của màn cần gắn (VD *Danh sách nhân viên*) → **Sửa**.
2. Sang tab **Actions** → bấm **+ Thêm action**. Điền:

   | Cột | Giá trị | Ghi chú |
   |---|---|---|
   | **Action_Code** | `export-hop-dong` | mã tùy ý, duy nhất trong View |
   | **Type** | `Export` (hoặc `Print`) | |
   | **Scope** | `Toolbar` (hoặc `Both`) | nút hiện trên thanh công cụ lưới |
   | **Export_Format** | `docx` hoặc `pdf` | |
   | **Engine** | `Server` | **bắt buộc** — để file render theo đúng bộ mẫu đã soạn |
   | **Req_Sel** | ✔ | buộc người dùng phải chọn 1 dòng trước khi bấm |
   | **Label (i18n)** | bấm 🌐 đặt nhãn "Xuất hợp đồng" | |
   | **Icon** | 📄 (tùy chọn) | |

3. Chọn dòng action vừa tạo → dùng combo **"Bộ mẫu (Xuất tài liệu):"** ở thanh trên → chọn bộ mẫu
   `HOP_DONG_LD`. Combo tự điền **Target** = mã bộ mẫu và đặt **Engine = Server** — không cần gõ tay.
4. Bấm **Lưu** View.

**Bạn sẽ thấy gì:** nút "Xuất hợp đồng" xuất hiện trên thanh công cụ của màn *Danh sách nhân viên*.

**Lỗi thường gặp:** nút báo *"chưa gắn bộ mẫu (Target)"* → do đặt `Engine=Server` nhưng chưa chọn
bộ mẫu qua combo — chọn lại bộ mẫu rồi Lưu View lần nữa.

---

### Bước 3 — Thử nghiệm trên web

**Mục đích:** xác nhận người dùng thật sự bấm được nút và tải đúng file mong muốn.

**Làm gì:**
1. Mở màn danh sách (VD *Danh sách nhân viên*) → tick chọn 1 dòng.
2. Bấm nút **"Xuất hợp đồng"** trên toolbar.

**Bạn sẽ thấy gì:** trình duyệt tải về file `.docx`/`.pdf` đã bơm đúng dữ liệu của dòng đang chọn.

**Lỗi thường gặp:**
- Chưa chọn dòng → hệ thống báo *"Hãy chọn (tick) 1 dòng trước khi xuất tài liệu."*
- Proc chưa đăng ký / mẫu chưa soạn xong master → báo lỗi rõ ràng từ server, xem thêm bảng lỗi ở
  mục 5 trong [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên bảng/tham số kỹ thuật.

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
