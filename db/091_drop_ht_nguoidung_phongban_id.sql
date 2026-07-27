-- =============================================================================
-- File    : 091_drop_ht_nguoidung_phongban_id.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Gỡ HT_NguoiDung.PhongBan_Id (FK→TC_PhongBan) — cột chưa từng được dùng:
--           không handler/repository nào đọc/ghi, không Ui_Field/Ui_Form nào map tới.
-- Vì sao xóa: Phòng ban là thuộc tính của CON NGƯỜI (NS_NhanVien), không phải của
--           TÀI KHOẢN (HT_NguoiDung) — xem docs/spec/11_DATA_DB_SCHEMA.md, cột này tự
--           ghi chú "có thể suy từ NhanVien". Giữ lại sẽ tạo 2 nguồn sự thật (tài khoản
--           vs nhân viên) một khi module NS_ triển khai. Về sau, phòng ban của người
--           dùng suy qua HT_NguoiDung.NhanVien_Id → NS_NhanVien.PhongBan_Id.
-- Lưu ý   : HT_NguoiDung hiện có dữ liệu (tài khoản bootstrap) — DROP COLUMN, không phải
--           bảng rỗng như 079. Không có index riêng trên PhongBan_Id (chỉ FK constraint).
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §3 (HT_NguoiDung).
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.HT_NguoiDung', N'U') IS NULL
BEGIN
    RAISERROR(N'HT_NguoiDung chưa tồn tại — chạy migration tạo bảng trước.', 16, 1);
    RETURN;
END;
GO

IF COL_LENGTH('dbo.HT_NguoiDung', 'PhongBan_Id') IS NOT NULL
BEGIN
    DECLARE @fk SYSNAME =
        (SELECT TOP 1 fk.name FROM sys.foreign_keys fk
         JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
         JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
         WHERE fk.parent_object_id = OBJECT_ID('dbo.HT_NguoiDung') AND c.name = 'PhongBan_Id');
    IF @fk IS NOT NULL
        EXEC('ALTER TABLE dbo.HT_NguoiDung DROP CONSTRAINT ' + @fk);

    DECLARE @ix SYSNAME =
        (SELECT TOP 1 i.name FROM sys.indexes i
         JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
         WHERE i.object_id = OBJECT_ID('dbo.HT_NguoiDung') AND c.name = 'PhongBan_Id');
    IF @ix IS NOT NULL
        EXEC('DROP INDEX ' + @ix + ' ON dbo.HT_NguoiDung');

    ALTER TABLE dbo.HT_NguoiDung DROP COLUMN PhongBan_Id;
END;
GO

PRINT N'Migration 091 completed — HT_NguoiDung: -PhongBan_Id (+FK, +index nếu có).';
GO
