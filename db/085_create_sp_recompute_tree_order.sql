-- =============================================================================
-- File    : 085_create_sp_recompute_tree_order.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : ADR-027 — proc GENERIC recompute cache sắp xếp cây cho bảng cây bất kỳ
--           (Cap/ThuTuCay/DuongDanCay dẫn xuất từ {ParentColumn} + ThuTu input).
--           Dùng chung mọi bảng cây (TC_PhongBan, TC_CongTy, HT_ChucNang...) — chỉ
--           truyền @TableName + @ParentColumn, KHÔNG hardcode cho 1 bảng.
-- Quy ước : PK luôn là cột `Id` (ADR-019); 3 cột cache luôn tên cố định `Cap`/
--           `ThuTuCay`/`DuongDanCay`; cột input thứ tự luôn tên cố định `ThuTu`.
--           Chỉ tên bảng + tên cột cha (`{Bang}_Cha_Id`) khác nhau giữa các bảng.
--           Gốc = {ParentColumn} IS NULL hoặc = 0.
-- An toàn : @TableName/@ParentColumn/@SchemaName chỉ chấp nhận identifier đã tồn tại
--           thật trong sys.tables/sys.columns (chống injection qua dynamic SQL).
-- Spec    : .claude-rules/database-design.md §6 (cây) · ADR-027.
-- Idempotent: CREATE OR ALTER.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER PROCEDURE dbo.sp_RecomputeTreeOrder
    @TableName    SYSNAME,
    @ParentColumn SYSNAME,
    @SchemaName   SYSNAME = N'dbo'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- ── Validate bảng tồn tại ────────────────────────────────────────────────
    IF NOT EXISTS (
        SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @TableName AND s.name = @SchemaName)
    BEGIN
        RAISERROR(N'sp_RecomputeTreeOrder: bảng %s.%s không tồn tại.', 16, 1, @SchemaName, @TableName);
        RETURN;
    END;

    -- ── Validate cột cha (@ParentColumn) tồn tại ────────────────────────────
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @TableName AND s.name = @SchemaName AND c.name = @ParentColumn)
    BEGIN
        RAISERROR(N'sp_RecomputeTreeOrder: cột cha %s không tồn tại trên %s.%s.',
                   16, 1, @ParentColumn, @SchemaName, @TableName);
        RETURN;
    END;

    -- ── Validate đủ 5 cột quy ước (Id/ThuTu/Cap/ThuTuCay/DuongDanCay) ───────
    DECLARE @missing NVARCHAR(400) = N'';
    SELECT @missing = @missing + CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @TableName AND s.name = @SchemaName AND c.name = col.v)
        THEN N'' ELSE col.v + N' ' END
    FROM (VALUES (N'Id'), (N'ThuTu'), (N'Cap'), (N'ThuTuCay'), (N'DuongDanCay')) AS col(v);

    IF LEN(@missing) > 0
    BEGIN
        RAISERROR(N'sp_RecomputeTreeOrder: %s.%s thiếu cột quy ước: %s (xem ADR-027).',
                   16, 1, @SchemaName, @TableName, @missing);
        RETURN;
    END;

    -- ── Đệ quy CTE: Cap (gốc=1) + DuongDanCay (materialized path) + SortKey
    --    (chuỗi ThuTu+Id zero-pad từng cấp, nối '.' — sort lexicographic = DFS
    --    pre-order đúng thứ tự cây, tôn trọng ThuTu trong từng nhóm anh em). ──
    DECLARE @qualified NVARCHAR(300) = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
    DECLARE @parentCol NVARCHAR(128) = QUOTENAME(@ParentColumn);
    DECLARE @sql NVARCHAR(MAX) = N'
    ;WITH cte AS (
        SELECT Id, CAST(1 AS INT) AS Cap,
               CAST(CAST(Id AS NVARCHAR(20)) AS NVARCHAR(400)) AS DuongDanCay,
               CAST(RIGHT(''000000000'' + CAST(ThuTu AS VARCHAR(9)), 9) + N''.'' +
                    RIGHT(''000000000'' + CAST(Id AS VARCHAR(9)), 9) AS NVARCHAR(MAX)) AS SortKey
        FROM ' + @qualified + N'
        WHERE ' + @parentCol + N' IS NULL OR ' + @parentCol + N' = 0
        UNION ALL
        SELECT child.Id, parent.Cap + 1,
               parent.DuongDanCay + N''/'' + CAST(child.Id AS NVARCHAR(20)),
               parent.SortKey + N''.'' +
                    RIGHT(''000000000'' + CAST(child.ThuTu AS VARCHAR(9)), 9) + N''.'' +
                    RIGHT(''000000000'' + CAST(child.Id AS VARCHAR(9)), 9)
        FROM ' + @qualified + N' child
        JOIN cte parent ON child.' + @parentCol + N' = parent.Id
    ),
    ranked AS (
        SELECT Id, Cap, DuongDanCay, ROW_NUMBER() OVER (ORDER BY SortKey) AS ThuTuCay
        FROM cte
    )
    UPDATE tgt
    SET tgt.Cap = r.Cap,
        tgt.DuongDanCay = r.DuongDanCay,
        tgt.ThuTuCay = r.ThuTuCay
    FROM ' + @qualified + N' AS tgt
    JOIN ranked AS r ON r.Id = tgt.Id
    OPTION (MAXRECURSION 100);';

    EXEC sp_executesql @sql;
END;
GO

PRINT N'Migration 085 completed — sp_RecomputeTreeOrder (ADR-027, generic mọi bảng cây).';
GO
