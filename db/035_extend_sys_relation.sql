-- =============================================================================
-- File    : 035_extend_sys_relation.sql
-- Purpose : Mở rộng Sys_Relation thành registry quan hệ tường minh — phục vụ
--           (1) soft-check FK khi xóa và (2) Master-Detail UI 1:N.
--           Thêm: Relation_Code, Master_Key_Column, Detail_FK_Column, On_Delete.
--           Giữ nguyên các cột cũ (Display_Column/Value_Column cho hiển thị).
-- Note    : Idempotent — chạy lại nhiều lần không lỗi.
-- =============================================================================

USE [ICare247_Config];
GO

-- ── Relation_Code: mã quan hệ ổn định, để tham chiếu ────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Sys_Relation') AND name = 'Relation_Code')
BEGIN
    ALTER TABLE dbo.Sys_Relation ADD Relation_Code NVARCHAR(100) NULL;
END;
GO

-- ── Master_Key_Column: cột khóa ở bảng cha (mặc định 'Id' theo convention Data DB) ──
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Sys_Relation') AND name = 'Master_Key_Column')
BEGIN
    ALTER TABLE dbo.Sys_Relation ADD Master_Key_Column NVARCHAR(100) NOT NULL
        CONSTRAINT DF_Sys_Relation_MasterKey DEFAULT (N'Id');
END;
GO

-- ── Detail_FK_Column: cột FK vật lý ở bảng con (vd NoiSinh_TinhThanhPhoID) ──
--    THEN-CHỐT cho soft-check + master-detail (xác định cột nối, không đoán theo tên).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Sys_Relation') AND name = 'Detail_FK_Column')
BEGIN
    ALTER TABLE dbo.Sys_Relation ADD Detail_FK_Column NVARCHAR(100) NULL;
END;
GO

-- ── On_Delete: hành vi khi xóa bản ghi master ───────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Sys_Relation') AND name = 'On_Delete')
BEGIN
    ALTER TABLE dbo.Sys_Relation ADD On_Delete NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Sys_Relation_OnDelete DEFAULT (N'Restrict');
END;
GO

-- ── Ràng buộc giá trị On_Delete + unique Relation_Code (chỉ khi có giá trị) ──
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Sys_Relation_OnDelete')
BEGIN
    ALTER TABLE dbo.Sys_Relation ADD CONSTRAINT CHK_Sys_Relation_OnDelete
        CHECK (On_Delete IN (N'Restrict', N'Cascade', N'SetNull', N'NoAction'));
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Sys_Relation_Code')
BEGIN
    CREATE UNIQUE INDEX UQ_Sys_Relation_Code
        ON dbo.Sys_Relation (Relation_Code) WHERE Relation_Code IS NOT NULL;
END;
GO

-- ── Index hỗ trợ soft-check: tra nhanh quan hệ theo bảng master ─────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Sys_Relation_Master')
BEGIN
    CREATE INDEX IX_Sys_Relation_Master
        ON dbo.Sys_Relation (Master_Table_Id, Is_Active);
END;
GO

PRINT N'Migration 035 completed — Sys_Relation extended (Relation_Code, Master_Key_Column, Detail_FK_Column, On_Delete).';
GO
