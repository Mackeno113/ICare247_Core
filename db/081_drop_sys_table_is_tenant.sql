-- =============================================================================
-- File    : 081_drop_sys_table_is_tenant.sql
-- Database: ICare247_Config
-- Purpose : Gỡ HẲN cột Sys_Table.Is_Tenant — cột thừa (vestigial) sau ADR-035/ADR-018.
--           DB-per-tenant: cả Config/Data DB đã thuộc 1 tenant nên cờ "bảng có dữ liệu
--           riêng theo tenant" không phân biệt được gì. Khảo sát live 2026-07-11:
--           11/11 bảng Is_Tenant=1 → mọi filter `Is_Tenant=1` đều lọc rỗng (no-op).
--           Backend/API: 0 tham chiếu. ConfigStudio: đã gỡ UI + filter (deploy TRƯỚC migration).
-- Thứ tự  : DROP DEFAULT constraint (auto-name) → DROP COLUMN. Idempotent.
-- Tiền đề : Chạy SAU khi deploy ConfigStudio đã bỏ mọi tham chiếu Is_Tenant.
-- Spec    : .claude-rules/database-design.md §1.1 · ADR-035 · ADR-018.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Sys_Table', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.Sys_Table', N'Is_Tenant') IS NOT NULL
    BEGIN
        DECLARE @sql nvarchar(max);

        -- 1) DROP DEFAULT constraint trên Is_Tenant (tên auto DF__Sys_Table__Is_Te__...)
        SELECT @sql = N'ALTER TABLE dbo.Sys_Table DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM   sys.default_constraints dc
        JOIN   sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE  dc.parent_object_id = OBJECT_ID(N'dbo.Sys_Table') AND c.name = N'Is_Tenant';

        IF @sql IS NOT NULL
        BEGIN
            EXEC sys.sp_executesql @sql;
            PRINT N'✔ Đã gỡ DEFAULT constraint trên Is_Tenant.';
        END

        -- 2) DROP COLUMN
        ALTER TABLE dbo.Sys_Table DROP COLUMN Is_Tenant;
        PRINT N'✔ Đã DROP COLUMN Sys_Table.Is_Tenant.';
    END
    ELSE
        PRINT N'• Sys_Table.Is_Tenant không tồn tại — bỏ qua (idempotent).';

    COMMIT TRANSACTION;
    PRINT N'════ 081 hoàn tất — Sys_Table không còn cột Is_Tenant. ════';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    PRINT N'✖ 081 THẤT BẠI — đã ROLLBACK, DB giữ nguyên.';
    THROW;
END CATCH
GO
