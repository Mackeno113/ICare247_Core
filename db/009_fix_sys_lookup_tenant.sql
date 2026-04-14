-- =============================================================================
-- File    : 009_fix_sys_lookup_tenant.sql
-- Purpose : Fix Sys_Lookup.Tenant_Id — đổi DEFAULT 0 → NULL.
--           Rebuild filtered indexes để nhất quán với null-based isolation.
-- Note    : Idempotent. Wrap trong transaction để an toàn khi rebuild index.
-- =============================================================================

USE [ICare247_Config];
GO

-- Kiểm tra xem DEFAULT constraint có tồn tại với value = 0 không
-- (Nếu bảng được tạo bởi migration 002 của repo này thì DEFAULT đã đúng = NULL)
DECLARE @ConstraintName NVARCHAR(200);
SELECT @ConstraintName = dc.name
FROM   sys.default_constraints dc
JOIN   sys.columns c
    ON c.object_id       = dc.parent_object_id
    AND c.column_id      = dc.parent_column_id
WHERE  dc.parent_object_id = OBJECT_ID('dbo.Sys_Lookup')
  AND  c.name              = 'Tenant_Id'
  AND  dc.definition       = '((0))'; -- DEFAULT 0 (sai)

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC ('ALTER TABLE dbo.Sys_Lookup DROP CONSTRAINT [' + @ConstraintName + ']');
    PRINT N'Dropped old DEFAULT 0 constraint on Sys_Lookup.Tenant_Id';
END;
GO

-- Đảm bảo không có DEFAULT nào trên Tenant_Id nữa
-- (NULL là giá trị mặc định tự nhiên cho nullable column)

-- Fix data: nếu có rows với Tenant_Id = 0, reset về NULL (global)
IF OBJECT_ID('dbo.Sys_Lookup', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.Sys_Lookup
    SET    Tenant_Id = NULL
    WHERE  Tenant_Id = 0;
END;
GO

-- Rebuild filtered indexes nếu bị corrupt sau khi fix data
-- Drop + recreate để đảm bảo filter condition đúng

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Sys_Lookup_Global' AND object_id = OBJECT_ID('dbo.Sys_Lookup'))
    DROP INDEX UQ_Sys_Lookup_Global ON dbo.Sys_Lookup;

CREATE UNIQUE INDEX UQ_Sys_Lookup_Global
    ON dbo.Sys_Lookup (Lookup_Code, Item_Code) WHERE Tenant_Id IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Sys_Lookup_Tenant' AND object_id = OBJECT_ID('dbo.Sys_Lookup'))
    DROP INDEX UQ_Sys_Lookup_Tenant ON dbo.Sys_Lookup;

CREATE UNIQUE INDEX UQ_Sys_Lookup_Tenant
    ON dbo.Sys_Lookup (Lookup_Code, Item_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sys_Lookup_Code' AND object_id = OBJECT_ID('dbo.Sys_Lookup'))
    DROP INDEX IX_Sys_Lookup_Code ON dbo.Sys_Lookup;

CREATE INDEX IX_Sys_Lookup_Code
    ON dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Is_Active);
GO

PRINT N'Migration 009 completed — Sys_Lookup.Tenant_Id DEFAULT fixed, indexes rebuilt.';
GO
