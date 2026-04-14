-- =============================================================================
-- File    : 010_field_behavior_columns.sql
-- Purpose : Thêm Is_Required và Is_Enabled vào Ui_Field.
--           ADR-010: cả 4 behavior flags (Is_Visible, Is_ReadOnly, Is_Required,
--           Is_Enabled) phải là cột DB tĩnh — đồng nhất thiết kế.
-- Note    : Is_Required = true nghĩa là field luôn bắt buộc (tĩnh).
--           SET_REQUIRED event = dynamic (thay đổi theo runtime condition).
--           Is_Enabled = false → grayout, KHÔNG submit lên server.
-- =============================================================================

USE [ICare247_Config];
GO

-- Is_Required: bắt buộc nhập — cột DB, không phải Val_Rule
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Is_Required'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Is_Required BIT NOT NULL DEFAULT 0;
END;
GO

-- Is_Enabled: false = grayout, không tương tác, KHÔNG submit
-- Khác với Is_ReadOnly (hiển thị giá trị, vẫn submit)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Is_Enabled'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Is_Enabled BIT NOT NULL DEFAULT 1;
END;
GO

PRINT N'Migration 010 completed — Ui_Field: Is_Required, Is_Enabled added (ADR-010).';
GO
