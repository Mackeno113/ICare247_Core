-- =============================================================================
-- File    : 069_alter_ui_field_lookup_tree_selectable_level.sql
-- Database: ICare247_Config
-- Purpose : Nâng cấp TreeLookupBox (port từ TreePicker nhánh dynamic-tree) — thêm cột
--           Ui_Field_Lookup.Tree_Selectable_Level: giới hạn node được phép chọn.
--             'all'    (mặc định, NULL) — chọn mọi node
--             'leaf'   — chỉ node lá (không có con). VD chỉ chọn Xã, không chọn Tỉnh/Huyện
--             'branch' — chỉ node nhánh (có con)
-- Note    : Idempotent (COL_LENGTH guard). ConfigSync đọc cột động → thêm cột an toàn.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Ui_Field_Lookup', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_Field_Lookup', 'Tree_Selectable_Level') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_Field_Lookup ADD Tree_Selectable_Level NVARCHAR(10) NULL;
        PRINT N'✔ Đã thêm Ui_Field_Lookup.Tree_Selectable_Level.';
    END
    ELSE
        PRINT N'• Ui_Field_Lookup.Tree_Selectable_Level đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_Field_Lookup chưa tồn tại.';
GO

PRINT N'Migration 069 completed — Tree_Selectable_Level cho TreeLookupBox.';
GO
