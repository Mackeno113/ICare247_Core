-- =============================================================================
-- File    : 086_alter_ui_view_allow_reorder.sql
-- Database: ICare247_Config (Config DB)
-- Purpose : ADR-027 — thêm cờ Allow_Reorder vào Ui_View: bật kéo-thả sắp xếp cho
--           TreeList (DxTreeList AllowDragRows) khi View_Type=TreeList. Backend
--           dùng cờ này để cho phép/chặn endpoint /views/{code}/reorder.
-- Spec    : .claude-rules/database-design.md §6 (cây) · ADR-027.
-- Convention: Idempotent (COL_LENGTH guard).
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID(N'dbo.Ui_View', N'U') IS NULL
BEGIN
    RAISERROR(N'Ui_View chưa tồn tại — chạy migration 031 trước.', 16, 1);
    RETURN;
END;
GO

IF COL_LENGTH('dbo.Ui_View', 'Allow_Reorder') IS NULL
    ALTER TABLE dbo.Ui_View ADD Allow_Reorder BIT NOT NULL DEFAULT 0;
GO

PRINT N'Migration 086 completed — Ui_View thêm cột Allow_Reorder.';
GO
