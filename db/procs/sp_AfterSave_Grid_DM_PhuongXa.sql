-- =============================================================================
-- File    : sp_AfterSave_Grid_DM_PhuongXa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : SVHOOK-6 — HẬU XỬ LÝ sau khi ghi cho màn Xã/Phường (engine-driven).
--           @Id = id thật vừa ghi (Insert: id mới · Update: id cũ). Chạy trong CÙNG
--           transaction với INSERT/UPDATE → trả result set lỗi (cùng contract spc_)
--           sẽ ROLLBACK cả bản ghi; rỗng = OK.
-- Mẫu     : Hiện pass-through (không làm gì). Bên dưới là ví dụ các việc hậu xử lý
--           thường gặp (để mở khi cần) — KHÔNG bật mặc định.
-- Spec    : docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029.
-- Idempotent: CREATE OR ALTER (chạy lại = cập nhật).
-- =============================================================================

USE [ICare247_Solution];
GO

-- Hợp đồng v2 (ADR-034 §12.1): thêm @Source + @ImportSessionId (DEFAULT) — engine chỉ truyền khi IMPORT.
-- Save tay: engine giữ EXEC cũ (không truyền 2 tham số này) → proc dùng giá trị DEFAULT.
CREATE OR ALTER PROCEDURE dbo.sp_AfterSave_Grid_DM_PhuongXa
    @Id              BIGINT,
    @TenantId        INT,
    @NguoiDungID     BIGINT,
    @LangCode        NVARCHAR(10),
    @PayloadJson     NVARCHAR(MAX),
    @Source          NVARCHAR(20)     = N'MANUAL',   -- 'MANUAL' (nhập tay) | 'IMPORT'
    @ImportSessionId UNIQUEIDENTIFIER = NULL          -- phiên import (NULL khi nhập tay)
AS
BEGIN
    SET NOCOUNT ON;

    -- @Id 0 = thêm mới · >0 = cập nhật · @NguoiDungID = người thực hiện · @Source = ngữ cảnh.
    -- Ví dụ: chỉ chạy khi import → IF @Source = N'IMPORT' BEGIN ... END
    -- Ví dụ hậu xử lý (mở khi cần):
    --   • Chuẩn hóa dữ liệu liên quan, tính cột dẫn xuất.
    --   • Ghi nhật ký nghiệp vụ (kèm @ImportSessionId để truy mẻ import).
    --   • Ràng buộc CHỈ biết sau khi ghi → vi phạm thì trả result set lỗi:
    --       SELECT N'sys.val.Conflict' AS error_key, NULL AS args_json,
    --              NULL AS field_name,  N'error' AS severity;   -- => rollback cả bản ghi

    RETURN;   -- pass-through: không lỗi, không hậu xử lý.
END;
GO

PRINT N'Created/Altered dbo.sp_AfterSave_Grid_DM_PhuongXa.';
GO
