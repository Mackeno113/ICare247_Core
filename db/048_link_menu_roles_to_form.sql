-- =============================================================================
-- File    : 048_link_menu_roles_to_form.sql
-- Purpose : Nối node menu "Vai trò" (administration.roles) tới form danh mục thật HT_VaiTro:
--           đổi DuongDan → /master/HT_VaiTro, gắn DoiTuong/LoaiDoiTuong để enforce + ẩn nút áp.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md (AUTHZ-UI-2) · ADR-023.
-- Context : Data DB tenant, sau 045/046 + 047 (form HT_VaiTro ở Config DB). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId BIGINT = (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

UPDATE dbo.HT_ChucNang
SET DuongDan     = N'/master/HT_VaiTro',
    DoiTuong     = N'HT_VaiTro',
    LoaiDoiTuong = N'Form',
    UpdatedBy    = @AdminId,
    UpdatedAt    = SYSUTCDATETIME()
WHERE Ma = N'administration.roles' AND IsDeleted = 0;
GO
