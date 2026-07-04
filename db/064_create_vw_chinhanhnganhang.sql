-- =============================================================================
-- File    : 064_create_vw_chinhanhnganhang.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : View đọc cho danh mục Chi nhánh ngân hàng (FK NganHang_Id → DM_NganHang)
--           — engine-driven Grid hiển thị + lọc/sắp xếp/xuất theo TÊN ngân hàng.
--           Bổ sung cột TenNganHang (= nh.Ten) cho lưới; KHÔNG đổi base table.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §5 (mặt LƯỚI) · ADR-033 · ADR-024.
--           Mẫu: db/052 (vw_DM_TinhThanhPho) · db/051 (vw_TC_CongTy + TenNganHang).
-- Dùng    : Ui_View.Source_Type='View', Source_Object='vw_DM_ChiNhanhNganHang'.
--           Edit_Form vẫn trỏ base table DM_ChiNhanhNganHang. Cấu hình ở db/065.
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent
--           (CREATE OR ALTER). Lọc IsDeleted=0.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- Chi nhánh ngân hàng + tên ngân hàng (lookup form: NganHang_Id → DM_NganHang).
CREATE OR ALTER VIEW dbo.vw_DM_ChiNhanhNganHang
AS
SELECT
    b.Id,
    b.Ma,
    b.Ten,
    b.DiaChi,
    b.NganHang_Id,
    nh.Ten          AS TenNganHang
FROM        dbo.DM_ChiNhanhNganHang b
LEFT JOIN   dbo.DM_NganHang         nh ON nh.Id = b.NganHang_Id
WHERE       b.IsDeleted = 0;
GO

PRINT N'Migration 064 completed — view vw_DM_ChiNhanhNganHang (+TenNganHang).';
GO
