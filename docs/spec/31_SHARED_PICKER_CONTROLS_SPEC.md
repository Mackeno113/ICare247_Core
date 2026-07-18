# Shared Data Picker Controls — control dữ liệu dùng chung

**Spec:** 31_SHARED_PICKER_CONTROLS_SPEC
**Phiên bản:** 1.0 (draft — chờ user duyệt trước khi code)
**Ngày:** 2026-07-16
**Liên quan:** spec 19 (Sys_Context_Param / token ngữ cảnh) · spec 25 (FK lookup) · spec 12 (cascade lookup) ·
spec 16 (ConfigSync) · db/060 (token) · db/068 (Reload_Trigger_Fields) · db/082 (HT_VaiTro_CongTy)

---

## 1. Vấn đề & Mục tiêu

Nhiều màn hình lặp lại cùng một "kiểu lấy dữ liệu" nhưng mỗi màn tự chế lại:

- **Chọn công ty**: luôn lọc theo phân quyền (gán riêng ∪ theo vai trò), luôn theo semantics
  `@CongTyID_Active` (0 = tất cả trong phạm vi quyền, >0 = đúng 1 công ty). Hiện switcher và
  tab "Công ty truy cập" màn Người dùng là 2 bản cài riêng.
- **Bộ địa chỉ**: ô địa chỉ chi tiết + Xã/Phường (cascade) + Tỉnh/Thành **suy ra** (không lưu trùng).
- **Danh sách nhân viên tại thời điểm T**: T là biến truyền vào, nhưng **tên field nguồn mỗi màn
  mỗi khác** (NgayHieuLuc / NgayChamCong / NgayNghiPhep…).

**Mục tiêu:** đóng gói mỗi "kiểu lấy dữ liệu" thành control dùng chung — *sửa 1 nơi áp mọi màn* —
phục vụ **cả 2 thế giới**: màn bespoke (Razor viết tay) và màn engine (no-code, Ui_Field_Lookup).

## 2. Nguyên tắc thiết kế (BẮT BUỘC)

1. **Tách Hợp-đồng-dữ-liệu khỏi Vỏ-UI.** Mỗi control chung = ① 1 nguồn dữ liệu chuẩn (API/SQL template)
   + ② bộ **tham số canonical** tên cố định + ③ vỏ UI chuẩn. Màn hình chỉ *map giá trị* vào tham số.
2. **Tham số canonical, màn hình map tên.** Control khai báo `ThoiDiem`; màn hợp đồng bind
   `NgayHieuLuc`, màn chấm công bind `NgayChamCong`. Control không biết nghiệp vụ nguồn.
3. **Phân quyền enforce server-side.** Client không gửi `userId`; `@NguoiDungID`/`@CongTyID_Active`
   bind theo spec 19. Danh sách trả về đã lọc quyền — UI chỉ hiển thị.
4. **Semantics `CongTyId` thống nhất:** `0`/null = "tất cả trong phạm vi quyền" (khớp default
   token `@CongTyID_Active = 0`), `>0` = đúng 1 công ty. Mọi picker nhận ngữ cảnh công ty đều theo quy ước này.
5. **Tên component tiền tố `Ic`** (IcCompanyPicker…), đặt trong RCL `ICare247.UI.Shared`.
   Mọi chuỗi hiển thị qua i18n (`Loc.L`), key nhóm `common.picker.*`.
6. **Dropdown/popup phải teleport-safe** (gotcha DraggableModal: popup trong container transform bị cắt
   → dùng `icare.teleportPopup` hoặc overlay fixed như CompanySwitcher).

## 3. Tầng 1 — Picker API chuẩn (backend, dùng chung)

| Mã nguồn dữ liệu | Endpoint | Tham số canonical | Ghi chú |
|---|---|---|---|
| `companies` | `GET /api/v1/me/companies` | *(ngầm: @NguoiDungID)* | **ĐÃ CÓ** — trả cây + `CanAccess` (tổ tiên disabled), gán riêng ∪ theo vai trò (db/082) |
| `dia-ban` | `GET /api/v1/pickers/dia-ban?parentId=` | `ParentId` | Tỉnh (ParentId null) / Xã-Phường (ParentId=tỉnh). Nguồn `DM_TinhThanhPho`/`DM_PhuongXa` |
| `nhan-vien` | `GET /api/v1/pickers/nhan-vien?thoiDiem=&congTyId=&keyword=` | `ThoiDiem`, `CongTyId`, `Keyword` | ⚠️ Phụ thuộc `NS_NhanVien` (đợt NS_) — **thiết kế trước, code sau**. "Hợp lệ tại T" = đang làm việc tại T, thuộc phạm vi công ty |

Quy ước chung: response gọn `{id, ma, ten, parentId?}`; các picker danh mục lớn hỗ trợ `Keyword`
(server-side contains) + `Top` mặc định 50. Controller chung `PickersController`, mỗi nguồn 1 query CQRS.

## 4. Tầng 2a — Bộ component RCL (màn bespoke)

### 4.1 `IcCompanyPicker` — 2 chế độ (user chốt 2026-07-16)

| Chế độ | Dùng cho | Hành vi |
|---|---|---|
| `Single` (mặc định) | Field `CongTy_Id` trên form nghiệp vụ (Phòng ban, chứng từ…), company switcher | Dropdown cây (indent theo cấp), node `CanAccess=false` mờ không chọn được; prop `AllowAll` thêm option "Tất cả công ty" (→ value null/0) |
| `MultiCheck` | Cây gán quyền (tab Công ty truy cập màn Người dùng, view Phạm vi công ty màn Phân quyền) | Cây checkbox **WYSIWYG**: tick cha → tự tick toàn nhánh trên UI, bỏ tick từng con tùy ý, bỏ tick con KHÔNG rớt cha; slot `NodeExtra` (RenderFragment per-node) cắm radio "Mặc định"/badge "Theo vai trò" |

Props chính: `Mode`, `Value/ValueChanged` (Single), `SelectedIds/SelectedIdsChanged` (MultiCheck),
`AllowAll`, `Disabled`, `NodeExtra`. Nguồn: mặc định `/me/companies`; MultiCheck cho phép truyền
`Items` ngoài (màn admin cần cây + cờ riêng như GanRieng/TheoVaiTro thì tự load rồi đưa vào).

> Refactor kèm: `CompanySwitcher` + tab Công ty màn Người dùng + view Phạm vi công ty màn Phân quyền
> chuyển sang dùng `IcCompanyPicker` — 3 bản cài cây hiện tại gom về 1.

### 4.2 `IcAddressBlock` — cụm địa chỉ composite

- Gồm: ô text địa chỉ chi tiết (full-width) + lookup **Xã/Phường** (search server-side theo `Keyword`,
  cascade từ Tỉnh) + Tỉnh/Thành hiển thị **suy ra** từ Xã/Phường (KHÔNG lưu trùng cột Tỉnh — đúng
  blueprint màn Công ty).
- Bind: `@bind-DiaChi` + `@bind-PhuongXaId`. Layout theo skill admin-ui (label top, field co theo data).

### 4.3 `IcEmployeePicker` — nhân viên tại thời điểm

- Props: `ThoiDiem` (DateTime?, canonical — màn nào bind field ngày của màn đó), `CongTyId`
  (mặc định lấy từ `AppState.ActiveCompanyId`), `Value/ValueChanged`, `Keyword` gõ-tìm (debounce
  theo rule blazor-ui, tránh `oninput` re-render).
- `ThoiDiem`/`CongTyId` đổi → tự reload (Blazor parameter binding, không cần cơ chế riêng).
- ⚠️ Chỉ code khi có `NS_NhanVien`.

### 4.4 Nền chung

- `IcPickerBase<TValue>`: chuẩn hóa Value/ValueChanged, Disabled, Placeholder (i18n), trạng thái
  loading/empty, đóng-mở popup + backdrop.
- **Cache L0 phía client** theo khóa `(nguồn, tham số)` scoped per-phiên (`AppState` hoặc service riêng):
  địa bàn/công ty gọi 1 lần mỗi phiên; nguồn có `ThoiDiem` KHÔNG cache (biến thiên theo form).

## 5. Tầng 2b — Lookup Template (màn engine no-code) — ✅ ĐÃ CODE (P4, 2026-07-16)

Đóng gói cấu hình lookup lặp lại thành **mẫu chọn được từ ConfigStudio** (quy tắc: mọi cấu hình có ô
trên WPF, không SQL tay). *Cập nhật theo thực tế engine (khác draft ban đầu): field lookup engine dùng
bộ `Query_Mode / Source_Name / Filter_Sql` (DynamicLookupRepository), KHÔNG phải `Lookup_Sql`.*

- **Bảng `Ui_Lookup_Template`** (Config DB — db/083): `Template_Code` (unique), `Ten`, `Mo_Ta`,
  bộ định nghĩa truy vấn cùng ngữ nghĩa `Ui_Field_Lookup` (`Query_Mode/Source_Name/Value_Column/
  Display_Column/Code_Field/Filter_Sql/Order_By/Popup_Columns_Json/Parent_Column`),
  `Canonical_Params` (JSON: `[{"name","type","required","moTa"}]`), `Is_Active` + 4 cờ sync CFGSYNC-1.
- **`Ui_Field_Lookup` thêm 2 cột** (db/083): `Template_Code` (NULL = tự cấu hình như cũ) +
  `Param_Map` (JSON `{"TinhId": "TinhThanhPho_Id"}` — giá trị = Field_Code / `"@Token"` / **hằng số**
  number/bool — câu hỏi mở #2 chốt CÓ).
- **Runtime resolve (DynamicLookupRepository + FormRepository):** field chọn mẫu → định nghĩa truy vấn
  lấy TRỌN từ mẫu (Popup/Code/Parent field override được); tenant chưa chạy db/083 → fallback SQL cũ
  (try/catch). **Token `Sys_Context_Param` engine TỰ resolve cho mọi @param còn thiếu** (qua
  `IContextParamResolver` — spec 19) → mẫu dùng `@NguoiDungID` không cần map. ⚠️ Cache key hash SAU
  khi bind đủ tham số — token theo user phải vào key.
- **⚠️ Chuỗi con DDL/DML bị chặn trong Filter_Sql/custom_sql** (kể cả `IsDeleted` chứa "DELETE") →
  nguồn mẫu trỏ **view `vw_*`** (db/051/052) hoặc **inline TVF** (`fnt_CongTyTheoQuyen` — db/084).
- **Reload:** field nguồn trong `Param_Map` tự merge vào `Reload_Trigger_Fields` (FormRepository) —
  không bắt admin khai 2 lần.
- **ConfigStudio (đã làm):** LookupBoxPropsPanel section "Mẫu lookup dùng chung" — combo mẫu + lưới
  map tham số (GridControl); IO qua 3 method phòng thủ trên FieldDataService (pattern Import_Global_Code).
- **ConfigSync (đã làm):** descriptor `Ui_Lookup_Template` (khóa `Template_Code`, trước Ui_Field_Lookup).
- **Seed (db/083):** `TPL_CONG_TY` (theo quyền — fnt_CongTyTheoQuyen db/084), `TPL_TINH_THANH`, `TPL_PHUONG_XA`.
  `TPL_NHAN_VIEN_TAI_THOI_DIEM` **để đợt NS_** (bảng chưa có — không seed SQL trỏ bảng ma).

## 6. Lộ trình đề xuất

| Pha | Nội dung | Phụ thuộc |
|---|---|---|
| P1 | Spec này (duyệt) | — |
| P2 | `IcPickerBase` + `IcCompanyPicker` 2 chế độ + refactor 3 chỗ đang tự chế (switcher, tab Công ty, view Phạm vi công ty) | db/082 đã có |
| P3 | `PickersController` + nguồn `dia-ban` + `IcAddressBlock` (dùng ngay cho màn Phòng ban/Công ty) | — |
| P4 | Tầng engine: `Ui_Lookup_Template` + `Param_Map` + ô ConfigStudio + ConfigSync + seed mẫu | migration Config DB |
| P5 | Nguồn `nhan-vien` + `IcEmployeePicker` | đợt `NS_NhanVien` |

## 7. Câu hỏi mở (chốt khi code từng pha)

1. P3: bảng địa bàn thật tên gì/cấu trúc live DB (quy tắc verify-live-db-schema trước khi code)?
2. P4: `Param_Map` cho phép map **hằng số** không (vd `{"CongTyId": 5}`)? Đề xuất: có, ít tốn thêm.
3. P5: định nghĩa "nhân viên hợp lệ tại T" (thử việc? nghỉ không lương?) — chốt cùng spec NS_.
