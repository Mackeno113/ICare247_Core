-- =============================================================================
-- File    : 043_create_sys_menu_catalog.sql
-- Purpose : Tạo bảng MASTER menu ở Config DB (dùng chung mọi tenant): Sys_Menu (bộ
--           menu) + Sys_MenuCatalog (cây chức năng base). DEV định nghĩa qua WPF;
--           đồng bộ xuống HT_ChucNang mỗi tenant (UPSERT theo Func_Code).
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md §4.1–4.2 · ADR-023.
-- Context : Config DB (ICare247_Config). Quy ước Config DB: snake_case, Tenant_Id NULL
--           = bản dùng chung, Is_Active, Created_At/Updated_At (KHÔNG có Created_By).
-- Note    : Idempotent — IF OBJECT_ID(...) IS NULL.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ── Sys_Menu — bộ menu (cho phép nhiều menu: Sidebar/Top/Mobile/Context) ─────
IF OBJECT_ID('dbo.Sys_Menu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Menu
    (
        Menu_Id     INT             IDENTITY(1,1) NOT NULL,
        Menu_Code   NVARCHAR(50)    NOT NULL,             -- 'MAIN','MOBILE'...
        Menu_Name   NVARCHAR(200)   NOT NULL,
        Menu_Type   NVARCHAR(20)    NOT NULL DEFAULT 'Sidebar',  -- Sidebar/Top/Mobile/Context
        Tenant_Id   INT             NULL,                 -- NULL = dùng chung
        Is_Active   BIT             NOT NULL DEFAULT 1,
        Created_At  DATETIME        NOT NULL DEFAULT GETDATE(),
        Updated_At  DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Menu PRIMARY KEY (Menu_Id)
    );
    CREATE UNIQUE INDEX UQ_Sys_Menu_Code_Global ON dbo.Sys_Menu (Menu_Code) WHERE Tenant_Id IS NULL;
    CREATE UNIQUE INDEX UQ_Sys_Menu_Code_Tenant ON dbo.Sys_Menu (Menu_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
END;
GO

-- ── Sys_MenuCatalog — cây chức năng MASTER (định nghĩa, KHÔNG chứa quyền) ────
IF OBJECT_ID('dbo.Sys_MenuCatalog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_MenuCatalog
    (
        Catalog_Id      BIGINT          IDENTITY(1,1) NOT NULL,
        Menu_Id         INT             NOT NULL,             -- thuộc bộ menu nào
        Func_Code       NVARCHAR(100)   NOT NULL,             -- khóa nghiệp vụ ổn định = HT_ChucNang.Ma
        Func_Name       NVARCHAR(200)   NOT NULL,
        Parent_Code     NVARCHAR(100)   NULL,                 -- Func_Code của cha (cây — dùng code cho bền khi đồng bộ)
        Func_Type       NVARCHAR(20)    NOT NULL DEFAULT 'Menu',   -- Menu/ManHinh/ChucNangCon
        Module          NVARCHAR(20)    NULL,                 -- 'TC','NS','TL','TM','CN','BC','HT'
        Route           NVARCHAR(300)   NULL,                 -- DuongDan; NULL với node nhóm
        Icon            NVARCHAR(100)   NULL,                 -- tên icon Lucide
        Display_Pos     NVARCHAR(20)    NOT NULL DEFAULT 'Sidebar',  -- ViTriHienThi: Sidebar/TrongMan/Ca2
        Display_Order   INT             NOT NULL DEFAULT 0,   -- ThuTu
        Default_Enabled BIT             NOT NULL DEFAULT 1,   -- giá trị khởi tạo HT_ChucNang.KichHoat khi đồng bộ
        Tenant_Id       INT             NULL,                 -- NULL = base dùng chung
        [Version]       INT             NOT NULL DEFAULT 1,   -- đồng bộ dựa vào để biết bản mới
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_MenuCatalog PRIMARY KEY (Catalog_Id),
        CONSTRAINT FK_Sys_MenuCatalog_Menu FOREIGN KEY (Menu_Id) REFERENCES dbo.Sys_Menu (Menu_Id)
    );
    CREATE UNIQUE INDEX UQ_Sys_MenuCatalog_Func_Global ON dbo.Sys_MenuCatalog (Menu_Id, Func_Code) WHERE Tenant_Id IS NULL;
    CREATE INDEX IX_Sys_MenuCatalog_Parent ON dbo.Sys_MenuCatalog (Menu_Id, Parent_Code);
END;
GO
