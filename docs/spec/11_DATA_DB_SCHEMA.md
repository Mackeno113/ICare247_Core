# 11 — Data DB Schema (Đợt 1: Nền tảng `HT_` / `DM_` / `TC_`)

> **Phạm vi:** Database **dữ liệu vận hành per-tenant** (`ICare247_Data` của từng tenant), KHÁC
> với Config DB (`Sys_/Ui_/Val_/Gram_/Evt_` — metadata Engine, xem `02_DATABASE_SCHEMA.md`).
> **Đợt này** chỉ thiết kế nhóm **nền tảng** làm móng cho mọi module nghiệp vụ:
> `HT_` (Hệ Thống — người dùng + phân quyền), `DM_` (Danh Mục dùng chung), `TC_` (Tổ Chức).
>
> **Trạng thái:** 🟡 SPEC ĐỀ XUẤT — chờ duyệt, **chưa sinh SQL migration**.

---

## 0. Convention áp dụng (ADR-018/019/022)

| Quy tắc | Chi tiết |
|---|---|
| **Mô hình** | **Database-per-tenant** → các bảng Data DB **KHÔNG có cột `Tenant_Id`** (cả DB đã là 1 tenant). |
| **Tên bảng** | Tiền tố nhóm theo module + **tiếng Việt không dấu PascalCase**. VD: `TC_CongTy`, `HT_NguoiDung`. |
| **Cột nghiệp vụ** | Tiếng Việt không dấu: `Ma`, `Ten`, `DiaChi`, `DienThoai`... |
| **Cột hệ thống/auto** | Tiếng Anh (xem §0.1). |
| **PK** | Luôn là `Id` BIGINT IDENTITY → tốt cho engine generic + Dapper `splitOn` mặc định. |
| **FK** | `{Bang}_Id` (ngữ nghĩa, có thể tiền tố vai trò: `NoiSinh_PhuongXa_Id`). |
| **Soft delete** | `IsDeleted = 1` (không xóa vật lý). Soft-check FK khi xóa qua registry `Sys_Relation`. |
| **Concurrency** | `Ver` INT — optimistic lock (tăng mỗi lần update). |
| **i18n** | Danh mục hệ thống dùng chung resolve label qua `Sys_Resource` nếu cần; danh mục nghiệp vụ lưu `Ten` trực tiếp. |

### 0.1 Khối cột auto — áp cho MỌI bảng

Mọi bảng dưới đây **đều có** khối cột auto sau (không liệt kê lại ở từng bảng để gọn). Đây là các cột
**thật sự universal** — KHÔNG bao gồm `Ma`/`Ten` (xem §0.2). Phần "Cột nghiệp vụ" ở mỗi bảng là phần
**bổ sung** trên khối này.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Id | bigint | PK IDENTITY | Khóa chính đồng nhất |
| CreatedBy | bigint | NOT NULL | `HT_NguoiDung.Id` người tạo |
| CreatedAt | datetime2 | NOT NULL DEFAULT sysutcdatetime() | |
| UpdatedBy | bigint | NULL | Người sửa gần nhất |
| UpdatedAt | datetime2 | NULL | |
| IsDeleted | bit | NOT NULL DEFAULT 0 | Soft delete |
| Ver | int | NOT NULL DEFAULT 0 | Optimistic concurrency |

### 0.2 `Ma` / `Ten` — cột theo archetype (KHÔNG phải auto-column)

`Ma`, `Ten`, `MoTa` là **cột nghiệp vụ theo loại bảng**, không ép cứng mọi bảng. Áp theo bảng sau:

| Archetype | `Ma` | `Ten` | Ghi chú |
|---|---|---|---|
| Danh mục / Master (DM_*, TC_CongTy, TC_PhongBan, TC_Cap*, HT_VaiTro, HT_ChucNang) | ✅ UNIQUE | ✅ | Tra cứu/nhập theo mã + tên |
| Con người (HT_NguoiDung, NS_NhanVien sau này) | ✅ UNIQUE | ⚠️ dùng `HoTen`/`TenDangNhap` | Không có `Ten` chung chung |
| Chứng từ / Header (GD_* sau này) | ✅ = số chứng từ | ❌ | Có Số + Ngày + đối tượng, không có "tên" |
| Chi tiết / Line (*_Line sau này) | ❌ | ❌ | Chỉ `RowNo` + FK header + mặt hàng |
| Map N-N (HT_NguoiDung_VaiTro, HT_VaiTro_Quyen, HT_NguoiDung_CongTy) | ❌ | ❌ | Chỉ cặp FK |
| Kỹ thuật / Log (HT_RefreshToken, NK_* sau này) | ❌ | ❌ | Không là đối tượng nghiệp vụ tra mã |

> `Ma` khi có: `nvarchar(50)`, **filtered UNIQUE** `WHERE IsDeleted = 0` (phạm vi cả DB vì đã 1 tenant).

**Quy ước đặt tên cột danh tính (quan trọng cho engine generic):**

| Loại cột | Quy ước | Vì sao |
|---|---|---|
| Cột danh tính của chính bảng (`Ma`, `Ten`, `MoTa`) | **Generic, KHÔNG entity-suffix** | Chỉ 1/bảng → `Ui_Field_Lookup`/FormRunner/lưới giả định `SELECT Id, Ma, Ten` đồng nhất. KHÔNG đặt `MaDonViTinh`, `TenTinhThanhPho`. |
| Cột FK / tham chiếu | **Entity-suffix `{Bang}_Id`** (+ tiền tố vai trò nếu nhiều) | Một bảng có thể nhiều FK → phải phân biệt: `TinhThanhPho_Id`, `CongTy_Cha_Id`, `NoiSinh_PhuongXa_Id`. |

> Va chạm tên lúc JOIN nhiều bảng → xử lý bằng **alias** (`tt.Ten AS TenTinhThanhPho`), không entity-suffix ở cấp lưu trữ.

---

## 1. Nhóm `DM_` — Danh Mục dùng chung

> Danh mục **editable, dùng nhiều nơi**. Danh mục nhỏ cố định (Gender, Trạng thái...) vẫn nằm
> ở `Sys_Lookup` (Config DB). `DM_` chỉ cho danh mục lớn/biến động theo tenant.

### DM_QuocGia
> Quốc gia (mã ISO).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(10) | UNIQUE NOT NULL | ISO alpha-2/3: 'VN', 'US' |
| Ten | nvarchar(150) | NOT NULL | Tên quốc gia |
| MaDienThoai | nvarchar(10) | NULL | Mã vùng điện thoại: '+84' |

### DM_TinhThanhPho
> **Cấp 1** hành chính (mô hình VN 2025 — 2 cấp). Bỏ cấp Quận/Huyện.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | Mã tỉnh/thành |
| Ten | nvarchar(150) | NOT NULL | Tên |
| LoaiHinh | nvarchar(20) | NULL | 'Tinh' / 'ThanhPhoTW' |
| QuocGia_Id | bigint | NULL FK→DM_QuocGia | Mặc định VN |

**Indexes:** `IX_DM_TinhThanhPho_Ten (Ten)`

### DM_PhuongXa
> **Cấp 2** hành chính — Phường/Xã/Thị trấn, **trực thuộc thẳng Tỉnh/Thành** (không qua Huyện).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | Mã phường/xã |
| Ten | nvarchar(150) | NOT NULL | Tên |
| LoaiHinh | nvarchar(20) | NULL | 'Phuong' / 'Xa' / 'ThiTran' |
| TinhThanhPho_Id | bigint | NOT NULL FK→DM_TinhThanhPho | Cấp cha |

**Indexes:** `IX_DM_PhuongXa_Tinh (TinhThanhPho_Id)`

> **Seed:** dữ liệu hành chính VN — HOÃN, seed sau khi có nguồn chuẩn (đợt này chỉ dựng cấu trúc).

### DM_DonViTinh
> Đơn vị tính (kg, cái, thùng, m²...).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | 'KG', 'CAI', 'THUNG' |
| Ten | nvarchar(100) | NOT NULL | Tên hiển thị |
| GhiChu | nvarchar(255) | NULL | |

### DM_NganHang
> Ngân hàng (phục vụ tài khoản công ty / đối tác sau này).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | Mã NH (VCB, BIDV...) |
| Ten | nvarchar(200) | NOT NULL | Tên đầy đủ |
| TenVietTat | nvarchar(50) | NULL | |

---

## 2. Nhóm `TC_` — Tổ Chức

> **Hai cây tự tham chiếu** (self-reference tree): cây **Công ty** và cây **Phòng ban**.
> Mỗi nút có nút cha + thuộc một **cấp** (phân loại, lấy từ danh mục cấp riêng).

### TC_CapCongTy
> Danh mục **cấp công ty** (vd: Tổng công ty / Công ty / Chi nhánh / VPĐD). Editable theo tenant.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | 'TONGCT', 'CT', 'CN', 'VPDD' |
| Ten | nvarchar(100) | NOT NULL | Tên cấp |
| ThuTu | int | NOT NULL DEFAULT 0 | Thứ tự/độ sâu gợi ý |

### TC_CapPhongBan
> Danh mục **cấp phòng ban** (vd: Khối / Phòng / Tổ / Nhóm).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(20) | UNIQUE NOT NULL | 'KHOI', 'PHONG', 'TO', 'NHOM' |
| Ten | nvarchar(100) | NOT NULL | Tên cấp |
| ThuTu | int | NOT NULL DEFAULT 0 | |

### TC_CongTy  🌳 *(cây 1)*
> Cây công ty đa cấp. `CongTy_Cha_Id` = NULL → gốc (tập đoàn/tổng công ty).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(50) | UNIQUE NOT NULL | Mã công ty |
| Ten | nvarchar(300) | NOT NULL | Tên đầy đủ |
| TenVietTat | nvarchar(100) | NULL | Tên viết tắt |
| CongTy_Cha_Id | bigint | NULL FK→TC_CongTy (self) | NULL = gốc |
| CapCongTy_Id | bigint | NOT NULL FK→TC_CapCongTy | Cấp công ty |
| MaSoThue | nvarchar(20) | NULL | |
| DiaChi | nvarchar(500) | NULL | Số nhà, đường |
| PhuongXa_Id | bigint | NULL FK→DM_PhuongXa | Tỉnh/Thành suy ra qua `DM_PhuongXa.TinhThanhPho_Id` (không lưu trùng) |
| DienThoai | nvarchar(50) | NULL | |
| Email | nvarchar(150) | NULL | |
| Website | nvarchar(200) | NULL | |
| NguoiDaiDien | nvarchar(200) | NULL | Người đại diện pháp luật |
| GiamDoc | nvarchar(200) | NULL | |
| KeToanTruong | nvarchar(200) | NULL | |
| NganHang_Id | bigint | NULL FK→DM_NganHang | |
| SoTaiKhoan | nvarchar(50) | NULL | |
| Logo_Id | bigint | NULL | → `TT_TepDinhKem` (nhóm TT_, đợt sau) |
| TrangThai | nvarchar(20) | NOT NULL DEFAULT 'HoatDong' | Lookup `TRANGTHAI_DONVI` |

**Indexes:** `IX_TC_CongTy_Cha (CongTy_Cha_Id)`, `IX_TC_CongTy_Ten (Ten)`

### TC_PhongBan  🌳 *(cây 2)*
> Cây phòng ban đa cấp, mỗi phòng ban thuộc 1 công ty. `PhongBan_Cha_Id` = NULL → gốc trong công ty.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(50) | UNIQUE NOT NULL | Mã phòng ban |
| Ten | nvarchar(300) | NOT NULL | Tên |
| TenVietTat | nvarchar(100) | NULL | |
| PhongBan_Cha_Id | bigint | NULL FK→TC_PhongBan (self) | NULL = gốc |
| CongTy_Id | bigint | NOT NULL FK→TC_CongTy | Thuộc công ty nào |
| CapPhongBan_Id | bigint | NOT NULL FK→TC_CapPhongBan | Cấp phòng ban |
| TruongDonVi_Id | bigint | NULL FK→HT_NguoiDung | Trưởng phòng/đơn vị |
| DienThoai | nvarchar(50) | NULL | |
| Email | nvarchar(150) | NULL | |
| ThuTu | int | NOT NULL DEFAULT 0 | Thứ tự hiển thị |
| TrangThai | nvarchar(20) | NOT NULL DEFAULT 'HoatDong' | Lookup `TRANGTHAI_DONVI` |

**Indexes:** `IX_TC_PhongBan_Cha (PhongBan_Cha_Id)`, `IX_TC_PhongBan_CongTy (CongTy_Id)`

> **Ghi chú:** Chức vụ (`TC_ChucVu`/positions) thuộc nghiệp vụ nhân sự → đẩy sang module **`NS_`** (Hr) đợt sau.

---

## 3. Nhóm `HT_` — Hệ Thống (Identity + Phân quyền)

> **Toàn bộ phân quyền ở Data DB** (quyết định đã chốt). `Sys_Role/Sys_Permission` (Config DB)
> chỉ phục vụ quyền cấu hình Engine (Form/Field), KHÔNG dùng cho phân quyền nghiệp vụ end-user.

### HT_NguoiDung
> Tài khoản đăng nhập end-user. **Nằm ở Data DB** → login phụ thuộc tenant (suy từ subdomain).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(50) | UNIQUE NOT NULL | Mã người dùng / mã NV |
| TenDangNhap | nvarchar(100) | UNIQUE NOT NULL | Username đăng nhập |
| LoaiTaiKhoan | nvarchar(20) | NOT NULL DEFAULT 'Local' | Lookup `LOAI_TAIKHOAN`: 'Local'/'AD'/'SSO'/'Portal' (Item_Code bất biến) |
| MatKhauHash | nvarchar(256) | NULL | Hash PBKDF2 (`PasswordHasher<T>`, gồm salt+version). **NULL khi AD/SSO** (xác thực ngoài) |
| NhanVien_Id | bigint | NULL* | → `NS_NhanVien` — **mỗi tài khoản gắn đúng 1 nhân viên**; `HoTen`/`Email`/`DienThoai`/ảnh lấy qua đây (KHÔNG lưu trùng ở tài khoản). `*` nullable tạm ở đợt nền tảng → siết **NOT NULL + FK + UNIQUE** ở đợt `NS_` (trừ tài khoản hệ thống bootstrap, xem §6.7) |
| CongTyMacDinh_Id | bigint | NULL FK→TC_CongTy | Công ty mặc định khi đăng nhập |
| PhongBan_Id | bigint | NULL FK→TC_PhongBan | Phòng ban trực thuộc (có thể suy từ NhanVien) |
| TrangThai | nvarchar(20) | NOT NULL DEFAULT 'HoatDong' | Lookup `TRANGTHAI_NGUOIDUNG`: HoatDong/TamKhoa/NgungHoatDong |
| LaQuanTri | bit | NOT NULL DEFAULT 0 | Super-admin tenant (bỏ qua check quyền) |
| KichHoatMobile | bit | NOT NULL DEFAULT 0 | Cho phép đăng nhập app mobile |
| HetHanTaiKhoan | datetime2 | NULL | Hạn dùng tài khoản (tạm/CTV); quá hạn → vô hiệu |
| HinhThuc2FA | nvarchar(20) | NOT NULL DEFAULT 'None' | Lookup `HINHTHUC_2FA`: None/App/Email/SMS |
| Khoa2FA | nvarchar(500) | NULL | Secret TOTP (mã hóa) khi `HinhThuc2FA='App'` |
| LanDangNhapCuoi | datetime2 | NULL | |
| LanDangXuatCuoi | datetime2 | NULL | |
| SoLanDangNhapSai | int | NOT NULL DEFAULT 0 | Đếm để khóa tạm |
| KhoaDenKhi | datetime2 | NULL | Lockout đến thời điểm |
| DoiMatKhauLanSau | bit | NOT NULL DEFAULT 0 | Bắt đổi mật khẩu lần đăng nhập kế |

**Indexes:** `IX_HT_NguoiDung_Email (Email)`

### HT_VaiTro
> Vai trò (role) — gom nhóm quyền, gán cho người dùng.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(50) | UNIQUE NOT NULL | Mã vai trò |
| Ten | nvarchar(200) | NOT NULL | Tên hiển thị |
| MoTa | nvarchar(500) | NULL | |
| LaHeThong | bit | NOT NULL DEFAULT 0 | Vai trò hệ thống — không cho xóa/sửa mã |

### HT_NguoiDung_VaiTro  *(map N-N)*
> Gán vai trò cho người dùng. Không có `Ma`.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| NguoiDung_Id | bigint | NOT NULL FK→HT_NguoiDung | |
| VaiTro_Id | bigint | NOT NULL FK→HT_VaiTro | |

**Constraints:** UNIQUE `(NguoiDung_Id, VaiTro_Id)` WHERE `IsDeleted = 0`

### HT_ChucNang
> Cây chức năng / menu — đối tượng để phân quyền. Tự tham chiếu (menu cha-con).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Ma | nvarchar(100) | UNIQUE NOT NULL | Mã chức năng (vd 'HR.NhanVien') |
| Ten | nvarchar(200) | NOT NULL | Tên hiển thị (hoặc resource key) |
| ChucNang_Cha_Id | bigint | NULL FK→HT_ChucNang (self) | NULL = gốc menu |
| Loai | nvarchar(20) | NOT NULL DEFAULT 'Menu' | 'Menu' / 'ManHinh' / 'ChucNangCon' |
| Module | nvarchar(20) | NULL | Tiền tố module: 'NS','TM','CN'... |
| DuongDan | nvarchar(300) | NULL | Route Blazor / mã form |
| Icon | nvarchar(100) | NULL | |
| ThuTu | int | NOT NULL DEFAULT 0 | Thứ tự trong menu |

**Indexes:** `IX_HT_ChucNang_Cha (ChucNang_Cha_Id)`

### HT_VaiTro_Quyen  *(map vai trò × chức năng + cờ thao tác)*
> Phân quyền chi tiết: mỗi vai trò trên mỗi chức năng có các cờ CRUD + duyệt. Không có `Ma`.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| VaiTro_Id | bigint | NOT NULL FK→HT_VaiTro | |
| ChucNang_Id | bigint | NOT NULL FK→HT_ChucNang | |
| Xem | bit | NOT NULL DEFAULT 0 | |
| Them | bit | NOT NULL DEFAULT 0 | |
| Sua | bit | NOT NULL DEFAULT 0 | |
| Xoa | bit | NOT NULL DEFAULT 0 | |
| Duyet | bit | NOT NULL DEFAULT 0 | Quyền duyệt/xác nhận chứng từ |
| InAn | bit | NOT NULL DEFAULT 0 | Quyền in/xuất |

**Constraints:** UNIQUE `(VaiTro_Id, ChucNang_Id)` WHERE `IsDeleted = 0`

### HT_NguoiDung_CongTy  *(phạm vi dữ liệu — company switcher)*
> Người dùng được phép truy cập những công ty nào (đa công ty). Hỗ trợ switcher công ty ở UI.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| NguoiDung_Id | bigint | NOT NULL FK→HT_NguoiDung | |
| CongTy_Id | bigint | NOT NULL FK→TC_CongTy | |
| LaMacDinh | bit | NOT NULL DEFAULT 0 | Công ty mặc định khi đăng nhập |

**Constraints:** UNIQUE `(NguoiDung_Id, CongTy_Id)` WHERE `IsDeleted = 0`

### HT_RefreshToken  *(phiên đăng nhập / refresh JWT)*
> Lưu refresh token để cấp lại access token (JWT access stateless). Cần cho scale-out + đăng xuất.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| NguoiDung_Id | bigint | NOT NULL FK→HT_NguoiDung | |
| TokenHash | nvarchar(200) | NOT NULL | Hash của refresh token (không lưu plaintext) |
| HetHan | datetime2 | NOT NULL | Thời điểm hết hạn |
| DaThuHoi | bit | NOT NULL DEFAULT 0 | Revoked (đăng xuất / xoay token) |
| ThuHoiLuc | datetime2 | NULL | |
| DiaChiIp | nvarchar(50) | NULL | |
| ThietBi | nvarchar(300) | NULL | User-Agent |

**Indexes:** `IX_HT_RefreshToken_NguoiDung (NguoiDung_Id)`, `IX_HT_RefreshToken_Hash (TokenHash)`
> Bảng này KHÔNG có `Ma`; có thể bỏ `IsDeleted` (dùng `DaThuHoi`) — giữ khối chuẩn cho đồng nhất.

---

## 4. Sơ đồ quan hệ (tóm tắt)

```
DM_QuocGia ──< DM_TinhThanhPho ──< DM_PhuongXa
                     │                  │
TC_CapCongTy ──< TC_CongTy >── PhuongXa / DM_NganHang   (Tỉnh suy qua PhuongXa)
                  │  └─(self) CongTy_Cha
                  │
TC_CapPhongBan ─< TC_PhongBan >── CongTy_Id
                     └─(self) PhongBan_Cha   └── TruongDonVi ──> HT_NguoiDung

HT_NguoiDung ──< HT_NguoiDung_VaiTro >── HT_VaiTro ──< HT_VaiTro_Quyen >── HT_ChucNang (self tree)
     ├──< HT_NguoiDung_CongTy >── TC_CongTy
     ├──< HT_RefreshToken
     ├── NhanVien ──> NS_NhanVien  (đợt NS_; bắt buộc — HoTen/Email/ĐT/ảnh lấy qua đây)
     ├── CongTyMacDinh ──> TC_CongTy
     └── PhongBan ──> TC_PhongBan
```

---

## 5. Thống kê đợt 1

| Nhóm | Số bảng | Bảng |
|---|---|---|
| `DM_` | 5 | QuocGia, TinhThanhPho, PhuongXa, DonViTinh, NganHang |
| `TC_` | 4 | CapCongTy, CapPhongBan, CongTy, PhongBan |
| `HT_` | 7 | NguoiDung, VaiTro, NguoiDung_VaiTro, ChucNang, VaiTro_Quyen, NguoiDung_CongTy, RefreshToken |
| **Tổng** | **16** | |

---

## 6. Điểm cần chốt thêm trước khi sinh SQL

1. ✅ **CHỐT — `TrangThai` dùng `Sys_Lookup` (Config DB).** Mỗi miền trạng thái = 1 `Lookup_Code`
   (`TRANGTHAI_NGUOIDUNG`, `TRANGTHAI_DONVI`, `TRANGTHAI_NHANVIEN`, `TRANGTHAI_XETDUYET`,
   `LOAI_TAIKHOAN`, `HINHTHUC_2FA`...).
   - **Danh sách trạng thái** (registry) lưu ở **Config DB / `Sys_Lookup`**; **Data DB chỉ lưu chuỗi `Item_Code`**
     đã chọn trong cột nghiệp vụ (vd `HT_NguoiDung.TrangThai = 'HoatDong'`).
   - Resolve label dropdown/badge qua `ConfigCache` (HybridCache L1+L2) → **không JOIN cross-DB**, map ở tầng app.
   - ⚠️ **`Item_Code` của trạng thái hệ thống là HẰNG SỐ bất biến** (code logic phụ thuộc, vd chặn login khi
     `TrangThai='TamKhoa'`): seed global (`Tenant_Id=NULL`), tenant chỉ sửa **label** (Sys_Resource),
     KHÔNG được đổi/xóa `Item_Code`.
2. ✅ **CHỐT — Hash mật khẩu = PBKDF2** qua `PasswordHasher<HT_NguoiDung>` (Microsoft.AspNetCore.Identity,
   không thêm dependency, tự versioning + rehash khi login). Cột `MatKhauHash` rút còn **`nvarchar(256)`**.
3. **`TT_TepDinhKem`** (file/ảnh: Logo công ty) thuộc đợt nhóm `TT_` sau — đợt này chỉ để cột FK `*_Id`
   (chưa ràng buộc). **Ảnh đại diện người dùng** lấy từ `NS_NhanVien` (không lưu ở `HT_NguoiDung`).
4. **`HT_NguoiDung` ↔ nhân viên (`NS_NhanVien`)**: tài khoản **bắt buộc gắn 1 nhân viên** (thông tin cá nhân
   `HoTen`/`Email`/`DienThoai`/ảnh lấy qua nhân viên, không lưu trùng). Đợt nền tảng để `NhanVien_Id` **nullable**;
   đợt `NS_` siết **NOT NULL + FK + UNIQUE(NhanVien_Id)** (1 nhân viên ≤ 1 tài khoản).
5. **Đăng ký `Sys_Relation`** cho các FK trên (soft-check khi xóa): ✅ **HOÃN** — dựa `ReferenceCheckService`
   name-match (dò theo tên cột `{Bang}_Id`) cho giai đoạn này. Đăng ký `Sys_Table` + `Sys_Relation` đầy đủ
   khi đưa bảng vào metadata Engine. (Lookup trạng thái đã seed: `db/039_seed_config_lookup_foundation.sql`.)
6. **`015_create_cf_data_schema.sql`** (schema `Cf_*` convention cũ): ✅ **GIỮ LÀM THAM KHẢO** — không chạy
   vào Data DB chuẩn mới. Dùng làm nguồn phân tích nghiệp vụ khi phát triển **module mua bán cà phê nhân**
   (sẽ chuẩn hóa lại sang nhóm `TM_` Thương Mại theo convention mới: bỏ `Tenant_Id`, đổi tên tiếng Việt,
   khối cột auto chuẩn). KHÔNG migrate trực tiếp.
7. ⚠️ **Tài khoản hệ thống bootstrap (chicken-egg):** vì tài khoản phải gắn nhân viên, nhưng khi provisioning
   tenant **chưa có nhân viên nào** → cần tài khoản đầu tiên để đăng nhập. Hơn nữa `NS_NhanVien` thuộc đợt sau
   nên đợt nền tảng chưa thể FK. **Phương án (chốt khi làm Auth):** seed sẵn **1 nhân viên hệ thống** +
   **1 tài khoản super-admin** (`LaQuanTri=1`) lúc provisioning; hoặc cho phép `NhanVien_Id` NULL **chỉ** với
   tài khoản hệ thống. → Giữ `NhanVien_Id` nullable ở đợt nền tảng để không kẹt thứ tự migration.
