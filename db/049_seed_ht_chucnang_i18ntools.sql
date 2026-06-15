-- =============================================================================
-- File    : 049_seed_ht_chucnang_i18ntools.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 045)
-- Purpose : Thêm node menu "Tra cứu i18n" (/dev/i18n) vào HT_ChucNang để màn này
--           xuất hiện ở menu server-driven (cụm Dev, cạnh "Công cụ (Dev)"), thay vì
--           chỉ có ở fallback AppNav tĩnh. Grant toàn quyền cho SUPERADMIN.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md §10 · ADR-023.
-- Context : Data DB tenant, sau 045 (seed base có node 'devtools'). Idempotent (theo Ma).
--           CreatedBy đặt tường minh = tài khoản admin (không dựa DEFAULT).
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @MenuId  INT    = 1;   -- MAIN (Config DB Sys_Menu)

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Chưa có tài khoản admin — chạy 038_seed_data_db_bootstrap.sql trước.', 16, 1);
    RETURN;
END;

-- ── 1. INSERT node 'i18ntools' nếu chưa có (ManHinh gốc, cụm Dev → _bottom) ──
--    BuildFromApi xếp vào _bottom vì DuongDan bắt đầu '/dev'; TitleKey = nav.i18ntools.
INSERT INTO dbo.HT_ChucNang
    (Ma, Ten, ChucNang_Cha_Id, Loai, Module, DuongDan, Icon, ThuTu,
     Menu_Id, LaHeThong, KichHoat, ViTriHienThi, CreatedBy, CreatedAt)
SELECT N'i18ntools', N'Tra cứu i18n', NULL, N'ManHinh', NULL, N'/dev/i18n', N'languages', 91,
       @MenuId, 1, 1, N'Sidebar', @AdminId, SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_ChucNang c WHERE c.Ma = N'i18ntools' AND c.IsDeleted = 0);
GO

-- ── 2. Grant SUPERADMIN toàn quyền cho node mới (Duyệt=0 → workflow) ─────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @SuperRole BIGINT =
    (SELECT Id FROM dbo.HT_VaiTro WHERE Ma = N'SUPERADMIN' AND IsDeleted = 0);

IF @SuperRole IS NOT NULL
BEGIN
    INSERT INTO dbo.HT_VaiTro_Quyen
        (VaiTro_Id, ChucNang_Id, Xem, Them, Sua, Xoa, Duyet, InAn, CreatedBy, CreatedAt)
    SELECT @SuperRole, c.Id, 1, 1, 1, 1, 0, 1, @AdminId, SYSUTCDATETIME()
    FROM dbo.HT_ChucNang c
    WHERE c.Ma = N'i18ntools' AND c.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_Quyen q
                      WHERE q.VaiTro_Id = @SuperRole AND q.ChucNang_Id = c.Id AND q.IsDeleted = 0);
END;
GO
