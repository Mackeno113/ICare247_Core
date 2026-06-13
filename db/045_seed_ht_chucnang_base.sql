-- =============================================================================
-- File    : 045_seed_ht_chucnang_base.sql
-- Purpose : Seed cây HT_ChucNang BASE cho 1 tenant (bootstrap dev) từ cây AppNav +
--           grant toàn quyền cho vai trò SUPERADMIN (để admin thấy đủ như hiện tại).
--           Mọi node LaHeThong=1 (base), KichHoat=1, ViTriHienThi='Sidebar'.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md §10 · ADR-023.
-- Context : Data DB tenant, sau 042 (cột) + 038 (admin + SUPERADMIN). Idempotent (theo Ma).
-- Note    : Menu_Id = 1 (bộ menu MAIN ở Config DB, cài mới IDENTITY=1). Bản đồng bộ
--           master→tenant thật (backend) sẽ map Menu_Id chuẩn — đây là bootstrap dev.
--           CreatedBy đặt tường minh = tài khoản admin (không dựa DEFAULT).
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @MenuId  INT    = 1;   -- MAIN (Config DB Sys_Menu)

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Chưa có tài khoản admin — chạy 038_seed_data_db_bootstrap.sql trước.', 16, 1);
    RETURN;
END;

-- ── 1. Danh sách node (Ma, ChaMa, Ten, Loai, Module, DuongDan, Icon, ThuTu) ──
DECLARE @t TABLE
(
    Ma NVARCHAR(100), ChaMa NVARCHAR(100), Ten NVARCHAR(200),
    Loai NVARCHAR(20), Module NVARCHAR(20), DuongDan NVARCHAR(300), Icon NVARCHAR(100), ThuTu INT
);

INSERT INTO @t (Ma, ChaMa, Ten, Loai, Module, DuongDan, Icon, ThuTu) VALUES
(N'dashboard',        NULL, N'Tổng quan',     N'ManHinh', NULL, N'/',          N'layout-grid', 0),
(N'group.operations', NULL, N'Vận hành',      N'Menu',    NULL, NULL,          NULL,           10),
(N'group.business',   NULL, N'Kinh doanh',    N'Menu',    NULL, NULL,          NULL,           20),
(N'group.system',     NULL, N'Hệ thống',      N'Menu',    NULL, NULL,          NULL,           30),
(N'devtools',         NULL, N'Công cụ (Dev)', N'ManHinh', NULL, N'/dev/forms', N'wrench',      90),
(N'organization',   N'group.operations', N'Tổ chức',           N'Menu', N'TC', N'/m/organization',   N'building',    1),
(N'hr',             N'group.operations', N'Nhân sự',           N'Menu', N'NS', N'/m/hr',             N'users',       2),
(N'payroll',        N'group.operations', N'Chấm công – Lương', N'Menu', N'TL', N'/m/payroll',        N'clock',       3),
(N'trade',          N'group.business',   N'Bán hàng & Kho',    N'Menu', N'TM', N'/m/trade',          N'package',     1),
(N'finance',        N'group.business',   N'Công nợ',           N'Menu', N'CN', N'/m/finance',        N'credit-card', 2),
(N'reporting',      N'group.business',   N'Báo cáo',           N'Menu', N'BC', N'/m/reporting',      N'bar-chart',   3),
(N'administration', N'group.system',     N'Quản trị hệ thống', N'Menu', N'HT', N'/m/administration', N'sliders',     1),
(N'organization.company',       N'organization', N'Công ty',           N'ManHinh', N'TC', N'/m/organization/company',       NULL, 1),
(N'organization.department',    N'organization', N'Phòng ban',         N'ManHinh', N'TC', N'/m/organization/department',    NULL, 2),
(N'organization.position',      N'organization', N'Vị trí công việc',  N'ManHinh', N'TC', N'/m/organization/position',      NULL, 3),
(N'organization.title',         N'organization', N'Chức danh',         N'ManHinh', N'TC', N'/m/organization/title',         NULL, 4),
(N'organization.headcount',     N'organization', N'Định biên nhân sự', N'ManHinh', N'TC', N'/m/organization/headcount',     NULL, 5),
(N'organization.position-plan', N'organization', N'Hoạch định vị trí', N'ManHinh', N'TC', N'/m/organization/position-plan', NULL, 6),
(N'organization.hr-cost',       N'organization', N'Chi phí nhân sự',   N'ManHinh', N'TC', N'/m/organization/hr-cost',       NULL, 7),
(N'hr.employee', N'hr', N'Hồ sơ nhân viên',      N'ManHinh', N'NS', N'/m/hr/employee', NULL, 1),
(N'hr.contract', N'hr', N'Hợp đồng lao động',    N'ManHinh', N'NS', N'/m/hr/contract', NULL, 2),
(N'hr.process',  N'hr', N'Quá trình công tác',   N'ManHinh', N'NS', N'/m/hr/process',  NULL, 3),
(N'hr.transfer', N'hr', N'Điều chuyển',          N'ManHinh', N'NS', N'/m/hr/transfer', NULL, 4),
(N'hr.reward',   N'hr', N'Khen thưởng – Kỷ luật',N'ManHinh', N'NS', N'/m/hr/reward',   NULL, 5),
(N'payroll.timesheet', N'payroll', N'Chấm công',       N'ManHinh', N'TL', N'/m/payroll/timesheet', NULL, 1),
(N'payroll.period',    N'payroll', N'Kỳ lương',        N'ManHinh', N'TL', N'/m/payroll/period',    NULL, 2),
(N'payroll.payslip',   N'payroll', N'Bảng lương',      N'ManHinh', N'TL', N'/m/payroll/payslip',   NULL, 3),
(N'payroll.config',    N'payroll', N'Thiết lập lương', N'ManHinh', N'TL', N'/m/payroll/config',    NULL, 4),
(N'trade.product',   N'trade', N'Danh mục hàng hóa', N'ManHinh', N'TM', N'/m/trade/product',   NULL, 1),
(N'trade.purchase',  N'trade', N'Mua hàng',          N'ManHinh', N'TM', N'/m/trade/purchase',  NULL, 2),
(N'trade.sales',     N'trade', N'Bán hàng',          N'ManHinh', N'TM', N'/m/trade/sales',     NULL, 3),
(N'trade.stock-in',  N'trade', N'Nhập kho',          N'ManHinh', N'TM', N'/m/trade/stock-in',  NULL, 4),
(N'trade.stock-out', N'trade', N'Xuất kho',          N'ManHinh', N'TM', N'/m/trade/stock-out', NULL, 5),
(N'trade.stock',     N'trade', N'Tồn kho',           N'ManHinh', N'TM', N'/m/trade/stock',     NULL, 6),
(N'finance.receivable', N'finance', N'Công nợ phải thu', N'ManHinh', N'CN', N'/m/finance/receivable', NULL, 1),
(N'finance.payable',    N'finance', N'Công nợ phải trả', N'ManHinh', N'CN', N'/m/finance/payable',    NULL, 2),
(N'reporting.inventory', N'reporting', N'Báo cáo tồn kho',    N'ManHinh', N'BC', N'/m/reporting/inventory', NULL, 1),
(N'reporting.debt',      N'reporting', N'Báo cáo công nợ',    N'ManHinh', N'BC', N'/m/reporting/debt',      NULL, 2),
(N'reporting.pnl',       N'reporting', N'Kết quả kinh doanh', N'ManHinh', N'BC', N'/m/reporting/pnl',       NULL, 3),
(N'reporting.hr',        N'reporting', N'Báo cáo nhân sự',    N'ManHinh', N'BC', N'/m/reporting/hr',        NULL, 4),
(N'reporting.payroll',   N'reporting', N'Báo cáo công lương', N'ManHinh', N'BC', N'/m/reporting/payroll',   NULL, 5),
(N'administration.users',       N'administration', N'Người dùng',        N'ManHinh', N'HT', N'/m/administration/users',       NULL, 1),
(N'administration.roles',       N'administration', N'Vai trò',           N'ManHinh', N'HT', N'/m/administration/roles',       NULL, 2),
(N'administration.permissions', N'administration', N'Phân quyền',        N'ManHinh', N'HT', N'/m/administration/permissions', NULL, 3),
(N'administration.settings',    N'administration', N'Cấu hình hệ thống', N'ManHinh', N'HT', N'/m/administration/settings',    NULL, 4);

-- ── 2. INSERT node còn thiếu (cha để NULL, nối ở bước 3) ────────────────────
INSERT INTO dbo.HT_ChucNang
    (Ma, Ten, ChucNang_Cha_Id, Loai, Module, DuongDan, Icon, ThuTu,
     Menu_Id, LaHeThong, KichHoat, ViTriHienThi, CreatedBy, CreatedAt)
SELECT t.Ma, t.Ten, NULL, t.Loai, t.Module, t.DuongDan, t.Icon, t.ThuTu,
       @MenuId, 1, 1, N'Sidebar', @AdminId, SYSUTCDATETIME()
FROM @t t
WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_ChucNang c WHERE c.Ma = t.Ma AND c.IsDeleted = 0);

-- ── 3. Nối cha-con theo Ma (bền vững dù chạy lại) ───────────────────────────
UPDATE c
SET c.ChucNang_Cha_Id = p.Id, c.UpdatedBy = @AdminId, c.UpdatedAt = SYSUTCDATETIME()
FROM dbo.HT_ChucNang c
JOIN @t t            ON t.Ma = c.Ma AND t.ChaMa IS NOT NULL
JOIN dbo.HT_ChucNang p ON p.Ma = t.ChaMa AND p.IsDeleted = 0
WHERE c.IsDeleted = 0 AND (c.ChucNang_Cha_Id IS NULL OR c.ChucNang_Cha_Id <> p.Id);
GO

-- ── 4. Grant SUPERADMIN toàn quyền (Xem/Thêm/Sửa/Xóa/In; Duyệt=0 → workflow) ─
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);
DECLARE @SuperRole BIGINT =
    (SELECT Id FROM dbo.HT_VaiTro WHERE Ma = N'SUPERADMIN' AND IsDeleted = 0);

IF @SuperRole IS NOT NULL
BEGIN
    INSERT INTO dbo.HT_VaiTro_Quyen
        (VaiTro_Id, ChucNang_Id, Xem, Them, Sua, Xoa, Duyet, InAn, CreatedBy, CreatedAt)
    SELECT @SuperRole, c.Id, 1, 1, 1, 1, 0, 1, @AdminId, SYSUTCDATETIME()
    FROM dbo.HT_ChucNang c
    WHERE c.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_Quyen q
                      WHERE q.VaiTro_Id = @SuperRole AND q.ChucNang_Id = c.Id AND q.IsDeleted = 0);
END;
GO
