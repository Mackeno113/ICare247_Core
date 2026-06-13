-- =============================================================================
-- File    : 044_seed_sys_menu_catalog.sql
-- Purpose : Seed bộ menu MAIN + cây chức năng MASTER (Sys_MenuCatalog) từ cây AppNav
--           hiện tại (3 nhóm · 7 phân hệ · 33 màn + Tổng quan + Công cụ Dev).
--           Func_Code = HT_ChucNang.Ma; Icon = tên Lucide; Route = route hiện hành.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md §4.2, §10 · ADR-023.
-- Context : Config DB (ICare247_Config), sau 043. Idempotent (NOT EXISTS theo Func_Code).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ── 1. Bộ menu MAIN (Sidebar) ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Menu WHERE Menu_Code = 'MAIN' AND Tenant_Id IS NULL)
    INSERT INTO dbo.Sys_Menu (Menu_Code, Menu_Name, Menu_Type) VALUES ('MAIN', N'Menu chính', 'Sidebar');
GO

DECLARE @MenuId INT = (SELECT Menu_Id FROM dbo.Sys_Menu WHERE Menu_Code = 'MAIN' AND Tenant_Id IS NULL);

-- ── 2. Cây chức năng base ───────────────────────────────────────────────────
DECLARE @t TABLE
(
    Func_Code NVARCHAR(100), Func_Name NVARCHAR(200), Parent_Code NVARCHAR(100),
    Func_Type NVARCHAR(20), Module NVARCHAR(20), Route NVARCHAR(300), Icon NVARCHAR(100),
    Display_Order INT
);

INSERT INTO @t (Func_Code, Func_Name, Parent_Code, Func_Type, Module, Route, Icon, Display_Order) VALUES
-- Cấp 0: Tổng quan + 3 nhóm + Dev
(N'dashboard',        N'Tổng quan',          NULL, N'ManHinh', NULL, N'/',         N'layout-grid', 0),
(N'group.operations', N'Vận hành',           NULL, N'Menu',    NULL, NULL,         NULL,           10),
(N'group.business',   N'Kinh doanh',         NULL, N'Menu',    NULL, NULL,         NULL,           20),
(N'group.system',     N'Hệ thống',           NULL, N'Menu',    NULL, NULL,         NULL,           30),
(N'devtools',         N'Công cụ (Dev)',      NULL, N'ManHinh', NULL, N'/dev/forms',N'wrench',      90),
-- Phân hệ (Menu) dưới nhóm
(N'organization',   N'Tổ chức',            N'group.operations', N'Menu', N'TC', N'/m/organization',   N'building',    1),
(N'hr',             N'Nhân sự',            N'group.operations', N'Menu', N'NS', N'/m/hr',             N'users',       2),
(N'payroll',        N'Chấm công – Lương',  N'group.operations', N'Menu', N'TL', N'/m/payroll',        N'clock',       3),
(N'trade',          N'Bán hàng & Kho',     N'group.business',   N'Menu', N'TM', N'/m/trade',          N'package',     1),
(N'finance',        N'Công nợ',            N'group.business',   N'Menu', N'CN', N'/m/finance',        N'credit-card', 2),
(N'reporting',      N'Báo cáo',            N'group.business',   N'Menu', N'BC', N'/m/reporting',      N'bar-chart',   3),
(N'administration', N'Quản trị hệ thống',  N'group.system',     N'Menu', N'HT', N'/m/administration', N'sliders',     1),
-- Màn (ManHinh) dưới phân hệ
(N'organization.company',       N'Công ty',             N'organization', N'ManHinh', N'TC', N'/m/organization/company',       NULL, 1),
(N'organization.department',    N'Phòng ban',           N'organization', N'ManHinh', N'TC', N'/m/organization/department',    NULL, 2),
(N'organization.position',      N'Vị trí công việc',    N'organization', N'ManHinh', N'TC', N'/m/organization/position',      NULL, 3),
(N'organization.title',         N'Chức danh',           N'organization', N'ManHinh', N'TC', N'/m/organization/title',         NULL, 4),
(N'organization.headcount',     N'Định biên nhân sự',   N'organization', N'ManHinh', N'TC', N'/m/organization/headcount',     NULL, 5),
(N'organization.position-plan', N'Hoạch định vị trí',   N'organization', N'ManHinh', N'TC', N'/m/organization/position-plan', NULL, 6),
(N'organization.hr-cost',       N'Chi phí nhân sự',     N'organization', N'ManHinh', N'TC', N'/m/organization/hr-cost',       NULL, 7),
(N'hr.employee', N'Hồ sơ nhân viên',     N'hr', N'ManHinh', N'NS', N'/m/hr/employee', NULL, 1),
(N'hr.contract', N'Hợp đồng lao động',   N'hr', N'ManHinh', N'NS', N'/m/hr/contract', NULL, 2),
(N'hr.process',  N'Quá trình công tác',  N'hr', N'ManHinh', N'NS', N'/m/hr/process',  NULL, 3),
(N'hr.transfer', N'Điều chuyển',         N'hr', N'ManHinh', N'NS', N'/m/hr/transfer', NULL, 4),
(N'hr.reward',   N'Khen thưởng – Kỷ luật',N'hr',N'ManHinh', N'NS', N'/m/hr/reward',   NULL, 5),
(N'payroll.timesheet', N'Chấm công',       N'payroll', N'ManHinh', N'TL', N'/m/payroll/timesheet', NULL, 1),
(N'payroll.period',    N'Kỳ lương',        N'payroll', N'ManHinh', N'TL', N'/m/payroll/period',    NULL, 2),
(N'payroll.payslip',   N'Bảng lương',      N'payroll', N'ManHinh', N'TL', N'/m/payroll/payslip',   NULL, 3),
(N'payroll.config',    N'Thiết lập lương', N'payroll', N'ManHinh', N'TL', N'/m/payroll/config',    NULL, 4),
(N'trade.product',   N'Danh mục hàng hóa', N'trade', N'ManHinh', N'TM', N'/m/trade/product',   NULL, 1),
(N'trade.purchase',  N'Mua hàng',          N'trade', N'ManHinh', N'TM', N'/m/trade/purchase',  NULL, 2),
(N'trade.sales',     N'Bán hàng',          N'trade', N'ManHinh', N'TM', N'/m/trade/sales',     NULL, 3),
(N'trade.stock-in',  N'Nhập kho',          N'trade', N'ManHinh', N'TM', N'/m/trade/stock-in',  NULL, 4),
(N'trade.stock-out', N'Xuất kho',          N'trade', N'ManHinh', N'TM', N'/m/trade/stock-out', NULL, 5),
(N'trade.stock',     N'Tồn kho',           N'trade', N'ManHinh', N'TM', N'/m/trade/stock',     NULL, 6),
(N'finance.receivable', N'Công nợ phải thu', N'finance', N'ManHinh', N'CN', N'/m/finance/receivable', NULL, 1),
(N'finance.payable',    N'Công nợ phải trả', N'finance', N'ManHinh', N'CN', N'/m/finance/payable',    NULL, 2),
(N'reporting.inventory', N'Báo cáo tồn kho',    N'reporting', N'ManHinh', N'BC', N'/m/reporting/inventory', NULL, 1),
(N'reporting.debt',      N'Báo cáo công nợ',    N'reporting', N'ManHinh', N'BC', N'/m/reporting/debt',      NULL, 2),
(N'reporting.pnl',       N'Kết quả kinh doanh', N'reporting', N'ManHinh', N'BC', N'/m/reporting/pnl',       NULL, 3),
(N'reporting.hr',        N'Báo cáo nhân sự',    N'reporting', N'ManHinh', N'BC', N'/m/reporting/hr',        NULL, 4),
(N'reporting.payroll',   N'Báo cáo công lương', N'reporting', N'ManHinh', N'BC', N'/m/reporting/payroll',   NULL, 5),
(N'administration.users',       N'Người dùng',          N'administration', N'ManHinh', N'HT', N'/m/administration/users',       NULL, 1),
(N'administration.roles',       N'Vai trò',             N'administration', N'ManHinh', N'HT', N'/m/administration/roles',       NULL, 2),
(N'administration.permissions', N'Phân quyền',          N'administration', N'ManHinh', N'HT', N'/m/administration/permissions', NULL, 3),
(N'administration.settings',    N'Cấu hình hệ thống',   N'administration', N'ManHinh', N'HT', N'/m/administration/settings',    NULL, 4);

INSERT INTO dbo.Sys_MenuCatalog
    (Menu_Id, Func_Code, Func_Name, Parent_Code, Func_Type, Module, Route, Icon, Display_Pos, Display_Order, Default_Enabled)
SELECT @MenuId, t.Func_Code, t.Func_Name, t.Parent_Code, t.Func_Type, t.Module, t.Route, t.Icon, N'Sidebar', t.Display_Order, 1
FROM @t t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Sys_MenuCatalog c
    WHERE c.Menu_Id = @MenuId AND c.Func_Code = t.Func_Code AND c.Tenant_Id IS NULL
);
GO
