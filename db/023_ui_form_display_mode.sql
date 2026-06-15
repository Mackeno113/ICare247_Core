-- Migration 023: Thêm Display_Mode vào Ui_Form
-- Database  : ICare247_Config
-- Quyết định cách render form Thêm/Sửa của một danh mục (Master Data):
--   'Popup' : mở dialog modal nổi (mặc định — không vỡ form cũ).
--   'Tab'   : mở routed page nội bộ SPA (List -> Detail -> Back).
-- Chỉ áp cho thao tác Thêm/Sửa; thao tác Xóa luôn là confirm dialog.
-- Thay thế vai trò cột Layout_Engine (Grid/Flex/Custom) vốn không engine nào đọc.

ALTER TABLE dbo.Ui_Form
    ADD Display_Mode nvarchar(20) NOT NULL
        CONSTRAINT DF_Ui_Form_Display_Mode DEFAULT 'Popup';
GO

ALTER TABLE dbo.Ui_Form
    ADD CONSTRAINT CHK_Ui_Form_Display_Mode
        CHECK (Display_Mode IN ('Popup', 'Tab'));
GO
