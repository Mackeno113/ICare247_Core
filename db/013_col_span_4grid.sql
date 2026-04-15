-- =============================================================================
-- File    : 013_col_span_4grid.sql
-- Purpose : Đổi Col_Span từ 3-column sang 4-column grid (ADR-013).
--           Mapping: cũ Col_Span=3 (full width) → mới Col_Span=4 (full width).
--           Thêm giá trị 3 = 3/4 width (mới hoàn toàn).
--           Blazor CSS: grid-template-columns: repeat(4, 1fr) + grid-column: span X.
-- =============================================================================

USE [ICare247_Config];
GO

-- BƯỚC 1: Drop constraint cũ (1-3)
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_ColSpan' AND parent_object_id = OBJECT_ID('dbo.Ui_Field')
)
BEGIN
    ALTER TABLE dbo.Ui_Field DROP CONSTRAINT CHK_Ui_Field_ColSpan;
END;
GO

-- BƯỚC 2: Migrate data — cũ 3 (full) → mới 4 (full)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Col_Span')
BEGIN
    UPDATE dbo.Ui_Field SET Col_Span = 4 WHERE Col_Span = 3;
END;
GO

-- BƯỚC 3: Thêm constraint mới (1-4)
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_ColSpan' AND parent_object_id = OBJECT_ID('dbo.Ui_Field')
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_ColSpan CHECK (Col_Span BETWEEN 1 AND 4);
END;
GO

PRINT N'Migration 013 completed — Col_Span: 3-col → 4-col grid (ADR-013). Data migrated.';
GO
