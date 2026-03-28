-- Migration : 015_ui_field_add_required_error_key.sql
-- Date       : 2026-03-29
-- Purpose    : Thêm cột Required_Error_Key vào Ui_Field — lưu i18n key cho thông báo lỗi bắt buộc nhập.
--              Tương tự Label_Key / Placeholder_Key / Tooltip_Key — dedicated column, không lưu trong Control_Props_Json.
-- Pattern key: {tableCode}.val.{fieldCode}.required  (vd: nhanvien.val.manhanvien.required)

ALTER TABLE dbo.Ui_Field
    ADD Required_Error_Key nvarchar(150) NULL;
GO
