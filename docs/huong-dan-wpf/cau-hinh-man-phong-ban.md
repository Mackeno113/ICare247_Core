# Cấu hình màn Phòng ban — ráp 3 control dùng chung (no-code, ConfigStudio)

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh tên cột/migration kỹ
> thuật, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Tạo ra màn hình **"Phòng ban"** — danh mục dạng **cây** (Khối → Phòng →
> Tổ → Nhóm), mỗi công ty chỉ thấy phòng ban của công ty mình, và có thể **kéo-thả** để đổi thứ tự
> hiển thị — **không cần viết code**. Bài này dùng chung nền tảng với
> [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) và
> [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) (khuyên đọc trước 2 bài đó), điểm khác biệt ở đây
> là dùng **3 ô/nút cấu hình đặc biệt** ("Sinh Form/Sinh Lưới" tự động, radio chống chọn nhầm phòng ban
> cha, và 2 ô tick riêng cho kéo-thả + lọc theo công ty) — chỉ cần **tick chọn**, không cấu hình tay
> từng field như bài Công ty.
>
> Ví dụ xuyên suốt cả bài: **màn Phòng ban (`TC_PhongBan`)**.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Cây lồng** | Danh sách hiển thị dạng cây cha–con lồng nhau (giống lưới cây ở bài Công ty). |
| **Vòng lặp (chọn nhầm cha)** | Lỗi khi 1 phòng ban vô tình được chọn làm cha của chính nó hoặc của phòng ban con/cháu của nó → cây bị rối, không hiển thị đúng phân cấp. Hệ thống phải tự chặn lỗi này. |
| **Kéo-thả sắp xếp** | Dùng chuột kéo 1 dòng trong lưới cây sang vị trí khác để đổi thứ tự hiển thị, thay vì phải sửa số thứ tự bằng tay. |
| **Switcher công ty** | Ô chọn "công ty đang làm việc" ở đầu trang web — chọn công ty nào thì các màn liên quan (như Phòng ban) chỉ hiện dữ liệu của công ty đó. |
| **Bảng dữ liệu / Form / View / ConfigStudio / Đồng bộ cấu hình** | Xem lại [bảng thuật ngữ ở bài Danh mục](cau-hinh-man-danh-muc.md#vài-thuật-ngữ-cần-biết-trước-khi-đọc) nếu chưa rõ. |

---

## Phần A — Làm theo từng bước

### Bước 1 — Sinh Form và Sinh Lưới tự động

**Mục đích:** tạo nhanh màn nhập liệu (Form) và màn danh sách (Lưới) cho Phòng ban mà không cần khai
tay từng ô, vì bảng `TC_PhongBan` đã có sẵn trong hệ thống.

**Làm gì:**
1. Mở ConfigStudio → màn **Sys_Table** → chọn dòng **`TC_PhongBan`**.
2. Bấm **📝 Sinh Form** (1-chạm) → tự tạo màn nhập liệu với field cho mọi cột nghiệp vụ.
3. Bấm **📊 Sinh Lưới** → tự tạo màn danh sách với cột hiển thị tương ứng.

**Bạn sẽ thấy gì:** 1 Form và 1 Lưới của Phòng ban xuất hiện, đã có sẵn field/cột theo đúng cấu trúc
bảng dữ liệu — các cột kỹ thuật hệ thống (người tạo, ngày tạo...) tự động bị loại, không cần bạn tự bỏ
tick.

**Lỗi thường gặp:** 2 nút này độc lập, không bắt buộc bấm cùng lúc và không ảnh hưởng lẫn nhau — nếu
chỉ cần thêm cột mới cho lưới thì chỉ cần bấm lại **📊 Sinh Lưới**, không cần bấm lại **📝 Sinh Form**.

---

### Bước 2 — Cấu hình ô "Phòng ban cha" để không cho chọn nhầm

**Mục đích:** khi sửa 1 phòng ban, ô chọn "phòng ban cha" **không được phép** cho chọn chính nó hoặc
phòng ban con/cháu của nó — nếu không sẽ tạo ra **vòng lặp**, cây hiển thị sai.

**Làm gì:**
1. Mở Form vừa sinh ở Bước 1 → chọn field **`PhongBan_Cha_Id`**.
2. Đặt **Editor type = LookupBox**.
3. Ở nhóm **"Chế độ truy vấn"** → chọn radio **"Cha trong cùng bảng (chống vòng lặp)"**.
4. Khai **Parent_Column = `PhongBan_Cha_Id`** (cột lưu phòng ban cha, tự tham chiếu tới chính bảng).
5. Đặt **Value_Field = `Id`**, **Display_Field = `Ten`** (hoặc `Ma — Ten`).

**Bạn sẽ thấy gì:** khi **Sửa** 1 phòng ban đã có sẵn, mở ô chọn "Phòng ban cha" sẽ **tự động ẩn** chính
phòng ban đó và toàn bộ phòng ban con/cháu của nó khỏi danh sách chọn. Khi **Thêm mới** (chưa có gì để
loại), danh sách hiện đầy đủ bình thường.

**Lỗi thường gặp:** ô chọn "Phòng ban cha" vẫn liệt kê cả chính nó → kiểm tra lại đã chọn đúng radio
"Cha trong cùng bảng (chống vòng lặp)" và điền đúng Parent_Column chưa. Nếu radio hiển thị bất thường
(lựa chọn hiện sai/lệch), báo IT kiểm tra đang dùng bản ConfigStudio đã được vá lỗi này chưa.

---

### Bước 3 — Cho phép kéo-thả sắp xếp lại thứ tự trong cây

**Mục đích:** người dùng có thể kéo-thả 1 phòng ban sang vị trí khác trong cây để đổi thứ tự hiển thị,
không cần sửa số thứ tự bằng tay.

**Làm gì:**
1. Mở màn **Quản lý View** của Phòng ban → tab **"Cơ bản"**.
2. **Khai cột cha** để lưới trở thành dạng cây (cột cha = `PhongBan_Cha_Id`) — nếu không khai cột cha,
   lưới chỉ hiển thị phẳng, không có cây để kéo-thả.
3. Tick ô: **"Allow_Reorder — cho phép kéo-thả sắp xếp (ADR-027)"**.

**Bạn sẽ thấy gì:** lưới phòng ban hiển thị dạng **cây lồng**; kéo-thả 1 dòng sang vị trí khác, thứ tự
được lưu lại kể cả sau khi tải lại trang (F5). Nếu thử kéo 1 phòng ban vào chính con/cháu của nó, hệ
thống sẽ **tự chặn** (không cho thả).

**Lỗi thường gặp:** quên khai cột cha → lưới chỉ hiện phẳng, không kéo-thả được. Kéo-thả xong tải lại
trang thì mất thứ tự → kiểm tra lại đã tick ô Allow_Reorder chưa, hoặc báo IT kiểm tra phần xử lý tính
lại thứ tự phía sau đã được triển khai đầy đủ chưa.

---

### Bước 4 — Chỉ hiện phòng ban thuộc công ty đang chọn

**Mục đích:** mỗi công ty chỉ thấy phòng ban của công ty mình (không lẫn công ty khác), tự động lọc
theo công ty đang chọn ở switcher trên đầu trang web.

**Làm gì:**
1. Vẫn ở màn **Quản lý View** của Phòng ban, tab **"Cơ bản"**.
2. Tick ô: **"Scope_By_Company — tự lọc theo công ty (quyền + đang chọn)"**.

**Bạn sẽ thấy gì:** người dùng chỉ thấy phòng ban thuộc công ty nằm trong phạm vi quyền của mình; đổi
công ty ở switcher → danh sách phòng ban tự đổi theo ngay.

**Lỗi thường gặp:** ô này **không hiện ra** để tick → lưới đang cấu hình theo nguồn dữ liệu kiểu thủ
tục/câu lệnh SQL riêng (không phải bảng/view thông thường) — trường hợp này cần báo IT tự thêm điều
kiện lọc theo công ty bằng tay trong câu lệnh đó.

---

### Bước 5 — Đưa cấu hình ra hệ thống thật + gắn vào menu

**Mục đích:** những gì cấu hình ở Bước 1-4 hiện chỉ nằm trong ConfigStudio, và màn chưa xuất hiện ở đâu
để nhân viên bấm vào.

**Làm gì:**
- Chạy **đồng bộ cấu hình** (giống các bài trước — vào ứng dụng web, **Quản trị › Đồng bộ cấu hình**
  → **Xem trước** → **Áp dụng từ master**) để đẩy Form + Lưới Phòng ban ra hệ thống thật.
- Báo người phụ trách cấu hình menu thêm mục **"Phòng ban"** trỏ đúng tới màn vừa tạo, đặt đúng vị trí
  hiển thị trong cây menu.

**Bạn sẽ thấy gì:** mục Phòng ban xuất hiện trong menu điều hướng, bấm vào mở đúng màn vừa cấu hình.

**Lỗi thường gặp:** quên đồng bộ cấu hình → nơi nhân viên đang dùng chưa thấy gì mới, dù ConfigStudio
đã lưu.

---

### Bước 6 — Kiểm tra kết quả

- Mở màn Phòng ban → lưới hiện dạng **cây lồng** (không phẳng). ✅
- Chỉ thấy phòng ban thuộc **công ty đang chọn** ở switcher; đổi công ty → danh sách đổi theo. ✅
- **Kéo-thả** 1 dòng sang vị trí khác → thứ tự lưu lại sau khi tải lại trang (F5). ✅
- Thử kéo 1 dòng vào **con của chính nó** → bị chặn. ✅
- Thêm/Sửa 1 phòng ban → ô **"Phòng ban cha"** mở picker, **không** liệt kê chính nó + con/cháu. ✅

Nếu không đúng như trên, xem bảng [Lỗi thường gặp](#8-lỗi-thường-gặp) trong Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên cột/migration kỹ thuật.
>
> **Mục tiêu:** dựng màn **Phòng ban** (`TC_PhongBan`) — một **danh mục dạng cây, đa công ty** —
> hoàn toàn qua ConfigStudio, KHÔNG viết SQL tay. Màn này là ví dụ chuẩn để ráp 3 "control dùng chung"
> xây ở session 88.
>
> **Nguyên tắc cốt lõi:** 3 control KHÔNG phải thứ bạn kéo-thả lên canvas. Chúng là **cờ khai báo**
> (checkbox / radio) bật trên **Form** hoặc **View**; engine runtime tự lắp hành vi. Bạn chỉ khai
> báo **Sys_Table → Form → View** rồi tick đúng ô.

### 0. Điều kiện tiên quyết

| Hạng mục | Trạng thái cần có |
|---|---|
| Migration | `db/085` (sp_RecomputeTreeOrder) · `086` (Allow_Reorder) · `087` (Scope_By_Company) · `088` (self_parent) — **đã chạy trên Config DB + Data DB tenant** |
| Bảng `TC_PhongBan` | đã có, đã chuẩn hóa qua `db/037` + `db/079` |
| API | đã build lại + restart **sau** khi chạy migration (vì `ViewRepository.GetByCodeAsync` thêm cột SELECT) |

**Cột `TC_PhongBan` liên quan (đã verify):**

| Cột | Vai trò | Dùng cho |
|---|---|---|
| `Id` | PK | — |
| `Ma`, `Ten`, `MoTa` | mã / tên / mô tả | field thường |
| `PhongBan_Cha_Id` | phòng ban cha (self-ref, NULL = gốc) | **Feature B** |
| `CongTy_Id` | thuộc công ty nào (NOT NULL) | **Feature A** |
| `CapPhongBan_Id` | cấp (Khối/Phòng/Tổ/Nhóm) | field lookup thường |
| `ThuTu` | thứ tự nhập tay (input) | **Feature C** |
| `Cap`, `ThuTuCay`, `DuongDanCay` | cache cây ADR-027 (dẫn xuất) | **Feature C** — proc tự tính, KHÔNG nhập tay |

### Toàn cảnh luồng

```
Sys_Table: TC_PhongBan (đã có)
   │
   ├─► [📝 Sinh Form] ─► Ui_Form + fields
   │        └─► Bước 2: field "PhongBan_Cha_Id" = LookupBox self_parent      → Feature B
   │
   └─► [📊 Sinh Lưới] ─► Ui_View
            ├─► Bước 3: khai cột cha → TreeList  +  tick Allow_Reorder        → Feature C
            └─► Bước 4: tick Scope_By_Company                                 → Feature A
   │
   ├─► Bước 5: ConfigSync (master → tenant)
   └─► Bước 6: gắn vào Menu (HT_ChucNang)
```

### 1. Sinh Form + Sinh Lưới từ Sys_Table

1. Mở ConfigStudio → màn **Sys_Table** → chọn dòng `TC_PhongBan`.
2. Bấm **📝 Sinh Form** (1-chạm, headless) → tạo `Ui_Form` + section + field cho mọi cột nghiệp vụ.
   Khối cột audit (`CreatedBy/CreatedAt/…/IsDeleted/Ver`) tự bị loại.
3. Bấm **📊 Sinh Lưới** → tạo `Ui_View` + cột hiển thị.

> 2 nút độc lập — không bắt buộc cùng chạy. Hiện chỉ sinh 1 form / 1 lưới đơn.

### 2. Feature B — Chọn "phòng ban cha" trong CHÍNH bảng (chống vòng lặp)

**Ở đâu:** màn **Cấu hình Field** của Form Phòng ban → panel **LookupBox**.

1. Mở Form vừa sinh → chọn field **`PhongBan_Cha_Id`**.
2. Đặt **Editor type = LookupBox**.
3. Nhóm **"Chế độ truy vấn"** → chọn radio **"Cha trong cùng bảng (chống vòng lặp)"**
   (đây chính là `Query_Mode = self_parent`).
4. Khai **Parent_Column = `PhongBan_Cha_Id`** (cột cha tự tham chiếu).
5. `Value_Field = Id`, `Display_Field = Ten` (hoặc `Ma — Ten`).

**Engine tự làm:** khi mở picker lúc **đang sửa** một phòng ban, tự loại **chính nó + toàn bộ hậu duệ**
khỏi danh sách → không thể chọn con/cháu làm cha của chính mình. Lúc **Thêm mới** (chưa có Id) thì
không loại gì (đúng nghĩa "chưa có gì để loại").

> ⚠️ Đây là panel vừa sửa **bug ④** (commit `9810d5b`): trước đó radio TVF/SQL hiển thị sai do lệch
> literal `function/sql` vs canonical `tvf/custom_sql`. Nếu radio hiển thị bất thường → dùng bản
> ConfigStudio sau `9810d5b`.

### 3. Feature C — Kéo-thả sắp xếp cây (ADR-027)

**Ở đâu:** màn **Quản lý View** → tab **"Cơ bản"**.

1. Mở View của `TC_PhongBan`.
2. **Khai cột cha** để View thành **TreeList** (cây lồng) — cột cha = `PhongBan_Cha_Id`.
   Không khai cha → chỉ là lưới phẳng, không có cây để kéo-thả.
3. Tick ô: **"Allow_Reorder — cho phép kéo-thả sắp xếp (ADR-027)"**.

**Engine tự làm:** bật `DxTreeList.AllowDragRows`; khi thả 1 node, gọi API reorder → cập nhật `ThuTu`
+ chạy `sp_RecomputeTreeOrder` tính lại `Cap/ThuTuCay/DuongDanCay`. API chặn tạo vòng lặp (không cho
thả node vào chính hậu duệ của nó).

> **Nguồn sự thật sắp xếp = `ThuTu`** (input). 3 cột `Cap/ThuTuCay/DuongDanCay` là **cache dẫn xuất** —
> proc tự ghi, KHÔNG cho người dùng nhập.

### 4. Feature A — Tự lọc theo công ty (quyền + công ty đang chọn)

**Ở đâu:** cùng màn **Quản lý View**, tab **"Cơ bản"**.

1. Vẫn ở View của `TC_PhongBan`.
2. Tick ô: **"Scope_By_Company — tự lọc theo công ty (quyền + đang chọn)"**.

**Engine tự làm:** tự JOIN `fnt_CongTyTheoQuyen` (các công ty user được phép) + lọc theo `@CongTyID_Active`
(công ty đang chọn ở switcher). Người dùng chỉ thấy phòng ban thuộc công ty trong phạm vi quyền của mình.

> Ô này **chỉ hiện** khi View là loại **bảng/view** (`CanScopeByCompany`). View kiểu **Sp/Sql** thì tự
> viết SQL và JOIN `fnt_CongTyTheoQuyen` bằng tay. Cột dùng để lọc: `TC_PhongBan.CongTy_Id`.

### 5. ConfigSync master → tenant

Config vừa tạo nằm ở **Config DB master**. Chạy **ConfigSync** để đẩy Form + View sang tenant
(UPSERT theo mã, re-link FK). Không đồng bộ → tenant chưa thấy màn mới.

### 6. Gắn vào Menu

Thêm mục menu trong **HT_ChucNang** (ADR-023) trỏ tới `View_Code` của Phòng ban, đặt `ViTriHienThi`
đúng nhánh. Menu server-driven sẽ tự hiện sau khi phân quyền cho vai trò.

### 7. Checklist nghiệm thu (runtime, trên web)

- [ ] Mở màn Phòng ban → lưới hiện dạng **cây lồng** (không phẳng).
- [ ] Chỉ thấy phòng ban thuộc **công ty đang chọn** ở switcher; đổi công ty → danh sách đổi theo.
- [ ] **Kéo-thả** 1 node sang vị trí khác → thứ tự lưu lại sau khi F5.
- [ ] Thử kéo 1 node vào **con của chính nó** → bị chặn (chống vòng lặp).
- [ ] Thêm/Sửa 1 phòng ban → ô **"Phòng ban cha"** mở picker, **không** liệt kê chính nó + hậu duệ.

### 8. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử |
|---|---|---|
| Mọi màn `/view/...` lỗi *"Invalid column name"* | API chạy code mới nhưng migration `085–088` **chưa** chạy | Chạy đủ migration đúng thứ tự → restart API |
| Lưới phẳng, không kéo-thả được | Chưa khai **cột cha** → View không thành TreeList | ViewManager → khai cột cha `PhongBan_Cha_Id` |
| Kéo-thả xong F5 mất thứ tự | Chưa tick `Allow_Reorder`, hoặc `sp_RecomputeTreeOrder` chưa deploy | Tick ô + xác nhận `db/085` đã chạy |
| Thấy phòng ban của mọi công ty | Chưa tick `Scope_By_Company` (hoặc View kiểu Sp/Sql) | Tick ô; View Sp/Sql phải JOIN `fnt_CongTyTheoQuyen` tay |
| Picker "cha" liệt kê cả chính nó | Field chưa đặt `self_parent` hoặc thiếu `Parent_Column` | Cấu hình lại theo Bước 2 |
| Radio TVF/SQL panel LookupBox sai | ConfigStudio cũ hơn commit `9810d5b` (bug ④) | Dùng bản mới |

---

**Tài liệu liên quan:** [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) ·
[cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md) ·
[cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) · ADR-027 (sắp xếp cây) · ADR-023 (menu/phân quyền).
