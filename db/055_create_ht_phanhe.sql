-- =============================================================================
-- File    : 055_create_ht_phanhe.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 037)
-- Purpose : Tạo bảng danh mục PHÂN HỆ (module) dbo.HT_PhanHe + seed 7 phân hệ nền tảng.
--           Dropdown "Module" ở màn Quản lý menu (Menu Builder) đọc từ bảng này — dễ thêm
--           phân hệ mới mà không sửa code. Ma khớp cột HT_ChucNang.Module (TC/NS/TL/...).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md. Mirror cấu trúc danh mục TC_CapCongTy (037).
-- Context : Chạy TRONG ngữ cảnh Data DB của tenant. Idempotent (IF OBJECT_ID + NOT EXISTS).
--           CreatedBy đặt tường minh = tài khoản admin (như 054).
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── 1. Bảng danh mục phân hệ ────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.HT_PhanHe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_PhanHe
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Ma          NVARCHAR(20)    NOT NULL,           -- 'TC','NS','TL','TM','CN','BC','HT'
        Ten         NVARCHAR(100)   NOT NULL,
        ThuTu       INT             NOT NULL DEFAULT 0,
        KichHoat    BIT             NOT NULL DEFAULT 1,

        CreatedBy   BIGINT          NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT          NULL,
        UpdatedAt   DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL DEFAULT 0,
        Ver         INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_PhanHe PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_PhanHe_Ma ON dbo.HT_PhanHe (Ma) WHERE IsDeleted = 0;
END;
GO

-- ── 2. Seed 7 phân hệ nền tảng (theo cây menu base — 044/045) ────────────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Chưa có tài khoản admin — chạy 038_seed_data_db_bootstrap.sql trước.', 16, 1);
    RETURN;
END;

DECLARE @seed TABLE (Ma NVARCHAR(20), Ten NVARCHAR(100), ThuTu INT);
INSERT INTO @seed (Ma, Ten, ThuTu) VALUES
    (N'TC', N'Tổ chức',           1),
    (N'NS', N'Nhân sự',           2),
    (N'TL', N'Chấm công – Lương', 3),
    (N'TM', N'Bán hàng & Kho',    4),
    (N'CN', N'Công nợ',           5),
    (N'BC', N'Báo cáo',           6),
    (N'HT', N'Quản trị hệ thống', 7);

INSERT INTO dbo.HT_PhanHe (Ma, Ten, ThuTu, KichHoat, CreatedBy, CreatedAt)
SELECT s.Ma, s.Ten, s.ThuTu, 1, @AdminId, SYSUTCDATETIME()
FROM   @seed s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.HT_PhanHe p WHERE p.Ma = s.Ma AND p.IsDeleted = 0);
GO

PRINT N'Migration 055 completed — bảng HT_PhanHe + seed 7 phân hệ.';
GO
