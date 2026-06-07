-- =============================================================================
-- File    : 028_ui_form_title_key.sql
-- Purpose : Thêm Ui_Form.Title_Key — tiêu đề hiển thị form (đa ngôn ngữ).
--           Convention: {table_code}.form.title (xem docs/spec/10_RESOURCE_KEY_CONVENTION.md).
--           Resolve qua Sys_Resource theo Lang_Code khi runtime.
-- =============================================================================

USE [ICare247_Config];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Form') AND name = 'Title_Key'
)
BEGIN
    ALTER TABLE dbo.Ui_Form ADD Title_Key NVARCHAR(150) NULL;
END;
GO

PRINT N'Migration 028 completed — Ui_Form.Title_Key added.';
GO
