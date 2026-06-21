-- =============================================================================
-- File    : 057_seed_ht_chucnang_config_sync.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 045)
-- Purpose : Thêm node menu "Đồng bộ cấu hình" (/m/administration/config-sync) vào
--           HT_ChucNang để màn ConfigSync xuất hiện trong cụm Quản trị + cấp quyền
--           theo chức năng "administration.config-sync" (RequirePermission đọc theo Ma).
--           Super admin vốn đã chạy được nhờ bypass; node này để (a) hiện trên menu,
--           (b) cấp quyền cho role khác mà không cần bypass.
-- Spec    : docs/spec/16_CONFIG_SYNC_SPEC.md (F1) · docs/spec/15_AUTHZ_NAVIGATION_SPEC.md.
--           Controller: AdminConfigSyncController — [RequirePermission("administration.config-sync", Xem/Sua)].
-- Context : Data DB tenant, sau 045 (đã có node 'administration'). Idempotent (theo Ma).
--           Icon=NULL (hiện dấu chấm mặc định, đồng bộ với users/roles/permissions/settings).
--           CreatedBy đặt tường minh = tài khoản admin.
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId  BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @MenuId   INT    = 1;   -- MAIN (Config DB Sys_Menu)
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

-- ── 1. INSERT node 'administration.config-sync' (màn con của Quản trị hệ thống) ──
INSERT INTO dbo.HT_ChucNang
    (Ma, Ten, ChucNang_Cha_Id, Loai, Module, DuongDan, Icon, ThuTu,
     Menu_Id, LaHeThong, KichHoat, ViTriHienThi, CreatedBy, CreatedAt)
SELECT N'administration.config-sync', N'Đồng bộ cấu hình', @ParentId, N'ManHinh', N'HT',
       N'/m/administration/config-sync', NULL, 6,
       @MenuId, 1, 1, N'Sidebar', @AdminId, SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_ChucNang c WHERE c.Ma = N'administration.config-sync' AND c.IsDeleted = 0);
GO

-- ── 2. Grant SUPERADMIN toàn quyền cho node mới (cần Xem=1 để hiện trên menu) ──
DECLARE @AdminId   BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @SuperRole BIGINT =
    (SELECT Id FROM dbo.HT_VaiTro WHERE Ma = N'SUPERADMIN' AND IsDeleted = 0);

IF @SuperRole IS NOT NULL
BEGIN
    INSERT INTO dbo.HT_VaiTro_Quyen
        (VaiTro_Id, ChucNang_Id, Xem, Them, Sua, Xoa, Duyet, InAn, CreatedBy, CreatedAt)
    SELECT @SuperRole, c.Id, 1, 1, 1, 1, 0, 1, @AdminId, SYSUTCDATETIME()
    FROM dbo.HT_ChucNang c
    WHERE c.Ma = N'administration.config-sync' AND c.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_Quyen q
                      WHERE q.VaiTro_Id = @SuperRole AND q.ChucNang_Id = c.Id AND q.IsDeleted = 0);
END;
GO

PRINT N'Migration 057 completed — node administration.config-sync + grant SUPERADMIN.';
GO
