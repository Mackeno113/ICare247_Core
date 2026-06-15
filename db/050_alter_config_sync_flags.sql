-- =============================================================================
-- File    : 050_alter_config_sync_flags.sql
-- Database: ICare247_Config  (Config DB — master canonical + mỗi tenant 1 Config DB)
-- Purpose : CFGSYNC-1 — Thêm cờ đồng bộ config master→tenant cho các bảng cấu hình
--           + tạo bảng log sync. Nền tảng F1 (đồng bộ config), tiền đề engine-hóa
--           màn nghiệp vụ (F2 — ADR-024).
-- Spec    : docs/spec/16_CONFIG_SYNC_SPEC.md §4/§5/§7 · ADR-025 (1 DB/tenant) · ADR-023 (pattern gốc).
-- Cờ      : (5 quyết định đã duyệt 2026-06-15 — toàn bộ theo khuyến nghị)
--   • Is_System     BIT  1=bản gốc đồng bộ từ master (sync UPSERT) · 0=tenant/DEV tự thêm (không đụng).
--   • Is_Customized BIT  1=bản hệ thống nhưng tenant đã sửa → sync BỎ QUA (giữ bản tenant). (row-level)
--   • Synced_At     DATETIME  lần sync cuối áp lên dòng (NULL = chưa sync).
--   • Source_Ver    INT       Version bản master đã áp (phục vụ incremental sync sau).
-- Tombstone : master gỡ bản hệ thống → tenant đặt Is_Active=0 (KHÔNG hard-delete) — dùng cột Is_Active sẵn có.
-- Note    : Idempotent (COL_LENGTH/OBJECT_ID guard). Một chiều master→tenant (xem IConfigSyncService — CFGSYNC-2).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) Thêm 4 cột cờ vào 11 bảng config (theo spec §7).
--    Danh sách bám đúng phạm vi sync §2: bảng "đầu thực thể" mang cờ; con sát
--    (Sys_Column/Ui_Field_Lookup/lookup items) đi theo cha — bảo vệ ở mức cha.
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @tables TABLE (TableName SYSNAME);
INSERT INTO @tables (TableName) VALUES
    (N'Sys_Table'),
    (N'Sys_Resource'),
    (N'Sys_Lookup'),
    (N'Ui_Form'),
    (N'Ui_Tab'),
    (N'Ui_Section'),
    (N'Ui_Field'),
    (N'Ui_View'),
    (N'Ui_View_Column'),
    (N'Ui_View_Action'),
    (N'Val_Rule');

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
        -- Is_System — bản gốc từ master (1) vs tenant/DEV tự thêm (0). Mặc định 0 = an toàn
        -- (dòng có sẵn trước sync coi như "không phải bản master" → sync không ghi đè).
        IF COL_LENGTH(@full, 'Is_System') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @full + N' ADD Is_System BIT NOT NULL DEFAULT 0;';
            EXEC sys.sp_executesql @sql;
        END;

        -- Is_Customized — bản hệ thống tenant đã sửa → sync bỏ qua (giữ bản tenant). Mặc định 0.
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

        -- Source_Ver — Version bản master đã áp (incremental sync sau — CFGSYNC theo version).
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

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) Index lọc nhanh dòng tenant đã tùy biến (để sync skip) + dòng hệ thống.
--    Chỉ tạo cho các bảng lớn/truy vấn sync nhiều nhất.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Field', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Ui_Field', 'Is_System') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ui_Field_Sync' AND object_id = OBJECT_ID('dbo.Ui_Field'))
    CREATE INDEX IX_Ui_Field_Sync ON dbo.Ui_Field (Is_System, Is_Customized);
GO

IF OBJECT_ID('dbo.Sys_Resource', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Sys_Resource', 'Is_System') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sys_Resource_Sync' AND object_id = OBJECT_ID('dbo.Sys_Resource'))
    CREATE INDEX IX_Sys_Resource_Sync ON dbo.Sys_Resource (Is_System, Is_Customized);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3) Bảng log sync — audit + dry-run (spec §7/§8). Mỗi lần chạy sync ghi 1 dòng.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Config_Sync_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Config_Sync_Log
    (
        Sync_Id           INT             IDENTITY(1,1) NOT NULL,
        Tenant_Code       NVARCHAR(100)   NULL,           -- tenant đích (1 DB/tenant: mã khách); NULL nếu chạy tại chỗ.
        Started_At        DATETIME        NOT NULL DEFAULT GETDATE(),
        Finished_At       DATETIME        NULL,
        Source_Ver        INT             NULL,           -- version bản master đã áp đợt này.
        Is_DryRun         BIT             NOT NULL DEFAULT 0,  -- 1=preview (không ghi thật).
        Rows_Inserted     INT             NOT NULL DEFAULT 0,
        Rows_Updated      INT             NOT NULL DEFAULT 0,
        Rows_Deactivated  INT             NOT NULL DEFAULT 0,  -- tombstone Is_Active=0.
        Rows_Skipped      INT             NOT NULL DEFAULT 0,  -- bỏ qua vì Is_Customized=1.
        Status            NVARCHAR(20)    NOT NULL DEFAULT N'Running',  -- Running/Success/Failed.
        Detail_Json       NVARCHAR(MAX)   NULL,           -- breakdown theo bảng + diff (dry-run).
        Error_Message     NVARCHAR(MAX)   NULL,
        Triggered_By      NVARCHAR(100)   NULL,           -- super admin kích hoạt (hoặc 'provisioning').
        Created_At        DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Config_Sync_Log PRIMARY KEY (Sync_Id)
    );

    CREATE INDEX IX_Sys_Config_Sync_Log_Time
        ON dbo.Sys_Config_Sync_Log (Started_At DESC);
END;
GO

PRINT N'Migration 050 completed — cờ đồng bộ config (Is_System/Is_Customized/Synced_At/Source_Ver) + Sys_Config_Sync_Log.';
GO
