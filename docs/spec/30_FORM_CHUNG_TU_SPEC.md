# 30 — Form Chứng Từ (Master–Detail Editing) — Capability Spec

> Ngày lập: 2026-07-15 · Trạng thái: **Draft thiết kế** — chưa code.
> Xuất phát: spec 29 §10 — khoảng trống kiểu màn ③ (1 đơn / 1 khách / n dòng hàng / nhiều giá).
> Đây là **năng lực nền tảng Form Engine**, không phải màn bespoke: mọi module ERP
> (mua, bán, kho, kế toán) đều cần; khách hàng đầu tiên là vertical thu mua nông sản (spec 29).

---

## 1. Mục tiêu

Form Engine hiện render **form 1 bản ghi** (Ui_Form/Ui_Section/Ui_Field). Spec này mở rộng để
một form khai báo được **n lưới chi tiết editable** (bảng con), có:

1. Thêm/sửa/xóa dòng inline trên lưới, cột dựng từ config (tái dùng toàn bộ cơ chế Ui_Field).
2. **Công thức client-side** mức dòng + footer tổng (AST Grammar V1, chạy trong WASM).
3. **Sự kiện server** mức dòng (lấy giá theo chính sách, kiểm tồn…) qua contract UiDelta hiện có.
4. **Lưu master + n detail trong 1 transaction** (kèm điểm cắm validate/posting/after-save hook).
5. Cấu hình 100% qua **ConfigStudio WPF** (quy tắc "config qua WPF, không SQL") + sync ConfigSync.

Không thuộc phạm vi: màn list master-detail 2 lưới read-only (đó là `Detail_View_Id` — spec 14 §11);
posting engine định khoản (spec 29 §5 — chỉ định nghĩa điểm cắm ở đây).

---

## 2. Config model (Config DB)

### 2.1 `Ui_Form_Detail` — khai báo lưới chi tiết (MỚI)

```sql
CREATE TABLE dbo.Ui_Form_Detail
(
    Detail_Id         INT            IDENTITY(1,1) NOT NULL,
    Form_Id           INT            NOT NULL,       -- form master (Ui_Form)
    Detail_Code       NVARCHAR(50)   NOT NULL,       -- định danh trong form (vd 'ChiTiet')
    Detail_Form_Id    INT            NOT NULL,       -- Ui_Form CON: định nghĩa field dòng
    Section_Id        INT            NULL,           -- section đặt lưới (null = section riêng cuối form)
    Parent_Key_Column NVARCHAR(100)  NOT NULL,       -- cột FK bảng con trỏ về master (vd 'GiaoDich_Id')
    Edit_Mode         NVARCHAR(20)   NOT NULL DEFAULT 'EntryPanel', -- EntryPanel|CellInline|RowPopup (§3.1)
    Title_Key         NVARCHAR(150)  NULL,           -- i18n tiêu đề lưới
    Allow_Add         BIT NOT NULL DEFAULT 1,
    Allow_Delete      BIT NOT NULL DEFAULT 1,
    Allow_Reorder     BIT NOT NULL DEFAULT 0,        -- kéo thứ tự dòng (ghi cột ThuTu)
    Min_Rows          INT NOT NULL DEFAULT 0,        -- validate tối thiểu (vd đơn hàng ≥1 dòng)
    Summary_Json      NVARCHAR(MAX)  NULL,           -- footer: [{"field":"ThanhTien","func":"SUM"}]
    Order_No          INT NOT NULL DEFAULT 0,        -- thứ tự khi form có nhiều lưới
    Options_Json      NVARCHAR(MAX)  NULL,
    Version           INT NOT NULL DEFAULT 1,
    Is_Active         BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Ui_Form_Detail PRIMARY KEY (Detail_Id),
    CONSTRAINT FK_UFD_Form       FOREIGN KEY (Form_Id)        REFERENCES dbo.Ui_Form(Form_Id),
    CONSTRAINT FK_UFD_DetailForm FOREIGN KEY (Detail_Form_Id) REFERENCES dbo.Ui_Form(Form_Id),
    CONSTRAINT UQ_UFD UNIQUE (Form_Id, Detail_Code)
);
```

**Quyết định thiết kế — cột lưới = `Ui_Field` của form CON:** không tạo bảng cột riêng.
Form con là `Ui_Form` bình thường gắn `Sys_Table` bảng con → tái dùng nguyên vẹn: Editor_Type +
Ui_Control_Map (lookup, combo, numeric, date…), validation Val_Rule, i18n label key theo spec 10,
LockOnEdit/Is_Visible, ConfigStudio đã có sẵn màn field-config. `Order_No` của Ui_Field = thứ tự cột;
`Is_Visible=0` = không hiện cột. Field `Parent_Key_Column` tự ẩn (engine set, không render).

### 2.2 Công thức — thêm cột vào `Ui_Field` (migration)

```sql
ALTER TABLE dbo.Ui_Field ADD Formula_Json NVARCHAR(MAX) NULL;  -- AST Grammar V1 (spec 03)
```

- Field có `Formula_Json` → read-only, giá trị tính từ biểu thức.
- Scope biến trong biểu thức: field **cùng dòng** (`SoLuong`, `DonGia`), field **master** qua tiền tố
  (`Master.TyGia`), hàm aggregate lưới cho field đặt trên **master/footer** (`SUM(ChiTiet.ThanhTien)`).
- Dùng chung cho cả form 1 bản ghi (field tính trên master) — không giới hạn ở lưới chi tiết.

---

## 3. Runtime Blazor (RCL `ICare247.UI.DynamicForms`)

### 3.1 `DetailGridRenderer` (renderer MỚI — thứ 11) — 3 chế độ theo `Edit_Mode` (chốt 2026-07-15)

**`EntryPanel` (MẶC ĐỊNH — kiểu legacy quen tay):** khu nhập liệu phía trên (field form con render
như section thường) + lưới **chỉ hiển thị** bên dưới.
- Bấm **Lưu dòng** → validate dòng → đẩy vào lưới → panel tự làm trống, focus về field đầu → nhập tiếp.
- Click dòng trên lưới → nạp lên panel (chế độ sửa). Giảm rắc rối mà user đã chỉ ra
  ("lỡ chọn dòng, muốn thêm mới phải Lưu hoặc làm trống"): panel hiện **badge "Đang sửa dòng #n"**
  + nút **Thêm mới** luôn hiển thị để thoát chế độ sửa và làm trống panel bằng 1 click
  (có cảnh báo nếu panel đang có thay đổi chưa Lưu dòng).
- **`CellInline`:** gõ trực tiếp trên ô như Excel, Tab/Enter nhảy ô — cho phiếu nhiều dòng nhập nhanh.
- **`RowPopup`:** bấm sửa → dialog form dòng — cho dòng nhiều field không vừa bề ngang lưới.

Chung cả 3 chế độ:
- Nút ➕ thêm / 🗑 xóa dòng theo `Allow_Add/Allow_Delete`; footer summary theo `Summary_Json`.
- Editor map từ `Editor_Type` của Ui_Field form con → tái dùng renderer field hiện có
  (Lookup dùng `LookupComboBoxRenderer`).
- Row state giữ client: `Added / Modified / Deleted / Unchanged` — xóa dòng đã có Id chỉ đánh dấu,
  gửi lên khi save. Luôn ghim header + giới hạn height ([[project-grid-sticky-header]]);
  **`VirtualScrollingEnabled` bật từ FDOC-2** (đã chốt: có ca phiếu 100+ dòng) — đo hiệu năng
  recalc công thức trên phiếu lớn ngay từ FDOC-3.
- Thứ tự làm: **EntryPanel trước (FDOC-3)**; CellInline + RowPopup nối tiếp (FDOC-3b) —
  cùng row-state/công thức, chỉ khác lớp tương tác.

### 3.2 Công thức client-side — AST chạy trong WASM

**Trụ kiến trúc (đã verify 2026-07-15):** `AstParser`/`AstCompiler`/`EvaluationContext` nằm ở
`ICare247.Domain` (thuần C#, csproj **không có** PackageReference hạ tầng) → RCL DynamicForms
**tham chiếu thẳng `ICare247.Domain`**, compile 1 lần cache theo hash (như `AstEngine` backend).

- Khi field trong dòng đổi → build `EvaluationContext` từ dòng + master → recalc các field có
  `Formula_Json` phụ thuộc (thứ tự theo `Sys_Dependency` — dùng lại `/build-dependency-graph`)
  → cập nhật footer aggregate.
- **Không round-trip server, không JS interop.** Tôn trọng rule blazor-ui: numeric dùng
  `onchange`/debounce, không `oninput` re-render cả lưới; chỉ re-render cell/footer bị ảnh hưởng.
- Server **tính lại toàn bộ công thức khi save** (không tin giá trị client) — cùng engine, cùng
  Formula_Json ⇒ không lệch logic.

### 3.3 Sự kiện server mức dòng (nhiều giá, kiểm tồn…)

Mở rộng contract event hiện có của FormRunner (FIELD_CHANGED → `HandleEventAsync` → UiDelta):

- Request thêm `RowContext { Detail_Code, Row_Key }` + payload dòng hiện tại.
- `UiDeltaDto` thêm target dòng: `{ Detail_Code, Row_Key, Field_Code, Value | ReadOnly | Visible }`.
- Ca dùng chuẩn cho spec 29: chọn `HangHoa_Id` trên dòng → event server tra **chính sách giá**
  theo (DoiTac_Id master + HangHoa_Id + SoLuong) → delta set `DonGia` → công thức client tự
  tính `ThanhTien`. Chỉ field gắn cờ event server mới round-trip; còn lại thuần client.

---

## 4. Save tổ hợp — 1 transaction

`POST /api/v1/forms/{formCode}/save-document`

```jsonc
{
  "master": { "Id": null, "DoiTac_Id": 12, "NgayGiaoDich": "2026-07-15", ... },
  "details": {
    "ChiTiet": {
      "rows":       [ { "Id": null, "HangHoa_Id": 5, "SoLuong": 100, ... } ],
      "deletedIds": [ 88, 91 ]
    }
  }
}
```

Pipeline server (MỘT DB transaction — xóa vĩnh viễn kiểu `StepSave` legacy):

```
validate field/form (ValidationEngine, cả dòng detail)
  → validate hook store (spec 18, payload JSON+OPENJSON gồm master+details)
  → server recalc Formula_Json → UPSERT master → UPSERT/soft-delete details (set Parent_Key_Column)
  → [điểm cắm posting engine — spec 29 §5]
  → after-save hook → COMMIT
```

Lỗi bất kỳ → rollback toàn bộ + ProblemDetails RFC 7807. Payload lưu tôn trọng quy tắc
"payload = mọi field IsVisible" hiện hành.

**Bảng vệ tinh 1-1** (đã chốt §8.3): không nằm trong `details` — field của bảng vệ tinh render là
section trên form, payload gửi kèm object 1-1 (`"satellites": { "KD_CanHang": {...} }`),
engine UPSERT theo khóa liên kết trong cùng transaction.

---

## 5. ConfigStudio WPF

Thêm vào màn Form Config tab **"Lưới chi tiết"**: danh sách `Ui_Form_Detail` của form
(chọn form con, `Parent_Key_Column` từ Sys_Column bảng con, section đặt lưới, Min_Rows,
Summary_Json qua editor cột+hàm). Field editor thêm ô **Formula** (soạn AST — đợt đầu cho
paste JSON + validate parse, editor trực quan để sau). Đọc/ghi cột mới try/catch phòng thủ
theo mẫu [[feedback-config-via-wpf]].

## 6. ConfigSync

Descriptor thêm `Ui_Form_Detail` (theo-cha `Ui_Form`, khóa mã = `Form_Code + Detail_Code`,
re-link `Detail_Form_Id`/`Section_Id`); `Ui_Field.Formula_Json` đi theo descriptor Ui_Field sẵn có.

## 7. Thứ tự triển khai (khi được duyệt code)

| Bước | Nội dung | Ghi chú |
|---|---|---|
| FDOC-1 | Migration `Ui_Form_Detail` + `Ui_Field.Formula_Json` + ConfigStudio tab Lưới chi tiết | mở khóa cấu hình |
| FDOC-2 | `DetailGridRenderer` read-only + metadata API trả kèm detail | thấy được lưới |
| FDOC-3 | Edit chế độ **EntryPanel** + row state + công thức client (WASM ref Domain) + footer + aggregate lên master | trái tim UX |
| FDOC-3b | Chế độ CellInline + RowPopup (cùng row-state/công thức) | nối tiếp FDOC-3 |
| FDOC-4 | Save tổ hợp 1 transaction + server recalc + validate hook | dữ liệu an toàn |
| FDOC-5 | Event server mức dòng (chính sách giá) | "nhiều giá" |
| FDOC-6 | ConfigSync + E2E bằng màn Mua hàng tươi (spec 29 Đ3) | nghiệm thu thật |

## 8. Quyết định đã chốt (user, 2026-07-15) — FDOC-001 ✅

1. **EditMode:** hỗ trợ **cả 3 chế độ**, cấu hình per lưới qua `Ui_Form_Detail.Edit_Mode`;
   mặc định **EntryPanel** (khu nhập trên + lưới dưới, kiểu legacy) — chi tiết UX §3.1.
2. **Aggregate** (`SUM(ChiTiet.X)` cho field master): làm **ngay FDOC-3** cùng công thức dòng.
3. **Bảng vệ tinh 1-1** (KD_CanHang, KD_ChotGia…): render là **section field 1-1** trên form,
   engine save hiểu quan hệ 1-1 — KHÔNG dùng lưới 1 dòng.
4. **Virtual scrolling:** có ca phiếu 100+ dòng → bật `VirtualScrollingEnabled` từ FDOC-2
   + đo hiệu năng recalc công thức trên phiếu lớn.
