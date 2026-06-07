-- =============================================================================
-- File    : 027_ui_form_layout.sql
-- Purpose : Cấu hình layout per-form — bề rộng tối đa + số cột lưới.
--           Max_Width    : px, NULL = mặc định (FormRunner dùng 880).
--           Form_Columns : số cột lưới nền (1..4), NULL = mặc định 4.
--                          = 1 → mỗi field 1 dòng.
-- =============================================================================

USE [ICare247_Config];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Form') AND name = 'Max_Width'
)
BEGIN
    ALTER TABLE dbo.Ui_Form ADD Max_Width INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Form') AND name = 'Form_Columns'
)
BEGIN
    ALTER TABLE dbo.Ui_Form ADD Form_Columns TINYINT NULL;
END;
GO

-- Ràng buộc số cột hợp lệ 1..4 (chỉ thêm nếu chưa có)
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Form_Columns' AND parent_object_id = OBJECT_ID('dbo.Ui_Form')
)
BEGIN
    ALTER TABLE dbo.Ui_Form
        ADD CONSTRAINT CHK_Ui_Form_Columns
            CHECK (Form_Columns IS NULL OR Form_Columns BETWEEN 1 AND 4);
END;
GO

PRINT N'Migration 027 completed — Ui_Form.Max_Width + Form_Columns added.';
GO
