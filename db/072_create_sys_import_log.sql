-- =============================================================================
-- File    : 072_create_sys_import_log.sql
-- Database: ICare247_Solution  (Data DB per-tenant)
-- Purpose : IMPORT-0 (Pha 2 — ADR-034 / spec 25 §13). Log import Excel:
--           • Sys_Import_Log        — cấp MẺ: ai/file/mode/thống kê/status + Correlation_Id.
--           • Sys_Import_Log_Detail — CHỈ DÒNG LỖI (ERROR/SKIP): Row_Number/Operation/Error_Key/
--                                     Row_Json (đã LÀM MỜ cột nhạy cảm — §13.3).
-- Note    : Audit tường minh (ADR-022) — CreatedBy/CreatedAt set từ App, không dựa DEFAULT.
--           Idempotent (OBJECT_ID guard). Dòng thành công đã có audit-log JSON-diff (NK_ThayDoi) → không trùng.
-- Spec    : docs/spec/25_FK_LOOKUP_SPEC.md §13. ADR-034.
-- =============================================================================

USE [ICare247_Solution];
GO

SET XACT_ABORT ON;
GO

-- ── Sys_Import_Log (cấp mẻ) ─────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Sys_Import_Log', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Import_Log
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Sys_Import_Log PRIMARY KEY,
        ImportSessionId   UNIQUEIDENTIFIER NOT NULL,   -- nối validate → commit
        View_Code         NVARCHAR(100)  NOT NULL,
        Table_Name        NVARCHAR(128)  NULL,         -- bảng đích (edit-form của View)
        File_Name         NVARCHAR(260)  NULL,
        File_Size         BIGINT         NULL,
        File_Hash         NVARCHAR(64)   NULL,         -- SHA-256 hex (chống import trùng file)
        [Mode]            NVARCHAR(20)   NOT NULL,     -- insert | upsert
        Total_Rows        INT            NOT NULL CONSTRAINT DF_Sys_Import_Log_Total   DEFAULT 0,
        Inserted          INT            NOT NULL CONSTRAINT DF_Sys_Import_Log_Ins     DEFAULT 0,
        Updated           INT            NOT NULL CONSTRAINT DF_Sys_Import_Log_Upd     DEFAULT 0,
        Error_Count       INT            NOT NULL CONSTRAINT DF_Sys_Import_Log_Err     DEFAULT 0,
        Skipped           INT            NOT NULL CONSTRAINT DF_Sys_Import_Log_Skip    DEFAULT 0,
        [Status]          NVARCHAR(20)   NOT NULL,     -- Validating | Committed | PartialSuccess | Failed
        Started_At        DATETIME2(3)   NOT NULL,
        Finished_At       DATETIME2(3)   NULL,
        Duration_Ms       INT            NULL,
        Correlation_Id    NVARCHAR(64)   NULL,         -- nối sang logs/icare247-*.log
        CreatedBy         BIGINT         NOT NULL,     -- = ai import
        CreatedAt         DATETIME2(3)   NOT NULL
    );

    CREATE UNIQUE INDEX UX_Sys_Import_Log_Session ON dbo.Sys_Import_Log (ImportSessionId);
    CREATE INDEX IX_Sys_Import_Log_View ON dbo.Sys_Import_Log (View_Code, CreatedAt DESC);

    PRINT N'✔ Đã tạo bảng Sys_Import_Log.';
END
ELSE
    PRINT N'• Sys_Import_Log đã tồn tại — bỏ qua.';
GO

-- ── Sys_Import_Log_Detail (chỉ dòng lỗi) ────────────────────────────────────
IF OBJECT_ID(N'dbo.Sys_Import_Log_Detail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Import_Log_Detail
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Sys_Import_Log_Detail PRIMARY KEY,
        Import_Log_Id   BIGINT        NOT NULL
            CONSTRAINT FK_Sys_Import_Log_Detail_Log
                REFERENCES dbo.Sys_Import_Log (Id),
        Row_Number      INT           NOT NULL,        -- số dòng trong Excel (dò đúng ô)
        Operation       NVARCHAR(20)  NOT NULL,        -- INSERT | UPDATE | ERROR | SKIP
        Record_Id       BIGINT        NULL,            -- Id bản ghi đã ghi (null nếu lỗi)
        Error_Key       NVARCHAR(200) NULL,            -- i18n key (ADR-029)
        Error_Args_Json NVARCHAR(MAX) NULL,            -- args (đã LÀM MỜ nếu chạm cột nhạy cảm)
        Field_Name      NVARCHAR(128) NULL,            -- cột lỗi (tô đỏ); NULL = lỗi cấp dòng
        Row_Json        NVARCHAR(MAX) NULL             -- đủ cột của dòng, đã LÀM MỜ (§13.3)
    );

    CREATE INDEX IX_Sys_Import_Log_Detail_Log ON dbo.Sys_Import_Log_Detail (Import_Log_Id);

    PRINT N'✔ Đã tạo bảng Sys_Import_Log_Detail.';
END
ELSE
    PRINT N'• Sys_Import_Log_Detail đã tồn tại — bỏ qua.';
GO

PRINT N'Migration 072 completed — bảng log import (mẻ + chi tiết dòng lỗi).';
GO
