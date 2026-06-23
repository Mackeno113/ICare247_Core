-- =============================================================================
-- File    : 061_ensure_audit_columns.sql
-- Database: Data DB (Target DB per-tenant — ICare247_Solution / ICare247_Data)
-- Purpose : "Guard" idempotent — bổ sung KHỐI CỘT AUTO chuẩn (§0.1 spec 11) còn
--           thiếu cho mọi bảng nghiệp vụ, + tạo INDEX trên cột FK còn thiếu index.
--           Dev hay quên các cột này khi thiết kế bảng → chạy script này lúc
--           provisioning / migrate tenant để chuẩn hoá toàn DB một lần.
-- Cơ chế  : Quét sys.tables theo PREFIX module (whitelist), TRỪ danh sách opt-out
--           (bảng cố ý lệch chuẩn). Mỗi cột bọc IF COL_LENGTH(...) IS NULL → chỉ
--           ADD khi thiếu. Chạy lại bao nhiêu lần cũng an toàn (idempotent).
-- Quy ước : Khối cột auto = CreatedBy / CreatedAt / UpdatedBy / UpdatedAt /
--           IsDeleted / Ver (KHÔNG gồm Ma/Ten — theo archetype §0.2).
--           - CreatedBy : bigint NOT NULL, KHÔNG để DEFAULT (insert phải set tường
--                         minh — feedback "explicit-audit-columns"). Backfill bản ghi
--                         cũ = 0 qua default tạm rồi DROP ngay.
--           - CreatedAt : datetime2 NOT NULL DEFAULT sysutcdatetime() (giữ default).
--           - IsDeleted : bit NOT NULL DEFAULT 0.   - Ver: int NOT NULL DEFAULT 0.
--           - UpdatedBy/UpdatedAt: NULL.
--           - Id (PK identity) THIẾU → chỉ CẢNH BÁO (PRINT), KHÔNG tự thêm
--             (thêm identity PK vào bảng có dữ liệu = rủi ro → xử lý tay).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §0.1/§0.2. Mở rộng prefix/opt-out ở §1-§2.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ── Cờ điều khiển ────────────────────────────────────────────────────────────
DECLARE @CreateFkIndexes BIT = 1;   -- 0 = bỏ qua phần tạo index FK

-- ─────────────────────────────────────────────────────────────────────────────
-- §1. PREFIX module được áp khối cột auto (whitelist). Thêm module mới tại đây.
--     '_' được escape (ESCAPE '\') để là ký tự literal, không phải wildcard.
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @Prefixes TABLE (p NVARCHAR(20) PRIMARY KEY);
INSERT INTO @Prefixes (p) VALUES
    (N'DM\_'),   -- Danh mục dùng chung
    (N'TC\_'),   -- Tổ chức
    (N'HT\_'),   -- Hệ thống (identity + phân quyền)
    (N'NS\_'),   -- Nhân sự (Hr)
    (N'TM\_'),   -- Thương mại
    (N'GD\_'),   -- Giao dịch / chứng từ
    (N'TT\_'),   -- Tệp / tài liệu đính kèm
    (N'NK\_');   -- Nhật ký / log nghiệp vụ

-- ─────────────────────────────────────────────────────────────────────────────
-- §2. OPT-OUT — bảng CỐ Ý lệch chuẩn (không áp khối cột auto). Thêm bảng tại đây.
--     Lý do từng bảng ghi rõ ở spec 11 (§3). Map N-N / RefreshToken VẪN theo chuẩn.
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @Exclude TABLE (schemaName SYSNAME, tableName SYSNAME, PRIMARY KEY (schemaName, tableName));
INSERT INTO @Exclude (schemaName, tableName) VALUES
    (N'dbo', N'HT_NguoiDung_LuoiLayout');   -- preference tối giản, không dùng khối auto đầy đủ

-- ── Tập bảng ứng viên ─────────────────────────────────────────────────────────
DECLARE @Tables TABLE (schemaName SYSNAME, tableName SYSNAME);
INSERT INTO @Tables (schemaName, tableName)
SELECT s.name, t.name
FROM   sys.tables  t
JOIN   sys.schemas s ON s.schema_id = t.schema_id
WHERE  t.is_ms_shipped = 0
  AND  EXISTS (SELECT 1 FROM @Prefixes p WHERE t.name LIKE p.p + N'%' ESCAPE '\')
  AND  NOT EXISTS (SELECT 1 FROM @Exclude e WHERE e.schemaName = s.name AND e.tableName = t.name);

-- ── Vòng lặp bổ sung cột ──────────────────────────────────────────────────────
DECLARE @schema SYSNAME, @table SYSNAME, @full NVARCHAR(300), @sql NVARCHAR(MAX), @df SYSNAME;
DECLARE @added INT = 0, @tablesTouched INT = 0;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT schemaName, tableName FROM @Tables ORDER BY schemaName, tableName;
OPEN cur;
FETCH NEXT FROM cur INTO @schema, @table;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @full = QUOTENAME(@schema) + N'.' + QUOTENAME(@table);
    DECLARE @before INT = @added;

    -- Id (PK identity) — chỉ cảnh báo, không tự thêm
    IF COL_LENGTH(@full, 'Id') IS NULL
        PRINT N'  [!] ' + @full + N' — THIẾU cột Id (PK identity). Thêm tay (rủi ro với bảng có dữ liệu).';

    -- CreatedBy: bigint NOT NULL, default tạm 0 cho bản ghi cũ rồi DROP (insert phải set tường minh)
    IF COL_LENGTH(@full, 'CreatedBy') IS NULL
    BEGIN
        SET @df = N'DF_' + @table + N'_CreatedBy_tmp';
        SET @sql = N'ALTER TABLE ' + @full + N' ADD CreatedBy bigint NOT NULL CONSTRAINT ' + QUOTENAME(@df) + N' DEFAULT(0);';
        EXEC sys.sp_executesql @sql;
        SET @sql = N'ALTER TABLE ' + @full + N' DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
        EXEC sys.sp_executesql @sql;
        SET @added += 1;
    END

    -- CreatedAt: datetime2 NOT NULL DEFAULT sysutcdatetime() (giữ default theo spec)
    IF COL_LENGTH(@full, 'CreatedAt') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE ' + @full + N' ADD CreatedAt datetime2 NOT NULL CONSTRAINT '
                 + QUOTENAME(N'DF_' + @table + N'_CreatedAt') + N' DEFAULT(sysutcdatetime());';
        EXEC sys.sp_executesql @sql;
        SET @added += 1;
    END

    -- UpdatedBy / UpdatedAt: NULL
    IF COL_LENGTH(@full, 'UpdatedBy') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE ' + @full + N' ADD UpdatedBy bigint NULL;';
        EXEC sys.sp_executesql @sql; SET @added += 1;
    END
    IF COL_LENGTH(@full, 'UpdatedAt') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE ' + @full + N' ADD UpdatedAt datetime2 NULL;';
        EXEC sys.sp_executesql @sql; SET @added += 1;
    END

    -- IsDeleted: bit NOT NULL DEFAULT 0
    IF COL_LENGTH(@full, 'IsDeleted') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE ' + @full + N' ADD IsDeleted bit NOT NULL CONSTRAINT '
                 + QUOTENAME(N'DF_' + @table + N'_IsDeleted') + N' DEFAULT(0);';
        EXEC sys.sp_executesql @sql; SET @added += 1;
    END

    -- Ver: int NOT NULL DEFAULT 0
    IF COL_LENGTH(@full, 'Ver') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE ' + @full + N' ADD Ver int NOT NULL CONSTRAINT '
                 + QUOTENAME(N'DF_' + @table + N'_Ver') + N' DEFAULT(0);';
        EXEC sys.sp_executesql @sql; SET @added += 1;
    END

    IF @added > @before
    BEGIN
        SET @tablesTouched += 1;
        PRINT N'  [+] ' + @full + N' — bổ sung ' + CAST(@added - @before AS NVARCHAR(10)) + N' cột auto.';
    END

    FETCH NEXT FROM cur INTO @schema, @table;
END
CLOSE cur; DEALLOCATE cur;

PRINT N'== Cột auto: bổ sung ' + CAST(@added AS NVARCHAR(10)) + N' cột trên '
    + CAST(@tablesTouched AS NVARCHAR(10)) + N' bảng. ==';

-- ─────────────────────────────────────────────────────────────────────────────
-- §3. INDEX trên cột FK còn thiếu (IX_<Table>_<Col>) — chỉ những cột FK chưa làm
--     khóa dẫn đầu (key_ordinal = 1) của bất kỳ index nào. Idempotent.
-- ─────────────────────────────────────────────────────────────────────────────
IF @CreateFkIndexes = 1
BEGIN
    DECLARE @ixName SYSNAME, @ixTable NVARCHAR(300), @ixCol SYSNAME;
    DECLARE @ixCount INT = 0;

    DECLARE ixcur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT
               N'IX_' + t.name + N'_' + c.name      AS ixName,
               QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) AS ixTable,
               c.name                                AS ixCol
        FROM   sys.foreign_key_columns fkc
        JOIN   sys.tables  t ON t.object_id = fkc.parent_object_id
        JOIN   sys.schemas s ON s.schema_id = t.schema_id
        JOIN   sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE  EXISTS (SELECT 1 FROM @Tables tt WHERE tt.schemaName = s.name AND tt.tableName = t.name)
          AND  NOT EXISTS (
                   SELECT 1
                   FROM   sys.index_columns ic
                   WHERE  ic.object_id = fkc.parent_object_id
                     AND  ic.column_id = fkc.parent_column_id
                     AND  ic.key_ordinal = 1);

    OPEN ixcur;
    FETCH NEXT FROM ixcur INTO @ixName, @ixTable, @ixCol;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = @ixName AND object_id = OBJECT_ID(@ixTable))
        BEGIN
            SET @sql = N'CREATE INDEX ' + QUOTENAME(@ixName) + N' ON ' + @ixTable + N'(' + QUOTENAME(@ixCol) + N');';
            EXEC sys.sp_executesql @sql;
            SET @ixCount += 1;
            PRINT N'  [+] index ' + @ixName + N' ON ' + @ixTable + N'(' + @ixCol + N')';
        END
        FETCH NEXT FROM ixcur INTO @ixName, @ixTable, @ixCol;
    END
    CLOSE ixcur; DEALLOCATE ixcur;

    PRINT N'== Index FK: tạo mới ' + CAST(@ixCount AS NVARCHAR(10)) + N' index. ==';
END
ELSE
    PRINT N'== Index FK: BỎ QUA (@CreateFkIndexes = 0). ==';
GO
