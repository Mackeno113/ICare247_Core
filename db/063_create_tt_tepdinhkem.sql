-- =============================================================================
-- File    : 063_create_tt_tepdinhkem.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Module upload file (TT_ — tệp đính kèm). Tạo bảng TT_TepDinhKem lưu file
--           (logo công ty + đính kèm khác) + FK TC_CongTy.Logo_Id → TT_TepDinhKem.Id.
-- Quyết định lưu trữ: bytes TRONG DB (VARBINARY) — phương án A (logo nhỏ, ít; nhất quán
--           giao dịch, portable đa máy, cô lập theo tenant). Cột Storage_Kind/Storage_Key
--           để VỀ SAU cắm backend FileSystem (đường dẫn TƯƠNG ĐỐI) / Object storage cho file lớn.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md (nhóm TT_) · ADR-022 (khối audit mọi bảng).
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── TT_TepDinhKem ────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_TepDinhKem
    (
        Id            BIGINT          IDENTITY(1,1) NOT NULL,
        TenFile       NVARCHAR(255)   NOT NULL,                       -- tên gốc khi upload
        ContentType   NVARCHAR(100)   NOT NULL,                       -- image/png, image/jpeg, ...
        KichThuoc     BIGINT          NOT NULL DEFAULT 0,             -- bytes
        Storage_Kind  NVARCHAR(20)    NOT NULL DEFAULT N'Db',         -- Db | FileSystem | Object
        NoiDung       VARBINARY(MAX)  NULL,                           -- bytes (khi Storage_Kind = Db)
        Storage_Key   NVARCHAR(500)   NULL,                           -- path tương đối / object key (tương lai)
        Loai          NVARCHAR(30)    NULL,                           -- phân loại: 'Logo', ...
        Checksum      NVARCHAR(64)    NULL,                           -- sha256 (ETag + toàn vẹn + dedup)

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

-- ── FK TC_CongTy.Logo_Id → TT_TepDinhKem.Id (cột Logo_Id đã có sẵn) ───────────
IF OBJECT_ID(N'dbo.TC_CongTy', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.TC_CongTy', 'Logo_Id') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_TC_CongTy_Logo' AND parent_object_id = OBJECT_ID('dbo.TC_CongTy'))
BEGIN
    ALTER TABLE dbo.TC_CongTy
        ADD CONSTRAINT FK_TC_CongTy_Logo FOREIGN KEY (Logo_Id) REFERENCES dbo.TT_TepDinhKem (Id);
END;
GO

PRINT N'Migration 063 completed — TT_TepDinhKem + FK TC_CongTy.Logo_Id.';
GO
