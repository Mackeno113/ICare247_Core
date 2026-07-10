# Database Design — ICare247

> Quy tắc **thiết kế bảng/quan hệ/index**. Cách *truy vấn* xem `dapper-patterns.md`.
> Nguồn chuẩn: ADR-018/019/020/022 (`.claude/memory/architecture_decisions.md`),
> `docs/spec/02_DATABASE_SCHEMA.md` (Config DB), `docs/spec/11_DATA_DB_SCHEMA.md` (Data DB).

## Quy tắc cứng

1. **ĐỌC DDL LIVE TRƯỚC** khi thiết kế trên bảng đã có — DB thật lệch file `db/*.sql` cũ. Không đoán schema.
2. **HỎI TRƯỚC** khi thêm bảng/cột — không tự suy diễn tên bảng, tên cột, hay kiểu dữ liệu.
3. Xác định đang làm **Config DB hay Data DB** — hai bộ convention KHÁC NHAU (bảng dưới).
4. Mọi bảng Data DB có đủ **khối cột auto** (§2). Bảng Config DB có `Is_Active` + `Created_At`/`Updated_At`.
5. **KHÔNG xóa vật lý** — soft delete.
6. **KHÔNG có FK nào từ Data DB trỏ sang Config DB**, và không JOIN cross-DB (ghép trong RAM ở tầng app).
7. INSERT/seed **set `CreatedBy`/`CreatedAt` tường minh**, không dựa DEFAULT của DB.
8. Sau khi đổi schema → rà tác động **cache/version** (`Sys_Cache_Invalidation`, `Sys_Version`).

## 1. Hai database — đừng trộn convention

| | **Config DB** (`ICare247_Config`) | **Data DB** (`ICare247_Solution`, per-tenant) |
|---|---|---|
| Chứa gì | Metadata Engine (IP nền tảng) | Data nghiệp vụ + người dùng của tenant |
| Tiền tố bảng | Tiếng Anh: `Sys_` `Ui_` `Val_` `Gram_` `Evt_` | Tiếng Việt theo module (§3) |
| Tên cột | Tiếng Anh, snake: `Form_Code`, `Is_Active` | Nghiệp vụ tiếng Việt (`Ma`, `Ten`) + auto tiếng Anh |
| PK | `{Table}_Id` int IDENTITY | **Luôn `Id`** bigint IDENTITY |
| Soft delete | `Is_Active = 0` | `IsDeleted = 1` |
| Timestamp | `Created_At` / `Updated_At` (datetime) | Khối cột auto §2 (datetime2) |
| Tenant | **KHÔNG có cột `Tenant_Id`** (ADR-035 — đang gỡ, xem §1.1) | **KHÔNG có cột `Tenant_Id`** — cả DB đã là 1 tenant (DB-per-tenant, ADR-018) |
| Text hiển thị | Lưu **resource key** (`_Key`) → tra `Sys_Resource` | Lưu `Ten` trực tiếp |

> Data DB không tự mô tả label: trạng thái lưu dạng **chuỗi code** (`TrangThai='HoatDong'`), resolve
> label ở tầng app (C# enum + i18n shell, hoặc `ConfigCache` cho màn no-code). Đây là **chủ đích** —
> khách cầm/backup Data DB không thấy cấu hình.

### 1.1 `Tenant_Id` — KHÔNG dùng ở bất kỳ bảng nào (ADR-035)

**Đích: 0 bảng có cột `Tenant_Id`, ở cả hai DB.** Lý do: ADR-018 cho mỗi tenant **1 Config DB riêng**
+ 1 Data DB riêng, cô lập vật lý → cột định danh tenant *bên trong* một DB đã-thuộc-về-1-tenant không
phân biệt được gì. Vai trò "bản ghi chuẩn từ master vs. tenant tự tùy biến" mà `Sys_Table.Tenant_Id`
từng gánh nay do ConfigSync đảm nhiệm tường minh bằng **`Is_System` / `Is_Customized` / `Source_Ver`**
(db/050). Giữ cả hai = hai nguồn sự thật cho cùng một câu hỏi.

- **KHÔNG thêm `Tenant_Id`** vào bảng mới, ở bất kỳ DB nào.
- **KHÔNG viết `WHERE Tenant_Id = @TenantId`** trong SQL mới.
- Bản ghi master vs. tùy biến → dùng `Is_System` / `Is_Customized`.

> ⚠️ **`TenantId` ở tầng runtime thì GIỮ.** Bỏ *cột*, không bỏ *khái niệm*: `ITenantContext.TenantId`
> vẫn cần để (a) `ITenantConnectionResolver` chọn connection string, và (b) làm thành phần cache key
> (Redis L2 **dùng chung** giữa các tenant — xem `caching.md`). Đừng gỡ tham số `tenantId` khỏi
> repository/CacheKeys.

**Nợ kỹ thuật — 9 bảng còn cột** (khảo sát DB live 2026-07-10; `db/*.sql` KHÔNG phản ánh đủ):

| Nhóm | Bảng | Hành xử hiện tại | Độ khó gỡ |
|---|---|---|---|
| 1. Global/override | `Sys_Table`, `Sys_Lookup`, `Ui_View` | nullable; `= @TenantId OR IS NULL`; `Ui_View` có `ROW_NUMBER` khử trùng `View_Code` | Cao — có ngữ nghĩa thật |
| 2. Cột chết | `Sys_Config`, `Sys_Role` | nullable; **không repo nào lọc** | Thấp |
| 3. Lọc cứng | `Doc_Template`, `Doc_Proc_Registry` | **NOT NULL**, không default; `WHERE Tenant_Id=@tenantId`, không fallback global; ConfigStudio ghi cột này | Trung — phải sửa cả đường ghi |
| 4. Master menu | `Sys_Menu`, `Sys_MenuCatalog` | Bảng **MASTER** (db/043, ADR-023 + spec 15): DEV định nghĩa qua WPF → sync xuống `HT_ChucNang` mỗi tenant. Chưa có code sync nên 0 tham chiếu C#. Dữ liệu hiện **toàn global** (1 + 45 dòng, 0 override) | Thấp — cơ chế override chưa dùng |

`Sys_Tenant` (cột là PK identity) cũng vestigial: resolver đọc `dbo.Tenant` ở **Catalog DB**, không đọc `Sys_Tenant`.

- Khi còn cột: global = **`Tenant_Id IS NULL`**, KHÔNG phải `= 0` (db/009 đã đổi; sentinel `0` là bug).
- `DocTemplateRenderer` có `NguonKey == "Tenant_Id"` — đó là **tên tham số binding** truyền `tenantId`
  runtime vào proc, KHÔNG phải tham chiếu cột. Giữ nguyên khi gỡ.

## 2. Khối cột auto — MỌI bảng Data DB

```sql
Id          bigint     PK IDENTITY
CreatedBy   bigint     NOT NULL                              -- HT_NguoiDung.Id, set tường minh
CreatedAt   datetime2  NOT NULL DEFAULT sysutcdatetime()
UpdatedBy   bigint     NULL
UpdatedAt   datetime2  NULL
IsDeleted   bit        NOT NULL DEFAULT 0
Ver         int        NOT NULL DEFAULT 0                    -- optimistic lock, tăng mỗi update
```

`Ma`/`Ten`/`MoTa` **KHÔNG thuộc khối auto** — chúng là cột nghiệp vụ theo archetype (§4).

**Guard tự vá:** `db/061_ensure_audit_columns.sql` (idempotent, quét theo prefix module, có whitelist opt-out).
Trong ConfigStudio: màn `Sys_Table` → nút **"Kiểm tra cột chuẩn"** để ALTER trực tiếp lên Target DB.

## 3. Tiền tố bảng Data DB (ADR-022 — theo module nghiệp vụ)

| Prefix | Nhóm | | Prefix | Nhóm |
|---|---|---|---|---|
| `HT_` | Hệ Thống (user + phân quyền) | | `TM_` | Thương Mại (gộp hàng hóa/mua/bán/kho) |
| `TC_` | Tổ Chức (công ty, phòng ban) | | `CN_` | Công Nợ |
| `DM_` | Danh Mục dùng chung | | `BC_` | Báo Cáo |
| `NS_` | Nhân Sự | | `NK_` | Nhật Ký (audit-log) |
| `TL_` | Tiền Lương | | `TT_` | Tệp Tin |

Tên bảng = prefix + **tiếng Việt không dấu PascalCase**: `TM_PhieuNhapKho`, `HT_NguoiDung`, `DM_TinhThanhPho`.

> `DM_` chỉ cho danh mục **lớn/biến động theo tenant**. Danh mục nhỏ cố định (Gender, trạng thái…)
> vẫn ở `Sys_Lookup` (Config DB).

## 4. `Ma` / `Ten` theo archetype — không ép cứng mọi bảng

| Archetype | `Ma` | `Ten` |
|---|---|---|
| Danh mục / Master (`DM_*`, `TC_CongTy`, `HT_VaiTro`, `HT_ChucNang`) | ✅ UNIQUE | ✅ |
| Con người (`HT_NguoiDung`, `NS_NhanVien`) | ✅ UNIQUE | ⚠️ dùng `HoTen`/`TenDangNhap` |
| Chứng từ / Header (`GD_*`) | ✅ = số chứng từ | ❌ |
| Chi tiết / Line | ❌ | ❌ |
| Map N-N (`HT_NguoiDung_VaiTro`) | ❌ | ❌ |
| Kỹ thuật / Log (`HT_RefreshToken`, `NK_*`) | ❌ | ❌ |

`Ma` khi có: `nvarchar(50)`, **filtered UNIQUE** `WHERE IsDeleted = 0`.

## 5. Đặt tên cột danh tính vs. FK

| Loại | Quy ước | Vì sao |
|---|---|---|
| Cột danh tính của **chính bảng** (`Ma`, `Ten`, `MoTa`) | **Generic, KHÔNG entity-suffix** | Chỉ 1/bảng → engine generic (`Ui_Field_Lookup`, FormRunner, lưới) giả định `SELECT Id, Ma, Ten` đồng nhất |
| **FK / tham chiếu** | **`{Bang}_Id`**, thêm tiền tố vai trò nếu nhiều | Một bảng có thể nhiều FK → phải phân biệt |

```sql
-- ✅ Đúng
Ma, Ten                                  -- trong DM_DonViTinh
TinhThanhPho_Id, CongTy_Cha_Id, NoiSinh_PhuongXa_Id

-- ❌ Sai
MaDonViTinh, TenTinhThanhPho             -- entity-suffix ở cột danh tính → vỡ engine generic
TinhThanhPhoID, IdTinhThanh              -- sai dạng {Bang}_Id
```

> Va chạm tên lúc JOIN → xử lý bằng **alias** (`tt.Ten AS TenTinhThanhPho`), không entity-suffix ở cấp lưu trữ.

## 6. Quan hệ & xóa

- FK khai báo thật trong DB, **đồng thời** đăng ký ở registry `Sys_Relation`
  (`Detail_FK_Column`, `Master_Key_Column`, `On_Delete`, `Relation_Code`).
- **Soft-check FK khi xóa = đọc `Sys_Relation`**, KHÔNG đoán theo tên cột — mới xử lý đúng
  trường hợp nhiều FK cùng trỏ về một bảng. (`ReferenceCheckService` còn fallback name-match
  cho bảng chưa khai — giai đoạn chuyển tiếp, không dựa vào.)
- Bảng cây: `Cha_Id` self-ref; gốc = `Cha_Id IS NULL OR Cha_Id = 0`. Sắp xếp dùng
  `ThuTu` (input) + `ThuTuCay`/`DuongDanCay` (dẫn xuất, recompute-on-write) — ADR-027.

## 7. Index

- Index mọi cột dùng ở `WHERE` / `JOIN` — đặc biệt **mọi cột FK**.
- Naming: `IX_{Bang}_{Ý nghĩa}` → `IX_DM_PhuongXa_Tinh (TinhThanhPho_Id)`, `IX_TC_CongTy_Cha (CongTy_Cha_Id)`.
- UNIQUE trên bảng soft-delete phải là **filtered index**: `WHERE IsDeleted = 0`.
- Query chậm / cần rewrite → gọi agent `sql-server-optimizer`.

## 8. Audit-log (ADR-020)

- Diff bắt ở **tầng Application** (handler CRUD generic), **KHÔNG dùng trigger** → chỉ thao tác qua UI mới
  ghi log, SQL tay vào DB bị bỏ qua (chủ đích).
- Lưu ở `NK_ThayDoi`: header + `ChiTiet` JSON `[{Cot,Cu,Moi}]` (truy vấn bằng `OPENJSON`).
- Bật/tắt theo **bảng + màn hình**: `Sys_Table.Audit_Enabled` + `Ui_Form.Audit_Enabled` (màn hình đè bảng).

## Cấm tuyệt đối

```sql
-- ❌ Cột Tenant_Id trong BẤT KỲ bảng nào, cả Config lẫn Data DB (ADR-035)
ALTER TABLE DM_QuocGia ADD Tenant_Id int;
ALTER TABLE Ui_Form   ADD Tenant_Id int;   -- master/tùy-biến → Is_System/Is_Customized

-- ❌ Sentinel 0 cho global (db/009 đã đổi sang NULL) — NULL = 0 là UNKNOWN, mất sạch dòng global
WHERE (Tenant_Id = @TenantId OR Tenant_Id = 0)   -- → OR Tenant_Id IS NULL

-- ❌ FK từ Data DB sang Config DB
ALTER TABLE HT_NguoiDung ADD CONSTRAINT FK_TrangThai FOREIGN KEY (TrangThai_Id) REFERENCES Sys_Lookup(Id);

-- ❌ Xóa vật lý
DELETE FROM TC_CongTy WHERE Id = @id;          -- → UPDATE ... SET IsDeleted = 1

-- ❌ UNIQUE không filtered trên bảng soft-delete (xóa rồi vẫn chiếm mã)
CONSTRAINT UQ_Ma UNIQUE (Ma);                   -- → UNIQUE (Ma) WHERE IsDeleted = 0

-- ❌ Dựa DEFAULT cho audit
INSERT INTO DM_QuocGia (Ma, Ten) VALUES (N'VN', N'Việt Nam');   -- thiếu CreatedBy/CreatedAt

-- ❌ Trộn convention hai DB
CREATE TABLE DM_DonViTinh (DonViTinh_Id int PK, Is_Active bit);  -- → Id bigint, IsDeleted bit
```
