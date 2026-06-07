-- =============================================================================
-- File    : 026_fix_sys_language_vietnamese.sql
-- Purpose : Sửa dữ liệu Sys_Language.Lang_Name bị mất dấu tiếng Việt ("Ti?ng Vi?t").
--           Nguyên nhân: seed cũ chạy qua pipeline không UTF-8 → Unicode→varchar mất ký tự.
--
-- An toàn : Dựng chuỗi bằng NCHAR(<code point>) → file chỉ chứa ASCII → KHÔNG phụ thuộc
--           encoding khi chạy (sqlcmd/SSMS kiểu gì cũng ra đúng). Idempotent.
--           ế = U+1EBF, ệ = U+1EC7.
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Sys_Language', 'U') IS NOT NULL
BEGIN
    -- "Tiếng Việt" = Ti + ế + ng Vi + ệ + t
    UPDATE dbo.Sys_Language
    SET    Lang_Name = N'Ti' + NCHAR(0x1EBF) + N'ng Vi' + NCHAR(0x1EC7) + N't'
    WHERE  Lang_Code = N'vi';

    UPDATE dbo.Sys_Language
    SET    Lang_Name = N'English'
    WHERE  Lang_Code = N'en';

    PRINT N'Migration 026 completed — Sys_Language.Lang_Name (vi) restored.';
END;
GO
