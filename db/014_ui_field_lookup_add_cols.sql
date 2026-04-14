-- =============================================================================
-- File    : 014_ui_field_lookup_add_cols.sql
-- Purpose : Thêm 5 cột vào Ui_Field_Lookup — hỗ trợ ComboBox/LookupBox nâng cao:
--           - Reload_Trigger_Field : field nào thay đổi thì reload options
--           - EditBox_Mode         : 'text'/'code'/'code_text' — cách hiển thị khi chọn
--           - Code_Field           : cột lưu mã rút gọn (bên cạnh display)
--           - DropDown_Width       : chiều rộng popup (px), NULL = auto
--           - DropDown_Height      : chiều cao popup (px), NULL = auto
-- =============================================================================

USE [ICare247_Config];
GO

-- Reload_Trigger_Field
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field_Lookup') AND name = 'Reload_Trigger_Field'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup ADD Reload_Trigger_Field NVARCHAR(100) NULL;
END;
GO

-- EditBox_Mode: cách hiển thị giá trị đã chọn trong edit box
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field_Lookup') AND name = 'EditBox_Mode'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup ADD EditBox_Mode NVARCHAR(20) NOT NULL DEFAULT 'text';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_Lookup_EditBoxMode' AND parent_object_id = OBJECT_ID('dbo.Ui_Field_Lookup')
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD CONSTRAINT CHK_Ui_Field_Lookup_EditBoxMode
            CHECK (EditBox_Mode IN ('text', 'code', 'code_text'));
END;
GO

-- Code_Field: cột lưu mã rút gọn trong result set
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field_Lookup') AND name = 'Code_Field'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup ADD Code_Field NVARCHAR(100) NULL;
END;
GO

-- DropDown_Width: chiều rộng dropdown popup (px)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field_Lookup') AND name = 'DropDown_Width'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup ADD DropDown_Width INT NULL;
END;
GO

-- DropDown_Height: chiều cao dropdown popup (px)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field_Lookup') AND name = 'DropDown_Height'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup ADD DropDown_Height INT NULL;
END;
GO

PRINT N'Migration 014 completed — Ui_Field_Lookup: 5 cột mới added (Reload, EditBoxMode, Code, Width, Height).';
GO
