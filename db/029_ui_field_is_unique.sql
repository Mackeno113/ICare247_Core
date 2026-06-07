-- =============================================================================
-- File    : 029_ui_field_is_unique.sql
-- Purpose : Thêm Ui_Field.Is_Unique — đánh dấu field phải duy nhất (chống trùng mã).
--           Backend check trước khi Insert/Update (Master Data + Lookup add-new).
--           Khuyến nghị: tạo thêm UNIQUE INDEX trên cột tương ứng ở bảng nghiệp vụ
--           (race-proof) — xem cuối file.
-- =============================================================================

USE [ICare247_Config];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Is_Unique'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Is_Unique BIT NOT NULL DEFAULT 0;
END;
GO

PRINT N'Migration 029 completed — Ui_Field.Is_Unique added.';
GO

-- =============================================================================
-- GỢI Ý (chạy trên DB DỮ LIỆU, thay tên bảng/cột thực tế) — chống trùng tuyệt đối:
--   CREATE UNIQUE INDEX UQ_DM_TrinhDoVanHoa_Ma
--       ON dbo.DM_TrinhDoVanHoa (Ma_TrinhDoVanHoa, Tenant_Id)
--       WHERE Is_Active = 1;   -- bỏ WHERE nếu bảng không có Is_Active
-- =============================================================================
