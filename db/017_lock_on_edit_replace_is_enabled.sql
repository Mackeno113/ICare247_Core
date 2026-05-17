-- =============================================================================
-- File    : 017_lock_on_edit_replace_is_enabled.sql
-- Purpose : Bo Is_Enabled (overlap voi ReadOnly + Visible) va them Lock_On_Edit
--           cho case "field nhap luc tao, khoa khi sua" (key/code/audit field).
-- Note    : ADR-017 — sau khi review thuc te:
--           1. Is_Enabled gan nhu chi cover them 1 use case "khong submit khi khoa"
--           2. ICare247 chua co partial-update API (PATCH), full update se ghi de
--              null vao field disabled → khong an toan
--           3. % case thuc su can "khong submit khi khoa" = nho
--           → Bo Is_Enabled. Logic disable trong UI dung Event/Rule engine khi can.
--
--           Lock_On_Edit = 1 → field readonly khi FormMode=Edit (record da ton tai),
--           van editable khi FormMode=Create. Mac dinh 0 (khong khoa).
--           Effective ReadOnly = Is_ReadOnly OR (Lock_On_Edit AND FormMode=Edit).
-- =============================================================================

USE [ICare247_Config];
GO

-- 1. Drop Is_Enabled (rollback ADR-010 partially)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Is_Enabled'
)
BEGIN
    ALTER TABLE dbo.Ui_Field DROP COLUMN Is_Enabled;
    PRINT N'Dropped column Ui_Field.Is_Enabled.';
END;
GO

-- 2. Add Lock_On_Edit
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Lock_On_Edit'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Lock_On_Edit BIT NOT NULL DEFAULT 0;
    PRINT N'Added column Ui_Field.Lock_On_Edit (BIT NOT NULL DEFAULT 0).';
END;
GO

PRINT N'Migration 017 completed — Ui_Field: Is_Enabled removed, Lock_On_Edit added (ADR-017).';
GO
