-- =============================================================================
-- File    : 077_create_doc_template.sql
-- Database: ICare247_Config  (Config DB — template là cấu hình, KHÔNG ở Data DB)
-- Purpose : DOC TEMPLATE — xuất Word/PDF theo mẫu (mail-merge, ghép fragment master+detail).
--           Tạo 4 bảng: Doc_Template (master), Doc_Template_Detail (1:N detail),
--           Doc_Proc_Registry (whitelist proc), Doc_Template_Param (ánh xạ tham số proc).
--           Tenant-local (per-tenant DB), KHÔNG vào ConfigSync. Proc dữ liệu chạy trên Data DB.
-- Note    : Idempotent (OBJECT_ID guard). Khối cột auto chuẩn (Spec 11 §0.1).
-- Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §4 (v1.0).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ── 1) Doc_Template — bộ mẫu (master, A4 dọc) ────────────────────────────────
IF OBJECT_ID(N'dbo.Doc_Template', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doc_Template
    (
        Id          BIGINT         IDENTITY(1,1) NOT NULL CONSTRAINT PK_Doc_Template PRIMARY KEY,
        Tenant_Id   INT            NOT NULL,
        Ma          NVARCHAR(50)   NOT NULL,
        Ten         NVARCHAR(200)  NOT NULL,
        Master_Proc NVARCHAR(128)  NOT NULL,
        Master_Docx VARBINARY(MAX) NULL,
        Mo_Ta       NVARCHAR(500)  NULL,
        Is_Active   BIT            NOT NULL CONSTRAINT DF_Doc_Template_Is_Active DEFAULT 1,
        -- khối cột auto
        CreatedBy   BIGINT         NOT NULL,
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Doc_Template_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT         NULL,
        UpdatedAt   DATETIME2      NULL,
        IsDeleted   BIT            NOT NULL CONSTRAINT DF_Doc_Template_IsDeleted DEFAULT 0,
        Ver         INT            NOT NULL CONSTRAINT DF_Doc_Template_Ver DEFAULT 0
    );
    CREATE UNIQUE INDEX UX_Doc_Template_Ma ON dbo.Doc_Template(Ma) WHERE IsDeleted = 0;
    PRINT N'✔ Đã tạo bảng Doc_Template.';
END
ELSE
    PRINT N'• Doc_Template đã tồn tại — bỏ qua.';
GO

-- ── 2) Doc_Template_Detail — mảnh chi tiết (1 master : N detail, A4 ngang) ────
IF OBJECT_ID(N'dbo.Doc_Template_Detail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doc_Template_Detail
    (
        Id          BIGINT         IDENTITY(1,1) NOT NULL CONSTRAINT PK_Doc_Template_Detail PRIMARY KEY,
        Template_Id BIGINT         NOT NULL
            CONSTRAINT FK_Doc_Template_Detail_Template REFERENCES dbo.Doc_Template(Id),
        Ma          NVARCHAR(50)   NOT NULL,
        Ten         NVARCHAR(200)  NOT NULL,
        Detail_Proc NVARCHAR(128)  NOT NULL,
        Detail_Docx VARBINARY(MAX) NULL,
        Thu_Tu      INT            NOT NULL CONSTRAINT DF_Doc_Template_Detail_Thu_Tu DEFAULT 0,
        Is_Active   BIT            NOT NULL CONSTRAINT DF_Doc_Template_Detail_Is_Active DEFAULT 1,
        CreatedBy   BIGINT         NOT NULL,
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Doc_Template_Detail_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT         NULL,
        UpdatedAt   DATETIME2      NULL,
        IsDeleted   BIT            NOT NULL CONSTRAINT DF_Doc_Template_Detail_IsDeleted DEFAULT 0,
        Ver         INT            NOT NULL CONSTRAINT DF_Doc_Template_Detail_Ver DEFAULT 0
    );
    CREATE UNIQUE INDEX UX_Doc_Template_Detail_Ma
        ON dbo.Doc_Template_Detail(Template_Id, Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_Doc_Template_Detail_Template ON dbo.Doc_Template_Detail(Template_Id);
    PRINT N'✔ Đã tạo bảng Doc_Template_Detail.';
END
ELSE
    PRINT N'• Doc_Template_Detail đã tồn tại — bỏ qua.';
GO

-- ── 3) Doc_Proc_Registry — whitelist proc hợp lệ (§13-B) ─────────────────────
IF OBJECT_ID(N'dbo.Doc_Proc_Registry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doc_Proc_Registry
    (
        Id         BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Doc_Proc_Registry PRIMARY KEY,
        Tenant_Id  INT           NOT NULL,
        Proc_Name  NVARCHAR(128) NOT NULL,
        Loai       NVARCHAR(20)  NOT NULL,   -- 'master' | 'detail'
        Mo_Ta      NVARCHAR(500) NULL,
        Is_Active  BIT           NOT NULL CONSTRAINT DF_Doc_Proc_Registry_Is_Active DEFAULT 1,
        CreatedBy  BIGINT        NOT NULL,
        CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_Doc_Proc_Registry_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedBy  BIGINT        NULL,
        UpdatedAt  DATETIME2     NULL,
        IsDeleted  BIT           NOT NULL CONSTRAINT DF_Doc_Proc_Registry_IsDeleted DEFAULT 0,
        Ver        INT           NOT NULL CONSTRAINT DF_Doc_Proc_Registry_Ver DEFAULT 0
    );
    CREATE UNIQUE INDEX UX_Doc_Proc_Registry_Name
        ON dbo.Doc_Proc_Registry(Proc_Name) WHERE IsDeleted = 0;
    PRINT N'✔ Đã tạo bảng Doc_Proc_Registry.';
END
ELSE
    PRINT N'• Doc_Proc_Registry đã tồn tại — bỏ qua.';
GO

-- ── 4) Doc_Template_Param — ánh xạ tham số proc (§13-C) ──────────────────────
IF OBJECT_ID(N'dbo.Doc_Template_Param', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doc_Template_Param
    (
        Id          BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_Doc_Template_Param PRIMARY KEY,
        Template_Id BIGINT        NOT NULL
            CONSTRAINT FK_Doc_Template_Param_Template REFERENCES dbo.Doc_Template(Id),
        Detail_Id   BIGINT        NULL   -- NULL = tham số proc master; có = proc detail cụ thể
            CONSTRAINT FK_Doc_Template_Param_Detail REFERENCES dbo.Doc_Template_Detail(Id),
        Param_Name  NVARCHAR(64)  NOT NULL,   -- VD '@NhanVien_Id'
        Nguon       NVARCHAR(20)  NOT NULL,   -- 'key' | 'context' | 'const'
        Nguon_Key   NVARCHAR(64)  NULL,
        Kieu        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Doc_Template_Param_Kieu DEFAULT N'string',
        Thu_Tu      INT           NOT NULL CONSTRAINT DF_Doc_Template_Param_Thu_Tu DEFAULT 0,
        CreatedBy   BIGINT        NOT NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Doc_Template_Param_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT        NULL,
        UpdatedAt   DATETIME2     NULL,
        IsDeleted   BIT           NOT NULL CONSTRAINT DF_Doc_Template_Param_IsDeleted DEFAULT 0,
        Ver         INT           NOT NULL CONSTRAINT DF_Doc_Template_Param_Ver DEFAULT 0
    );
    -- NULL Detail_Id được so sánh bằng nhau trong unique index → 1 master-param/Template.
    CREATE UNIQUE INDEX UX_Doc_Template_Param
        ON dbo.Doc_Template_Param(Template_Id, Detail_Id, Param_Name) WHERE IsDeleted = 0;
    CREATE INDEX IX_Doc_Template_Param_Detail ON dbo.Doc_Template_Param(Detail_Id);
    PRINT N'✔ Đã tạo bảng Doc_Template_Param.';
END
ELSE
    PRINT N'• Doc_Template_Param đã tồn tại — bỏ qua.';
GO

PRINT N'Migration 077 completed — 4 bảng Doc_Template* (xuất Word/PDF theo mẫu).';
GO
