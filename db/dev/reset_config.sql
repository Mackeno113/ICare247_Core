-- =============================================================================
-- File    : db/dev/reset_config.sql   (DEV ONLY — KHÔNG phải migration đánh số)
-- Database: ICare247_Config
-- Purpose : Xóa sạch CẤU HÌNH THIẾT KẾ (Sys_Table/Sys_Column/Sys_Relation, Ui_*,
--           Val_Rule, Evt_* config) để dựng lại từ đầu. KHÔNG hoàn tác.
-- Vì sao DELETE (không TRUNCATE): TRUNCATE lỗi 4712 trên bảng bị FK tham chiếu
--           (vd Ui_Form ← Ui_Field/Ui_Section/Ui_Tab/Evt_Definition/Sys_Dependency).
--           → DELETE theo thứ tự con → cha.
-- GIỮ NGUYÊN seed hệ thống: Sys_Tenant, Sys_Language, Ui_Control_Map, Val_Rule_Type,
--           Gram_*, Evt_Trigger_Type, Evt_Action_Type, Sys_Role, Sys_Config.
-- Dry-run : đổi COMMIT → ROLLBACK ở cuối để chạy thử (không xóa thật).
-- =============================================================================
USE [ICare247_Config];
GO
SET XACT_ABORT ON;
BEGIN TRAN;

-- 1) Event config (FK → Ui_Form/Ui_Field) — xóa trước Ui_*
IF OBJECT_ID('dbo.Evt_Execution_Log','U') IS NOT NULL DELETE FROM dbo.Evt_Execution_Log;
IF OBJECT_ID('dbo.Evt_Action','U')        IS NOT NULL DELETE FROM dbo.Evt_Action;
IF OBJECT_ID('dbo.Evt_Definition','U')    IS NOT NULL DELETE FROM dbo.Evt_Definition;
IF OBJECT_ID('dbo.Sys_Dependency','U')    IS NOT NULL DELETE FROM dbo.Sys_Dependency;

-- 2) View config (FK → Ui_View/Sys_Table/Ui_Form)
IF OBJECT_ID('dbo.Ui_View_Action','U') IS NOT NULL DELETE FROM dbo.Ui_View_Action;
IF OBJECT_ID('dbo.Ui_View_Column','U') IS NOT NULL DELETE FROM dbo.Ui_View_Column;
IF OBJECT_ID('dbo.Ui_View_Filter','U') IS NOT NULL DELETE FROM dbo.Ui_View_Filter;
IF OBJECT_ID('dbo.Ui_View','U')        IS NOT NULL DELETE FROM dbo.Ui_View;

-- 3) Rule + Field-lookup config (FK → Ui_Field) — xóa trước Ui_Field
IF OBJECT_ID('dbo.Val_Rule','U')        IS NOT NULL DELETE FROM dbo.Val_Rule;
IF OBJECT_ID('dbo.Ui_Field_Lookup','U') IS NOT NULL DELETE FROM dbo.Ui_Field_Lookup;

-- 4) Form tree: Field → Section/Tab → Form
IF OBJECT_ID('dbo.Ui_Field','U')   IS NOT NULL DELETE FROM dbo.Ui_Field;
IF OBJECT_ID('dbo.Ui_Section','U') IS NOT NULL DELETE FROM dbo.Ui_Section;
IF OBJECT_ID('dbo.Ui_Tab','U')     IS NOT NULL DELETE FROM dbo.Ui_Tab;
IF OBJECT_ID('dbo.Ui_Form','U')    IS NOT NULL DELETE FROM dbo.Ui_Form;

-- 5) Schema metadata: Relation/Column → Table
IF OBJECT_ID('dbo.Sys_Relation','U') IS NOT NULL DELETE FROM dbo.Sys_Relation;
IF OBJECT_ID('dbo.Sys_Column','U')   IS NOT NULL DELETE FROM dbo.Sys_Column;
IF OBJECT_ID('dbo.Sys_Table','U')    IS NOT NULL DELETE FROM dbo.Sys_Table;

-- 6) Log đồng bộ config
IF OBJECT_ID('dbo.Sys_Config_Sync_Log','U') IS NOT NULL DELETE FROM dbo.Sys_Config_Sync_Log;

-- 7) (TÙY CHỌN) i18n + lookup — chứa CẢ seed dùng chung. Bỏ comment nếu muốn xóa luôn.
--    Khôi phục seed sau khi xóa: chạy lại db/001, db/004, db/033.
-- IF OBJECT_ID('dbo.Sys_Resource','U') IS NOT NULL DELETE FROM dbo.Sys_Resource;
-- IF OBJECT_ID('dbo.Sys_Lookup','U')   IS NOT NULL DELETE FROM dbo.Sys_Lookup;

-- 8) Reset IDENTITY về 0 (Id mới bắt đầu từ 1) — chỉ các bảng vừa xóa
DBCC CHECKIDENT('dbo.Sys_Table',      RESEED, 0);
DBCC CHECKIDENT('dbo.Sys_Column',     RESEED, 0);
DBCC CHECKIDENT('dbo.Sys_Relation',   RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_Form',        RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_Section',     RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_Field',       RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_View',        RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_View_Column', RESEED, 0);
DBCC CHECKIDENT('dbo.Ui_View_Action', RESEED, 0);
DBCC CHECKIDENT('dbo.Val_Rule',       RESEED, 0);

COMMIT;   -- ⚠️ Đổi thành ROLLBACK; để chạy thử (dry-run) trước khi xóa thật.
GO
PRINT N'Đã xóa sạch cấu hình thiết kế Config DB (giữ seed hệ thống).';
GO
