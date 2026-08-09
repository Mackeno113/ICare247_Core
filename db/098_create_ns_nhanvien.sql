-- =============================================================================
-- File    : 098_create_ns_nhanvien.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Tạo hồ sơ nhân viên đợt NS_:
--             - NS_NhanVien              (bảng lõi — thuộc tính con người, ổn định)
--             - NS_NhanVien_DiaChi       (địa chỉ: thường trú / tạm trú / quê quán)
--             - NS_NhanVien_HocVan       (quá trình học vấn)
--             - NS_NhanVien_NgoaiNgu     (trình độ ngoại ngữ)
--             - NS_NhanVien_ChungChi     (chứng chỉ)
--             - NS_NhanVien_ThanNhan     (nhân thân / người phụ thuộc)
--             - NS_NhanVien_GiayToNuocNgoai (hộ chiếu/visa/GPLĐ — chỉ người nước ngoài)
--           + FK trễ HT_NguoiDung.NhanVien_Id → NS_NhanVien (liên kết 1-1 tài khoản).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.3  · ADR-022.
-- Design  : Công ty/phòng ban/chức vụ/chức danh/vị trí KHÔNG lưu ở đây —
--           SUY từ NS_BienDongNhanSu đang hiệu lực (Position Management, §7.3).
-- Prereq  : 037 (DM_/TC_/HT_), 063 (TT_TepDinhKem), 097 (DM_ nhân sự).
-- Convention (theo 037): auto block CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver;
--           filtered UNIQUE WHERE IsDeleted = 0; CreatedBy/UpdatedBy KHÔNG đặt FK.
-- Note    : Idempotent — IF OBJECT_ID(...) IS NULL trước mỗi bảng/FK.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 1. NS_NhanVien — HỒ SƠ LÕI
-- ═══════════════════════════════════════════════════════════════════════════════
IF OBJECT_ID(N'dbo.NS_NhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien
    (
        Id                  BIGINT          IDENTITY(1,1) NOT NULL,

        -- A. Định danh & hiển thị
        MaNhanVien          NVARCHAR(30)    NOT NULL,           -- mã NV/tenant
        HoTen               NVARCHAR(150)   NOT NULL,           -- tài khoản lấy ké
        TenThuongDung       NVARCHAR(100)   NULL,
        AnhDaiDien_Id       BIGINT          NULL,               -- → TT_TepDinhKem

        -- B. Thông tin cá nhân
        NgaySinh            DATE            NOT NULL,
        GioiTinh            TINYINT         NOT NULL,           -- 0=Nữ, 1=Nam, 2=Khác
        NoiSinh             NVARCHAR(200)   NULL,
        NoiSinh_PhuongXa_Id BIGINT          NULL,               -- → DM_PhuongXa (suy tỉnh)
        QuocTich_Id         BIGINT          NOT NULL,           -- → DM_QuocGia (mặc định VN)
        DanToc_Id           BIGINT          NULL,               -- → DM_DanToc
        TonGiao_Id          BIGINT          NULL,               -- → DM_TonGiao
        TinhTrangHonNhan    TINYINT         NULL,               -- 0=Độc thân,1=Kết hôn,2=Ly hôn,3=Góa
        NhomMau             NVARCHAR(5)     NULL,               -- A/B/O/AB (±)
        ChieuCao            SMALLINT        NULL,               -- cm
        CanNang             SMALLINT        NULL,               -- kg

        -- C. Giấy tờ tùy thân & thuế
        SoCCCD              NVARCHAR(20)    NULL,               -- bắt buộc với người VN (App validate)
        NgayCapCCCD         DATE            NULL,
        NoiCapCCCD          NVARCHAR(200)   NULL,
        SoCMND              NVARCHAR(15)    NULL,               -- chỉ giữ cho dữ liệu cũ
        MaSoThue            NVARCHAR(20)    NULL,               -- MST cá nhân

        -- D. Liên hệ (App validate: bắt buộc ≥1 trong DienThoai/Email)
        DienThoai           NVARCHAR(20)    NULL,
        Email               NVARCHAR(150)   NULL,               -- email công việc (tài khoản lấy ké)
        EmailCaNhan         NVARCHAR(150)   NULL,
        NguoiLienHeKhan     NVARCHAR(150)   NULL,
        DienThoaiKhan       NVARCHAR(20)    NULL,
        QuanHeLienHeKhan    NVARCHAR(50)    NULL,

        -- E. Bảo hiểm (ID cá nhân)
        SoBHXH              NVARCHAR(15)    NULL,
        SoBHYT              NVARCHAR(20)    NULL,
        NoiKhamChuaBenh_Id  BIGINT          NULL,               -- → DM_NoiKCB

        -- F. Ngân hàng (tài khoản chính)
        NganHang_Id         BIGINT          NULL,               -- → DM_NganHang
        ChiNhanhNganHang    NVARCHAR(150)   NULL,
        SoTaiKhoan          NVARCHAR(30)    NULL,
        TenChuTaiKhoan      NVARCHAR(150)   NULL,

        -- G. Học vấn (trình độ cao nhất; chi tiết ở NS_NhanVien_HocVan)
        TrinhDoHocVan_Id    BIGINT          NULL,               -- → DM_TrinhDoHocVan

        -- H. Mốc gốc & di trú dữ liệu
        NgayBatDauLamViec   DATE            NOT NULL,           -- mốc thâm niên
        MaNVCu              NVARCHAR(30)    NULL,               -- mã hệ thống cũ khi import
        GhiChu              NVARCHAR(500)   NULL,

        -- Khối auto
        CreatedBy           BIGINT          NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy           BIGINT          NULL,
        UpdatedAt           DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0,
        Ver                 INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien PRIMARY KEY (Id),
        CONSTRAINT FK_NS_NhanVien_QuocTich  FOREIGN KEY (QuocTich_Id)         REFERENCES dbo.DM_QuocGia (Id),
        CONSTRAINT FK_NS_NhanVien_DanToc    FOREIGN KEY (DanToc_Id)           REFERENCES dbo.DM_DanToc (Id),
        CONSTRAINT FK_NS_NhanVien_TonGiao   FOREIGN KEY (TonGiao_Id)          REFERENCES dbo.DM_TonGiao (Id),
        CONSTRAINT FK_NS_NhanVien_NoiSinh   FOREIGN KEY (NoiSinh_PhuongXa_Id) REFERENCES dbo.DM_PhuongXa (Id),
        CONSTRAINT FK_NS_NhanVien_TrinhDo   FOREIGN KEY (TrinhDoHocVan_Id)    REFERENCES dbo.DM_TrinhDoHocVan (Id),
        CONSTRAINT FK_NS_NhanVien_NganHang  FOREIGN KEY (NganHang_Id)         REFERENCES dbo.DM_NganHang (Id),
        CONSTRAINT FK_NS_NhanVien_NoiKCB    FOREIGN KEY (NoiKhamChuaBenh_Id)  REFERENCES dbo.DM_NoiKCB (Id),
        CONSTRAINT FK_NS_NhanVien_Anh       FOREIGN KEY (AnhDaiDien_Id)       REFERENCES dbo.TT_TepDinhKem (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_NhanVien_Ma   ON dbo.NS_NhanVien (MaNhanVien) WHERE IsDeleted = 0;
    CREATE UNIQUE INDEX UQ_NS_NhanVien_CCCD ON dbo.NS_NhanVien (SoCCCD)     WHERE IsDeleted = 0 AND SoCCCD IS NOT NULL;
    CREATE UNIQUE INDEX UQ_NS_NhanVien_BHXH ON dbo.NS_NhanVien (SoBHXH)     WHERE IsDeleted = 0 AND SoBHXH IS NOT NULL;
    CREATE INDEX IX_NS_NhanVien_HoTen ON dbo.NS_NhanVien (HoTen);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 2. BẢNG CON 1-N
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── NS_NhanVien_DiaChi (1 địa chỉ mỗi loại) ────────────────────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_DiaChi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_DiaChi
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id   BIGINT        NOT NULL,
        LoaiDiaChi    TINYINT       NOT NULL,           -- 1=Thường trú, 2=Tạm trú, 3=Quê quán
        SoNha         NVARCHAR(200) NULL,
        PhuongXa_Id   BIGINT        NULL,               -- → DM_PhuongXa (suy tỉnh)
        LaChuHo       BIT           NOT NULL DEFAULT 0,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_DiaChi PRIMARY KEY (Id),
        CONSTRAINT FK_NS_DiaChi_NhanVien FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_DiaChi_PhuongXa FOREIGN KEY (PhuongXa_Id) REFERENCES dbo.DM_PhuongXa (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_DiaChi_NV_Loai ON dbo.NS_NhanVien_DiaChi (NhanVien_Id, LoaiDiaChi) WHERE IsDeleted = 0;
    CREATE INDEX IX_NS_DiaChi_NhanVien ON dbo.NS_NhanVien_DiaChi (NhanVien_Id);
END;
GO

-- ── NS_NhanVien_HocVan ─────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_HocVan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_HocVan
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id       BIGINT        NOT NULL,
        TruongDaoTao      NVARCHAR(200) NULL,
        ChuyenNganh       NVARCHAR(200) NULL,
        TrinhDoHocVan_Id  BIGINT        NULL,               -- → DM_TrinhDoHocVan
        HeDaoTao          NVARCHAR(100) NULL,               -- Chính quy/Tại chức/Từ xa...
        XepLoai           NVARCHAR(50)  NULL,
        NamTotNghiep      SMALLINT      NULL,
        VanBang_File_Id   BIGINT        NULL,               -- → TT_TepDinhKem

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_HocVan PRIMARY KEY (Id),
        CONSTRAINT FK_NS_HocVan_NhanVien FOREIGN KEY (NhanVien_Id)      REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_HocVan_TrinhDo  FOREIGN KEY (TrinhDoHocVan_Id) REFERENCES dbo.DM_TrinhDoHocVan (Id),
        CONSTRAINT FK_NS_HocVan_VanBang  FOREIGN KEY (VanBang_File_Id)  REFERENCES dbo.TT_TepDinhKem (Id)
    );
    CREATE INDEX IX_NS_HocVan_NhanVien ON dbo.NS_NhanVien_HocVan (NhanVien_Id);
END;
GO

-- ── NS_NhanVien_NgoaiNgu (1 dòng mỗi ngoại ngữ) ────────────────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_NgoaiNgu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_NgoaiNgu
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id   BIGINT        NOT NULL,
        NgoaiNgu_Id   BIGINT        NOT NULL,           -- → DM_NgoaiNgu
        TrinhDo       NVARCHAR(50)  NULL,               -- A/B/C/B1/IELTS 6.5...
        XepLoai       NVARCHAR(50)  NULL,
        TenChungChi   NVARCHAR(150) NULL,
        NgayCap       DATE          NULL,
        NgayHetHan    DATE          NULL,

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_NgoaiNgu PRIMARY KEY (Id),
        CONSTRAINT FK_NS_NgoaiNgu_NhanVien FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_NgoaiNgu_DM       FOREIGN KEY (NgoaiNgu_Id) REFERENCES dbo.DM_NgoaiNgu (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_NgoaiNgu_NV ON dbo.NS_NhanVien_NgoaiNgu (NhanVien_Id, NgoaiNgu_Id) WHERE IsDeleted = 0;
    CREATE INDEX IX_NS_NgoaiNgu_NhanVien ON dbo.NS_NhanVien_NgoaiNgu (NhanVien_Id);
END;
GO

-- ── NS_NhanVien_ChungChi ───────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_ChungChi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_ChungChi
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id   BIGINT        NOT NULL,
        TenChungChi   NVARCHAR(200) NOT NULL,
        NoiCap        NVARCHAR(200) NULL,
        NgayCap       DATE          NULL,
        NgayHetHan    DATE          NULL,
        File_Id       BIGINT        NULL,               -- → TT_TepDinhKem

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_ChungChi PRIMARY KEY (Id),
        CONSTRAINT FK_NS_ChungChi_NhanVien FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_ChungChi_File     FOREIGN KEY (File_Id)     REFERENCES dbo.TT_TepDinhKem (Id)
    );
    CREATE INDEX IX_NS_ChungChi_NhanVien ON dbo.NS_NhanVien_ChungChi (NhanVien_Id);
END;
GO

-- ── NS_NhanVien_ThanNhan (nhân thân / người phụ thuộc thuế) ─────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_ThanNhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_ThanNhan
    (
        Id                  BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id         BIGINT        NOT NULL,
        HoTen               NVARCHAR(150) NOT NULL,
        QuanHe_Id           BIGINT        NULL,               -- → DM_QuanHeThanNhan
        NamSinh             SMALLINT      NULL,
        NgheNghiep          NVARCHAR(150) NULL,
        LaGiamTruThue       BIT           NOT NULL DEFAULT 0, -- người phụ thuộc giảm trừ
        MaSoThuePhuThuoc    NVARCHAR(20)  NULL,
        DienThoai           NVARCHAR(20)  NULL,

        CreatedBy           BIGINT        NOT NULL,
        CreatedAt           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy           BIGINT        NULL,
        UpdatedAt           DATETIME2     NULL,
        IsDeleted           BIT           NOT NULL DEFAULT 0,
        Ver                 INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_ThanNhan PRIMARY KEY (Id),
        CONSTRAINT FK_NS_ThanNhan_NhanVien FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_ThanNhan_QuanHe   FOREIGN KEY (QuanHe_Id)   REFERENCES dbo.DM_QuanHeThanNhan (Id)
    );
    CREATE INDEX IX_NS_ThanNhan_NhanVien ON dbo.NS_NhanVien_ThanNhan (NhanVien_Id);
END;
GO

-- ── NS_NhanVien_GiayToNuocNgoai (chỉ người nước ngoài) ─────────────────────
IF OBJECT_ID(N'dbo.NS_NhanVien_GiayToNuocNgoai', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NhanVien_GiayToNuocNgoai
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id   BIGINT        NOT NULL,
        LoaiGiayTo    TINYINT       NOT NULL,           -- 1=Hộ chiếu, 2=Visa, 3=Giấy phép LĐ
        SoGiayTo      NVARCHAR(50)  NOT NULL,
        NgayCap       DATE          NULL,
        NgayHetHan    DATE          NULL,
        NoiCap        NVARCHAR(200) NULL,
        TrangThai     NVARCHAR(30)  NULL,               -- ConHieuLuc/HetHan...
        File_Id       BIGINT        NULL,               -- → TT_TepDinhKem

        CreatedBy     BIGINT        NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT        NULL,
        UpdatedAt     DATETIME2     NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        Ver           INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NhanVien_GiayToNuocNgoai PRIMARY KEY (Id),
        CONSTRAINT FK_NS_GiayToNN_NhanVien FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_GiayToNN_File     FOREIGN KEY (File_Id)     REFERENCES dbo.TT_TepDinhKem (Id)
    );
    CREATE UNIQUE INDEX UQ_NS_GiayToNN_NV_Loai ON dbo.NS_NhanVien_GiayToNuocNgoai (NhanVien_Id, LoaiGiayTo) WHERE IsDeleted = 0;
    CREATE INDEX IX_NS_GiayToNN_NhanVien ON dbo.NS_NhanVien_GiayToNuocNgoai (NhanVien_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 3. FK TRỄ — liên kết tài khoản HT_NguoiDung ↔ nhân viên (1-1)
--    Foundation 037 để NhanVien_Id nullable, chờ đợt NS_ mới siết. Ở đây:
--      - Thêm FK (an toàn: giá trị NULL không bị kiểm).
--      - UNIQUE lọc: 1 tài khoản ↔ tối đa 1 nhân viên.
--    CHƯA ép NOT NULL: tài khoản bootstrap hệ thống (NhanVien_Id = NULL, xem 038)
--    và các tài khoản cũ cần backfill nhân viên trước — siết NOT NULL ở migration sau.
-- ═══════════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HT_NguoiDung_NhanVien')
BEGIN
    ALTER TABLE dbo.HT_NguoiDung
        ADD CONSTRAINT FK_HT_NguoiDung_NhanVien
            FOREIGN KEY (NhanVien_Id) REFERENCES dbo.NS_NhanVien (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_HT_NguoiDung_NhanVien' AND object_id = OBJECT_ID(N'dbo.HT_NguoiDung'))
BEGIN
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_NhanVien
        ON dbo.HT_NguoiDung (NhanVien_Id)
        WHERE NhanVien_Id IS NOT NULL AND IsDeleted = 0;
END;
GO
