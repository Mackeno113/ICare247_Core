-- =============================================================================
-- File    : 062_alter_config_sync_flags_lookup_filter.sql
-- Database: ICare247_Config  (Config DB — master canonical + mỗi tenant 1 Config DB)
-- Purpose : CFGSYNC-2 (mở rộng) — Bổ sung 4 cờ đồng bộ cho 2 bảng config-con còn thiếu:
--           • Ui_Field_Lookup — cấu hình lookup của field FK trong form (con của Ui_Field, 1-1).
--           • Ui_View_Filter  — cấu hình panel lọc cascade/ADR-030 (con của Ui_View).
--           db/050 đã cấp cờ cho 12 bảng nhưng BỎ SÓT 2 bảng này → không sync được khi
--           engine-hóa màn nghiệp vụ (F2): form FK-lookup + bộ lọc liên kết.
-- Spec    : docs/spec/16_CONFIG_SYNC_SPEC.md §2/§4/§7 · nối descriptor ConfigSyncTables.Order.
-- Cờ      : Is_System / Is_Customized / Synced_At / Source_Ver (giống db/050).
-- Note    : Idempotent (COL_LENGTH/OBJECT_ID guard). Chạy lại an toàn.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

DECLARE @tables TABLE (TableName SYSNAME);
INSERT INTO @tables (TableName) VALUES
    (N'Ui_Field_Lookup'),
    (N'Ui_View_Filter');

DECLARE @t SYSNAME, @full NVARCHAR(300), @sql NVARCHAR(MAX);

DECLARE tbl_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT TableName FROM @tables;
OPEN tbl_cur;
FETCH NEXT FROM tbl_cur INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @full = N'dbo.' + @t;

    IF OBJECT_ID(@full, N'U') IS NOT NULL
    BEGIN
        -- Is_System — bản gốc từ master (1) vs tenant/DEV tự thêm (0). Mặc định 0 = an toàn.
        IF COL_LENGTH(@full, 'Is_System') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @full + N' ADD Is_System BIT NOT NULL DEFAULT 0;';
            EXEC sys.sp_executesql @sql;
        END;

        -- Is_Customized — bản hệ thống tenant đã sửa → sync BỎ QUA (giữ bản tenant). Mặc định 0.
        IF COL_LENGTH(@full, 'Is_Customized') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @full + N' ADD Is_Customized BIT NOT NULL DEFAULT 0;';
            EXEC sys.sp_executesql @sql;
        END;

        -- Synced_At — thời điểm sync cuối áp lên dòng (NULL = chưa sync).
        IF COL_LENGTH(@full, 'Synced_At') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @full + N' ADD Synced_At DATETIME NULL;';
            EXEC sys.sp_executesql @sql;
        END;

        -- Source_Ver — Version bản master đã áp (incremental sync sau).
        IF COL_LENGTH(@full, 'Source_Ver') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @full + N' ADD Source_Ver INT NULL;';
            EXEC sys.sp_executesql @sql;
        END;
    END
    ELSE
    BEGIN
        PRINT N'⚠ Bỏ qua (chưa tồn tại): ' + @full + N' — chạy migration tạo bảng trước.';
    END;

    FETCH NEXT FROM tbl_cur INTO @t;
END;
CLOSE tbl_cur;
DEALLOCATE tbl_cur;
GO

PRINT N'Migration 062 completed — cờ đồng bộ cho Ui_Field_Lookup + Ui_View_Filter.';
GO
