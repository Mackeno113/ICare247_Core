# 17 — Cơ chế sắp xếp cây cha-con (dùng chung mọi cây)

> **Trạng thái:** ✅ thiết kế CHỐT (2026-06-21, ADR-027) — **CHƯA code**.
> **Bối cảnh:** mọi cây tự tham chiếu trong nền tảng (`TC_CongTy`, `TC_PhongBan`, `HT_ChucNang`, + cây generic
> qua `Ui_View.Parent_Field` / `Ui_Field_Lookup.Parent_Column`) cần **một cơ chế sắp xếp & đánh số thống nhất**:
> gốc theo thứ tự, con nằm ngay dưới cha, đánh số liên tục trên→dưới để **hiển thị** + **truy vấn** nhanh.

---

## 1. Hai khái niệm tách bạch

| Cột | Kiểu | Ai đặt | Ý nghĩa |
|---|---|---|---|
| **`ThuTu`** | INT | **Người dùng** | Thứ tự **trong cùng cha / cùng cấp** (anh em). Input — đã có sẵn ở các bảng. |
| **`ThuTuCay`** | INT | **Hệ thống** (dẫn xuất) | Số phẳng **liên tục 1..N cả bảng** sau khi duyệt cây trên→dưới. Phục vụ `ORDER BY` 1 cột + truy vấn. |
| **`DuongDanCay`** | NVARCHAR | **Hệ thống** (dẫn xuất) | Chuỗi đường dẫn phân cấp `001.002.003` — khóa sort chuẩn của cây. |

> **Cùng cha cùng cấp → `ThuTu` tăng dần. Khác cha → mỗi nhánh reset về `001`** (nhờ partition theo cha).

---

## 2. Quy ước nền tảng ICare247

- **Gốc chính = `Cha_Id IS NULL` HOẶC `Cha_Id = 0`** (hỗ trợ cả 2 quy ước).
- Luôn lọc **`IsDeleted = 0`** (và `KichHoat` / `TrangThai` nếu cây đó có).
- **Tiebreaker `Id`**: mọi `ORDER BY ThuTu` phải kèm `, Id` — `ThuTu` có thể trùng → tránh thứ tự bất định.
- **Độ rộng bậc = 3 chữ số** (`001`..`999`) — tối đa 999 node cùng cha/cùng cấp. Zero-pad cố định ⇒ sort chuỗi = sort số.
- **Đánh số phẳng `ThuTuCay` = liên tục cả bảng** (không reset theo scope).

### 2.1. `TC_PhongBan` — cây toàn bộ cấu trúc công ty
Cây phòng ban **vẽ lại toàn bộ cấu trúc tổ chức**: công ty (gốc) → tổ → bộ phận. Vì công ty đóng vai gốc nên đánh số
**liên tục cả bảng là đúng**, miễn **xếp gốc theo công ty trước, rồi `ThuTu`** để mỗi công ty nằm **liền khối**
(toàn bộ công ty 1 → toàn bộ công ty 2 → …). Đây là lý do proc nhận thêm tham số **cột Scope** để xếp gốc.

---

## 3. Tầng 1 — Cập nhật `ThuTu` theo thao tác người dùng

Mọi gesture chỉ đụng tới **một nhóm anh em** (cùng cha):

| Gesture | Cơ chế |
|---|---|
| **▲ / ▼ (mũi tên)** | **Swap `ThuTu`** giữa node và anh em liền kề trên/dưới (cùng cha). Chỉ 2 dòng đổi. |
| **Kéo-thả** | Drop cho biết *(cha mới, vị trí index trong nhóm đích)*: ① nếu đổi cha → set `Cha_Id` mới; ② **đánh lại `ThuTu` dày đặc `1..n`** cho nhóm anh em đích (chèn node vào đúng index), tùy chọn đánh lại cả nhóm nguồn. |

**Ràng buộc kéo-thả:** **chặn vòng lặp** — không cho thả một node vào chính con-cháu của nó (kiểm tra server + client,
như Menu Builder đã có).

> Không dùng kiểu gap/sparse (10, 20, 30…) hay LexoRank — quá mức cho quy mô phòng ban/menu. Đánh lại dày đặc
> `1..n` cho **riêng nhóm anh em bị ảnh hưởng** là đủ rẻ (1 cấp, vài dòng).

---

## 4. Tầng 2 — Tính lại cột dẫn xuất (recompute-on-write)

Ngay **sau khi Tầng 1 commit**, write-path gọi **1 stored proc generic** tính lại `ThuTuCay` + `DuongDanCay`.

### 4.1. Thuật toán (recursive CTE)

```
1. ROOT  : các node gốc (Cha_Id IS NULL OR Cha_Id = 0), IsDeleted = 0.
           DuongDanCay = RIGHT('000' + CAST(ROW_NUMBER() OVER (
                           PARTITION BY <Scope>  ORDER BY ThuTu, Id) AS VARCHAR(4)), 3)
2. CHILD : nối đệ quy theo Cha_Id.
           DuongDanCay = cha.DuongDanCay + '.' + RIGHT('000' + CAST(ROW_NUMBER() OVER (
                           PARTITION BY Cha_Id ORDER BY ThuTu, Id) AS VARCHAR(4)), 3)
3. FLAT  : ThuTuCay = ROW_NUMBER() OVER (ORDER BY DuongDanCay)   -- liên tục 1..N cả bảng
4. UPDATE: ghi cả 2 cột về bảng theo Id.
```

- **`<Scope>`** = khóa xếp gốc: `NULL`/không cho cây 1-gốc (`HT_ChucNang`, `TC_CongTy`); = thứ tự công ty cho
  `TC_PhongBan`. Node con luôn `PARTITION BY Cha_Id` (đã đủ tách nhánh).
- Sort chuỗi `DuongDanCay` = đúng thứ tự duyệt cây cha→con, trên→dưới ⇒ `ThuTuCay` liên tục khớp UI.

### 4.2. Vì sao 1 proc generic (không per-table)
Logic CTE giống hệt nhau giữa các cây, chỉ khác **tên bảng + tên cột** (Key / Parent / Order / Scope). Một proc
nhận tham số ⇒ thêm cây mới = khai báo tham số, **không lặp code**. Hợp kiến trúc engine-driven (cây generic qua
`Ui_View.Parent_Field` dùng lại cùng proc). Repository C# (write-path) là nơi gọi proc sau khi ghi.

---

## 5. Luồng tổng quát

```
User ▲▼ / kéo-thả
   └─► API write-path (C#)
        ├─ (kéo-thả) kiểm tra vòng lặp → set Cha_Id mới (nếu đổi cha)
        ├─ cập nhật ThuTu nhóm anh em (swap | renumber dày đặc 1..n)
        └─ EXEC proc generic tính lại ThuTuCay + DuongDanCay  (recompute-on-write)
   ◄── đọc lại: ORDER BY ThuTuCay → đúng thứ tự cây trên→dưới
```

---

## 6. Áp dụng & lộ trình triển khai (khi code)

**Cây áp dụng:** `TC_CongTy`, `TC_PhongBan`, `HT_ChucNang` (+ cây generic `Ui_View.Parent_Field` sau).

1. **Migration** — thêm 2 cột `ThuTuCay INT NULL` + `DuongDanCay NVARCHAR(400) NULL` + index (`ThuTuCay`) cho 3 bảng cây.
2. **Stored proc generic** — tính lại theo §4 (tham số bảng/Key/Parent/Order/Scope), idempotent.
3. **Write-path** — nối gesture ▲▼ / kéo-thả (§3) → gọi proc (§4) sau khi ghi.

> **Liên quan:** ADR-027 (`.claude/memory/architecture_decisions.md`), ADR-023 (cây menu `HT_ChucNang`),
> `docs/spec/11_DATA_DB_SCHEMA.md` (2 cây `TC_CongTy`/`TC_PhongBan`).
