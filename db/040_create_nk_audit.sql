-- =============================================================================
-- File    : 040_create_nk_audit.sql
-- Purpose : Bảng nhật ký hoạt động NK_NhatKyHoatDong (Data DB per-tenant) — ghi log
--           hành vi (đăng nhập, thao tác dữ liệu...) theo cơ chế ghi NỀN (non-blocking):
--           request chỉ enqueue → BackgroundService gộp lô + SqlBulkCopy vào bảng này.
-- Context : Chạy trên DB AUDIT RIÊNG của từng tenant (vd ICare247_Solution_Audit) — tách khỏi
--           Data DB nghiệp vụ để không tranh transaction-log/RAM/I-O. Idempotent.
--           (Chưa tách được thì tạm chạy trên Data DB; ConnectionStrings:Audit rỗng → ghi chung.)
-- Design  : APPEND-ONLY, tối giản cột để tối ưu bulk insert. KHÔNG FK (tránh chậm ghi +
--           NguoiDung_Id có thể NULL khi login sai chưa rõ user). KHÔNG soft-delete/Ver.
-- Owner   : db/ thuộc Codex — file này Claude tạo thay, đã ghi AI_HANDOFF.md.
-- =============================================================================

IF OBJECT_ID(N'dbo.NK_NhatKyHoatDong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NK_NhatKyHoatDong
    (
        Id            BIGINT         IDENTITY(1,1) NOT NULL,
        ThoiGian      DATETIME2      NOT NULL,                 -- UTC, set tường minh lúc enqueue
        Loai          NVARCHAR(30)   NOT NULL,                 -- 'Auth' | 'MasterData' | ...
        HanhDong      NVARCHAR(50)   NOT NULL,                 -- 'LOGIN_SUCCESS','DATA_UPDATE',...
        KetQua        NVARCHAR(20)   NULL,                     -- 'Success' | 'Failed'
        NguoiDung_Id  BIGINT         NULL,                     -- actor (NULL nếu login sai/ẩn danh)
        TenDangNhap   NVARCHAR(100)  NULL,
        DoiTuong      NVARCHAR(100)  NULL,                     -- object type (form code / bảng)
        DoiTuong_Id   NVARCHAR(100)  NULL,                     -- PK đối tượng (string: hỗ trợ mọi kiểu)
        GiaTriCu      NVARCHAR(MAX)  NULL,                     -- JSON trước thay đổi (nếu có)
        GiaTriMoi     NVARCHAR(MAX)  NULL,                     -- JSON sau thay đổi (nếu có)
        DiaChiIp      NVARCHAR(50)   NULL,
        ThietBi       NVARCHAR(300)  NULL,                     -- User-Agent rút gọn
        CorrelationId NVARCHAR(50)   NULL,                     -- nối với request log (Serilog)

        CONSTRAINT PK_NK_NhatKyHoatDong PRIMARY KEY (Id)
    );

    -- Index phục vụ tra cứu nhật ký (theo thời gian / người dùng / loại hành động)
    CREATE INDEX IX_NK_NhatKy_ThoiGian   ON dbo.NK_NhatKyHoatDong (ThoiGian DESC);
    CREATE INDEX IX_NK_NhatKy_NguoiDung  ON dbo.NK_NhatKyHoatDong (NguoiDung_Id, ThoiGian DESC);
    CREATE INDEX IX_NK_NhatKy_Loai       ON dbo.NK_NhatKyHoatDong (Loai, HanhDong, ThoiGian DESC);
END;
GO

PRINT N'Migration 040 completed — dbo.NK_NhatKyHoatDong created.';
GO
