-- =============================================================================
-- File    : 059_alter_ui_view_filter_cascade.sql
-- Database: ICare247_Config
-- Purpose : Bộ lọc liên kết (cascade) + đổ giá trị sang form Thêm mới — mở rộng
--           Ui_View_Filter (ADR-030, nối ADR-016). Thêm 3 cột:
--             1) Depends_On       — CSV Filter_Code cha (cascade nạp lại options con).
--             2) Default_To_Field — Field_Code trên form Thêm/Sửa nhận giá trị filter.
--             3) Default_Lock     — 1 = khóa (đổ sẵn, không cho sửa); 0 = cho sửa lại.
-- Note    : Idempotent — guard COL_LENGTH. Chạy lại an toàn.
--           Cascade/scope dùng token ngữ cảnh trong Lookup_Sql (xem db/060 + spec 19).
--           Whitelist bind: (Sys_Context_Param) ∪ (param khai trong Ui_View_Filter).
-- Spec    : docs/spec/14_VIEW_CONFIG_SPEC §10 · docs/spec/19_CONTEXT_PARAM_SPEC · ADR-030.
-- =============================================================================

USE [ICare247_Config];
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Depends_On — CSV các Filter_Code cha mà control này phụ thuộc (cascade).
--    NULL = độc lập. Cha đổi giá trị → engine nạp lại options control con.
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.Ui_View_Filter', 'Depends_On') IS NULL
    ALTER TABLE dbo.Ui_View_Filter ADD Depends_On NVARCHAR(255) NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Default_To_Field — Field_Code trên form (Edit_Form_Id) nhận giá trị filter
--    hiện tại khi bấm Thêm mới. NULL = không prefill.
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.Ui_View_Filter', 'Default_To_Field') IS NULL
    ALTER TABLE dbo.Ui_View_Filter ADD Default_To_Field NVARCHAR(100) NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Default_Lock — khi prefill: 1 = field read-only (khóa giá trị); 0 = cho sửa lại.
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.Ui_View_Filter', 'Default_Lock') IS NULL
    ALTER TABLE dbo.Ui_View_Filter ADD Default_Lock BIT NOT NULL DEFAULT 0;
GO

PRINT N'Migration 059 completed — Ui_View_Filter + Depends_On / Default_To_Field / Default_Lock.';
GO
