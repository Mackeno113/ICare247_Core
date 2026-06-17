-- =============================================================================
-- File    : 054_seed_ht_chucnang_menu_admin.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 045)
-- Purpose : Thêm node menu "Quản lý menu" (/m/administration/menu) vào HT_ChucNang để màn
--           Menu Builder xuất hiện trong cụm Quản trị. Quyền enforce theo chức năng
--           "administration.menu" (RequirePermission đọc theo Ma). Grant SUPERADMIN toàn quyền.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md · ADR-023. Menu Builder = ghi đơn-DB (Data tenant),
--           picker View đọc Config DB qua /api/v1/views.
-- Context : Data DB tenant, sau 045 (đã có node 'administration'). Idempotent (theo Ma).
--           CreatedBy đặt tường minh = tài khoản admin.
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @MenuId  INT    = 1;   -- MAIN (Config DB Sys_Menu)
DECLARE @ParentId BIGINT =
    (SELECT Id FROM dbo.HT_ChucNang WHERE Ma = N'administration' AND IsDeleted = 0);

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Chưa có tài khoản admin — chạy 038_seed_data_db_bootstrap.sql trước.', 16, 1);
    RETURN;
END;
IF @ParentId IS NULL
BEGIN
    RAISERROR(N'Chưa có node administration — chạy 045_seed_ht_chucnang_base.sql trước.', 16, 1);
    RETURN;
END;

-- ── 1. INSERT node 'administration.menu' (màn con của Quản trị hệ thống) ──────
INSERT INTO dbo.HT_ChucNang
    (Ma, Ten, ChucNang_Cha_Id, Loai, Module, DuongDan, Icon, ThuTu,
     Menu_Id, LaHeThong, KichHoat, ViTriHienThi, CreatedBy, CreatedAt)
SELECT N'administration.menu', N'Quản lý menu', @ParentId, N'ManHinh', N'HT',
       N'/m/administration/menu', N'menu', 5,
       @MenuId, 1, 1, N'Sidebar', @AdminId, SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_ChucNang c WHERE c.Ma = N'administration.menu' AND c.IsDeleted = 0);
GO

-- ── 2. Grant SUPERADMIN toàn quyền cho node mới (cần Xem=1 để hiện trên menu) ──
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
    WHERE c.Ma = N'administration.menu' AND c.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_Quyen q
                      WHERE q.VaiTro_Id = @SuperRole AND q.ChucNang_Id = c.Id AND q.IsDeleted = 0);
END;
GO

PRINT N'Migration 054 completed — node administration.menu + grant SUPERADMIN.';
GO
