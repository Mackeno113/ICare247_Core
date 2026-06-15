-- Migration 024: Thêm Show_In_List vào Ui_Field
-- Database  : ICare247_Config
-- Tách "field hiện ở form nhập liệu" (Is_Visible) khỏi "field hiện ở lưới List danh mục".
--   Show_In_List = 1 : cột này hiển thị trong MasterDataGrid (lưới danh sách bản ghi).
--   Show_In_List = 0 : chỉ dùng ở form nhập, không lên lưới (mặc định).
-- Cho phép List danh mục gọn (vài cột chính) trong khi form vẫn đủ field.

ALTER TABLE dbo.Ui_Field
    ADD Show_In_List bit NOT NULL
        CONSTRAINT DF_Ui_Field_Show_In_List DEFAULT 0;
GO

-- Seed gợi ý: bật Show_In_List cho các field đang Is_Visible của form danh mục hiện có.
-- (Tùy chọn — admin có thể tự cấu hình lại trong ConfigStudio.)
-- UPDATE dbo.Ui_Field SET Show_In_List = 1 WHERE Is_Visible = 1;
-- GO
