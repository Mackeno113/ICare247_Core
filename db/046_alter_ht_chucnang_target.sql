-- =============================================================================
-- File    : 046_alter_ht_chucnang_target.sql
-- Purpose : Liên kết node chức năng với "đối tượng" engine (Ui_Form / Ui_View) để enforce
--           quyền ở endpoint generic (master-data/{formCode}, views/{code}, forms/{formCode}).
--           DoiTuong = mã form/view; LoaiDoiTuong = 'Form'/'View'.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md (AUTHZ-SEC-2) · ADR-023.
-- Context : Data DB tenant, sau 042/045. Idempotent (COL_LENGTH).
-- Note    : Enforce-if-mapped — chỉ chặn khi node CÓ DoiTuong khớp; chưa gắn = cho qua.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- Mã đối tượng engine mà node này điều khiển (vd 'HT_VaiTro' cho master-data, mã view…).
IF COL_LENGTH('dbo.HT_ChucNang', 'DoiTuong') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD DoiTuong NVARCHAR(100) NULL;
GO

-- Loại đối tượng: 'Form' (Ui_Form/master-data/runtime) · 'View' (Ui_View) · NULL = không gắn.
IF COL_LENGTH('dbo.HT_ChucNang', 'LoaiDoiTuong') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD LoaiDoiTuong NVARCHAR(20) NULL;
GO

-- Index phục vụ tra cứu enforce theo (loại, mã đối tượng).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HT_ChucNang_DoiTuong' AND object_id = OBJECT_ID('dbo.HT_ChucNang'))
    CREATE INDEX IX_HT_ChucNang_DoiTuong ON dbo.HT_ChucNang (LoaiDoiTuong, DoiTuong) WHERE IsDeleted = 0;
GO
