-- =============================================================================
-- File    : 066_config_grid_chinhanhnganhang_autojoin.sql
-- Database: ICare247_Config  (Config DB per-tenant)
-- Purpose : Chuyển lưới Grid_DM_ChiNhanhNganHang sang cơ chế FK auto-JOIN (no-code) —
--           thay cho SQL View tay ở db/065. Engine tự JOIN DM_NganHang theo Props_Json.fkLookup.
--           Mô hình IN-PLACE (Model 2): đặt fkLookup ngay trên cột thật NganHang_Id → cột đó hiện TÊN.
--           (Bảng DM_ChiNhanhNganHang KHÔNG có cột TenNganHang → không tạo cột ảo; gọn + rõ.)
--           (1) đọc base table (Source_Type='Table'); (2) NganHang_Id hiện + fkLookup; (3) gỡ cột ảo TenNganHang.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §5a (auto-JOIN, in-place) · ADR-033.
--           Engine suy cột FK gốc (NganHang_Id) từ Field_Id=34 (Sys_Column) → JOIN _fk.Id = NganHang_Id.
-- Lưu ý   : View vw_DM_ChiNhanhNganHang (db/064) GIỮ lại làm ví dụ escape-hatch §5b — không dùng cho màn này.
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Config DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @ViewId INT =
    (SELECT TOP 1 View_Id FROM dbo.Ui_View
     WHERE View_Code = N'Grid_DM_ChiNhanhNganHang'
     ORDER BY CASE WHEN Tenant_Id IS NULL THEN 1 ELSE 0 END);

IF @ViewId IS NULL
BEGIN
    PRINT N'⚠ Ui_View Grid_DM_ChiNhanhNganHang không tồn tại — bỏ qua.';
    RETURN;
END

-- 1) Đọc base table — engine tự JOIN, không cần SQL View.
UPDATE dbo.Ui_View
SET    Source_Type   = N'Table',
       Source_Object = NULL,
       Updated_At    = GETDATE()
WHERE  View_Id = @ViewId;

-- 2) Cột thật NganHang_Id hiện TÊN (in-place) — trỏ định nghĩa FK (Field_Id=34). Caption giữ key cũ
--    "…col.nganhang_id.caption" = "Ngân hàng". Order_No=0 → đứng đầu.
UPDATE dbo.Ui_View_Column
SET    Is_Visible = 1, Order_No = 0, Render_Mode = N'Text', Is_Active = 1,
       Props_Json = N'{"fkLookup":{"fieldId":34}}'
WHERE  View_Id = @ViewId AND Field_Name = N'NganHang_Id';

-- 3) Gỡ cột ảo TenNganHang (đã thêm ở db/065) — Model 2 không cần; bảng cũng không có cột này.
DELETE FROM dbo.Ui_View_Column
WHERE  View_Id = @ViewId AND Field_Name = N'TenNganHang';

PRINT N'Migration 066 completed — Grid_DM_ChiNhanhNganHang dùng FK auto-JOIN in-place (cột NganHang_Id hiện tên).';
GO
