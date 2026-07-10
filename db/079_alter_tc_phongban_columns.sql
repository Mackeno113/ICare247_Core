-- =============================================================================
-- File    : 079_alter_tc_phongban_columns.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Chuẩn hóa dbo.TC_PhongBan về ĐƠN VỊ PHÒNG BAN CƠ BẢN (chưa quy mô hóa cấu trúc tổ chức):
--           1) Thêm MoTa (mô tả tự do).
--           2) Cache sắp xếp cây ADR-027: Cap / ThuTuCay / DuongDanCay (dẫn xuất từ PhongBan_Cha_Id +
--              ThuTu; recompute-on-write — proc generic chưa có nên tạm NULL cho tới khi triển khai ADR-027).
--           3) Vòng đời hiệu lực (NgayHieuLuc / NgayHetHieuLuc).
--           4) Gỡ TruongDonVi_Id — thông tin nhân sự/phân loại tổ chức để pha sau (quy mô hóa).
--           Màu hiển thị đặt ở cấp phòng ban (DM_CapPhongBan) để mọi PB cùng cấp đồng nhất — xem 080.
-- Phạm vi : Khối/nhóm nghiệp vụ/chức vụ lãnh đạo/người phụ trách CHƯA đưa vào — thuộc giai đoạn mở rộng.
-- Lưu ý   : TC_PhongBan hiện rỗng (0 dòng) → DROP cột an toàn, không mất dữ liệu.
--           Không khai FK vật lý (bám phong cách bảng hiện có: soft-check FK qua Sys_Relation ở Config DB).
-- Spec    : .claude-rules/database-design.md (§2 audit, §5 tên cột, §6 cây, §7 index) · ADR-022 · ADR-027.
-- Convention: KHÔNG USE/CREATE DATABASE (chạy trong ngữ cảnh Data DB tenant). Idempotent.
-- =============================================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.TC_PhongBan', N'U') IS NULL
BEGIN
    RAISERROR(N'TC_PhongBan chưa tồn tại — chạy migration tạo bảng trước.', 16, 1);
    RETURN;
END;
GO

-- ── 1. Mô tả ─────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.TC_PhongBan', 'MoTa') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD MoTa NVARCHAR(500) NULL;                 -- mô tả tự do
GO

-- ── 2. Cache sắp xếp cây (ADR-027 — dẫn xuất, recompute-on-write) ─────────────
-- Nguồn sự thật vẫn là PhongBan_Cha_Id + ThuTu (input, đã có). 3 cột dưới là CACHE:
-- proc generic sẽ tính lại mỗi khi ghi; hiện chưa code proc → giá trị NULL cho tới lúc đó.
IF COL_LENGTH('dbo.TC_PhongBan', 'Cap') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD Cap INT NULL;                           -- độ sâu trong cây (gốc = 1)
GO
IF COL_LENGTH('dbo.TC_PhongBan', 'ThuTuCay') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD ThuTuCay INT NULL;                      -- thứ tự trải phẳng toàn cây
GO
IF COL_LENGTH('dbo.TC_PhongBan', 'DuongDanCay') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD DuongDanCay NVARCHAR(400) NULL;         -- materialized path (chuỗi Id cha→con)
GO

-- ── 3. Vòng đời hiệu lực ─────────────────────────────────────────────────────
IF COL_LENGTH('dbo.TC_PhongBan', 'NgayHieuLuc') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD NgayHieuLuc DATE NULL;
GO
IF COL_LENGTH('dbo.TC_PhongBan', 'NgayHetHieuLuc') IS NULL
    ALTER TABLE dbo.TC_PhongBan ADD NgayHetHieuLuc DATE NULL;
GO

-- ── 4. Gỡ cột nhân sự/tổ chức (để pha mở rộng sau) ───────────────────────────
-- Bảng rỗng nên DROP trực tiếp; gỡ index/default phụ thuộc trước nếu có (phòng thủ).
IF COL_LENGTH('dbo.TC_PhongBan', 'TruongDonVi_Id') IS NOT NULL
BEGIN
    DECLARE @ix SYSNAME =
        (SELECT TOP 1 i.name FROM sys.indexes i
         JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
         WHERE i.object_id = OBJECT_ID('dbo.TC_PhongBan') AND c.name = 'TruongDonVi_Id');
    IF @ix IS NOT NULL
        EXEC('DROP INDEX ' + @ix + ' ON dbo.TC_PhongBan');

    ALTER TABLE dbo.TC_PhongBan DROP COLUMN TruongDonVi_Id;
END;
GO

-- ── 5. Index cột phân loại cơ bản đã có (CapPhongBan_Id) ─────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TC_PhongBan_CapPhongBan'
               AND object_id = OBJECT_ID('dbo.TC_PhongBan'))
    CREATE NONCLUSTERED INDEX IX_TC_PhongBan_CapPhongBan ON dbo.TC_PhongBan (CapPhongBan_Id);
GO

PRINT N'Migration 079 completed — TC_PhongBan cơ bản: +MoTa, +Cap/ThuTuCay/DuongDanCay (ADR-027), +NgayHieuLuc/NgayHetHieuLuc, -TruongDonVi_Id, +IX_CapPhongBan.';
GO
