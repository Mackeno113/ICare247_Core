-- =============================================================================
-- File    : dev/create_tt_attachment_full.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy TRONG ngữ cảnh Data DB của tenant)
-- Purpose : Tạo TRỌN nền hệ đính kèm trong 1 lần chạy = gộp db/063 (TT_TepDinhKem) + db/070
--           (TT_TepBlob + cột mở rộng + FK/index). Dùng khi DB CHƯA chạy 063/070 → sửa lỗi
--           "Invalid object name 'dbo.TT_TepDinhKem'". Idempotent: chạy lại nhiều lần vô hại.
-- Cách dùng: mở đúng Data DB tenant (VD: USE ICare247_Solution;) rồi chạy file này.
--            (KHÔNG tự USE/CREATE DATABASE ở đây để giữ đúng convention migration.)
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── 1. TT_TepDinhKem — bản ghi đính kèm (nền từ db/063) ──────────────────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_TepDinhKem
    (
        Id            BIGINT          IDENTITY(1,1) NOT NULL,
        TenFile       NVARCHAR(255)   NOT NULL,                       -- tên gốc khi upload
        ContentType   NVARCHAR(100)   NOT NULL,                       -- image/png, application/pdf, ...
        KichThuoc     BIGINT          NOT NULL DEFAULT 0,             -- bytes
        Storage_Kind  NVARCHAR(20)    NOT NULL DEFAULT N'Db',         -- Db | FileSystem | Object
        NoiDung       VARBINARY(MAX)  NULL,                           -- bytes (khi Storage_Kind = Db, dùng cho logo cũ)
        Storage_Key   NVARCHAR(500)   NULL,                           -- path tương đối / object key (tương lai)
        Loai          NVARCHAR(30)    NULL,                           -- phân loại: 'Logo', ...
        Checksum      NVARCHAR(64)    NULL,                           -- sha256 (ETag + toàn vẹn)

        CreatedBy     BIGINT          NOT NULL,
        CreatedAt     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT          NULL,
        UpdatedAt     DATETIME2       NULL,
        IsDeleted     BIT             NOT NULL DEFAULT 0,
        Ver           INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TT_TepDinhKem PRIMARY KEY (Id),
        CONSTRAINT CHK_TT_TepDinhKem_Storage CHECK (Storage_Kind IN (N'Db', N'FileSystem', N'Object'))
    );

    CREATE INDEX IX_TT_TepDinhKem_Loai ON dbo.TT_TepDinhKem (Loai, IsDeleted);
END;
GO

-- ── 2. TT_TepBlob — nội dung vật lý duy nhất theo Checksum (đơn vị dedup) ─────
IF OBJECT_ID(N'dbo.TT_TepBlob', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_TepBlob
    (
        Id            BIGINT          IDENTITY(1,1) NOT NULL,
        Checksum      NVARCHAR(64)    NOT NULL,                       -- sha256 hex — khóa dedup
        ContentType   NVARCHAR(100)   NOT NULL,
        KichThuoc     BIGINT          NOT NULL DEFAULT 0,
        Storage_Kind  NVARCHAR(20)    NOT NULL DEFAULT N'Db',         -- Db | FileSystem | Object
        NoiDung       VARBINARY(MAX)  NULL,                           -- bytes (khi Storage_Kind = Db)
        Storage_Key   NVARCHAR(500)   NULL,                           -- path TƯƠNG ĐỐI / object key
        RefCount      INT             NOT NULL DEFAULT 0,             -- số đính kèm trỏ tới; = 0 → dọn vật lý

        CreatedBy     BIGINT          NOT NULL,
        CreatedAt     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT          NULL,
        UpdatedAt     DATETIME2       NULL,
        IsDeleted     BIT             NOT NULL DEFAULT 0,
        Ver           INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TT_TepBlob PRIMARY KEY (Id),
        CONSTRAINT CHK_TT_TepBlob_Storage CHECK (Storage_Kind IN (N'Db', N'FileSystem', N'Object'))
    );

    CREATE UNIQUE INDEX UX_TT_TepBlob_Checksum ON dbo.TT_TepBlob (Checksum) WHERE IsDeleted = 0;
    CREATE INDEX IX_TT_TepBlob_Ref ON dbo.TT_TepBlob (RefCount, IsDeleted);
END;
GO

-- ── 3. Cột mở rộng TT_TepDinhKem (từ db/070) — thêm nếu thiếu ─────────────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Blob_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Blob_Id BIGINT NULL;

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'ThumbBlob_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD ThumbBlob_Id BIGINT NULL;

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Owner_Table') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Owner_Table NVARCHAR(128) NULL;

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Owner_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Owner_Id BIGINT NULL;

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Field_Ma') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Field_Ma NVARCHAR(128) NULL;
END;
GO

-- ── 4. FK Blob_Id / ThumbBlob_Id → TT_TepBlob.Id ─────────────────────────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.TT_TepBlob', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_TT_TepDinhKem_Blob' AND parent_object_id = OBJECT_ID('dbo.TT_TepDinhKem'))
        ALTER TABLE dbo.TT_TepDinhKem
            ADD CONSTRAINT FK_TT_TepDinhKem_Blob FOREIGN KEY (Blob_Id) REFERENCES dbo.TT_TepBlob (Id);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_TT_TepDinhKem_ThumbBlob' AND parent_object_id = OBJECT_ID('dbo.TT_TepDinhKem'))
        ALTER TABLE dbo.TT_TepDinhKem
            ADD CONSTRAINT FK_TT_TepDinhKem_ThumbBlob FOREIGN KEY (ThumbBlob_Id) REFERENCES dbo.TT_TepBlob (Id);
END;
GO

-- ── 5. Index tra đính kèm theo record chủ ────────────────────────────────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_TT_TepDinhKem_Owner' AND object_id = OBJECT_ID('dbo.TT_TepDinhKem'))
BEGIN
    CREATE INDEX IX_TT_TepDinhKem_Owner
        ON dbo.TT_TepDinhKem (Owner_Table, Owner_Id, IsDeleted);
END;
GO

-- ── 6. FK TC_CongTy.Logo_Id → TT_TepDinhKem.Id (nếu bảng/cột có sẵn) ──────────
IF OBJECT_ID(N'dbo.TC_CongTy', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.TC_CongTy', 'Logo_Id') IS NOT NULL
   AND OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_TC_CongTy_Logo' AND parent_object_id = OBJECT_ID('dbo.TC_CongTy'))
BEGIN
    ALTER TABLE dbo.TC_CongTy
        ADD CONSTRAINT FK_TC_CongTy_Logo FOREIGN KEY (Logo_Id) REFERENCES dbo.TT_TepDinhKem (Id);
END;
GO

PRINT N'OK — TT_TepDinhKem + TT_TepBlob + FK/index đã sẵn sàng (gộp db/063 + db/070).';
GO
