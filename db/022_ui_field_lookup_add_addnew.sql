-- Migration 022: Thêm Allow_Add_New + Add_Form_Code vào Ui_Field_Lookup
-- Cho phép LookupBox mở dialog "thêm mới" entity ngay trên control.
--   Allow_Add_New: bật/tắt nút "➕ Thêm mới" trong dropdown (mặc định tắt).
--   Add_Form_Code: Form_Code của Ui_Form dùng để render dialog nhập liệu entity nguồn.
--                  NULL khi Allow_Add_New = 0.

ALTER TABLE dbo.Ui_Field_Lookup
    ADD Allow_Add_New bit          NOT NULL CONSTRAINT DF_Ui_Field_Lookup_Allow_Add_New DEFAULT 0,
        Add_Form_Code  nvarchar(100) NULL;
GO
