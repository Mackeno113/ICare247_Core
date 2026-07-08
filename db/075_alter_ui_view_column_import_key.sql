-- =============================================================================
-- File    : 075_alter_ui_view_column_import_key.sql
-- Database: ICare247_Config  (Config DB — metadata Ui_)
-- Purpose : IMPORT — cờ per-cột Ui_View_Column.Is_Import_Key: các cột tick = KHÓA GHÉP
--           kiểm trùng khi import (upsert). Thay cho Ui_View.Import_Key_Fields (CSV) — cấu hình
--           bằng checkbox mỗi cột ở tab Cột (ConfigStudio Quản lý View), tick nhiều cột = khóa ghép.
-- Note    : Idempotent (COL_LENGTH guard). Ui_View.Import_Key_Fields (db/071) để lại — không dùng nữa.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §11.3. ADR-034.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Ui_View_Column', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_View_Column', 'Is_Import_Key') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_View_Column
            ADD Is_Import_Key BIT NOT NULL
                CONSTRAINT DF_Ui_View_Column_Is_Import_Key DEFAULT 0;
        PRINT N'✔ Đã thêm Ui_View_Column.Is_Import_Key.';
    END
    ELSE
        PRINT N'• Ui_View_Column.Is_Import_Key đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_View_Column chưa tồn tại.';
GO

PRINT N'Migration 075 completed — cờ Is_Import_Key (khóa ghép import per-cột).';
GO
