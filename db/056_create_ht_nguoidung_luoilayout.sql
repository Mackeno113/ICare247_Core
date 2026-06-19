-- File   : 056_create_ht_nguoidung_luoilayout.sql
-- DB     : Data DB (ICare247_Solution) — per-tenant, KHÔNG có cột Tenant_Id
-- Purpose: Lưu layout lưới (DxGrid GridPersistentLayout JSON) theo từng người dùng + từng View.
--          Dữ liệu SỞ THÍCH của user (preference) — không phải cấu hình admin (Ui_View ở Config DB).
--          Single-writer (chỉ chính user ghi) → backend cache mạnh, write-through, không invalidate chéo.

IF OBJECT_ID(N'dbo.HT_NguoiDung_LuoiLayout', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HT_NguoiDung_LuoiLayout
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL,
        NguoiDung_Id  BIGINT         NOT NULL,                                   -- FK HT_NguoiDung.Id
        View_Code     NVARCHAR(100)  NOT NULL,                                   -- Ui_View.View_Code
        Platform      VARCHAR(10)    NOT NULL CONSTRAINT DF_HT_NDLL_Platform DEFAULT ('web'),
        Layout_Json   NVARCHAR(MAX)  NOT NULL,                                   -- GridPersistentLayout serialize JSON
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_HT_NDLL_UpdatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_HT_NguoiDung_LuoiLayout PRIMARY KEY (Id),
        CONSTRAINT FK_HT_NDLL_NguoiDung FOREIGN KEY (NguoiDung_Id) REFERENCES dbo.HT_NguoiDung (Id)
    );

    -- 1 user + 1 View + 1 platform = 1 layout (UPSERT theo bộ khóa này).
    CREATE UNIQUE INDEX UQ_HT_NDLL ON dbo.HT_NguoiDung_LuoiLayout (NguoiDung_Id, View_Code, Platform);
END;
GO
