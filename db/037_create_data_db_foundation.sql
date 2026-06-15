-- =============================================================================
-- File    : 037_create_data_db_foundation.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Tạo nhóm bảng NỀN TẢNG cho Data DB per-tenant (ICare247_Data của 1 tenant):
--           DM_ (danh mục dùng chung), TC_ (tổ chức), HT_ (người dùng + phân quyền).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md  · ADR-018/019/022.
-- Convention:
--   - DB-per-tenant  → KHÔNG có cột Tenant_Id.
--   - PK = Id BIGINT IDENTITY. Cột nghiệp vụ tiếng Việt không dấu. FK = {Bang}_Id.
--   - Khối auto MỌI bảng: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, IsDeleted, Ver.
--   - Ma (khi có): filtered UNIQUE WHERE IsDeleted = 0.
--   - CreatedBy/UpdatedBy KHÔNG đặt FK (tránh vòng lặp bootstrap) — enforce ở tầng App.
-- Context : Chạy TRONG ngữ cảnh Data DB của tenant (provisioning tool chọn DB trước).
--           KHÔNG CREATE DATABASE / USE ở đây để dùng lại cho mọi tenant.
-- Note    : Idempotent — IF OBJECT_ID(...) IS NULL trước mỗi bảng/FK.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 1. DM_ — DANH MỤC DÙNG CHUNG
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── DM_QuocGia ─────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_QuocGia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_QuocGia
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(10)    NOT NULL,           -- ISO alpha-2/3: 'VN','US'
        Ten         NVARCHAR(150)   NOT NULL,
        MaDienThoai NVARCHAR(10)    NULL,               -- '+84'

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_QuocGia PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_QuocGia_Ma ON dbo.DM_QuocGia (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_TinhThanhPho (cấp 1 hành chính — mô hình VN 2025, 2 cấp) ─────────────
IF OBJECT_ID(N'dbo.DM_TinhThanhPho', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_TinhThanhPho
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,
        Ten         NVARCHAR(150)   NOT NULL,
        LoaiHinh    NVARCHAR(20)    NULL,               -- 'Tinh' / 'ThanhPhoTW'
        QuocGia_Id  BIGINT          NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_TinhThanhPho PRIMARY KEY (Id),
        CONSTRAINT FK_DM_TinhThanhPho_QuocGia FOREIGN KEY (QuocGia_Id) REFERENCES dbo.DM_QuocGia (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_TinhThanhPho_Ma ON dbo.DM_TinhThanhPho (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_DM_TinhThanhPho_Ten ON dbo.DM_TinhThanhPho (Ten);
END;
GO

-- ── DM_PhuongXa (cấp 2 — trực thuộc thẳng Tỉnh, không qua Huyện) ────────────
IF OBJECT_ID(N'dbo.DM_PhuongXa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_PhuongXa
    (
        Id              BIGINT          IDENTITY(1,1) NOT NULL,
        Ma              NVARCHAR(20)    NOT NULL,
        Ten             NVARCHAR(150)   NOT NULL,
        LoaiHinh        NVARCHAR(20)    NULL,           -- 'Phuong' / 'Xa' / 'ThiTran'
        TinhThanhPho_Id BIGINT          NOT NULL,

        CreatedBy       BIGINT          NOT NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy       BIGINT          NULL,
        UpdatedAt       DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        Ver             INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_PhuongXa PRIMARY KEY (Id),
        CONSTRAINT FK_DM_PhuongXa_Tinh FOREIGN KEY (TinhThanhPho_Id) REFERENCES dbo.DM_TinhThanhPho (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_PhuongXa_Ma ON dbo.DM_PhuongXa (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_DM_PhuongXa_Tinh ON dbo.DM_PhuongXa (TinhThanhPho_Id);
END;
GO

-- ── DM_DonViTinh ───────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_DonViTinh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_DonViTinh
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'KG','CAI','THUNG'
        Ten         NVARCHAR(100)   NOT NULL,
        GhiChu      NVARCHAR(255)   NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_DonViTinh PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_DonViTinh_Ma ON dbo.DM_DonViTinh (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── DM_NganHang ────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.DM_NganHang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DM_NganHang
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'VCB','BIDV'
        Ten         NVARCHAR(200)   NOT NULL,
        TenVietTat  NVARCHAR(50)    NULL,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_DM_NganHang PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_DM_NganHang_Ma ON dbo.DM_NganHang (Ma) WHERE IsDeleted = 0;
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 2. TC_ — TỔ CHỨC (2 cây tự tham chiếu: Công ty, Phòng ban)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── TC_CapCongTy (danh mục cấp công ty) ────────────────────────────────────
IF OBJECT_ID(N'dbo.TC_CapCongTy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TC_CapCongTy
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'TONGCT','CT','CN','VPDD'
        Ten         NVARCHAR(100)   NOT NULL,
        ThuTu       INT             NOT NULL DEFAULT 0,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TC_CapCongTy PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_TC_CapCongTy_Ma ON dbo.TC_CapCongTy (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── TC_CapPhongBan (danh mục cấp phòng ban) ────────────────────────────────
IF OBJECT_ID(N'dbo.TC_CapPhongBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TC_CapPhongBan
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'KHOI','PHONG','TO','NHOM'
        Ten         NVARCHAR(100)   NOT NULL,
        ThuTu       INT             NOT NULL DEFAULT 0,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TC_CapPhongBan PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_TC_CapPhongBan_Ma ON dbo.TC_CapPhongBan (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── TC_CongTy (cây 1 — self ref CongTy_Cha_Id) ─────────────────────────────
-- Tỉnh/Thành suy qua DM_PhuongXa.TinhThanhPho_Id (không lưu trùng).
IF OBJECT_ID(N'dbo.TC_CongTy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TC_CongTy
    (
        Id              BIGINT          IDENTITY(1,1) NOT NULL,
        Ma              NVARCHAR(50)    NOT NULL,
        Ten             NVARCHAR(300)   NOT NULL,
        TenVietTat      NVARCHAR(100)   NULL,
        CongTy_Cha_Id   BIGINT          NULL,           -- NULL = gốc (tổng công ty)
        CapCongTy_Id    BIGINT          NOT NULL,
        MaSoThue        NVARCHAR(20)    NULL,
        DiaChi          NVARCHAR(500)   NULL,
        PhuongXa_Id     BIGINT          NULL,
        DienThoai       NVARCHAR(50)    NULL,
        Email           NVARCHAR(150)   NULL,
        Website         NVARCHAR(200)   NULL,
        NguoiDaiDien    NVARCHAR(200)   NULL,
        GiamDoc         NVARCHAR(200)   NULL,
        KeToanTruong    NVARCHAR(200)   NULL,
        NganHang_Id     BIGINT          NULL,
        SoTaiKhoan      NVARCHAR(50)    NULL,
        Logo_Id         BIGINT          NULL,           -- → TT_TepDinhKem (đợt sau)
        TrangThai       NVARCHAR(20)    NOT NULL DEFAULT N'HoatDong',

        CreatedBy       BIGINT          NOT NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy       BIGINT          NULL,
        UpdatedAt       DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        Ver             INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_TC_CongTy PRIMARY KEY (Id),
        CONSTRAINT FK_TC_CongTy_Cha     FOREIGN KEY (CongTy_Cha_Id) REFERENCES dbo.TC_CongTy (Id),
        CONSTRAINT FK_TC_CongTy_Cap     FOREIGN KEY (CapCongTy_Id)  REFERENCES dbo.TC_CapCongTy (Id),
        CONSTRAINT FK_TC_CongTy_PhuongXa FOREIGN KEY (PhuongXa_Id)  REFERENCES dbo.DM_PhuongXa (Id),
        CONSTRAINT FK_TC_CongTy_NganHang FOREIGN KEY (NganHang_Id)  REFERENCES dbo.DM_NganHang (Id)
    );
    CREATE UNIQUE INDEX UQ_TC_CongTy_Ma ON dbo.TC_CongTy (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_TC_CongTy_Cha ON dbo.TC_CongTy (CongTy_Cha_Id);
    CREATE INDEX IX_TC_CongTy_Ten ON dbo.TC_CongTy (Ten);
END;
GO

-- ── TC_PhongBan (cây 2 — self ref PhongBan_Cha_Id, thuộc 1 công ty) ────────
-- FK TruongDonVi_Id → HT_NguoiDung thêm sau (ALTER ở mục 4 — tránh vòng lặp).
IF OBJECT_ID(N'dbo.TC_PhongBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TC_PhongBan
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(50)  NOT NULL,
        Ten               NVARCHAR(300) NOT NULL,
        TenVietTat        NVARCHAR(100) NULL,
        PhongBan_Cha_Id   BIGINT        NULL,           -- NULL = gốc trong công ty
        CongTy_Id         BIGINT        NOT NULL,
        CapPhongBan_Id    BIGINT        NOT NULL,
        TruongDonVi_Id    BIGINT        NULL,           -- → HT_NguoiDung (FK thêm sau)
        DienThoai         NVARCHAR(50)  NULL,
        Email             NVARCHAR(150) NULL,
        ThuTu             INT           NOT NULL DEFAULT 0,
        TrangThai         NVARCHAR(20)  NOT NULL DEFAULT N'HoatDong',

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_TC_PhongBan PRIMARY KEY (Id),
        CONSTRAINT FK_TC_PhongBan_Cha    FOREIGN KEY (PhongBan_Cha_Id) REFERENCES dbo.TC_PhongBan (Id),
        CONSTRAINT FK_TC_PhongBan_CongTy FOREIGN KEY (CongTy_Id)       REFERENCES dbo.TC_CongTy (Id),
        CONSTRAINT FK_TC_PhongBan_Cap    FOREIGN KEY (CapPhongBan_Id)  REFERENCES dbo.TC_CapPhongBan (Id)
    );
    CREATE UNIQUE INDEX UQ_TC_PhongBan_Ma ON dbo.TC_PhongBan (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_TC_PhongBan_Cha    ON dbo.TC_PhongBan (PhongBan_Cha_Id);
    CREATE INDEX IX_TC_PhongBan_CongTy ON dbo.TC_PhongBan (CongTy_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 3. HT_ — HỆ THỐNG (Identity + Phân quyền) — toàn bộ ở Data DB
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── HT_NguoiDung ───────────────────────────────────────────────────────────
-- NhanVien_Id tạm nullable (siết NOT NULL+FK+UNIQUE ở đợt NS_). HoTen/Email/ĐT lấy từ NhanVien.
IF OBJECT_ID(N'dbo.HT_NguoiDung', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_NguoiDung
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(50)  NOT NULL,
        TenDangNhap       NVARCHAR(100) NOT NULL,
        LoaiTaiKhoan      NVARCHAR(20)  NOT NULL DEFAULT N'Local',   -- Local/AD/SSO/Portal
        MatKhauHash       NVARCHAR(256) NULL,                        -- PBKDF2; NULL khi AD/SSO
        NhanVien_Id       BIGINT        NULL,                        -- → NS_NhanVien (đợt NS_)
        CongTyMacDinh_Id  BIGINT        NULL,
        PhongBan_Id       BIGINT        NULL,
        TrangThai         NVARCHAR(20)  NOT NULL DEFAULT N'HoatDong',
        LaQuanTri         BIT           NOT NULL DEFAULT 0,
        KichHoatMobile    BIT           NOT NULL DEFAULT 0,
        HetHanTaiKhoan    DATETIME2     NULL,
        HinhThuc2FA       NVARCHAR(20)  NOT NULL DEFAULT N'None',    -- None/App/Email/SMS
        Khoa2FA           NVARCHAR(500) NULL,
        LanDangNhapCuoi   DATETIME2     NULL,
        LanDangXuatCuoi   DATETIME2     NULL,
        SoLanDangNhapSai  INT           NOT NULL DEFAULT 0,
        KhoaDenKhi        DATETIME2     NULL,
        DoiMatKhauLanSau  BIT           NOT NULL DEFAULT 0,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_NguoiDung PRIMARY KEY (Id),
        CONSTRAINT FK_HT_NguoiDung_CongTy  FOREIGN KEY (CongTyMacDinh_Id) REFERENCES dbo.TC_CongTy (Id),
        CONSTRAINT FK_HT_NguoiDung_PhongBan FOREIGN KEY (PhongBan_Id)     REFERENCES dbo.TC_PhongBan (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_Ma          ON dbo.HT_NguoiDung (Ma)          WHERE IsDeleted = 0;
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_TenDangNhap ON dbo.HT_NguoiDung (TenDangNhap) WHERE IsDeleted = 0;
END;
GO

-- ── HT_VaiTro ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.HT_VaiTro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_VaiTro
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(50)    NOT NULL,
        Ten         NVARCHAR(200)   NOT NULL,
        MoTa        NVARCHAR(500)   NULL,
        LaHeThong   BIT             NOT NULL DEFAULT 0,     -- vai trò hệ thống, không cho xóa

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_VaiTro PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_VaiTro_Ma ON dbo.HT_VaiTro (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── HT_ChucNang (cây chức năng / menu — self ref) ──────────────────────────
IF OBJECT_ID(N'dbo.HT_ChucNang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_ChucNang
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        Ma                NVARCHAR(100) NOT NULL,         -- 'HR.NhanVien'
        Ten               NVARCHAR(200) NOT NULL,
        ChucNang_Cha_Id   BIGINT        NULL,
        Loai              NVARCHAR(20)  NOT NULL DEFAULT N'Menu',   -- Menu/ManHinh/ChucNangCon
        Module            NVARCHAR(20)  NULL,             -- 'NS','TM','CN'...
        DuongDan          NVARCHAR(300) NULL,
        Icon              NVARCHAR(100) NULL,
        ThuTu             INT           NOT NULL DEFAULT 0,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_ChucNang PRIMARY KEY (Id),
        CONSTRAINT FK_HT_ChucNang_Cha FOREIGN KEY (ChucNang_Cha_Id) REFERENCES dbo.HT_ChucNang (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_ChucNang_Ma  ON dbo.HT_ChucNang (Ma) WHERE IsDeleted = 0;
    CREATE INDEX IX_HT_ChucNang_Cha ON dbo.HT_ChucNang (ChucNang_Cha_Id);
END;
GO

-- ── HT_NguoiDung_VaiTro (map N-N — không có Ma) ────────────────────────────
IF OBJECT_ID(N'dbo.HT_NguoiDung_VaiTro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_NguoiDung_VaiTro
    (
        Id            BIGINT      IDENTITY(1,1) NOT NULL,
        NguoiDung_Id  BIGINT      NOT NULL,
        VaiTro_Id     BIGINT      NOT NULL,

        CreatedBy     BIGINT      NOT NULL,
        CreatedAt     DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT      NULL,
        UpdatedAt     DATETIME2   NULL,
        IsDeleted     BIT         NOT NULL DEFAULT 0,
        Ver           INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_NguoiDung_VaiTro PRIMARY KEY (Id),
        CONSTRAINT FK_HT_NDVT_NguoiDung FOREIGN KEY (NguoiDung_Id) REFERENCES dbo.HT_NguoiDung (Id),
        CONSTRAINT FK_HT_NDVT_VaiTro    FOREIGN KEY (VaiTro_Id)    REFERENCES dbo.HT_VaiTro (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_VaiTro ON dbo.HT_NguoiDung_VaiTro (NguoiDung_Id, VaiTro_Id) WHERE IsDeleted = 0;
END;
GO

-- ── HT_VaiTro_Quyen (map vai trò × chức năng + cờ thao tác) ─────────────────
IF OBJECT_ID(N'dbo.HT_VaiTro_Quyen', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_VaiTro_Quyen
    (
        Id            BIGINT      IDENTITY(1,1) NOT NULL,
        VaiTro_Id     BIGINT      NOT NULL,
        ChucNang_Id   BIGINT      NOT NULL,
        Xem           BIT         NOT NULL DEFAULT 0,
        Them          BIT         NOT NULL DEFAULT 0,
        Sua           BIT         NOT NULL DEFAULT 0,
        Xoa           BIT         NOT NULL DEFAULT 0,
        Duyet         BIT         NOT NULL DEFAULT 0,
        InAn          BIT         NOT NULL DEFAULT 0,

        CreatedBy     BIGINT      NOT NULL,
        CreatedAt     DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT      NULL,
        UpdatedAt     DATETIME2   NULL,
        IsDeleted     BIT         NOT NULL DEFAULT 0,
        Ver           INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_VaiTro_Quyen PRIMARY KEY (Id),
        CONSTRAINT FK_HT_VTQ_VaiTro   FOREIGN KEY (VaiTro_Id)   REFERENCES dbo.HT_VaiTro (Id),
        CONSTRAINT FK_HT_VTQ_ChucNang FOREIGN KEY (ChucNang_Id) REFERENCES dbo.HT_ChucNang (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_VaiTro_Quyen ON dbo.HT_VaiTro_Quyen (VaiTro_Id, ChucNang_Id) WHERE IsDeleted = 0;
END;
GO

-- ── HT_NguoiDung_CongTy (phạm vi dữ liệu — company switcher) ────────────────
IF OBJECT_ID(N'dbo.HT_NguoiDung_CongTy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_NguoiDung_CongTy
    (
        Id            BIGINT      IDENTITY(1,1) NOT NULL,
        NguoiDung_Id  BIGINT      NOT NULL,
        CongTy_Id     BIGINT      NOT NULL,
        LaMacDinh     BIT         NOT NULL DEFAULT 0,

        CreatedBy     BIGINT      NOT NULL,
        CreatedAt     DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT      NULL,
        UpdatedAt     DATETIME2   NULL,
        IsDeleted     BIT         NOT NULL DEFAULT 0,
        Ver           INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_NguoiDung_CongTy PRIMARY KEY (Id),
        CONSTRAINT FK_HT_NDCT_NguoiDung FOREIGN KEY (NguoiDung_Id) REFERENCES dbo.HT_NguoiDung (Id),
        CONSTRAINT FK_HT_NDCT_CongTy    FOREIGN KEY (CongTy_Id)    REFERENCES dbo.TC_CongTy (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_CongTy ON dbo.HT_NguoiDung_CongTy (NguoiDung_Id, CongTy_Id) WHERE IsDeleted = 0;
END;
GO

-- ── HT_RefreshToken (phiên / refresh JWT — không có Ma) ─────────────────────
IF OBJECT_ID(N'dbo.HT_RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_RefreshToken
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        NguoiDung_Id  BIGINT        NOT NULL,
        TokenHash     NVARCHAR(200) NOT NULL,
        HetHan        DATETIME2     NOT NULL,
        DaThuHoi      BIT           NOT NULL DEFAULT 0,
        ThuHoiLuc     DATETIME2     NULL,
        DiaChiIp      NVARCHAR(50)  NULL,
        ThietBi       NVARCHAR(300) NULL,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_RefreshToken PRIMARY KEY (Id),
        CONSTRAINT FK_HT_RefreshToken_NguoiDung FOREIGN KEY (NguoiDung_Id) REFERENCES dbo.HT_NguoiDung (Id)
    );
    CREATE INDEX IX_HT_RefreshToken_NguoiDung ON dbo.HT_RefreshToken (NguoiDung_Id);
    CREATE INDEX IX_HT_RefreshToken_Hash      ON dbo.HT_RefreshToken (TokenHash);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 4. FK trễ — phá vòng lặp TC_PhongBan ↔ HT_NguoiDung
-- ═══════════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TC_PhongBan_TruongDonVi')
BEGIN
    ALTER TABLE dbo.TC_PhongBan
        ADD CONSTRAINT FK_TC_PhongBan_TruongDonVi
            FOREIGN KEY (TruongDonVi_Id) REFERENCES dbo.HT_NguoiDung (Id);
END;
GO
