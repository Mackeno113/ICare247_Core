-- Migration : 019_ui_field_column_id_nullable.sql
-- Database  : ICare247_Config
-- Purpose   : Cho phép Column_Id = NULL trong Ui_Field.
--             Virtual field (Is_Virtual = 1) không map tới cột DB thực,
--             nên Column_Id hợp lệ là NULL.
--             Cần chạy sau 018_add_is_virtual_field.sql.

-- Xóa FK constraint trước khi đổi kiểu cột
ALTER TABLE dbo.Ui_Field DROP CONSTRAINT FK_Ui_Field_Column;
GO

-- Cho phép NULL
ALTER TABLE dbo.Ui_Field ALTER COLUMN Column_Id INT NULL;
GO

-- Tạo lại FK constraint (nullable FK)
ALTER TABLE dbo.Ui_Field
    ADD CONSTRAINT FK_Ui_Field_Column
        FOREIGN KEY (Column_Id) REFERENCES dbo.Sys_Column (Column_Id);
GO
