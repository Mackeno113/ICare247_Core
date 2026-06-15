-- =============================================================================
-- File    : 052_create_vw_danhmuc.sql
-- Database: ICare247_Solution  (Data DB per-tenant)
-- Purpose : View đọc cho 2 danh mục có FK (engine-driven Grid hiển thị TÊN cha):
--           vw_DM_TinhThanhPho (+TenQuocGia) · vw_DM_PhuongXa (+TenTinhThanhPho).
--           5 danh mục phẳng còn lại (DM_QuocGia/DM_DonViTinh/DM_NganHang/TC_CapCongTy/
--           TC_CapPhongBan) KHÔNG cần view — đăng ký thẳng base table vào Sys_Table.
-- Spec    : ADR-024 (màn nghiệp vụ engine-driven, đọc qua SQL View). Liên quan db/051 (vw_TC_CongTy).
-- Dùng    : đăng ký view vào Sys_Table (ConfigStudio) → Ui_View Grid. Edit_Form trỏ về base table.
-- Note    : CREATE OR ALTER — idempotent. Lọc IsDeleted=0.
-- =============================================================================

USE [ICare247_Solution];
GO

-- Tỉnh/Thành phố + tên quốc gia (lookup form: QuocGia_Id → DM_QuocGia).
CREATE OR ALTER VIEW dbo.vw_DM_TinhThanhPho
AS
SELECT
    t.Id,
    t.Ma,
    t.Ten,
    t.LoaiHinh,
    t.QuocGia_Id,
    qg.Ten AS TenQuocGia
FROM        dbo.DM_TinhThanhPho t
LEFT JOIN   dbo.DM_QuocGia       qg ON qg.Id = t.QuocGia_Id
WHERE       t.IsDeleted = 0;
GO

-- Phường/Xã + tên tỉnh (lookup form: TinhThanhPho_Id → DM_TinhThanhPho, cascade từ Tỉnh).
CREATE OR ALTER VIEW dbo.vw_DM_PhuongXa
AS
SELECT
    p.Id,
    p.Ma,
    p.Ten,
    p.LoaiHinh,
    p.TinhThanhPho_Id,
    t.Ten AS TenTinhThanhPho
FROM        dbo.DM_PhuongXa       p
LEFT JOIN   dbo.DM_TinhThanhPho   t ON t.Id = p.TinhThanhPho_Id
WHERE       p.IsDeleted = 0;
GO

PRINT N'Migration 052 completed — view vw_DM_TinhThanhPho + vw_DM_PhuongXa (danh mục có FK).';
GO
