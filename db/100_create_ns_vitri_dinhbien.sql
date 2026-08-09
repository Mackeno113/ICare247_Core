-- =============================================================================
-- File    : 100_create_ns_vitri_dinhbien.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Tạo vị trí công việc ("ghế") + kế hoạch định biên 2 cấp năm/tháng:
--             - NS_ViTriCongViec  (Chức danh × Phòng ban × hàm; đơn vị định biên)
--             - NS_DinhBien       (kế hoạch định biên theo ViTri × Năm × Tháng)
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.2  · ADR-022.
-- Design  : Vị trí KHÔNG lưu CongTy_Id — suy qua PhongBan_Id → TC_PhongBan.CongTy_Id.
--           Định biên gắn theo VỊ TRÍ để rollup phòng ban/chức danh tự nhiên.
-- Prereq  : 037 (TC_PhongBan), 099 (NS_ChucDanh, NS_ChucVu).
-- Convention (theo 037/097/098): auto block; filtered UNIQUE WHERE IsDeleted = 0;
--           CreatedBy/UpdatedBy KHÔNG đặt FK. Idempotent — IF OBJECT_ID(...) IS NULL.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── NS_ViTriCongViec ("ghế") ───────────────────────────────────────────────
IF OBJECT_ID(N'dbo.NS_ViTriCongViec', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_ViTriCongViec
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(50)  NOT NULL,
        Ten               NVARCHAR(200) NULL,           -- tùy chọn / auto
        ChucDanh_Id       BIGINT        NOT NULL,       -- → NS_ChucDanh
        ChucVu_Id         BIGINT        NULL,           -- → NS_ChucVu (NULL = không hàm)
        PhongBan_Id       BIGINT        NOT NULL,       -- → TC_PhongBan (suy công ty)
        SoNguoiToiDa      INT           NOT NULL DEFAULT 1,  -- sức chứa ghế (>1 = pool/thời vụ)
        TrangThai         NVARCHAR(20)  NOT NULL DEFAULT N'Trong', -- Trong/DaBoTri/DongBang
        NgayHieuLuc       DATE          NULL,
        NgayHetHieuLuc    DATE          NULL,
        GhiChu            NVARCHAR(500) NULL,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_ViTriCongViec PRIMARY KEY (Id),
        CONSTRAINT FK_NS_ViTri_ChucDanh FOREIGN KEY (ChucDanh_Id) REFERENCES dbo.NS_ChucDanh (Id),
        CONSTRAINT FK_NS_ViTri_ChucVu   FOREIGN KEY (ChucVu_Id)   REFERENCES dbo.NS_ChucVu (Id),
        CONSTRAINT FK_NS_ViTri_PhongBan FOREIGN KEY (PhongBan_Id) REFERENCES dbo.TC_PhongBan (Id),
        CONSTRAINT CK_NS_ViTri_SoNguoi  CHECK (SoNguoiToiDa >= 1)
    );
    CREATE UNIQUE INDEX UQ_NS_ViTri_Ma       ON dbo.NS_ViTriCongViec (Ma) WHERE IsDeleted = 0;
    CREATE INDEX        IX_NS_ViTri_PhongBan ON dbo.NS_ViTriCongViec (PhongBan_Id);
    CREATE INDEX        IX_NS_ViTri_ChucDanh ON dbo.NS_ViTriCongViec (ChucDanh_Id);
END;
GO

-- ── NS_DinhBien (kế hoạch định biên 2 cấp: Thang NULL = năm; 1..12 = tháng) ──
IF OBJECT_ID(N'dbo.NS_DinhBien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_DinhBien
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        ViTri_Id          BIGINT        NOT NULL,       -- → NS_ViTriCongViec
        Nam               INT           NOT NULL,
        Thang             TINYINT       NULL,           -- NULL = định biên NĂM; 1..12 = THÁNG
        SoLuongDinhBien   INT           NOT NULL DEFAULT 0,
        TrangThai         NVARCHAR(20)  NOT NULL DEFAULT N'DuThao', -- DuThao/DaDuyet
        GhiChu            NVARCHAR(300) NULL,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_DinhBien PRIMARY KEY (Id),
        CONSTRAINT FK_NS_DinhBien_ViTri FOREIGN KEY (ViTri_Id) REFERENCES dbo.NS_ViTriCongViec (Id),
        CONSTRAINT CK_NS_DinhBien_Thang CHECK (Thang IS NULL OR Thang BETWEEN 1 AND 12)
    );
    -- Mỗi vị trí × năm chỉ 1 dòng năm (Thang NULL) và tối đa 1 dòng mỗi tháng.
    CREATE UNIQUE INDEX UQ_NS_DinhBien_ViTri_Ky ON dbo.NS_DinhBien (ViTri_Id, Nam, Thang) WHERE IsDeleted = 0;
END;
GO
