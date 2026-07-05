-- =============================================================================
-- File    : 067_alter_ui_field_is_configured.sql
-- Database: ICare247_Config  (Config DB — nơi ConfigStudio đọc/ghi metadata form)
-- Purpose : Thêm cờ Ui_Field.Is_Configured — đánh dấu field ĐÃ được cấu hình.
--           Bật = 1 KHI user bấm "Lưu Field" ở màn cấu hình chi tiết (ConfigStudio).
--           Badge navigator "đã / chưa cấu hình" đọc thẳng cờ này (thay tiêu chí suy từ i18n
--           vốn không chắc — nhãn seed mặc định = mã cột gây dương tính giả).
-- Backfill : Field ĐÃ tồn tại trước khi có cờ → coi như ĐÃ cấu hình (= 1), chạy 1 lần lúc thêm cột.
--           Field TẠO MỚI sau này default 0 (DEFAULT), chỉ thành 1 khi bấm Lưu Field.
-- Note    : Idempotent (COL_LENGTH guard). ConfigSync đọc cột động (INFORMATION_SCHEMA, giao
--           master∩tenant) nên thêm cột này KHÔNG ảnh hưởng đồng bộ — tenant thiếu cột thì bỏ qua.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Ui_Field', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ui_Field', 'Is_Configured') IS NULL
    BEGIN
        ALTER TABLE dbo.Ui_Field
            ADD Is_Configured BIT NOT NULL
                CONSTRAINT DF_Ui_Field_Is_Configured DEFAULT 0;

        -- Backfill 1 lần: mọi field hiện có coi như đã cấu hình. Dùng EXEC để cột mới đã hiện diện
        -- (tránh lỗi biên dịch tham chiếu cột vừa ADD trong cùng batch).
        EXEC(N'UPDATE dbo.Ui_Field SET Is_Configured = 1;');

        PRINT N'✔ Đã thêm Ui_Field.Is_Configured + backfill field cũ = 1.';
    END
    ELSE
        PRINT N'• Ui_Field.Is_Configured đã tồn tại — bỏ qua.';
END
ELSE
    PRINT N'⚠ Bỏ qua: dbo.Ui_Field chưa tồn tại — chạy migration tạo bảng trước.';
GO

PRINT N'Migration 067 completed — cờ Is_Configured cho Ui_Field.';
GO
