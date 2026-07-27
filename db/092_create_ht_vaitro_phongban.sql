-- =============================================================================
-- File    : 092_create_ht_vaitro_phongban.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 037)
-- Purpose : Bảng HT_VaiTro_PhongBan — phạm vi PHÒNG BAN gán theo VAI TRÒ (kế thừa động).
--           Mirror HT_VaiTro_CongTy (db/082) nhưng ĐỘC LẬP HOÀN TOÀN với trục công ty —
--           không thu hẹp lồng nhau, dù TC_PhongBan luôn thuộc đúng 1 CongTy_Id.
--           Quyền phòng ban hiệu lực của user = HT_NguoiDung_PhongBan (gán riêng, db/093)
--           ∪ HT_VaiTro_PhongBan ⨝ HT_NguoiDung_VaiTro (theo vai trò).
-- Khác công ty: đúng node được gán, KHÔNG tự gộp phòng ban con dù cây phòng ban là cây
--           thật đa cấp (Khối/Phòng/Tổ/Nhóm) — chốt với user 2026-07-25, không cascade.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §3 (HT_) · TASKS.md AUTHZ-PB-1 · ADR-022 (khối cột auto).
-- Context : Data DB tenant. Idempotent (OBJECT_ID guard).
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── HT_VaiTro_PhongBan (map N-N vai trò × phòng ban — phạm vi dữ liệu theo nhóm) ──
IF OBJECT_ID(N'dbo.HT_VaiTro_PhongBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_VaiTro_PhongBan
    (
        Id          BIGINT      IDENTITY(1,1) NOT NULL,
        VaiTro_Id   BIGINT      NOT NULL,
        PhongBan_Id BIGINT      NOT NULL,

        CreatedBy   BIGINT      NOT NULL,
        CreatedAt   DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT      NULL,
        UpdatedAt   DATETIME2   NULL,
        IsDeleted   BIT         NOT NULL DEFAULT 0,
        Ver         INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_VaiTro_PhongBan PRIMARY KEY (Id),
        CONSTRAINT FK_HT_VTPB_VaiTro   FOREIGN KEY (VaiTro_Id)   REFERENCES dbo.HT_VaiTro (Id),
        CONSTRAINT FK_HT_VTPB_PhongBan FOREIGN KEY (PhongBan_Id) REFERENCES dbo.TC_PhongBan (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_VaiTro_PhongBan ON dbo.HT_VaiTro_PhongBan (VaiTro_Id, PhongBan_Id) WHERE IsDeleted = 0;
    CREATE INDEX IX_HT_VaiTro_PhongBan_PhongBan ON dbo.HT_VaiTro_PhongBan (PhongBan_Id);
END;
GO

PRINT N'Migration 092 completed — bảng HT_VaiTro_PhongBan (phạm vi phòng ban theo vai trò).';
GO
