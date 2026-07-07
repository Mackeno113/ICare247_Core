-- =============================================================================
-- File    : 074_alter_ui_field_lookup_import_global_code.sql
-- Database: ICare247_Config  (Config DB — metadata Ui_/Sys_)
-- Purpose : IMPORT (Phương án B — ADR-034) — cờ per-FK Ui_Field_Lookup.Import_Global_Code:
--           khi import, BỎ Filter_Sql (lọc cha cascade) → tra Mã con trên TOÀN bảng.
--           Chỉ bật cho FK có Mã con DUY NHẤT toàn cục (vd chi nhánh); trùng Mã ⇒ engine từ chối
--           (import.fk.ambiguous_code). + seed thông báo lỗi.
-- Note    : Idempotent (COL_LENGTH guard). ConfigSync đọc cột động → không ảnh hưởng đồng bộ.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §11 · ADR-034.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Ui_Field_Lookup', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_Field_Lookup', 'Import_Global_Code') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_Field_Lookup
            ADD Import_Global_Code BIT NOT NULL
                CONSTRAINT DF_Ui_Field_Lookup_Import_Global_Code DEFAULT 0;
        PRINT N'✔ Đã thêm Ui_Field_Lookup.Import_Global_Code.';
    END
    ELSE
        PRINT N'• Ui_Field_Lookup.Import_Global_Code đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_Field_Lookup chưa tồn tại.';
GO

-- Seed thông báo lỗi Mã nhập nhằng (resolve toàn cục nhưng Mã con trùng nhiều Id).
IF OBJECT_ID(N'dbo.Sys_Resource', N'U') IS NOT NULL
BEGIN
    DECLARE @seed TABLE (K NVARCHAR(200), L NVARCHAR(10), V NVARCHAR(400));
    INSERT INTO @seed (K, L, V) VALUES
        (N'import.fk.ambiguous_code', N'vi', N'Cột {0}: Mã bị trùng nhiều bản ghi (resolve toàn cục) — tắt "Mã toàn cục" hoặc dùng Mã duy nhất.'),
        (N'import.fk.ambiguous_code', N'en', N'Column {0}: code maps to multiple records (global resolve) — disable "global code" or use unique codes.');

    MERGE dbo.Sys_Resource AS t
    USING @seed AS s ON t.Resource_Key = s.K AND t.Lang_Code = s.L
    WHEN MATCHED AND ISNULL(t.Resource_Value, N'') <> s.V THEN
        UPDATE SET Resource_Value = s.V, Updated_At = GETDATE()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Resource_Key, Lang_Code, Resource_Value) VALUES (s.K, s.L, s.V);
END
GO

PRINT N'Migration 074 completed — Import_Global_Code + i18n import.fk.ambiguous_code.';
GO
