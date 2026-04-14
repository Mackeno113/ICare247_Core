-- =============================================================================
-- File    : 007_alter_ui_field_add_cols.sql
-- Purpose : Thêm 3 cột vào Ui_Field:
--           - Col_Span      (tinyint, 1-3 grid columns, default 1)
--           - Lookup_Source (nvarchar(20), 'static'/'dynamic'/NULL)
--           - Lookup_Code   (nvarchar(50), tham chiếu Sys_Lookup.Lookup_Code)
-- Note    : Col_Span ban đầu là 1-3 (sẽ đổi sang 1-4 trong migration 013).
-- =============================================================================

USE [ICare247_Config];
GO

-- ── Col_Span ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Col_Span'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Col_Span TINYINT NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_ColSpan' AND parent_object_id = OBJECT_ID('dbo.Ui_Field')
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_ColSpan CHECK (Col_Span BETWEEN 1 AND 3);
END;
GO

-- ── Lookup_Source ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Lookup_Source'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Lookup_Source NVARCHAR(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_LookupSource' AND parent_object_id = OBJECT_ID('dbo.Ui_Field')
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_LookupSource
            CHECK (Lookup_Source IN ('static', 'dynamic') OR Lookup_Source IS NULL);
END;
GO

-- ── Lookup_Code ───────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Ui_Field') AND name = 'Lookup_Code'
)
BEGIN
    ALTER TABLE dbo.Ui_Field ADD Lookup_Code NVARCHAR(50) NULL;
END;
GO

-- Consistency: static → Lookup_Code NOT NULL; dynamic/NULL → Lookup_Code NULL
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Ui_Field_LookupConsistency' AND parent_object_id = OBJECT_ID('dbo.Ui_Field')
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_LookupConsistency
            CHECK (
                (Lookup_Source = 'static' AND Lookup_Code IS NOT NULL)
                OR (Lookup_Source <> 'static' AND Lookup_Code IS NULL)
                OR Lookup_Source IS NULL
            );
END;
GO

PRINT N'Migration 007 completed — Ui_Field: Col_Span, Lookup_Source, Lookup_Code added.';
GO
