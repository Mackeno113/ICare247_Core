# Authorization & Navigation Spec — Menu động + Phân quyền (phase-auth)

**Spec:** 15_AUTHZ_NAVIGATION_SPEC
**Phiên bản:** 1.0 (thiết kế — CHƯA code)
**Ngày:** 2026-06-13
**ADR liên quan:** ADR-023 (architecture_decisions.md) · ADR-018 (DB-per-tenant) · ADR-022 (tiền tố bảng) · ADR-007 (ConfigStudio Direct DB)
**i18n:** key menu suy từ `Ma` chức năng — xem `10_RESOURCE_KEY_CONVENTION.md`

---

## 1. Mục tiêu

Menu (sidebar + sub-nav trong màn) và việc bật/tắt nút thao tác phải **sinh từ phân quyền theo
vai trò**, không hard-code. Mỗi tài khoản chỉ thấy đúng phần mình được phép. Hiện trạng:
`AppNav.cs` vẽ tĩnh, `NavMenu.CanShow()` luôn `true` → ai cũng thấy đủ. Đây là khoảng trống
`TODO(phase-auth)`.

### Nguyên tắc nền
- **Server là nguồn sự thật của menu.** Backend đọc DB phân quyền → trả cây menu đã lọc; frontend chỉ render.
- **Ẩn menu ≠ bảo mật.** Phải có **2 nửa**: (a) client lọc menu (UX) + (b) **server enforce trên mỗi endpoint** (deny-by-default). Gõ thẳng URL/gọi API vẫn bị chặn nếu thiếu quyền.
- **`AppNav.cs` → giáng cấp** thành nguồn **seed** (đổ menu base) + **fallback dev** (khi API lỗi/chưa cấu hình).

---

## 2. Hai khái niệm TÁCH BẠCH (đừng lẫn)

| | ① ĐỊNH NGHĨA menu | ② PHÂN QUYỀN menu |
|---|---|---|
| Bản chất | *Tính năng nào tồn tại* + route/icon/cấu trúc | *Ai được thấy/làm gì* trên tính năng đã có |
| Ai làm | **DEV / builder** (biết kỹ thuật) | **End user** (admin tenant) |
| Công cụ | **WPF ConfigStudio** (Direct DB) | **Web** (qua API backend) |
| DB ghi | **Config DB** (`Sys_Menu`, `Sys_MenuCatalog`) | **Data DB tenant** (`HT_VaiTro_Quyen`, `HT_NguoiDung_VaiTro`) |
| Thấy gì | route, icon, liên kết `Ui_Form`/`Ui_View` | chỉ **Tên** + ô tick Xem/Thêm/Sửa/Xóa/In |

> End user **không thể biết** tính năng gọi API nào, mở gì, icon gì → việc đó **không bao giờ** giao cho end user.

---

## 3. Mô hình master → tenant (Cách 3 — lai)

Tenant = khách hàng, **mỗi khách 1 Data DB riêng** (ADR-018).

```
                    ┌──────── CONFIG DB (platform, dùng chung) ────────┐
   DEV (WPF) ──────►│  Sys_Menu (bộ menu)                              │
                    │  Sys_MenuCatalog (menu MASTER/base)              │
                    └───────────────┬──────────────────────────────────┘
                                    │  đồng bộ (UPSERT theo Ma) khi provision / nâng cấp
              ┌─────────────────────┼─────────────────────┐
              ▼                     ▼                     ▼
   DATA DB acme            DATA DB beta            DATA DB gamma
   HT_ChucNang:            HT_ChucNang:            HT_ChucNang:
     base (LaHeThong=1)      base (LaHeThong=1)      base (LaHeThong=1)
     + custom (=0)                                   + custom (=0)   ← DEV thêm riêng/khách
              │
   END USER ──► gán quyền: HT_VaiTro_Quyen / HT_NguoiDung_VaiTro
```

- **Base** (`LaHeThong=1`): đồng bộ từ master; node tùy chọn có thể để **tắt sẵn** rồi khách tự bật.
- **Custom** (`LaHeThong=0`): DEV thêm riêng vào 1 tenant; đồng bộ master **không đụng tới**.
- **Đồng bộ = UPSERT theo `Ma`** (UNIQUE): `Ma` chưa có → INSERT; đã có & `Ver` mới hơn → UPDATE; chỉ chạm node `LaHeThong=1`.

---

## 4. Cấu trúc bảng

> Các bảng `HT_*` đã tồn tại trong `db/037_create_data_db_foundation.sql`. Spec này **bổ sung cột**
> cho `HT_ChucNang` và **2 bảng mới** ở Config DB. DDL đã viết (CHƯA áp vào DB):
> `db/042` (ALTER HT_ChucNang) · `db/043` (CREATE Sys_Menu/Sys_MenuCatalog) · `db/044` (seed master) ·
> `db/045` (seed HT_ChucNang base + grant SUPERADMIN).
>
> **Quy ước cột:** Config DB = **snake_case English** (`*_Id`, `Tenant_Id NULL`=dùng chung, `Is_Active`,
> `Created_At/Updated_At`, KHÔNG có `Created_By`). Data DB (`HT_*`) = **PascalCase Việt** + audit `CreatedBy`.
> Bảng dưới ghi tên cột **thực tế đã triển khai**.

### 4.1 `Sys_Menu` — bộ menu (Config DB) 🆕
Cho phép **nhiều menu** (không chỉ 1 sidebar).

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Menu_Id` | INT PK | |
| `Menu_Code` | NVARCHAR(50) UNIQUE/tenant | Mã bộ menu: `MAIN`, `MOBILE`… |
| `Menu_Name` | NVARCHAR(200) | Tên bộ menu |
| `Menu_Type` | NVARCHAR(20) | `Sidebar` / `Top` / `Mobile` / `Context` |
| `Tenant_Id` | INT NULL | NULL = dùng chung mọi tenant |
| `Is_Active`, `Created_At`, `Updated_At` | | chuẩn Config DB |

### 4.2 `Sys_MenuCatalog` — menu MASTER/base (Config DB) 🆕
Bản gốc 1 nơi, DEV sửa qua WPF → đồng bộ xuống tenant. Chứa **định nghĩa** (không chứa quyền).

| Trường | Kiểu | Ý nghĩa (↔ HT_ChucNang) |
|---|---|---|
| `Catalog_Id` | BIGINT PK | |
| `Menu_Id` | INT FK → Sys_Menu | Thuộc bộ menu nào |
| `Func_Code` | NVARCHAR(100) UNIQUE/menu | Khóa nghiệp vụ ổn định = `HT_ChucNang.Ma`, vd `hr.reward`. Dùng đồng bộ + enforce |
| `Func_Name` | NVARCHAR(200) | ↔ `Ten` — tên hiển thị (base/fallback; dịch qua i18n) |
| `Parent_Code` | NVARCHAR(100) NULL | Mã cha (cây) — dùng code thay Id để bền khi đồng bộ giữa DB |
| `Func_Type` | NVARCHAR(20) | ↔ `Loai`: `Menu` (nhóm/phân hệ) · `ManHinh` (màn) · `ChucNangCon` (pha sau) |
| `Module` | NVARCHAR(20) | Mã phân hệ: `TC`,`NS`,`TL`,`TM`,`CN`,`BC`,`HT` |
| `Route` | NVARCHAR(300) NULL | ↔ `DuongDan` — route màn mở; NULL với node nhóm |
| `Icon` | NVARCHAR(100) NULL | Tên icon Lucide (`building`,`users`…) — khớp component `<Icon Name>` |
| `Display_Pos` | NVARCHAR(20) | ↔ `ViTriHienThi`: `Sidebar` / `TrongMan` / `Ca2` (xem §5) |
| `Display_Order` | INT | ↔ `ThuTu` — thứ tự trong cùng cấp |
| `Default_Enabled` | BIT | Giá trị khởi tạo `HT_ChucNang.KichHoat` khi đồng bộ (node tùy chọn → 0) |
| `Tenant_Id` | INT NULL | NULL = base dùng chung |
| `Version` | INT | Phiên bản — đồng bộ dựa vào để biết bản mới |
| `Is_Active`, `Created_At`, `Updated_At` | | chuẩn Config DB |

### 4.3 `HT_ChucNang` — cây menu của tenant (Data DB) — BỔ SUNG CỘT
Cây tự trỏ (`ChucNang_Cha_Id`), **sâu tùy ý**. Cột hiện có: `Id, Ma, Ten, ChucNang_Cha_Id, Loai,
Module, DuongDan, Icon, ThuTu` + audit. **Thêm:**

| Trường 🆕 | Kiểu | Ý nghĩa |
|---|---|---|
| `Menu_Id` | INT | Thuộc bộ menu nào (đồng bộ từ Sys_Menu) |
| `LaHeThong` | BIT DEFAULT 0 | **1 = BASE** (đồng bộ từ master, khóa cấu trúc) · **0 = CUSTOM** (tenant/DEV thêm riêng) |
| `KichHoat` | BIT DEFAULT 1 | Tenant **bật/tắt** node (độc lập quyền). Tắt = không ai thấy dù có quyền |
| `ViTriHienThi` | NVARCHAR(20) DEFAULT `Sidebar` | `Sidebar` / `TrongMan` / `Ca2` (xem §5) |
| `DoiTuong` | NVARCHAR(100) NULL | Mã đối tượng engine node điều khiển (vd `HT_VaiTro` cho master-data/runtime; mã view) — dùng enforce endpoint generic (`db/046`) |
| `LoaiDoiTuong` | NVARCHAR(20) NULL | `Form` / `View` / NULL (chưa gắn). NULL = enforce-if-mapped bỏ qua |

> Quy ước `LaHeThong BIT` đã dùng sẵn ở `HT_VaiTro` (vai trò hệ thống) → nhất quán.

### 4.4 `HT_VaiTro` — vai trò (đã có)
`Id, Ma (UNIQUE), Ten, MoTa, LaHeThong (1=không cho xóa)` + audit.

### 4.5 `HT_VaiTro_Quyen` — CẤP QUYỀN (đã có, bảng trung tâm)
Mỗi dòng = "vai trò X có quyền gì trên chức năng Y".

| Trường | Ý nghĩa |
|---|---|
| `VaiTro_Id` FK → HT_VaiTro | Vai trò nào |
| `ChucNang_Id` FK → HT_ChucNang | Chức năng/màn nào |
| `Xem` | Thấy menu + mở màn (**cờ lọc sidebar**) |
| `Them` / `Sua` / `Xoa` / `InAn` | Bật/tắt nút thao tác trong màn |
| `Duyet` | ⛔ **KHÔNG dùng ở phân quyền** — để dành cho **workflow** (giữ cột) |
| UNIQUE | (VaiTro_Id, ChucNang_Id) |

**Deny-by-default:** không có dòng / cờ = 0 → không có quyền.

### 4.6 `HT_NguoiDung_VaiTro` — gán vai trò cho user (đã có)
`NguoiDung_Id`, `VaiTro_Id`, UNIQUE(cặp). **N-N:** 1 user nhiều vai trò; quyền = **hợp (OR)**.

---

## 5. Một cây — nhiều vị trí render (`ViTriHienThi`)

Tất cả chức năng cần quyền nằm chung **1 cây `HT_ChucNang` sâu tùy ý**. Nhưng **không đổ hết ra
sidebar** (sidebar sâu = không dùng được). Mỗi node khai báo nơi render:

| `ViTriHienThi` | Render ở | Dùng cho |
|---|---|---|
| `Sidebar` | thanh điều hướng trái | cấp nông: nhóm → phân hệ → màn chính |
| `TrongMan` | sub-nav **bên trong 1 màn** | cấp sâu: các "quá trình" của 1 bản ghi (vd 1 nhân viên) |
| `Ca2` | cả hai | |

Ví dụ Nhân sự (sâu nhiều cấp, mỗi node = 1 màn có quyền riêng):
```
Nhân sự (Sidebar)
└─ Hồ sơ nhân viên (Sidebar, ManHinh)
   ├─ Danh mục (TrongMan)
   └─ Quá trình khác (TrongMan)                  cấp 1
      ├─ Chế độ bảo hiểm                          cấp 2 (con)
      │  ├─ Bắt buộc                              cấp 3 (cháu)
      │  │  ├─ Quá trình đóng BHXH                lá · màn
      │  │  └─ Quá trình đóng BHYT                lá · màn
      │  └─ Tiêm vaccine                          cấp 3 · màn
      ├─ Quá trình lao động                       cấp 2 (con)
      │  ├─ Công tác · Kỷ luật · Khen thưởng · Điều chỉnh lương   (các màn)
      └─ Thời gian & công → Nghỉ phép · Làm thêm giờ
```
- **Sidebar** chỉ vẽ node `ViTriHienThi ∈ {Sidebar, Ca2}` → giữ nông.
- **Màn "Hồ sơ nhân viên"** đọc node con `∈ {TrongMan, Ca2}` → vẽ cây/tab sub-nav bên trong.
- Cả hai cùng lọc `Xem=1` → quyền nhất quán dù render ở đâu.

### Ranh giới NODE vs DỮ LIỆU
| Là node `HT_ChucNang` (vào cây + quyền) | Là dữ liệu nghiệp vụ (KHÔNG vào cây) |
|---|---|
| Màn "Quá trình kỷ luật" | Các **bản ghi kỷ luật** của 1 nhân viên |
| Màn "Danh mục Chức danh" | Các **chức danh** đã nhập |
| Màn "Nghỉ phép" | Các **đơn nghỉ phép** |

→ "Màn hình / chức năng" = node. "Hàng/giá trị bên trong màn" = data ở bảng nghiệp vụ riêng.

---

## 6. API

```
GET /api/v1/me/navigation     → cây node Xem=1 (gồm Ma, Ten, Module, DuongDan, Icon,
                                 ViTriHienThi, ThuTu, con[]). Suy userId/roles/tenant từ JWT.
GET /api/v1/me/permissions     → map { "HR.NhanVien.KyLuat": {xem,them,sua,xoa,inan}, ... }
```
- **Gộp 1 call sau login** (navigation + permissions) cho gọn.
- **Cache** theo `tenant + tập-role` (config đổi hiếm — dùng `IConfigCache`, đồng bộ ADR-014). Invalidate khi sửa `HT_VaiTro_Quyen` / `HT_ChucNang`.

### Truy vấn runtime (rút gọn — cây sâu = recursive CTE)
```sql
WITH cn AS (
  SELECT c.* FROM HT_ChucNang c
  JOIN HT_VaiTro_Quyen q      ON q.ChucNang_Id = c.Id AND q.Xem = 1 AND q.IsDeleted = 0
  JOIN HT_NguoiDung_VaiTro uv ON uv.VaiTro_Id = q.VaiTro_Id AND uv.IsDeleted = 0
  WHERE uv.NguoiDung_Id = @userId AND c.KichHoat = 1 AND c.IsDeleted = 0
)
SELECT DISTINCT * FROM cn ORDER BY ThuTu;   -- DISTINCT vì nhiều vai trò có thể trùng (OR)
```
→ dựng cây theo `ChucNang_Cha_Id`; client render sidebar (nhánh nông) + mỗi màn render nhánh con.

### Enforce server (nửa b) — bắt buộc
2 attribute (đọc `HT_VaiTro_Quyen` qua `IPermissionService`, bypass role `SUPERADMIN`, thiếu → 403):
```csharp
// Theo MÃ CHỨC NĂNG tĩnh (vd màn admin):
[RequirePermission("administration.permissions", PermissionOp.Sua)]

// Theo ĐỐI TƯỢNG engine lấy từ route (endpoint generic) — enforce-if-mapped:
[RequirePermissionForTarget("Form", PermissionOp.Sua, "formCode")]   // master-data/runtime
[RequirePermissionForTarget("View", PermissionOp.Xem, "code")]       // views
```
**Enforce-if-mapped:** nếu KHÔNG node `HT_ChucNang` nào gắn `(LoaiDoiTuong, DoiTuong)` khớp → **cho qua**
(chưa cấu hình thì không khóa, tránh vỡ app khi chuyển tiếp). Có gắn → áp deny-by-default theo cờ.

Đã gắn: `MasterDataController` (Form: GET=Xem · POST=Thêm · PUT=Sửa · DELETE=Xóa), `ViewController`
(View: Xem), `RuntimeController` (Form: Xem). `FormController` (config metadata) tạm chưa enforce.

> Để 1 màn được enforce thật: set `HT_ChucNang.DoiTuong` = mã form/view + `LoaiDoiTuong` = Form/View,
> rồi cấp quyền cho vai trò ở màn Phân quyền.

---

## 7. Cách hoạt động (4 pha)

1. **DEV định nghĩa** (WPF) → sửa `Sys_MenuCatalog`, tăng `Ver`.
2. **Đồng bộ** (provision/nâng cấp) → UPSERT theo `Ma` xuống `HT_ChucNang` mỗi tenant (`LaHeThong=1`); node custom (`=0`) giữ nguyên. DEV thêm màn riêng → INSERT `LaHeThong=0` vào tenant đó.
3. **End user cấu hình quyền** (Web): tạo `HT_VaiTro` → màn Phân quyền tick cờ → `HT_VaiTro_Quyen`; gán vai trò → `HT_NguoiDung_VaiTro`.
4. **Runtime**: login → `/me/navigation` lọc `Xem=1` → `NavMenu` vẽ. Endpoint enforce theo cờ.

---

## 8. UI cấu hình

### 8.1 Màn Phân quyền (bespoke — `/m/administration/permissions`)
- `DxComboBox` chọn vai trò → `DxTreeList` cây `HT_ChucNang` + **5 cột checkbox**: Xem · Thêm · Sửa · Xóa · In (KHÔNG có Duyệt).
- Tick cấp cha lan xuống con; cha một phần → **indeterminate**.
- **1 CTA "Lưu"** → ghi diff vào `HT_VaiTro_Quyen` → invalidate cache.
- Theo skill `icare247-admin-ui` (TreeList 70–80%, toolbar mỏng, phẳng).
- ⚠️ Thuộc tính `DxTreeList` (bind cột checkbox, tri-state) **tra qua reflection DLL trước khi code** (không đoán).

### 8.2 Vai trò / Người dùng (no-code — engine MasterData)
- **Vai trò:** khai báo `Ui_Form` cho `HT_VaiTro` → CRUD qua engine, route `/master/HT_VaiTro` (`db/047` seed
  `Sys_Table`+`Sys_Column`+`Ui_Form`+`Ui_Field`; `db/048` trỏ node menu `administration.roles` → form + `DoiTuong`).
- **Engine đã nâng:** MasterData **tự bơm audit** `CreatedBy/CreatedAt` (insert) + `UpdatedBy/UpdatedAt` (update)
  theo cột TỒN TẠI trên bảng đích (bảng `HT_*`/`DM_*` cần `CreatedBy NOT NULL`); userId luồn qua
  `SaveMasterDataCommand` ← claim sub. Không khai field audit trong form.
- **Người dùng (`HT_NguoiDung`):** KHÔNG dùng form generic (field nhạy cảm: MatKhauHash/2FA/lockout) → **bespoke** sau
  (đặt mật khẩu đúng cách, ẩn field hệ thống).

> **Nguyên tắc chung:** mặc định **Cách A (engine MasterData no-code)**; chỉ **bespoke** khi màn phải
> xử lý đặc thù engine không sinh được (vd ma trận TreeList × checkbox).

### 8.3 Quản lý cây menu / bật-tắt node (Web, gated dev/super-admin)
End user (admin tenant) chỉ **bật/tắt `KichHoat`** + thêm node custom (nếu được mở). Route/icon/liên kết
`Ui_Form` là của DEV (WPF master), ẩn với admin tenant.

---

## 9. Frontend

- `NavigationApiService` → gọi `/me/navigation`, `/me/permissions`.
- `AppState` giữ `NavTree` + `PermissionMap` (nạp 1 lần sau login, xóa khi logout).
- `NavMenu.razor`: thay `AppNav.Modules` → `AppState.NavTree`; bỏ `CanShow` (server đã lọc); thêm loading/empty.
- Màn nghiệp vụ: dùng `PermissionMap[Ma]` ẩn/hiện nút Thêm/Sửa/Xóa/In (khớp `FormPermission` engine sẵn có).
- `AppNav.cs`: nguồn **seed** `Sys_MenuCatalog` + **fallback dev** (API lỗi → menu tối thiểu).

---

## 10. Seed & migration (scripts ĐÃ VIẾT — chưa áp vào DB)

- **`db/042_alter_ht_chucnang_authz.sql`** (Data DB): thêm `Menu_Id, LaHeThong, KichHoat, ViTriHienThi` + index. Idempotent (`COL_LENGTH`).
- **`db/043_create_sys_menu_catalog.sql`** (Config DB): tạo `Sys_Menu` + `Sys_MenuCatalog`.
- **`db/044_seed_sys_menu_catalog.sql`** (Config DB): seed bộ `MAIN` + cây master từ `AppNav` (45 node; `Icon` Lucide; `Route`; `Display_Pos=Sidebar`).
- **`db/045_seed_ht_chucnang_base.sql`** (Data DB): seed `HT_ChucNang` base (`LaHeThong=1, KichHoat=1`), nối cha-con theo `Ma`; **grant `SUPERADMIN`** (vai trò sẵn ở `db/038`) `Xem/Thêm/Sửa/Xóa/In=1` toàn bộ (`Duyệt=0`). `CreatedBy=admin` tường minh.
- ⚠️ Thứ tự chạy: `037 → 038 → 042 → (043,044 trên Config DB) → 045`. `Menu_Id` trong Data DB bootstrap = 1 (MAIN); sync thật map chuẩn.
- Áp dụng theo pattern dự án (script trong repo, chạy tay) — như `db/040` audit.

---

## 11. Phân rã task (thứ tự đề xuất)

1. Migration: thêm cột `HT_ChucNang` + tạo `Sys_Menu`/`Sys_MenuCatalog` + seed base + grant ADMIN.
2. Backend: `GetMyNavigationQuery` + `MeController` + `NavigationRepository` (recursive CTE) + cache.
3. Frontend: `NavigationApiService` + `AppState` + `NavMenu` đọc tree (sidebar). `AppNav`→fallback.
4. `GetMyPermissionsQuery` + ẩn nút theo quyền trong màn + render sub-nav `TrongMan`.
5. **Enforce server** `[RequirePermission]` trên controller engine (bảo mật — trước production).
6. Màn Phân quyền (bespoke TreeList) + Vai trò/User qua MasterData.
7. (Pha sau) `ChucNangCon` quyền cấp nút · `Sys_Menu` nhiều bộ menu · `Duyet` cho workflow.

> Bước 1–3 đủ để menu "thật sự theo quyền". Bước 5 bắt buộc trước production.

---

## 12. Hoãn / pha sau (đã thống nhất)
- `ChucNangCon` (quyền cấp nút trong màn) — phân tích chi tiết sau.
- `Sys_Menu` nhiều bộ menu (Top/Mobile/Context) — schema chừa sẵn, dùng sau.
- `Duyet` — thuộc **workflow engine**, không nằm trong phân quyền.
