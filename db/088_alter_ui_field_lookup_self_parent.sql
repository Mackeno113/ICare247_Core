-- =============================================================================
-- File    : 088_alter_ui_field_lookup_self_parent.sql
-- Database: ICare247_Config (Config DB)
-- Purpose : Feature B (bộ control TreeList/Lookup dùng chung) — thêm Query_Mode mới
--           'self_parent' vào Ui_Field_Lookup: LookupBox chọn "cha" trong CHÍNH bảng của field
--           (vd PhongBan_Cha_Id → chọn trong TC_PhongBan), engine tự loại chính bản ghi đang sửa
--           + mọi hậu duệ của nó (chống tạo vòng lặp cây). Operator vẫn cấu hình Source_Name/
--           Value_Column/Display_Column/Filter_Sql NHƯ BÌNH THƯỜNG (table-mode) — chỉ đổi
--           Query_Mode để bật hành vi loại-trừ tự động, KHÔNG đổi cấu trúc cột nào khác.
-- Spec    : .claude-rules/database-design.md · db/008 (Ui_Field_Lookup gốc).
-- Idempotent: drop + re-add CHECK constraint theo tên cố định.
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID(N'dbo.Ui_Field_Lookup', N'U') IS NULL
BEGIN
    RAISERROR(N'Ui_Field_Lookup chưa tồn tại — chạy migration 008 trước.', 16, 1);
    RETURN;
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Ui_Field_Lookup_QueryMode')
    ALTER TABLE dbo.Ui_Field_Lookup DROP CONSTRAINT CHK_Ui_Field_Lookup_QueryMode;
GO

ALTER TABLE dbo.Ui_Field_Lookup
    ADD CONSTRAINT CHK_Ui_Field_Lookup_QueryMode
    CHECK (Query_Mode IN ('table', 'tvf', 'custom_sql', 'self_parent'));
GO

PRINT N'Migration 088 completed — Ui_Field_Lookup.Query_Mode cho phép self_parent.';
GO
