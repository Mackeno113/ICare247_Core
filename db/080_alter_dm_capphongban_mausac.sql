-- =============================================================================
-- File    : 080_alter_dm_capphongban_mausac.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Thêm MauSac vào DM_CapPhongBan — màu hiển thị đặt Ở CẤP phòng ban để mọi phòng ban cùng cấp
--           đồng nhất màu (không lặp màu trên từng TC_PhongBan). Cột NULL-able → thuần bổ sung.
-- Spec    : .claude-rules/database-design.md (§5 tên cột tiếng Việt) · liên quan migration 079.
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.DM_CapPhongBan', N'U') IS NULL
BEGIN
    RAISERROR(N'DM_CapPhongBan chưa tồn tại — chạy migration tạo bảng trước.', 16, 1);
    RETURN;
END;
GO

IF COL_LENGTH('dbo.DM_CapPhongBan', 'MauSac') IS NULL
    ALTER TABLE dbo.DM_CapPhongBan ADD MauSac INT NULL;   -- màu hiển thị (ARGB) dùng chung cho mọi PB cùng cấp
GO

PRINT N'Migration 080 completed — DM_CapPhongBan thêm cột MauSac.';
GO
