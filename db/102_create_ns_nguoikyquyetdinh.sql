-- =============================================================================
-- File    : 102_create_ns_nguoikyquyetdinh.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : NS_NguoiKyQuyetDinh — định tuyến chữ ký theo
--           (Công ty × Loại quyết định × khoảng hiệu lực). Map từ legacy
--           NS_NhanVien_KyQuyetDinh (bỏ cột denormalized + CoQuanBanHanh/NghiQuyet).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.5  · ADR-022.
-- Design  : CongTy_Id NULL = fallback áp mọi công ty (dòng có CongTy override).
--           Ảnh chữ ký + con dấu → TT_TepDinhKem (không lưu bytes). "1 người ký
--           mặc định / (CongTy, LoaiQĐ) đang hiệu lực" enforce ở App (do date-range).
-- Prereq  : 037 (TC_CongTy), 063 (TT_TepDinhKem), 098 (NS_NhanVien), 099 (NS_LoaiQuyetDinh).
-- Convention: auto block; CreatedBy/UpdatedBy KHÔNG đặt FK. Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.NS_NguoiKyQuyetDinh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_NguoiKyQuyetDinh
    (
        Id                  BIGINT        IDENTITY(1,1) NOT NULL,
        CongTy_Id           BIGINT        NULL,           -- → TC_CongTy; NULL = fallback mọi CT
        LoaiQuyetDinh_Id    BIGINT        NOT NULL,       -- → NS_LoaiQuyetDinh
        NhanVien_Id         BIGINT        NOT NULL,       -- → NS_NhanVien (người ký)
        ChucDanhKy          NVARCHAR(200) NULL,           -- chức danh in ở khối chữ ký
        LaNguoiKyMacDinh    BIT           NOT NULL DEFAULT 0,
        NgayHieuLuc         DATE          NOT NULL,
        NgayHetHieuLuc      DATE          NULL,           -- NULL = vô thời hạn
        ThongTinUyQuyen     NVARCHAR(500) NULL,           -- ký thay theo GUQ
        AnhChuKyConDau_Id   BIGINT        NULL,           -- → TT_TepDinhKem
        GhiChu              NVARCHAR(300) NULL,

        CreatedBy           BIGINT        NOT NULL,
        CreatedAt           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy           BIGINT        NULL,
        UpdatedAt           DATETIME2     NULL,
        IsDeleted           BIT           NOT NULL DEFAULT 0,
        Ver                 INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_NguoiKyQuyetDinh PRIMARY KEY (Id),
        CONSTRAINT FK_NS_NguoiKy_CongTy  FOREIGN KEY (CongTy_Id)         REFERENCES dbo.TC_CongTy (Id),
        CONSTRAINT FK_NS_NguoiKy_LoaiQD  FOREIGN KEY (LoaiQuyetDinh_Id)  REFERENCES dbo.NS_LoaiQuyetDinh (Id),
        CONSTRAINT FK_NS_NguoiKy_NhanVien FOREIGN KEY (NhanVien_Id)      REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_NguoiKy_ChuKy   FOREIGN KEY (AnhChuKyConDau_Id) REFERENCES dbo.TT_TepDinhKem (Id),
        CONSTRAINT CK_NS_NguoiKy_Ngay    CHECK (NgayHetHieuLuc IS NULL OR NgayHetHieuLuc >= NgayHieuLuc)
    );
    -- Tra người ký: lọc theo (LoaiQĐ, CongTy hoặc NULL, ngày ∈ hiệu lực).
    CREATE INDEX IX_NS_NguoiKy_Tra ON dbo.NS_NguoiKyQuyetDinh (LoaiQuyetDinh_Id, CongTy_Id, NgayHieuLuc);
END;
GO
