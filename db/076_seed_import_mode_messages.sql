-- =============================================================================
-- File    : 076_seed_import_mode_messages.sql
-- Database: ICare247_Config  (Config DB — Sys_Resource i18n)
-- Purpose : IMPORT — thông báo lỗi cho chế độ import (Chỉ cập nhật / Chỉ thêm).
--           import.key.not_found: chế độ "Chỉ cập nhật" nhưng Mã chưa tồn tại.
--           import.key.exists   : chế độ "Chỉ thêm" nhưng Mã đã tồn tại.
-- Note    : Idempotent (MERGE theo Resource_Key + Lang_Code). Sync xuống tenant qua config-sync.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §11. ADR-034.
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
    (N'import.key.not_found', N'vi', N'Mã chưa tồn tại — chế độ "Chỉ cập nhật" không thêm mới.'),
    (N'import.key.not_found', N'en', N'Code not found — "Update only" mode does not insert.'),
    (N'import.key.exists',    N'vi', N'Mã đã tồn tại — chế độ "Chỉ thêm mới" không cập nhật.'),
    (N'import.key.exists',    N'en', N'Code already exists — "Insert only" mode does not update.');

MERGE dbo.Sys_Resource AS t
USING @seed AS s ON t.Resource_Key = s.K AND t.Lang_Code = s.L
WHEN MATCHED AND ISNULL(t.Resource_Value, N'') <> s.V THEN
    UPDATE SET Resource_Value = s.V, Updated_At = GETDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value) VALUES (s.K, s.L, s.V);

PRINT N'Migration 076 completed — seed import.key.not_found / import.key.exists.';
GO
