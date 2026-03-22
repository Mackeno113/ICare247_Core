-- ============================================================
-- Migration 004: Thêm bảng Sys_Lookup cho danh mục dùng chung
-- Mục đích  : Lưu các danh mục nhỏ (Gender, MaritalStatus,...)
--             không cần bảng DM_ riêng.
--             Cột nghiệp vụ lưu Item_Code (nvarchar) thay vì số
--             → SQL query tự mô tả, không cần join để đọc hiểu.
-- Ngày      : 2026-03-22
-- Quy tắc   : Mọi string literal tiếng Việt dùng N'...' (Unicode)
-- ============================================================

-- ── 1. Tạo bảng Sys_Lookup ───────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Sys_Lookup')
BEGIN
    CREATE TABLE dbo.Sys_Lookup (
        Lookup_Id    int           IDENTITY(1,1) NOT NULL,
        Tenant_Id    int           NOT NULL CONSTRAINT DF_Sys_Lookup_Tenant DEFAULT 0,
        -- 0 = global (dùng chung mọi tenant)
        Lookup_Code  nvarchar(50)  NOT NULL,
        -- VD: N'GENDER', N'MARITAL_STATUS', N'BLOOD_TYPE'
        Item_Code    nvarchar(50)  NOT NULL,
        -- Giá trị lưu vào cột nghiệp vụ. VD: N'NAM', N'NU', N'KXD'
        Label_Key    nvarchar(200) NOT NULL,
        -- Key trong Sys_Resource để resolve label theo ngôn ngữ
        Sort_Order   int           NOT NULL CONSTRAINT DF_Sys_Lookup_Sort DEFAULT 0,
        Is_Active    bit           NOT NULL CONSTRAINT DF_Sys_Lookup_Active DEFAULT 1,

        CONSTRAINT PK_Sys_Lookup PRIMARY KEY (Lookup_Id),
        CONSTRAINT UQ_Sys_Lookup UNIQUE (Tenant_Id, Lookup_Code, Item_Code)
    );

    CREATE INDEX IX_Sys_Lookup_Code
        ON dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Is_Active);
END
GO

-- ── 2. Seed data: GENDER ─────────────────────────────────────
-- Item_Code là giá trị lưu vào cột nghiệp vụ (nvarchar)
-- Cột NhanVien.GioiTinh nên đổi thành nvarchar(10) và lưu N'NAM'/N'NU'/N'KXD'

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Lookup WHERE Lookup_Code = N'GENDER' AND Tenant_Id = 0)
BEGIN
    INSERT INTO dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order)
    VALUES
        (0, N'GENDER', N'NAM', N'common.gender.male',    1),
        (0, N'GENDER', N'NU',  N'common.gender.female',  2),
        (0, N'GENDER', N'KXD', N'common.gender.unknown', 3);
END
GO

-- ── 3. Seed i18n: common.gender.* → Sys_Resource ────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Resource WHERE Resource_Key = N'common.gender.male')
BEGIN
    INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value, Version)
    VALUES
        (N'common.gender.male',    N'vi', N'Nam',             1),
        (N'common.gender.male',    N'en', N'Male',            1),
        (N'common.gender.female',  N'vi', N'Nữ',              1),
        (N'common.gender.female',  N'en', N'Female',          1),
        (N'common.gender.unknown', N'vi', N'Không xác định',  1),
        (N'common.gender.unknown', N'en', N'Unknown',         1);
END
GO

-- ── 4. Seed Ui_Control_Map: RadioGroup + LookupComboBox ──────
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Control_Map WHERE Editor_Type = N'RadioGroup' AND Platform = N'web')
BEGIN
    INSERT INTO dbo.Ui_Control_Map (Editor_Type, Platform, Control_Name, Default_Props_Json)
    VALUES
        -- RadioGroup: dùng cho lookup ≤ 4 options, hiển thị tất cả
        (N'RadioGroup', N'web',    N'DxRadioGroup',   N'{"layout":"horizontal"}'),
        (N'RadioGroup', N'mobile', N'RadioGroup',     N'{"layout":"vertical"}'),
        (N'RadioGroup', N'wpf',    N'RadioGroup',     N'{"orientation":"horizontal"}'),
        -- LookupComboBox: dùng cho lookup nhiều options, có search
        (N'LookupComboBox', N'web',    N'DxComboBox',   N'{"allowNull":true,"searchEnabled":true}'),
        (N'LookupComboBox', N'mobile', N'Picker',        N'{"allowNull":true}'),
        (N'LookupComboBox', N'wpf',    N'ComboBoxEdit',  N'{"allowNull":true}');
END
GO

-- ── 5. Hướng dẫn migrate cột nghiệp vụ ──────────────────────
-- Nếu bảng nghiệp vụ đang dùng tinyint cho lookup, migrate như sau:
--
-- ALTER TABLE dbo.NhanVien ADD GioiTinh_New nvarchar(10) NULL;
-- GO
-- UPDATE dbo.NhanVien SET GioiTinh_New = CASE GioiTinh
--     WHEN 1 THEN N'NAM'
--     WHEN 2 THEN N'NU'
--     ELSE N'KXD'
-- END;
-- GO
-- ALTER TABLE dbo.NhanVien DROP COLUMN GioiTinh;
-- GO
-- EXEC sp_rename N'dbo.NhanVien.GioiTinh_New', N'GioiTinh', N'COLUMN';
-- GO
