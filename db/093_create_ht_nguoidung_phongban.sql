-- =============================================================================
-- File    : 093_create_ht_nguoidung_phongban.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 037)
-- Purpose : Bảng HT_NguoiDung_PhongBan — gán riêng phạm vi PHÒNG BAN cho từng user
--           (ngoài kế thừa qua vai trò ở HT_VaiTro_PhongBan, db/092). Mirror
--           HT_NguoiDung_CongTy (db/037 §432) nhưng KHÔNG có cột LaMacDinh — phòng ban
--           chưa cần khái niệm "mặc định"/switcher ở đợt này (khác công ty).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §3 (HT_) · TASKS.md AUTHZ-PB-1 · ADR-022 (khối cột auto).
-- Context : Data DB tenant. Idempotent (OBJECT_ID guard).
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── HT_NguoiDung_PhongBan (phạm vi dữ liệu — gán riêng theo user) ────────────
IF OBJECT_ID(N'dbo.HT_NguoiDung_PhongBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_NguoiDung_PhongBan
    (
        Id            BIGINT      IDENTITY(1,1) NOT NULL,
        NguoiDung_Id  BIGINT      NOT NULL,
        PhongBan_Id   BIGINT      NOT NULL,

        CreatedBy     BIGINT      NOT NULL,
        CreatedAt     DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy     BIGINT      NULL,
        UpdatedAt     DATETIME2   NULL,
        IsDeleted     BIT         NOT NULL DEFAULT 0,
        Ver           INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_NguoiDung_PhongBan PRIMARY KEY (Id),
        CONSTRAINT FK_HT_NDPB_NguoiDung FOREIGN KEY (NguoiDung_Id) REFERENCES dbo.HT_NguoiDung (Id),
        CONSTRAINT FK_HT_NDPB_PhongBan  FOREIGN KEY (PhongBan_Id)  REFERENCES dbo.TC_PhongBan (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_NguoiDung_PhongBan ON dbo.HT_NguoiDung_PhongBan (NguoiDung_Id, PhongBan_Id) WHERE IsDeleted = 0;
END;
GO

PRINT N'Migration 093 completed — bảng HT_NguoiDung_PhongBan (gán riêng phạm vi phòng ban).';
GO
