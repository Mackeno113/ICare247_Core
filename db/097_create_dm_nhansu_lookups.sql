-- =============================================================================
-- File    : 097_create_dm_nhansu_lookups.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Tạo 6 danh mục DM_ dùng chung cho đợt NS_ (Nhân sự):
--           DM_DanToc, DM_TonGiao, DM_TrinhDoHocVan, DM_NgoaiNgu,
--           DM_QuanHeThanNhan, DM_NoiKCB.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7  · ADR-022.
-- Convention (theo 037):
--   - PK = Id BIGINT IDENTITY. Cột nghiệp vụ tiếng Việt không dấu. FK = {Bang}_Id.
--   - Khối auto MỌI bảng: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, IsDeleted, Ver.
--   - Ma: filtered UNIQUE WHERE IsDeleted = 0.  CreatedBy/UpdatedBy KHÔNG đặt FK.
-- Note    : Idempotent. KHÔNG seed dữ liệu ở đây — danh mục thật nạp ở migration seed riêng.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── DM_DanToc (54 dân tộc VN) ──────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_DanToc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_DanToc
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'KINH','TAY','THAI'
        Ten         NVARCHAR(100)   NOT NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_DanToc PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_DanToc_Ma ON dbo.DM_DanToc (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_TonGiao ─────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_TonGiao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_TonGiao
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'KHONG','PHATGIAO','CONGGIAO'
        Ten         NVARCHAR(100)   NOT NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_TonGiao PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_TonGiao_Ma ON dbo.DM_TonGiao (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_TrinhDoHocVan (bằng cấp cao nhất — có thứ tự để lọc/so sánh) ─────────
IF OBJECT_ID(N'dbo.DM_TrinhDoHocVan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_TrinhDoHocVan
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'THPT','TC','CD','DH','THS','TS'
        Ten         NVARCHAR(100)   NOT NULL,
        CapDo       INT             NOT NULL DEFAULT 0, -- 0 thấp → n cao, dùng so sánh

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_TrinhDoHocVan PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_TrinhDoHocVan_Ma ON dbo.DM_TrinhDoHocVan (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_NgoaiNgu ────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_NgoaiNgu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_NgoaiNgu
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'EN','FR','JP','CN','KR'
        Ten         NVARCHAR(100)   NOT NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_NgoaiNgu PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_NgoaiNgu_Ma ON dbo.DM_NgoaiNgu (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_QuanHeThanNhan (quan hệ với người thân) ─────────────────────────────
IF OBJECT_ID(N'dbo.DM_QuanHeThanNhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_QuanHeThanNhan
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'CHA','ME','VO','CHONG','CON'
        Ten         NVARCHAR(100)   NOT NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_QuanHeThanNhan PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_QuanHeThanNhan_Ma ON dbo.DM_QuanHeThanNhan (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_NoiKCB (nơi đăng ký khám chữa bệnh ban đầu — BHYT) ───────────────────
IF OBJECT_ID(N'dbo.DM_NoiKCB', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_NoiKCB
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(30)    NOT NULL,           -- mã cơ sở KCB (theo BHXH)
        Ten         NVARCHAR(300)   NOT NULL,
        DiaChi      NVARCHAR(500)   NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_NoiKCB PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_NoiKCB_Ma ON dbo.DM_NoiKCB (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_DM_NoiKCB_Ten ON dbo.DM_NoiKCB (Ten);
END;
GO
