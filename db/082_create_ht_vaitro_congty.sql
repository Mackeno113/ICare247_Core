-- =============================================================================
-- File    : 082_create_ht_vaitro_congty.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy sau 037)
-- Purpose : Bảng HT_VaiTro_CongTy — phạm vi CÔNG TY gán theo VAI TRÒ (kế thừa động).
--           Quyền công ty hiệu lực của user = HT_NguoiDung_CongTy (gán riêng)
--           ∪ HT_VaiTro_CongTy ⨝ HT_NguoiDung_VaiTro (theo vai trò) — cùng cơ chế
--           join động như HT_VaiTro_Quyen (trục chức năng): add user vào vai trò
--           là kế thừa ngay, sửa vai trò lan ngay, không copy, không rác.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §3 (HT_) · ADR-022 (khối cột auto).
-- Context : Data DB tenant. Idempotent (OBJECT_ID guard). LaMacDinh (công ty mặc
--           định khi đăng nhập) chỉ tồn tại ở gán riêng HT_NguoiDung_CongTy —
--           vai trò KHÔNG mang khái niệm mặc định per-user.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- ── HT_VaiTro_CongTy (map N-N vai trò × công ty — phạm vi dữ liệu theo nhóm) ──
IF OBJECT_ID(N'dbo.HT_VaiTro_CongTy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_VaiTro_CongTy
    (
        Id          BIGINT      IDENTITY(1,1) NOT NULL,
        VaiTro_Id   BIGINT      NOT NULL,
        CongTy_Id   BIGINT      NOT NULL,

        CreatedBy   BIGINT      NOT NULL,
        CreatedAt   DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy   BIGINT      NULL,
        UpdatedAt   DATETIME2   NULL,
        IsDeleted   BIT         NOT NULL DEFAULT 0,
        Ver         INT         NOT NULL DEFAULT 0,

        CONSTRAINT PK_HT_VaiTro_CongTy PRIMARY KEY (Id),
        CONSTRAINT FK_HT_VTCT_VaiTro FOREIGN KEY (VaiTro_Id) REFERENCES dbo.HT_VaiTro (Id),
        CONSTRAINT FK_HT_VTCT_CongTy FOREIGN KEY (CongTy_Id) REFERENCES dbo.TC_CongTy (Id)
    );
    CREATE UNIQUE INDEX UQ_HT_VaiTro_CongTy ON dbo.HT_VaiTro_CongTy (VaiTro_Id, CongTy_Id) WHERE IsDeleted = 0;
    CREATE INDEX IX_HT_VaiTro_CongTy_CongTy ON dbo.HT_VaiTro_CongTy (CongTy_Id);
END;
GO

PRINT N'Migration 082 completed — bảng HT_VaiTro_CongTy (phạm vi công ty theo vai trò).';
GO
