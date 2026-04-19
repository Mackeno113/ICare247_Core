-- ============================================================
-- Migration 016: Thêm cột hỗ trợ Tree Control và Multi-Trigger Cascading
-- Mục đích  : (1) Dạng 2 — Multi-trigger cascading: Reload_Trigger_Fields
--                 cho phép một lookup field lắng nghe nhiều trigger field.
--             (2) Dạng 1 — Tree Control: Tree_Parent_Column, Tree_Root_Filter,
--                 Tree_Selectable_Level, Tree_Load_Mode
--                 cho phép hiển thị dữ liệu phân cấp dạng cây (TreePicker).
-- Phụ thuộc : Migration 014 (bảng Ui_Field_Lookup đã tồn tại)
-- Ngày      : 2026-04-19
-- ============================================================

-- ────────────────────────────────────────────────────────────
-- PHẦN A: Multi-Trigger Cascading (Dạng 2)
-- ────────────────────────────────────────────────────────────

-- A1. Reload_Trigger_Fields
--     Danh sách FieldCode phân cách nhau bởi dấu phẩy — khi bất kỳ field nào
--     trong danh sách thay đổi, lookup field sẽ clear và reload data.
--     VD: N'ProvinceId,DistrictId' — chọn tỉnh HOẶC quận → reload xã.
--     Ưu tiên hơn Reload_Trigger_Field (đơn lẻ) khi được set.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Reload_Trigger_Fields'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Reload_Trigger_Fields nvarchar(500) NULL;
    PRINT N'Đã thêm cột Reload_Trigger_Fields';
END
GO

-- ────────────────────────────────────────────────────────────
-- PHẦN B: Tree Control (Dạng 1) — chỉ dùng khi Editor_Type = TreePicker
-- ────────────────────────────────────────────────────────────

-- B1. Tree_Parent_Column
--     Tên cột chứa ID cha trong bảng nguồn — dùng để build cây phân cấp.
--     VD: N'Parent_Id' — bảng Sys_Area có Area_Id + Parent_Id.
--     NULL = không phải tree config.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Tree_Parent_Column'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Tree_Parent_Column nvarchar(100) NULL;
    PRINT N'Đã thêm cột Tree_Parent_Column';
END
GO

-- B2. Tree_Root_Filter
--     Mệnh đề WHERE bổ sung để xác định node gốc (không có cha).
--     VD: N'Parent_Id IS NULL' hoặc N'Level = 1'.
--     Chỉ dùng cho lazy load mode (Tree_Load_Mode = 'lazy').
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Tree_Root_Filter'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Tree_Root_Filter nvarchar(500) NULL;
    PRINT N'Đã thêm cột Tree_Root_Filter';
END
GO

-- B3. Tree_Selectable_Level
--     Quy định node nào user được phép chọn:
--     'all'    = mọi node (mặc định)
--     'leaf'   = chỉ node lá (không có con) — VD chỉ chọn phường/xã
--     'branch' = chỉ node nhánh (có con) — VD chỉ chọn tỉnh/quận
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Tree_Selectable_Level'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Tree_Selectable_Level nvarchar(20) NULL
            CONSTRAINT DF_UiFL_TreeSelectableLevel DEFAULT N'all';
    PRINT N'Đã thêm cột Tree_Selectable_Level';
END
GO

-- B4. Tree_Load_Mode
--     Cách load dữ liệu cây:
--     'all_at_once' = load toàn bộ flat list rồi build cây client-side (mặc định)
--     'lazy'        = load từng cấp khi user expand (cần backend hỗ trợ — future)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Tree_Load_Mode'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Tree_Load_Mode nvarchar(20) NULL
            CONSTRAINT DF_UiFL_TreeLoadMode DEFAULT N'all_at_once';
    PRINT N'Đã thêm cột Tree_Load_Mode';
END
GO

-- ── Verify ────────────────────────────────────────────────────────────────────
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT
FROM   INFORMATION_SCHEMA.COLUMNS
WHERE  TABLE_NAME = N'Ui_Field_Lookup'
ORDER BY ORDINAL_POSITION;
GO
