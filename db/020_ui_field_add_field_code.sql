-- Migration : 020_ui_field_add_field_code.sql
-- Purpose   : Thêm cột Field_Code vào Ui_Field để virtual field có FieldCode riêng.
--             Field thường: FieldCode = COALESCE(Field_Code, Sys_Column.Column_Code)
--             Field ảo    : FieldCode = Field_Code (bắt buộc phải có)

ALTER TABLE dbo.Ui_Field
    ADD Field_Code NVARCHAR(100) NULL;
GO
