-- =============================================================================
-- File    : 095_create_nk_loihethong.sql
-- Purpose : Bảng nhật ký lỗi hệ thống NK_LoiHeThong (Audit DB per-tenant) — lưu chi tiết
--           exception 500 chưa xử lý (message, stack trace, request context) để màn Admin
--           "Nhật ký lỗi" tra theo Mã lỗi (CorrelationId) thay vì phải mở file logs/icare247-*.log.
-- Context : Chạy trên DB AUDIT RIÊNG của từng tenant (vd ICare247_Solution_Audit) — cùng chỗ
--           với NK_NhatKyHoatDong (xem db/040_create_nk_audit.sql). Chưa tách được thì tạm
--           chạy trên Data DB (ConnectionStrings:Audit rỗng → dùng Data DB). Idempotent.
-- Design  : Archetype "Kỹ thuật / Log" (docs/spec/11_DATA_DB_SCHEMA.md §0.2) — KHÔNG Ma/Ten,
--           KHÔNG FK (actor có thể null/không xác định lúc lỗi), KHÔNG soft-delete/Ver.
--           Ghi qua ErrorLogQueue (Channel bounded, drop khi đầy) → ErrorLogBackgroundService
--           INSERT trực tiếp từng dòng (KHÔNG SqlBulkCopy — lỗi hiếm hơn audit trail nhiều).
-- =============================================================================

IF OBJECT_ID(N'dbo.NK_LoiHeThong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NK_LoiHeThong
    (
        Id            BIGINT         IDENTITY(1,1) NOT NULL,
        ThoiGian      DATETIME2      NOT NULL,                 -- UTC, set tường minh lúc enqueue
        CorrelationId NVARCHAR(64)   NOT NULL,                 -- "Mã lỗi" hiển thị cho user (8 ký tự đầu)
        Nguon         NVARCHAR(300)  NULL,                     -- Exception type / class gây lỗi
        ThongDiep     NVARCHAR(MAX)  NULL,                     -- ex.Message
        ChiTiet       NVARCHAR(MAX)  NULL,                     -- Stack trace / ex.ToDetail()
        DuongDan      NVARCHAR(500)  NULL,                     -- Request path
        PhuongThuc    NVARCHAR(10)   NULL,                     -- HTTP method
        NguoiDung_Id  BIGINT         NULL,                     -- actor (NULL nếu chưa xác thực)
        TenDangNhap   NVARCHAR(100)  NULL,
        DiaChiIp      NVARCHAR(50)   NULL,
        ThietBi       NVARCHAR(300)  NULL,                     -- User-Agent rút gọn

        CONSTRAINT PK_NK_LoiHeThong PRIMARY KEY (Id)
    );

    -- Index phục vụ tra cứu: theo thời gian (mặc định lưới mới nhất trước) + theo Mã lỗi (tra prefix)
    CREATE INDEX IX_NK_LoiHeThong_ThoiGian      ON dbo.NK_LoiHeThong (ThoiGian DESC);
    CREATE INDEX IX_NK_LoiHeThong_CorrelationId ON dbo.NK_LoiHeThong (CorrelationId);
END;
GO

PRINT N'Migration 095 completed — dbo.NK_LoiHeThong created.';
GO
