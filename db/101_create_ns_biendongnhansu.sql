-- =============================================================================
-- File    : 101_create_ns_biendongnhansu.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : NS_BienDongNhanSu — người ↔ ghế theo thời gian (quá trình công tác).
--           Mỗi dòng = 1 lần 1 người giữ 1 ghế trong 1 khoảng, kèm loại biến động
--           + chứng từ. Đọc theo NhanVien_Id sắp TuNgay = toàn bộ lịch sử.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.3  · ADR-022.
-- Design  : Đổi phòng ban = đóng bản cũ (DenNgay) + mở bản mới (ViTri thuộc PB mới,
--           Loai=DIEU_DONG). Kiêm nhiệm = KHÔNG đóng bản cũ (cờ từ NS_LoaiBienDong).
-- Prereq  : 098 (NS_NhanVien), 099 (NS_LoaiBienDong), 100 (NS_ViTriCongViec).
-- Convention: auto block; CreatedBy/UpdatedBy KHÔNG đặt FK. Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.NS_BienDongNhanSu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NS_BienDongNhanSu
    (
        Id                BIGINT        IDENTITY(1,1) NOT NULL,
        NhanVien_Id       BIGINT        NOT NULL,       -- → NS_NhanVien
        ViTri_Id          BIGINT        NOT NULL,       -- → NS_ViTriCongViec (ghế được bố trí)
        LoaiBienDong_Id   BIGINT        NOT NULL,       -- → NS_LoaiBienDong (quyết định hành vi ghế)
        TuNgay            DATE          NOT NULL,
        DenNgay           DATE          NULL,           -- NULL = đang giữ ghế
        SoQuyetDinh       NVARCHAR(50)  NULL,
        NgayQuyetDinh     DATE          NULL,
        LyDo              NVARCHAR(300) NULL,
        GhiChu            NVARCHAR(500) NULL,

        CreatedBy         BIGINT        NOT NULL,
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         BIGINT        NULL,
        UpdatedAt         DATETIME2     NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        Ver               INT           NOT NULL DEFAULT 0,

        CONSTRAINT PK_NS_BienDongNhanSu PRIMARY KEY (Id),
        CONSTRAINT FK_NS_BienDong_NhanVien FOREIGN KEY (NhanVien_Id)     REFERENCES dbo.NS_NhanVien (Id),
        CONSTRAINT FK_NS_BienDong_ViTri    FOREIGN KEY (ViTri_Id)        REFERENCES dbo.NS_ViTriCongViec (Id),
        CONSTRAINT FK_NS_BienDong_Loai     FOREIGN KEY (LoaiBienDong_Id) REFERENCES dbo.NS_LoaiBienDong (Id),
        CONSTRAINT CK_NS_BienDong_Ngay     CHECK (DenNgay IS NULL OR DenNgay >= TuNgay)
    );
    -- Tra "đang giữ ghế" của 1 người (DenNgay NULL) và lịch sử theo thời gian.
    CREATE INDEX IX_NS_BienDong_NhanVien ON dbo.NS_BienDongNhanSu (NhanVien_Id, DenNgay);
    CREATE INDEX IX_NS_BienDong_ViTri    ON dbo.NS_BienDongNhanSu (ViTri_Id, DenNgay);
END;
GO
