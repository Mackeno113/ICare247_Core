-- =============================================================================
-- File    : 073_seed_import_messages.sql
-- Database: ICare247_Config  (Config DB — Sys_Resource i18n)
-- Purpose : IMPORT-4 — seed thông báo lỗi import (ADR-029 error_key + token {0}/{1}).
--           Engine trả error_key + args; handler resolve text qua IConfigCache.ResolveKeyAsync.
-- Token   : {0} = tham số thứ nhất · {1} = thứ hai (theo thứ tự args engine phát).
-- Note    : Idempotent (MERGE theo Resource_Key + Lang_Code). Sync xuống tenant qua config-sync.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §11.2. ADR-034.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Sys_Resource', N'U') IS NULL
BEGIN
    PRINT N'⚠ Bỏ qua: dbo.Sys_Resource chưa tồn tại.';
    RETURN;
END
GO

DECLARE @seed TABLE (K NVARCHAR(200), L NVARCHAR(10), V NVARCHAR(400));

INSERT INTO @seed (K, L, V) VALUES
    (N'import.file.invalid',      N'vi', N'Tệp không hợp lệ hoặc không đọc được.'),
    (N'import.file.invalid',      N'en', N'The file is invalid or unreadable.'),
    (N'import.column.missing',    N'vi', N'Thiếu cột bắt buộc: {0}'),
    (N'import.column.missing',    N'en', N'Missing required column: {0}'),
    (N'import.fk.no_code_field',  N'vi', N'Cột {0} chưa cấu hình Mã tham chiếu.'),
    (N'import.fk.no_code_field',  N'en', N'Column {0} has no reference Code configured.'),
    (N'import.required.missing',  N'vi', N'{0} là bắt buộc.'),
    (N'import.required.missing',  N'en', N'{0} is required.'),
    (N'import.fk.code_not_found', N'vi', N'{0}: mã "{1}" không tồn tại hoặc ngoài phạm vi cho phép.'),
    (N'import.fk.code_not_found', N'en', N'{0}: code "{1}" not found or out of allowed scope.'),
    (N'import.format.invalid',    N'vi', N'{0}: giá trị "{1}" sai định dạng.'),
    (N'import.format.invalid',    N'en', N'{0}: value "{1}" has an invalid format.'),
    (N'import.duplicate.key',     N'vi', N'Trùng khóa ({0}) trong tệp.'),
    (N'import.duplicate.key',     N'en', N'Duplicate key ({0}) in the file.');

MERGE dbo.Sys_Resource AS t
USING @seed AS s
    ON t.Resource_Key = s.K AND t.Lang_Code = s.L
WHEN MATCHED AND ISNULL(t.Resource_Value, N'') <> s.V THEN
    UPDATE SET Resource_Value = s.V, Updated_At = GETDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value)
    VALUES (s.K, s.L, s.V);

PRINT N'Migration 073 completed — seed thông báo import.* (vi/en).';
GO
