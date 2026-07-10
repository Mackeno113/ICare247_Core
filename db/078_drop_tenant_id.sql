-- =============================================================================
-- File    : 078_drop_tenant_id.sql
-- Database: ICare247_Config
-- Purpose : ADR-035 — bỏ HẲN cột Tenant_Id khỏi Config DB. Tenant cô lập ở tầng
--           connection (ADR-018: mỗi tenant 1 Config DB riêng), nên cột định danh
--           tenant BÊN TRONG một DB đã-thuộc-1-tenant không phân biệt được gì.
--           Vai trò "bản ghi master vs tenant tùy biến" đã do ConfigSync đảm nhiệm
--           tường minh qua Is_System / Is_Customized / Source_Ver (db/050).
--
--           9 bảng: Sys_Table, Sys_Lookup, Ui_View, Sys_Config, Sys_Role,
--                   Sys_Menu, Sys_MenuCatalog, Doc_Template, Doc_Proc_Registry.
--           + DROP bảng Sys_Tenant (mồ côi sau khi gỡ 5 FK; resolver đọc
--             dbo.Tenant ở Catalog DB, KHÔNG đọc bảng này).
--
--           Thứ tự BẮT BUỘC: FK → filtered index → DEFAULT → cột → dựng lại UNIQUE.
--           Cặp UQ_*_Global (filtered WHERE Tenant_Id IS NULL) + UQ_*_Tenant
--           (filtered WHERE Tenant_Id IS NOT NULL) hợp nhất thành 1 UNIQUE thường.
--
-- Tiền đề : Khảo sát DB live 2026-07-10 — SoTenantKhacNhau = 1 ở mọi bảng; không có
--           va chạm (Lookup_Code, Item_Code) giữa dòng global và tenant.
--           Migration TỰ KIỂM lại (bước 0) và ROLLBACK nếu sai.
--
-- Note    : Idempotent. TOÀN BỘ nằm trong 1 batch + 1 transaction + TRY/CATCH —
--           KHÔNG chèn `GO` vào giữa, nếu không guard sẽ không chặn được các bước sau.
--           DDL dùng dynamic SQL để tránh lỗi biên dịch khi cột đã bị gỡ.
--           Chạy SAU khi deploy code đã gỡ mọi predicate Tenant_Id.
-- Spec    : .claude-rules/database-design.md §1.1 · ADR-035.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @sql nvarchar(max);
    DECLARE @err nvarchar(500);

    -- ─────────────────────────────────────────────────────────────────────────
    -- 0) GUARD — dừng nếu tiền đề sai hoặc drop cột sẽ vỡ UNIQUE
    -- ─────────────────────────────────────────────────────────────────────────

    -- 0a. Nhiều Tenant_Id khác nhau → Config DB này phục vụ >1 tenant.
    IF OBJECT_ID(N'dbo.Sys_Table', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.Sys_Table', N'Tenant_Id') IS NOT NULL
    BEGIN
        SET @sql = N'IF (SELECT COUNT(DISTINCT Tenant_Id) FROM dbo.Sys_Table WHERE Tenant_Id IS NOT NULL) > 1
                         THROW 50035, ''DUNG: Sys_Table chua nhieu Tenant_Id khac nhau -> Config DB nay phuc vu >1 tenant. Tien de ADR-035 KHONG dung cho DB nay.'', 1;';
        EXEC sys.sp_executesql @sql;
    END

    -- 0b. Sys_Lookup: (Lookup_Code, Item_Code) trùng giữa dòng global và tenant
    --     → sau khi drop cột, UNIQUE thường sẽ vỡ. Phải quyết dữ liệu trước.
    IF OBJECT_ID(N'dbo.Sys_Lookup', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Sys_Lookup
                   GROUP BY Lookup_Code, Item_Code HAVING COUNT(*) > 1)
    BEGIN
        SET @err = N'DỪNG: Sys_Lookup có (Lookup_Code, Item_Code) trùng → drop Tenant_Id sẽ vỡ UNIQUE. '
                 + N'Quyết dữ liệu trùng trước (giữ bản tenant hay bản global?).';
        THROW 50035, @err, 1;
    END

    -- 0c. Mã định danh phải duy nhất sau khi bỏ Tenant_Id.
    IF OBJECT_ID(N'dbo.Sys_Table', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Sys_Table GROUP BY Table_Code HAVING COUNT(*) > 1)
    BEGIN
        SET @err = N'DỪNG: Sys_Table.Table_Code trùng → drop Tenant_Id sẽ vỡ UNIQUE.';
        THROW 50035, @err, 1;
    END

    IF OBJECT_ID(N'dbo.Ui_View', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Ui_View GROUP BY View_Code HAVING COUNT(*) > 1)
    BEGIN
        SET @err = N'DỪNG: Ui_View.View_Code trùng → drop Tenant_Id sẽ vỡ UNIQUE.';
        THROW 50035, @err, 1;
    END

    PRINT N'✔ 0) Guard qua — tiền đề ADR-035 đúng với DB này.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 1) DROP FOREIGN KEY trên cột Tenant_Id (chặn DROP COLUMN)
    -- ─────────────────────────────────────────────────────────────────────────
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(tp.schema_id)) + N'.' + QUOTENAME(tp.name)
                       + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13)
    FROM   sys.foreign_keys fk
    JOIN   sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN   sys.tables  tp ON tp.object_id = fkc.parent_object_id
    JOIN   sys.columns cp ON cp.object_id = fkc.parent_object_id
                         AND cp.column_id = fkc.parent_column_id
    WHERE  cp.name = N'Tenant_Id';

    IF @sql <> N'' EXEC sys.sp_executesql @sql;
    PRINT N'✔ 1) Đã gỡ FK trên Tenant_Id.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 2) DROP INDEX chứa Tenant_Id (key column) hoặc filter theo Tenant_Id
    -- ─────────────────────────────────────────────────────────────────────────
    SET @sql = N'';
    SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON '
                       + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
    FROM   sys.indexes i
    JOIN   sys.tables  t ON t.object_id = i.object_id
    WHERE  i.is_primary_key = 0
      AND  i.name IS NOT NULL
      AND (EXISTS (SELECT 1
                   FROM   sys.index_columns ic
                   JOIN   sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE  ic.object_id = i.object_id AND ic.index_id = i.index_id
                     AND  c.name = N'Tenant_Id')
           OR (i.has_filter = 1 AND i.filter_definition LIKE N'%Tenant_Id%'));

    IF @sql <> N'' EXEC sys.sp_executesql @sql;
    PRINT N'✔ 2) Đã gỡ index tham chiếu Tenant_Id.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 3) DROP DEFAULT constraint trên Tenant_Id (nếu còn sót — xem db/009)
    -- ─────────────────────────────────────────────────────────────────────────
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                       + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(13)
    FROM   sys.default_constraints dc
    JOIN   sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    JOIN   sys.tables  t ON t.object_id = dc.parent_object_id
    WHERE  c.name = N'Tenant_Id';

    IF @sql <> N'' EXEC sys.sp_executesql @sql;
    PRINT N'✔ 3) Đã gỡ DEFAULT constraint trên Tenant_Id.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 4) DROP COLUMN Tenant_Id (trừ Sys_Tenant — ở đó là PK, drop cả bảng ở bước 6)
    -- ─────────────────────────────────────────────────────────────────────────
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                       + N' DROP COLUMN ' + QUOTENAME(c.name) + N';' + CHAR(13)
    FROM   sys.columns c
    JOIN   sys.tables  t ON t.object_id = c.object_id
    WHERE  c.name = N'Tenant_Id'
      AND  t.name <> N'Sys_Tenant';

    IF @sql <> N'' EXEC sys.sp_executesql @sql;
    PRINT N'✔ 4) Đã DROP COLUMN Tenant_Id.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 5) DỰNG LẠI UNIQUE thường (thay cặp UQ_*_Global / UQ_*_Tenant đã drop)
    --    + IX_Sys_Lookup_Code: bỏ cột dẫn đầu Tenant_Id (vô dụng khi DB 1 tenant)
    -- ─────────────────────────────────────────────────────────────────────────
    SET @sql = N'';

    IF OBJECT_ID(N'dbo.Sys_Table', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_Table_Code' AND object_id = OBJECT_ID(N'dbo.Sys_Table'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Table_Code ON dbo.Sys_Table (Table_Code);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Ui_View', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Ui_View_Code' AND object_id = OBJECT_ID(N'dbo.Ui_View'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Ui_View_Code ON dbo.Ui_View (View_Code);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Sys_Role', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_Role_Code' AND object_id = OBJECT_ID(N'dbo.Sys_Role'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Role_Code ON dbo.Sys_Role (Role_Code);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Sys_Config', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_Config_Key' AND object_id = OBJECT_ID(N'dbo.Sys_Config'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Config_Key ON dbo.Sys_Config (Config_Key, Scope);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Sys_Menu', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_Menu_Code' AND object_id = OBJECT_ID(N'dbo.Sys_Menu'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Menu_Code ON dbo.Sys_Menu (Menu_Code);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Sys_MenuCatalog', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_MenuCatalog_Func' AND object_id = OBJECT_ID(N'dbo.Sys_MenuCatalog'))
        SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_MenuCatalog_Func ON dbo.Sys_MenuCatalog (Menu_Id, Func_Code);' + CHAR(13);

    IF OBJECT_ID(N'dbo.Sys_Lookup', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Sys_Lookup_Item' AND object_id = OBJECT_ID(N'dbo.Sys_Lookup'))
            SET @sql = @sql + N'CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Lookup_Item ON dbo.Sys_Lookup (Lookup_Code, Item_Code);' + CHAR(13);

        -- Index cũ dẫn đầu bằng Tenant_Id → không phục vụ được truy vấn lọc Lookup_Code.
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sys_Lookup_Code' AND object_id = OBJECT_ID(N'dbo.Sys_Lookup'))
            SET @sql = @sql + N'CREATE NONCLUSTERED INDEX IX_Sys_Lookup_Code ON dbo.Sys_Lookup (Lookup_Code, Is_Active);' + CHAR(13);
    END

    IF @sql <> N'' EXEC sys.sp_executesql @sql;
    PRINT N'✔ 5) Đã dựng lại UNIQUE thường + IX_Sys_Lookup_Code.';

    -- ─────────────────────────────────────────────────────────────────────────
    -- 6) DROP bảng Sys_Tenant — mồ côi (0 tham chiếu C#; resolver đọc Catalog dbo.Tenant)
    -- ─────────────────────────────────────────────────────────────────────────
    IF OBJECT_ID(N'dbo.Sys_Tenant', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.Sys_Tenant;
        PRINT N'✔ 6) Đã DROP TABLE Sys_Tenant.';
    END

    COMMIT TRANSACTION;
    PRINT N'════ 078 hoàn tất — Config DB không còn cột Tenant_Id (ADR-035). ════';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    PRINT N'✖ 078 THẤT BẠI — đã ROLLBACK, DB giữ nguyên.';
    THROW;
END CATCH
GO
