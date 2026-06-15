-- =============================================================================
-- File    : 051_create_vw_tc_congty.sql
-- Database: ICare247_Solution  (Data DB per-tenant)
-- Purpose : ORG-CFG-2 — View đọc danh sách Công ty cho màn engine-driven (TreeList).
--           JOIN sẵn cấp công ty / phường-xã / tỉnh / ngân hàng / công ty cha để lưới hiển
--           thị TÊN, form sửa vẫn ghi theo *_Id ở bảng gốc TC_CongTy.
-- Spec    : docs/spec/16 (F1) + ADR-024 (màn nghiệp vụ engine-driven, đọc qua SQL View).
-- Dùng    : đăng ký vw_TC_CongTy vào Sys_Table (ConfigStudio) → Ui_View TreeList
--           (Key=Id, Parent=CongTy_Cha_Id). Edit_Form trỏ về Ui_Form trên bảng gốc TC_CongTy.
-- Note    : CREATE OR ALTER — idempotent. Lọc IsDeleted=0 (chỉ công ty còn hiệu lực).
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER VIEW dbo.vw_TC_CongTy
AS
SELECT
    ct.Id,
    ct.Ma,
    ct.Ten,
    ct.TenVietTat,
    ct.CongTy_Cha_Id,
    cha.Ten            AS TenCongTyCha,
    ct.CapCongTy_Id,
    cap.Ten            AS TenCapCongTy,
    ct.MaSoThue,
    ct.DiaChi,
    ct.PhuongXa_Id,
    px.Ten             AS TenPhuongXa,
    px.TinhThanhPho_Id,
    tinh.Ten           AS TenTinhThanhPho,
    ct.DienThoai,
    ct.Email,
    ct.Website,
    ct.NguoiDaiDien,
    ct.GiamDoc,
    ct.KeToanTruong,
    ct.NganHang_Id,
    nh.Ten             AS TenNganHang,
    ct.SoTaiKhoan,
    ct.TrangThai
FROM        dbo.TC_CongTy        ct
LEFT JOIN   dbo.TC_CongTy        cha  ON cha.Id  = ct.CongTy_Cha_Id
LEFT JOIN   dbo.TC_CapCongTy     cap  ON cap.Id  = ct.CapCongTy_Id
LEFT JOIN   dbo.DM_PhuongXa      px   ON px.Id   = ct.PhuongXa_Id
LEFT JOIN   dbo.DM_TinhThanhPho  tinh ON tinh.Id = px.TinhThanhPho_Id
LEFT JOIN   dbo.DM_NganHang      nh   ON nh.Id   = ct.NganHang_Id
WHERE       ct.IsDeleted = 0;
GO

PRINT N'Migration 051 completed — view dbo.vw_TC_CongTy (danh sách Công ty + tên JOIN cho lưới cây).';
GO
