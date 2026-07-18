-- =============================================================================
-- File    : 087_add_ui_view_scope_by_company.sql
-- Database: ICare247_Config (Config DB) — không cần đổi schema Data DB (fnt_CongTyTheoQuyen đã có
--           sẵn từ db/084, ViewRepository.GetDataAsync chỉ tham chiếu hàm này khi build WHERE).
-- Purpose : Feature A (bộ control TreeList/Lookup dùng chung) — cờ khai báo Grid/TreeList
--           tự lọc theo công ty: JOIN fnt_CongTyTheoQuyen(@NguoiDungID) (ranh giới quyền CỨNG)
--           + lọc @CongTyID_Active (company-switcher, MỀM). Chỉ hiệu lực khi bảng/view nguồn
--           có cột CongTy_Id — ViewRepository tự kiểm tra qua sys.columns, không có thì bỏ qua
--           (phòng thủ, không chặn màn).
--           Đồng thời fix Sys_Context_Param.CongTyID_Active.Validate_Sql: bản seed (db/060)
--           chỉ check gán riêng (HT_NguoiDung_CongTy), thiếu nhánh theo vai trò
--           (HT_VaiTro_CongTy) mà fnt_CongTyTheoQuyen (db/084) đã có → user chỉ có quyền qua
--           vai trò bị switcher từ chối ngầm (reset về 0). Dùng thẳng hàm đã có, khớp đúng
--           "luật duy nhất" một chỗ.
-- Spec    : .claude-rules/database-design.md · ADR-030 (Sys_Context_Param) · PICKER-P4 (fnt_CongTyTheoQuyen).
-- Idempotent.
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID(N'dbo.Ui_View', N'U') IS NULL
BEGIN
    RAISERROR(N'Ui_View chưa tồn tại — chạy migration 031 trước.', 16, 1);
    RETURN;
END;
GO

IF COL_LENGTH('dbo.Ui_View', 'Scope_By_Company') IS NULL
    ALTER TABLE dbo.Ui_View ADD Scope_By_Company BIT NOT NULL DEFAULT 0;
GO

IF EXISTS (SELECT 1 FROM dbo.Sys_Context_Param WHERE Param_Name = N'CongTyID_Active')
    UPDATE dbo.Sys_Context_Param
    SET Validate_Sql = N'SELECT 1 FROM dbo.fnt_CongTyTheoQuyen(@NguoiDungID) WHERE Id = @val'
    WHERE Param_Name = N'CongTyID_Active';
GO

PRINT N'Migration 087 (Config DB) completed — Ui_View.Scope_By_Company + fix Validate_Sql CongTyID_Active.';
GO
