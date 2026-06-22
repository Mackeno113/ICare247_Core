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

CREATE OR ALTER PROCEDURE dbo.sp_AfterSave_Grid_DM_PhuongXa
    @Id            BIGINT,
    @TenantId      INT,
    @NguoiThucHien BIGINT,
    @LangCode      NVARCHAR(10),
    @PayloadJson   NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Ví dụ hậu xử lý (mở khi cần):
    --   • Chuẩn hóa dữ liệu liên quan, tính cột dẫn xuất.
    --   • Ghi nhật ký nghiệp vụ vào bảng riêng.
    --   • Kiểm tra ràng buộc CHỈ biết sau khi ghi → nếu vi phạm, trả result set lỗi:
    --       SELECT N'sys.val.Conflict' AS error_key, NULL AS args_json,
    --              NULL AS field_name,  N'error' AS severity;   -- => rollback cả bản ghi
    --
    --   Lấy field từ payload nếu cần: JSON_VALUE(@PayloadJson, '$.Ma') ...

    RETURN;   -- pass-through: không lỗi, không hậu xử lý.
END;
GO

PRINT N'Created/Altered dbo.sp_AfterSave_Grid_DM_PhuongXa.';
GO
