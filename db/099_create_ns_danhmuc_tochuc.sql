-- =============================================================================
-- File    : 099_create_ns_danhmuc_tochuc.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Tạo 5 danh mục tổ chức/nhân sự đợt NS_ (Position Management):
--             - NS_NhomChucDanh   (nhóm/họ chức danh — tự tham chiếu 2 cấp)
--             - NS_ChucDanh       (chức danh / job — "làm gì")
--             - NS_ChucVu         (chức vụ / rank — "hàm quản lý")
--             - NS_LoaiBienDong   (danh mục RIÊNG của biến động + cờ hành vi)
--             - NS_LoaiQuyetDinh  (danh mục loại quyết định — định tuyến người ký)
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.1, §7.3, §7.5  · ADR-022.
-- Design  : Chức danh (job) TÁCH khỏi chức vụ (rank). NS_LoaiBienDong KHÔNG nối
--           NS_LoaiQuyetDinh (2 trục độc lập; biến động NS chỉ là 1 loại quyết định).
-- Prereq  : 037 (nền TC_/DM_/HT_).
-- Convention (theo 037/097): auto block CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/
--           IsDeleted/Ver; Ma filtered UNIQUE WHERE IsDeleted = 0; CreatedBy/UpdatedBy
--           KHÔNG đặt FK. Idempotent — IF OBJECT_ID(...) IS NULL. KHÔNG seed ở đây (xem 103).
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── NS_NhomChucDanh (tự tham chiếu 2 cấp: họ nghề → nhóm chức năng) ─────────
IF OBJECT_ID(N'dbo.NS_NhomChucDanh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhomChucDanh
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        Ma            NVARCHAR(30)  NOT NULL,
        Ten           NVARCHAR(200) NOT NULL,
        Nhom_Cha_Id   BIGINT        NULL,               -- self; NULL = họ nghề gốc
        ThuTu         INT           NOT NULL DEFAULT 0,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhomChucDanh PRIMARY KEY (Id),
        CONSTRAINT FK_NS_NhomChucDanh_Cha FOREIGN KEY (Nhom_Cha_Id) REFERENCES dbo.NS_NhomChucDanh (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_NhomChucDanh_Ma ON dbo.NS_NhomChucDanh (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── NS_ChucDanh (job) ──────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.NS_ChucDanh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_ChucDanh
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(30)  NOT NULL,
        Ten               NVARCHAR(200) NOT NULL,
        NhomChucDanh_Id   BIGINT        NOT NULL,       -- → NS_NhomChucDanh
        MoTa              NVARCHAR(500) NULL,           -- JD tóm tắt

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_ChucDanh PRIMARY KEY (Id),
        CONSTRAINT FK_NS_ChucDanh_Nhom FOREIGN KEY (NhomChucDanh_Id) REFERENCES dbo.NS_NhomChucDanh (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_ChucDanh_Ma ON dbo.NS_ChucDanh (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_NS_ChucDanh_Nhom ON dbo.NS_ChucDanh (NhomChucDanh_Id);
END;
GO

-- ── NS_ChucVu (rank) ───────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.NS_ChucVu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_ChucVu
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        Ma            NVARCHAR(30)  NOT NULL,
        Ten           NVARCHAR(200) NOT NULL,
        CapQuanLy     INT           NOT NULL DEFAULT 0, -- 0=NV … n=cấp cao (dựng cây báo cáo)
        LaLanhDao     BIT           NOT NULL DEFAULT 0, -- 1 → suy "trưởng đơn vị" của phòng ban
        ThuTu         INT           NOT NULL DEFAULT 0,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_ChucVu PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_ChucVu_Ma ON dbo.NS_ChucVu (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── NS_LoaiBienDong (danh mục RIÊNG của NS_BienDongNhanSu + cờ hành vi) ─────
IF OBJECT_ID(N'dbo.NS_LoaiBienDong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_LoaiBienDong
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(30)  NOT NULL,       -- BO_NHIEM/DIEU_DONG/KIEM_NHIEM…
        Ten               NVARCHAR(100) NOT NULL,
        -- Cờ hành vi (engine chạy theo dữ liệu, không hardcode)
        MoGheMoi          BIT           NOT NULL DEFAULT 1, -- mở bản giữ-ghế mới?
        DongGheCu         BIT           NOT NULL DEFAULT 0, -- đóng bản đang hiệu lực?
        DongTatCaGhe      BIT           NOT NULL DEFAULT 0, -- đóng TẤT CẢ ghế (NGHI_VIEC)
        LaKiemNhiem       BIT           NOT NULL DEFAULT 0, -- bản mới giữ song song
        YeuCauQuyetDinh   BIT           NOT NULL DEFAULT 1, -- bắt buộc số/ngày QĐ
        LaHeThong         BIT           NOT NULL DEFAULT 0, -- loại chuẩn — khóa xóa
        ThuTu             INT           NOT NULL DEFAULT 0,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_LoaiBienDong PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_LoaiBienDong_Ma ON dbo.NS_LoaiBienDong (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── NS_LoaiQuyetDinh (taxonomy ĐỘC LẬP — định tuyến người ký) ───────────────
IF OBJECT_ID(N'dbo.NS_LoaiQuyetDinh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_LoaiQuyetDinh
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        Ma            NVARCHAR(30)  NOT NULL,       -- BIEN_DONG_NHAN_SU/HOP_DONG/KY_LUAT…
        Ten           NVARCHAR(200) NOT NULL,
        Nhom          NVARCHAR(30)  NULL,           -- HopDong/BienDong/Luong/KhenThuong/KyLuat/Khac
        LaHeThong     BIT           NOT NULL DEFAULT 0,
        ThuTu         INT           NOT NULL DEFAULT 0,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_LoaiQuyetDinh PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_LoaiQuyetDinh_Ma ON dbo.NS_LoaiQuyetDinh (Ma) WHERE IsDeleted = 0;
END;
GO
