-- =============================================================================
-- File    : 068_alter_ui_field_lookup_reload_trigger_fields.sql
-- Database: ICare247_Config
-- Purpose : Multi-Trigger cascading — thêm cột Ui_Field_Lookup.Reload_Trigger_Fields
--           (danh sách FieldCode cha, phân cách dấu phẩy). Runtime reload LookupBox khi
--           BẤT KỲ field trong danh sách đổi giá trị — bổ sung cho:
--             • ReloadTriggerField (đơn, Migration 014), và
--             • auto-dò @param trong Filter SQL (renderer hiện tại).
--           Dùng cho TVF/Full SQL hoặc field cha KHÔNG xuất hiện trong Filter SQL.
-- Note    : Idempotent (COL_LENGTH guard). ConfigSync đọc cột động → thêm cột an toàn.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Ui_Field_Lookup', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_Field_Lookup', 'Reload_Trigger_Fields') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_Field_Lookup ADD Reload_Trigger_Fields NVARCHAR(500) NULL;
        PRINT N'✔ Đã thêm Ui_Field_Lookup.Reload_Trigger_Fields.';
    END
    ELSE
        PRINT N'• Ui_Field_Lookup.Reload_Trigger_Fields đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_Field_Lookup chưa tồn tại.';
GO

PRINT N'Migration 068 completed — Multi-Trigger Reload_Trigger_Fields.';
GO
