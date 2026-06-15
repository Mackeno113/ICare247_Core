-- Migration : 018_add_is_virtual_field.sql
-- Database  : ICare247_Config
-- Purpose   : Thêm cột Is_Virtual vào Ui_Field.
--             Virtual field là field UI-only — hiển thị trên form nhưng không map
--             tới cột DB thực. Ví dụ: TinhThanh helper cho cascading XaPhuong.
--             FormRunner / submit layer bỏ qua virtual fields khi build save payload.

ALTER TABLE dbo.Ui_Field
    ADD Is_Virtual BIT NOT NULL DEFAULT 0;
GO
