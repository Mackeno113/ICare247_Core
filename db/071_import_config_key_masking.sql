-- =============================================================================
-- File    : 071_import_config_key_masking.sql
-- Database: ICare247_Config  (Config DB — metadata Ui_/Sys_)
-- Purpose : IMPORT-0 (Pha 2 — ADR-034 / spec 25 §11–§14). Cấu hình import Excel:
--           (1) Ui_View.Import_Key_Fields — CSV field-code làm KHOÁ GHÉP upsert (rỗng ⇒ insert-only).
--           (2) Sys_Column.Is_Log_Masked + Log_Mask_Mode — làm mờ cột nhạy cảm trong LOG import.
--           (3) Set Ui_Field_Lookup.Code_Field='Ma' cho field FK Ngân hàng (Field 34) — cầu Mã↔Id
--               (đang NULL → import/template không resolve được).
-- Note    : Idempotent (COL_LENGTH guard). ConfigSync đọc cột động (INFORMATION_SCHEMA) → thêm cột
--           KHÔNG ảnh hưởng đồng bộ; tenant thiếu cột thì bỏ qua.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §11 (khoá ghép) · §13 (masking). ADR-034.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ── (1) Ui_View.Import_Key_Fields ───────────────────────────────────────────
IF OBJECT_ID(N'dbo.Ui_View', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_View', 'Import_Key_Fields') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_View
            ADD Import_Key_Fields NVARCHAR(400) NULL;   -- CSV field-code, vd 'CongTy_Id,Ma'
        PRINT N'✔ Đã thêm Ui_View.Import_Key_Fields.';
    END
    ELSE
        PRINT N'• Ui_View.Import_Key_Fields đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_View chưa tồn tại.';
GO

-- ── (2) Sys_Column.Is_Log_Masked + Log_Mask_Mode ────────────────────────────
IF OBJECT_ID(N'dbo.Sys_Column', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Sys_Column', 'Is_Log_Masked') IS NULL
    BEGIN
        ALTER TABLE dbo.Sys_Column
            ADD Is_Log_Masked BIT NOT NULL
                CONSTRAINT DF_Sys_Column_Is_Log_Masked DEFAULT 0;
        PRINT N'✔ Đã thêm Sys_Column.Is_Log_Masked.';
    END
    ELSE
        PRINT N'• Sys_Column.Is_Log_Masked đã tồn tại — bỏ qua.';

    IF COL_LENGTH(N'dbo.Sys_Column', 'Log_Mask_Mode') IS NULL
    BEGIN
        ALTER TABLE dbo.Sys_Column
            ADD Log_Mask_Mode NVARCHAR(20) NULL;   -- Full (mặc định) | Partial | Hash; NULL ⇒ Full khi Is_Log_Masked=1
        PRINT N'✔ Đã thêm Sys_Column.Log_Mask_Mode.';
    END
    ELSE
        PRINT N'• Sys_Column.Log_Mask_Mode đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Sys_Column chưa tồn tại.';
GO

-- ── (3) Cầu Mã↔Id cho FK Ngân hàng (Field 34) ───────────────────────────────
--     CHỈ set khi field tồn tại + Code_Field đang NULL (không đè cấu hình đã có).
IF OBJECT_ID(N'dbo.Ui_Field_Lookup', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Ui_Field_Lookup
        SET Code_Field = N'Ma'
    WHERE Field_Id = 34
      AND (Code_Field IS NULL OR LTRIM(RTRIM(Code_Field)) = N'');

    IF @@ROWCOUNT > 0
        PRINT N'✔ Đã set Ui_Field_Lookup.Code_Field=''Ma'' cho Field 34.';
    ELSE
        PRINT N'• Field 34 không cần cập nhật (không tồn tại hoặc Code_Field đã có).';
END
GO

PRINT N'Migration 071 completed — cấu hình import (khoá ghép + masking + cầu Mã↔Id).';
GO
