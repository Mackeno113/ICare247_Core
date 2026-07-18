# Cấu hình màn Phòng ban — ráp 3 control dùng chung (no-code, ConfigStudio)

> **Mục tiêu:** dựng màn **Phòng ban** (`TC_PhongBan`) — một **danh mục dạng cây, đa công ty** —
> hoàn toàn qua ConfigStudio, KHÔNG viết SQL tay. Màn này là ví dụ chuẩn để ráp 3 "control dùng chung"
> xây ở session 88.
>
> **Nguyên tắc cốt lõi:** 3 control KHÔNG phải thứ bạn kéo-thả lên canvas. Chúng là **cờ khai báo**
> (checkbox / radio) bật trên **Form** hoặc **View**; engine runtime tự lắp hành vi. Bạn chỉ khai
> báo **Sys_Table → Form → View** rồi tick đúng ô.

---

## 0. Điều kiện tiên quyết

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

---

## Toàn cảnh luồng

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

---

## 1. Sinh Form + Sinh Lưới từ Sys_Table

1. Mở ConfigStudio → màn **Sys_Table** → chọn dòng `TC_PhongBan`.
2. Bấm **📝 Sinh Form** (1-chạm, headless) → tạo `Ui_Form` + section + field cho mọi cột nghiệp vụ.
   Khối cột audit (`CreatedBy/CreatedAt/…/IsDeleted/Ver`) tự bị loại.
3. Bấm **📊 Sinh Lưới** → tạo `Ui_View` + cột hiển thị.

> 2 nút độc lập — không bắt buộc cùng chạy. Hiện chỉ sinh 1 form / 1 lưới đơn.

---

## 2. Feature B — Chọn "phòng ban cha" trong CHÍNH bảng (chống vòng lặp)

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

---

## 3. Feature C — Kéo-thả sắp xếp cây (ADR-027)

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

---

## 4. Feature A — Tự lọc theo công ty (quyền + công ty đang chọn)

**Ở đâu:** cùng màn **Quản lý View**, tab **"Cơ bản"**.

1. Vẫn ở View của `TC_PhongBan`.
2. Tick ô: **"Scope_By_Company — tự lọc theo công ty (quyền + đang chọn)"**.

**Engine tự làm:** tự JOIN `fnt_CongTyTheoQuyen` (các công ty user được phép) + lọc theo `@CongTyID_Active`
(công ty đang chọn ở switcher). Người dùng chỉ thấy phòng ban thuộc công ty trong phạm vi quyền của mình.

> Ô này **chỉ hiện** khi View là loại **bảng/view** (`CanScopeByCompany`). View kiểu **Sp/Sql** thì tự
> viết SQL và JOIN `fnt_CongTyTheoQuyen` bằng tay. Cột dùng để lọc: `TC_PhongBan.CongTy_Id`.

---

## 5. ConfigSync master → tenant

Config vừa tạo nằm ở **Config DB master**. Chạy **ConfigSync** để đẩy Form + View sang tenant
(UPSERT theo mã, re-link FK). Không đồng bộ → tenant chưa thấy màn mới.

---

## 6. Gắn vào Menu

Thêm mục menu trong **HT_ChucNang** (ADR-023) trỏ tới `View_Code` của Phòng ban, đặt `ViTriHienThi`
đúng nhánh. Menu server-driven sẽ tự hiện sau khi phân quyền cho vai trò.

---

## 7. Checklist nghiệm thu (runtime, trên web)

- [ ] Mở màn Phòng ban → lưới hiện dạng **cây lồng** (không phẳng).
- [ ] Chỉ thấy phòng ban thuộc **công ty đang chọn** ở switcher; đổi công ty → danh sách đổi theo.
- [ ] **Kéo-thả** 1 node sang vị trí khác → thứ tự lưu lại sau khi F5.
- [ ] Thử kéo 1 node vào **con của chính nó** → bị chặn (chống vòng lặp).
- [ ] Thêm/Sửa 1 phòng ban → ô **"Phòng ban cha"** mở picker, **không** liệt kê chính nó + hậu duệ.

---

## 8. Lỗi thường gặp

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
