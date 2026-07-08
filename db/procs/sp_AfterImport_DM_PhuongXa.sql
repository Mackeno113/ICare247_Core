-- =============================================================================
-- File    : sp_AfterImport_DM_PhuongXa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : IMPORT-6 (ADR-034 §12.2) — HOOK SAU IMPORT cho màn Xã/Phường. Chạy 1 LẦN cuối mẻ
--           import (CommitImportCommand), transaction riêng SAU khi các dòng đã commit.
--           Lỗi ở đây KHÔNG rollback dữ liệu đã ghi (không thể) → chỉ ghi cảnh báo phía App.
-- Tham số : @ImportSessionId (GUID mẻ) · @NguoiDungID (ai import) · @TenantId ·
--           @InsertedCount/@UpdatedCount/@ErrorCount · @RecordIdsJson (mảng Id đã ghi) · @ImportedAt.
-- Mẫu     : pass-through. Mở khi cần tổng hợp/tính lại/đẩy thông báo cuối mẻ.
-- Opt-in  : App gọi qua OBJECT_ID — proc chưa tồn tại thì bỏ qua (màn chưa bật hook chạy như thường).
-- Idempotent: CREATE OR ALTER.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER PROCEDURE dbo.sp_AfterImport_DM_PhuongXa
    @ImportSessionId UNIQUEIDENTIFIER,
    @NguoiDungID     BIGINT,
    @TenantId        INT,
    @InsertedCount   INT,
    @UpdatedCount    INT,
    @ErrorCount      INT,
    @RecordIdsJson   NVARCHAR(MAX),
    @ImportedAt      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    -- Ví dụ hậu xử lý cuối mẻ (mở khi cần):
    --   • Tính lại thứ tự cây / cột dẫn xuất cho các bản ghi vừa import.
    --       -- Duyệt Id: SELECT value FROM OPENJSON(@RecordIdsJson)
    --   • Đẩy thông báo tổng hợp cho @NguoiDungID.
    --   • Ghi nhật ký mẻ import vào bảng riêng.

    RETURN;   -- pass-through: không hậu xử lý.
END;
GO

PRINT N'Created/Altered dbo.sp_AfterImport_DM_PhuongXa.';
GO
