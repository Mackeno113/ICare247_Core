-- =============================================================================
-- File    : 006_alter_ui_section_add_tab.sql
-- Purpose : Thêm cột Tab_Id vào Ui_Section — gắn section vào tab.
--           NULL = form không dùng tab (backward compat).
-- =============================================================================

USE [ICare247_Config];
GO

-- Thêm cột Tab_Id (nullable)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Section') AND name = 'Tab_Id'
)
BEGIN
    ALTER TABLE dbo.Ui_Section ADD Tab_Id INT NULL;
END;
GO

-- FK Ui_Section → Ui_Tab
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Ui_Section_Tab' AND parent_object_id = OBJECT_ID('dbo.Ui_Section')
)
BEGIN
    ALTER TABLE dbo.Ui_Section
        ADD CONSTRAINT FK_Ui_Section_Tab
            FOREIGN KEY (Tab_Id) REFERENCES dbo.Ui_Tab (Tab_Id);
END;
GO

-- Index query sections theo tab
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Ui_Section_Tab' AND object_id = OBJECT_ID('dbo.Ui_Section')
)
BEGIN
    CREATE INDEX IX_Ui_Section_Tab
        ON dbo.Ui_Section (Tab_Id, Is_Active, Order_No) WHERE Tab_Id IS NOT NULL;
END;
GO

PRINT N'Migration 006 completed — Ui_Section.Tab_Id added.';
GO
