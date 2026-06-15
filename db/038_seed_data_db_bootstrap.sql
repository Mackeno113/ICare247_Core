-- =============================================================================
-- File    : 038_seed_data_db_bootstrap.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 037)
-- Purpose : Seed bootstrap cho Data DB nền tảng — tài khoản super-admin để phát
--           triển (chưa cần phân quyền) + vài danh mục cấp cơ bản.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §6.7 (bootstrap chicken-egg).
-- Context : Chạy SAU 037, trong ngữ cảnh Data DB của tenant.
-- Note    : Idempotent — chỉ insert khi chưa tồn tại (theo Ma / TenDangNhap).
--
--   ⚠️ Tài khoản dev:  TenDangNhap = 'admin'   |  Mật khẩu = 'Admin@12345'
--      MatKhauHash dưới đây là PBKDF2 định dạng ASP.NET Core Identity v3
--      (PRF=HMACSHA256, iter=100000) — PasswordHasher<T>.VerifyHashedPassword đọc
--      tham số nhúng trong blob nên verify đúng bất kể cấu hình mặc định runtime.
--      → ĐỔI MẬT KHẨU NGAY khi lên môi trường thật.
--
--   NhanVien_Id = NULL: ngoại lệ DUY NHẤT cho tài khoản hệ thống bootstrap
--      (đợt NS_ sẽ siết NOT NULL cho tài khoản thường, miễn trừ tài khoản này).
--
--   Lookup trạng thái (TRANGTHAI_*, LOAI_TAIKHOAN, HINHTHUC_2FA) thuộc CONFIG DB
--      (Sys_Lookup) → seed ở migration Config DB riêng, KHÔNG nằm ở file này.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── 1. Tài khoản super-admin ───────────────────────────────────────────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

IF @AdminId IS NULL
BEGIN
    INSERT INTO dbo.HT_NguoiDung
        (Ma, TenDangNhap, LoaiTaiKhoan, MatKhauHash, NhanVien_Id,
         TrangThai, LaQuanTri, HinhThuc2FA, CreatedBy, CreatedAt)
    VALUES
        (N'ADMIN', N'admin', N'Local',
         N'AQAAAAEAAYagAAAAEETBlaIVFWYRUyDdUeYOTrcFbNwProeNQtldxCKjlEUkT5+hXUfwNi57cjo//JKLBA==',
         NULL, N'HoatDong', 1, N'None', 0, SYSUTCDATETIME());

    SET @AdminId = SCOPE_IDENTITY();
    -- Tự trỏ CreatedBy về chính mình (không FK trên cột audit → an toàn)
    UPDATE dbo.HT_NguoiDung SET CreatedBy = @AdminId WHERE Id = @AdminId;
END;
GO

-- ── 2. Vai trò hệ thống "Quản trị hệ thống" + gán cho admin ─────────────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

IF NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro WHERE Ma = N'SUPERADMIN' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.HT_VaiTro (Ma, Ten, MoTa, LaHeThong, CreatedBy, CreatedAt)
    VALUES (N'SUPERADMIN', N'Quản trị hệ thống', N'Toàn quyền hệ thống', 1, @AdminId, SYSUTCDATETIME());
END;

DECLARE @VaiTroId BIGINT =
    (SELECT Id FROM dbo.HT_VaiTro WHERE Ma = N'SUPERADMIN' AND IsDeleted = 0);

IF @AdminId IS NOT NULL AND @VaiTroId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_VaiTro
                   WHERE NguoiDung_Id = @AdminId AND VaiTro_Id = @VaiTroId AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.HT_NguoiDung_VaiTro (NguoiDung_Id, VaiTro_Id, CreatedBy, CreatedAt)
    VALUES (@AdminId, @VaiTroId, @AdminId, SYSUTCDATETIME());
END;
GO

-- ── 3. Danh mục cơ bản ─────────────────────────────────────────────────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

-- 3.1 Quốc gia Việt Nam
IF NOT EXISTS (SELECT 1 FROM dbo.DM_QuocGia WHERE Ma = N'VN' AND IsDeleted = 0)
    INSERT INTO dbo.DM_QuocGia (Ma, Ten, MaDienThoai, CreatedBy, CreatedAt)
    VALUES (N'VN', N'Việt Nam', N'+84', @AdminId, SYSUTCDATETIME());

-- 3.2 Cấp công ty
INSERT INTO dbo.TC_CapCongTy (Ma, Ten, ThuTu, CreatedBy, CreatedAt)
SELECT v.Ma, v.Ten, v.ThuTu, @AdminId, SYSUTCDATETIME()
FROM (VALUES
        (N'TONGCT', N'Tổng công ty',          1),
        (N'CT',     N'Công ty',               2),
        (N'CN',     N'Chi nhánh',             3),
        (N'VPDD',   N'Văn phòng đại diện',    4)
     ) AS v(Ma, Ten, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TC_CapCongTy c WHERE c.Ma = v.Ma AND c.IsDeleted = 0);

-- 3.3 Cấp phòng ban
INSERT INTO dbo.TC_CapPhongBan (Ma, Ten, ThuTu, CreatedBy, CreatedAt)
SELECT v.Ma, v.Ten, v.ThuTu, @AdminId, SYSUTCDATETIME()
FROM (VALUES
        (N'KHOI',  N'Khối',  1),
        (N'PHONG', N'Phòng', 2),
        (N'TO',    N'Tổ',    3),
        (N'NHOM',  N'Nhóm',  4)
     ) AS v(Ma, Ten, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TC_CapPhongBan c WHERE c.Ma = v.Ma AND c.IsDeleted = 0);
GO
