-- =============================================================================
-- File    : 042_alter_ht_chucnang_authz.sql
-- Purpose : Bổ sung cột cho HT_ChucNang phục vụ menu động + phân quyền (phase-auth).
--           Thêm: Menu_Id (thuộc bộ menu), LaHeThong (base/custom), KichHoat (tenant
--           bật/tắt), ViTriHienThi (Sidebar/TrongMan/Ca2).
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md §4.3 · ADR-023.
-- Context : Chạy TRONG ngữ cảnh Data DB của tenant (sau 037). KHÔNG USE/CREATE DATABASE.
-- Note    : Idempotent — kiểm tra COL_LENGTH trước mỗi ALTER.
-- =============================================================================

SET XACT_ABORT ON;
GO

-- Menu_Id — node thuộc bộ menu nào (đồng bộ từ Sys_Menu.Menu_Id ở Config DB).
IF COL_LENGTH('dbo.HT_ChucNang', 'Menu_Id') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD Menu_Id INT NULL;
GO

-- LaHeThong — 1=BASE (đồng bộ từ master, khóa cấu trúc) · 0=CUSTOM (tenant/DEV thêm riêng).
IF COL_LENGTH('dbo.HT_ChucNang', 'LaHeThong') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD LaHeThong BIT NOT NULL DEFAULT 0;
GO

-- KichHoat — tenant bật/tắt node (độc lập với quyền). 0 = không ai thấy dù có quyền.
IF COL_LENGTH('dbo.HT_ChucNang', 'KichHoat') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD KichHoat BIT NOT NULL DEFAULT 1;
GO

-- ViTriHienThi — nơi render: 'Sidebar' (thanh trái) / 'TrongMan' (sub-nav trong màn) / 'Ca2'.
IF COL_LENGTH('dbo.HT_ChucNang', 'ViTriHienThi') IS NULL
    ALTER TABLE dbo.HT_ChucNang ADD ViTriHienThi NVARCHAR(20) NOT NULL DEFAULT N'Sidebar';
GO

-- Index phục vụ truy vấn cây menu theo bộ menu + lọc node đang bật.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HT_ChucNang_Menu' AND object_id = OBJECT_ID('dbo.HT_ChucNang'))
    CREATE INDEX IX_HT_ChucNang_Menu ON dbo.HT_ChucNang (Menu_Id, KichHoat) WHERE IsDeleted = 0;
GO
