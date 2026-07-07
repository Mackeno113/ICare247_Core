-- =============================================================================
-- File    : 070_alter_tt_tep_blob_attachment.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Phase 1 hệ đính kèm tổng quát — tách NỘI DUNG VẬT LÝ (dedup) khỏi BẢN GHI ĐÍNH KÈM.
--           1) Tạo TT_TepBlob: đơn vị dedup theo Checksum (SHA256), RefCount, Storage_Kind
--              (Db|FileSystem|Object) — bytes trong DB HOẶC path/key tương đối (di dời gốc = đổi config).
--           2) Mở rộng TT_TepDinhKem: trỏ Blob_Id/ThumbBlob_Id + gắn Owner_Table/Owner_Id/Field_Ma
--              (đính kèm vào record/field của Form Engine). Giữ nguyên cột cũ (logo) → KHÔNG phá code hiện tại.
-- Ràng buộc: đường dẫn lưu trong DB LUÔN tương đối; gốc chứa (BaseRoot) nằm ở config theo deployment,
--            dùng chung mọi node sau load-balancer. Xem thiết kế Phase 1 (module Files).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md (nhóm TT_) · ADR-022 (khối audit mọi bảng).
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── 1. TT_TepBlob — nội dung vật lý duy nhất theo Checksum (đơn vị dedup) ──────
IF OBJECT_ID(N'dbo.TT_TepBlob', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_TepBlob
    (
        Id            BIGINT          IDENTITY(1,1) NOT NULL,
        Checksum      NVARCHAR(64)    NOT NULL,                       -- sha256 hex — khóa dedup
        ContentType   NVARCHAR(100)   NOT NULL,                       -- image/png, application/pdf, ...
        KichThuoc     BIGINT          NOT NULL DEFAULT 0,             -- bytes
        Storage_Kind  NVARCHAR(20)    NOT NULL DEFAULT N'Db',         -- Db | FileSystem | Object
        NoiDung       VARBINARY(MAX)  NULL,                           -- bytes (khi Storage_Kind = Db)
        Storage_Key   NVARCHAR(500)   NULL,                           -- path TƯƠNG ĐỐI / object key (FileSystem|Object)
        RefCount      INT             NOT NULL DEFAULT 0,             -- số bản ghi đính kèm đang trỏ tới; = 0 → job dọn xóa vật lý

        CreatedBy     BIGINT          NOT NULL,
        CreatedAt     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT          NULL,
        UpdatedAt     DATETIME2       NULL,
        IsDeleted     BIT             NOT NULL DEFAULT 0,
        Ver           INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TT_TepBlob PRIMARY KEY (Id),
        CONSTRAINT CHK_TT_TepBlob_Storage CHECK (Storage_Kind IN (N'Db', N'FileSystem', N'Object'))
    );

    -- Dedup: 1 checksum = 1 blob còn sống. Filtered (IsDeleted=0) để cho phép tái tạo sau khi đã dọn.
    CREATE UNIQUE INDEX UX_TT_TepBlob_Checksum ON dbo.TT_TepBlob (Checksum) WHERE IsDeleted = 0;
    -- Quét blob mồ côi (RefCount = 0) để dọn file vật lý.
    CREATE INDEX IX_TT_TepBlob_Ref ON dbo.TT_TepBlob (RefCount, IsDeleted);
END;
GO

-- ── 2. Mở rộng TT_TepDinhKem — bản ghi đính kèm trỏ tới Blob + gắn Owner/Field ─
-- Cột cũ (NoiDung/ContentType/KichThuoc/Storage_*/Checksum) GIỮ NGUYÊN cho tương thích logo hiện tại;
-- bản ghi mới dùng Blob_Id. Thêm cột đều NULL-able → migration thuần bổ sung, không phá dữ liệu/code cũ.
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Blob_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Blob_Id BIGINT NULL;            -- FK → TT_TepBlob (nội dung chính)

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'ThumbBlob_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD ThumbBlob_Id BIGINT NULL;       -- FK → TT_TepBlob (thumbnail ảnh, P4)

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Owner_Table') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Owner_Table NVARCHAR(128) NULL; -- bảng chủ (record đính kèm gắn vào)

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Owner_Id') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Owner_Id BIGINT NULL;           -- Id record chủ

    IF COL_LENGTH('dbo.TT_TepDinhKem', 'Field_Ma') IS NULL
        ALTER TABLE dbo.TT_TepDinhKem ADD Field_Ma NVARCHAR(128) NULL;    -- mã field (control Attachment) chứa tệp
END;
GO

-- ── 3. FK Blob_Id / ThumbBlob_Id → TT_TepBlob.Id ─────────────────────────────
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

-- ── 4. Index tra đính kèm theo record chủ (Owner_Table + Owner_Id) ───────────
IF OBJECT_ID(N'dbo.TT_TepDinhKem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_TT_TepDinhKem_Owner' AND object_id = OBJECT_ID('dbo.TT_TepDinhKem'))
BEGIN
    CREATE INDEX IX_TT_TepDinhKem_Owner
        ON dbo.TT_TepDinhKem (Owner_Table, Owner_Id, IsDeleted);
END;
GO

PRINT N'Migration 070 completed — TT_TepBlob (dedup) + TT_TepDinhKem mở rộng (Blob/Owner/Field).';
GO
