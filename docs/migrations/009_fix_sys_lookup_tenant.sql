-- ============================================================
-- Migration 009: Fix Sys_Lookup.Tenant_Id — đổi DEFAULT 0
--                sang NULL = global (nhất quán với toàn hệ thống)
-- Vấn đề    : Migration 004 tạo Sys_Lookup với Tenant_Id DEFAULT 0
--             (0 = global). Toàn bộ hệ thống còn lại dùng NULL = global
--             (Sys_Table, Sys_Config, Sys_Role,...) → không nhất quán.
-- Thực hiện : Chạy trong transaction. Rollback nếu lỗi.
-- Tác động  : Data cũ Tenant_Id = 0 → đổi thành NULL
--             Unique constraint + index được tạo lại theo pattern
--             filtered index (giống Sys_Table, Sys_Config, Sys_Role)
-- Ngày      : 2026-03-25
-- ============================================================

BEGIN TRANSACTION;

BEGIN TRY

    -- ── Bước 1: Xóa index + constraint cũ phụ thuộc vào cột ──
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Sys_Lookup')
          AND name = N'IX_Sys_Lookup_Code'
    )
        DROP INDEX IX_Sys_Lookup_Code ON dbo.Sys_Lookup;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Sys_Lookup')
          AND name = N'UQ_Sys_Lookup'
    )
        ALTER TABLE dbo.Sys_Lookup DROP CONSTRAINT UQ_Sys_Lookup;

    -- ── Bước 2: Xóa default constraint DF_Sys_Lookup_Tenant ──
    IF EXISTS (
        SELECT 1 FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Sys_Lookup')
          AND name = N'DF_Sys_Lookup_Tenant'
    )
        ALTER TABLE dbo.Sys_Lookup
            DROP CONSTRAINT DF_Sys_Lookup_Tenant;

    -- ── Bước 3: Migrate data 0 → NULL (global records) ───────
    -- Tenant_Id = 0 là convention cũ cho "global"
    -- Sau migration: NULL = global (nhất quán với toàn hệ thống)
    UPDATE dbo.Sys_Lookup
    SET    Tenant_Id = NULL
    WHERE  Tenant_Id = 0;

    -- ── Bước 4: Đổi cột thành nullable + thêm FK ─────────────
    ALTER TABLE dbo.Sys_Lookup
        ALTER COLUMN Tenant_Id int NULL;

    -- Chỉ thêm FK nếu Sys_Tenant đã tồn tại
    IF OBJECT_ID(N'dbo.Sys_Tenant') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.Sys_Lookup')
              AND name = N'FK_Sys_Lookup_Tenant'
        )
    BEGIN
        ALTER TABLE dbo.Sys_Lookup
            ADD CONSTRAINT FK_Sys_Lookup_Tenant
                FOREIGN KEY (Tenant_Id) REFERENCES dbo.Sys_Tenant (Tenant_Id);
    END

    -- ── Bước 5: Tạo lại unique constraints theo filtered pattern
    -- Giống Sys_Table, Sys_Config, Sys_Role: tách global / per-tenant
    CREATE UNIQUE INDEX UQ_Sys_Lookup_Global
        ON dbo.Sys_Lookup (Lookup_Code, Item_Code)
        WHERE Tenant_Id IS NULL;

    CREATE UNIQUE INDEX UQ_Sys_Lookup_Tenant
        ON dbo.Sys_Lookup (Lookup_Code, Item_Code, Tenant_Id)
        WHERE Tenant_Id IS NOT NULL;

    -- ── Bước 6: Tạo lại index query ──────────────────────────
    CREATE INDEX IX_Sys_Lookup_Code
        ON dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Is_Active);

    COMMIT TRANSACTION;
    PRINT N'Migration 009 hoàn thành: Sys_Lookup.Tenant_Id đã đổi sang NULL = global.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @err nvarchar(max) = ERROR_MESSAGE();
    RAISERROR(N'Migration 009 thất bại — rollback: %s', 16, 1, @err);
END CATCH
GO
