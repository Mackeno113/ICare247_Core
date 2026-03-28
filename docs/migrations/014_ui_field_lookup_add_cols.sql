-- ============================================================
-- Migration 014: Thêm cột LookupBox vào Ui_Field_Lookup
-- Mục đích  : Lưu cấu hình hiển thị EditBox + popup dimensions
--             cho Editor_Type = LookupBox (DxDropDownBox).
--             5 cột mới — tất cả nullable để backward compat
--             với các record LookupBox cũ.
-- Phụ thuộc : Migration 008 (bảng Ui_Field_Lookup đã tồn tại)
-- Ngày      : 2026-03-28
-- ============================================================

-- ── 1. EditBox_Mode — chế độ hiển thị trong EditBox khi chọn xong ──
--    'TextOnly'    : chỉ hiện cột Display (mặc định)
--    'CodeAndName' : mã code nhỏ + tên (dùng Code_Field bên dưới)
--    'Custom'      : template Blazor tùy chỉnh
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'EditBox_Mode'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD EditBox_Mode nvarchar(20) NULL
            CONSTRAINT DF_UiFL_EditBoxMode DEFAULT N'TextOnly';
    PRINT N'Đã thêm cột EditBox_Mode';
END
GO

-- ── 2. Code_Field — cột mã code trong data source ──────────────────
--    Chỉ dùng khi EditBox_Mode = 'CodeAndName'.
--    VD: N'PhongBan_Code'
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Code_Field'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Code_Field nvarchar(100) NULL;
    PRINT N'Đã thêm cột Code_Field';
END
GO

-- ── 3. DropDown_Width — chiều rộng popup grid (px) ─────────────────
--    Mặc định 600. NULL = dùng default trong component.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'DropDown_Width'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD DropDown_Width int NULL
            CONSTRAINT DF_UiFL_DropDownWidth DEFAULT 600;
    PRINT N'Đã thêm cột DropDown_Width';
END
GO

-- ── 4. DropDown_Height — chiều cao popup grid (px) ─────────────────
--    Mặc định 400. NULL = dùng default trong component.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'DropDown_Height'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD DropDown_Height int NULL
            CONSTRAINT DF_UiFL_DropDownHeight DEFAULT 400;
    PRINT N'Đã thêm cột DropDown_Height';
END
GO

-- ── 5. Reload_Trigger_Field — cascading trigger ─────────────────────
--    FieldCode của field khác trong form. Khi field đó thay đổi giá trị,
--    LookupBox tự động clear SelectedId + reload data source.
--    VD: N'ChiNhanhId' (chọn chi nhánh → reload danh sách phòng ban)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID(N'dbo.Ui_Field_Lookup')
      AND  name      = N'Reload_Trigger_Field'
)
BEGIN
    ALTER TABLE dbo.Ui_Field_Lookup
        ADD Reload_Trigger_Field nvarchar(100) NULL;
    PRINT N'Đã thêm cột Reload_Trigger_Field';
END
GO

-- ── Verify ────────────────────────────────────────────────────────────
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM   INFORMATION_SCHEMA.COLUMNS
WHERE  TABLE_NAME = N'Ui_Field_Lookup'
ORDER BY ORDINAL_POSITION;
GO
