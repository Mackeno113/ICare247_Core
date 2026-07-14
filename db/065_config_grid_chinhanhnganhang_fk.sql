-- =============================================================================
-- File    : 065_config_grid_chinhanhnganhang_fk.sql
-- Database: ICare247_Config  (Config DB per-tenant)
-- Purpose : Cấu hình lưới Grid_DM_ChiNhanhNganHang hiển thị TÊN ngân hàng (FK Phase 1):
--           (1) đọc qua SQL View vw_DM_ChiNhanhNganHang (JOIN tên) → lọc/sort/xuất theo tên;
--           (2) thêm cột TenNganHang (i18n) + tham chiếu định nghĩa FK (Field_Id=34) qua Props_Json;
--           (3) ẩn cột khóa ngoại thô NganHang_Id.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §5 + §3b (Q1 tường minh) · ADR-033.
-- Phụ thuộc: db/064 (tạo view ở Data DB) phải chạy trước.
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Config DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @ViewId INT =
    (SELECT TOP 1 View_Id FROM dbo.Ui_View
     WHERE View_Code = N'Grid_DM_ChiNhanhNganHang');

IF @ViewId IS NULL
BEGIN
    PRINT N'⚠ Ui_View Grid_DM_ChiNhanhNganHang không tồn tại — bỏ qua.';
    RETURN;
END

-- 1) Trỏ View đọc qua SQL View (JOIN sẵn tên ngân hàng).
UPDATE dbo.Ui_View
SET    Source_Type   = N'View',
       Source_Object = N'vw_DM_ChiNhanhNganHang',
       Updated_At    = GETDATE()
WHERE  View_Id = @ViewId;

-- 2) i18n caption cột TenNganHang (vi + en) — không ghi đè nếu đã có.
MERGE dbo.Sys_Resource AS t
USING (VALUES
    (N'dm_chinhanhnganhang.view.grid_dm_chinhanhnganhang.col.tennganhang.caption', N'vi', N'Ngân hàng'),
    (N'dm_chinhanhnganhang.view.grid_dm_chinhanhnganhang.col.tennganhang.caption', N'en', N'Bank')
) AS s(Resource_Key, Lang_Code, Resource_Value)
ON  t.Resource_Key = s.Resource_Key AND t.Lang_Code = s.Lang_Code
WHEN NOT MATCHED THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value)
    VALUES (s.Resource_Key, s.Lang_Code, s.Resource_Value);

-- 3) Cột hiển thị TÊN ngân hàng (unbound — cột của View, Column_Id=NULL theo ADR-028).
--    Props_Json tham chiếu tường minh định nghĩa FK (Field_Id=34 Edit_Form → Ui_Field_Lookup DM_NganHang)
--    cho import/template (ADR-033 §3b, Q1). Order_No=0 → đứng đầu (chỗ NganHang_Id cũ).
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_View_Column WHERE View_Id = @ViewId AND Field_Name = N'TenNganHang')
    INSERT dbo.Ui_View_Column
        (View_Id, Column_Id, Field_Name, Caption_Key, Column_Kind, Render_Mode,
         Is_Visible, Order_No, Allow_Sort, Allow_Filter, Allow_Export, Is_Active, Props_Json)
    VALUES
        (@ViewId, NULL, N'TenNganHang',
         N'dm_chinhanhnganhang.view.grid_dm_chinhanhnganhang.col.tennganhang.caption',
         N'Data', N'Text', 1, 0, 1, 1, 1, 1, N'{"fkLookup":{"fieldId":34}}');
ELSE
    UPDATE dbo.Ui_View_Column
    SET    Is_Visible  = 1, Order_No = 0, Render_Mode = N'Text', Is_Active = 1,
           Caption_Key = N'dm_chinhanhnganhang.view.grid_dm_chinhanhnganhang.col.tennganhang.caption',
           Props_Json  = N'{"fkLookup":{"fieldId":34}}'
    WHERE  View_Id = @ViewId AND Field_Name = N'TenNganHang';

-- 4) Ẩn cột khóa ngoại thô NganHang_Id (giữ là cột dữ liệu phụ, không hiển thị).
UPDATE dbo.Ui_View_Column
SET    Is_Visible = 0
WHERE  View_Id = @ViewId AND Field_Name = N'NganHang_Id';

PRINT N'Migration 065 completed — Grid_DM_ChiNhanhNganHang đọc qua vw_, hiện TenNganHang, ẩn NganHang_Id.';
GO
