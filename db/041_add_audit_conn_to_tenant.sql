-- =============================================================================
-- File    : 041_add_audit_conn_to_tenant.sql
-- Purpose : Thêm cột Audit_Conn_Encrypted vào CATALOG dbo.Tenant — chuỗi kết nối DB
--           nhật ký (audit) RIÊNG của từng tenant (mã hóa, giải mã bằng Catalog:EncryptionKey).
--           NULL/rỗng = tạm dùng chung Data DB (resolver tự fallback).
-- Context : Chạy trên CATALOG DB master (vd ICare247_Master). Idempotent.
-- Owner   : db/ thuộc Codex — Claude tạo thay, đã ghi AI_HANDOFF.md.
-- Liên quan: 036_create_catalog.sql, ADR-018.
-- =============================================================================

IF OBJECT_ID('dbo.Tenant', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Tenant', 'Audit_Conn_Encrypted') IS NULL
BEGIN
    ALTER TABLE dbo.Tenant ADD Audit_Conn_Encrypted NVARCHAR(MAX) NULL;
    PRINT N'Migration 041 — đã thêm cột dbo.Tenant.Audit_Conn_Encrypted.';
END
ELSE
    PRINT N'Migration 041 — bỏ qua (chưa có bảng Tenant hoặc cột đã tồn tại).';
GO
