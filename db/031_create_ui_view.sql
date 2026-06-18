-- =============================================================================
-- File    : 031_create_ui_view.sql
-- Purpose : Tạo cụm bảng cấu hình hiển thị danh sách (Grid/TreeList) — VIEW-1a.
--           Ui_View (header) + Ui_View_Column (cột) + Ui_View_Action (nút toolbar/row).
--           Tách khỏi form sửa (Ui_Form/Ui_Field). Xem spec 14_VIEW_CONFIG_SPEC + ADR-015.
-- Note    : Idempotent — guard IF OBJECT_ID/IF NOT EXISTS, chạy lại an toàn.
--           Seed view mặc định (VIEW-1b) tách script riêng — không gộp ở đây.
-- =============================================================================

USE [ICare247_Config];
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Ui_View — header (datasource + hành vi + export/print + TreeList)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_View', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_View
    (
        View_Id             INT            IDENTITY(1,1) NOT NULL,
        View_Code           NVARCHAR(100)  NOT NULL,                  -- định danh kỹ thuật (route /view/{code})
        View_Type           NVARCHAR(30)   NOT NULL DEFAULT 'Grid',   -- Grid | TreeList | Cards
        Table_Id            INT            NOT NULL,                  -- bảng nguồn (base)
        Source_Type         NVARCHAR(30)   NOT NULL DEFAULT 'Table',  -- Table | View | Sp | Api
        Source_Object       NVARCHAR(MAX)  NULL,                      -- tên view/SP/SQL/endpoint khi ≠ Table
        Title_Key           NVARCHAR(150)  NULL,                      -- i18n tiêu đề màn
        Edit_Form_Id        INT            NULL,                      -- Ui_Form Thêm/Sửa (null = chỉ đọc)

        -- Hành vi lưới
        Page_Size           INT            NOT NULL DEFAULT 20,
        Allow_Paging        BIT            NOT NULL DEFAULT 1,
        Virtual_Scroll      BIT            NOT NULL DEFAULT 0,
        Show_Filter_Row     BIT            NOT NULL DEFAULT 1,
        Show_Group_Panel    BIT            NOT NULL DEFAULT 0,
        Show_Search_Box     BIT            NOT NULL DEFAULT 1,
        Show_Column_Chooser BIT            NOT NULL DEFAULT 0,
        Selection_Mode      NVARCHAR(20)   NOT NULL DEFAULT 'none',   -- none | single | multiple
        Allow_Add           BIT            NOT NULL DEFAULT 1,
        Allow_Edit          BIT            NOT NULL DEFAULT 1,
        Allow_Delete        BIT            NOT NULL DEFAULT 1,

        -- Export / Print (cờ nhanh; nút chi tiết ở Ui_View_Action)
        Allow_Export        BIT            NOT NULL DEFAULT 1,
        Export_Formats      NVARCHAR(100)  NULL,                      -- 'xlsx,csv,pdf,docx'
        Export_File_Name_Key NVARCHAR(150) NULL,                      -- i18n tên file (null = View_Code)
        Allow_Print         BIT            NOT NULL DEFAULT 0,

        -- TreeList
        Key_Field           NVARCHAR(100)  NULL,                      -- cột khóa
        Parent_Field        NVARCHAR(100)  NULL,                      -- cột cha (hierarchy)
        Expand_Level        INT            NULL,                      -- mở sẵn tới cấp

        -- Master-detail / lọc mặc định
        Detail_View_Id      INT            NULL,                      -- view con (row detail)
        Default_Filter_Json NVARCHAR(MAX)  NULL,                      -- bộ lọc khởi tạo

        Options_Json        NVARCHAR(MAX)  NULL,                      -- thuộc tính phụ (thoát hiểm)
        Tenant_Id           INT            NULL,
        Version             INT            NOT NULL DEFAULT 1,
        Is_Active           BIT            NOT NULL DEFAULT 1,
        Created_At          DATETIME       NOT NULL DEFAULT GETDATE(),
        Updated_At          DATETIME       NOT NULL DEFAULT GETDATE(),
        Description         NVARCHAR(500)  NULL,

        CONSTRAINT PK_Ui_View PRIMARY KEY (View_Id),
        CONSTRAINT FK_Ui_View_Table    FOREIGN KEY (Table_Id)       REFERENCES dbo.Sys_Table(Table_Id),
        CONSTRAINT FK_Ui_View_EditForm FOREIGN KEY (Edit_Form_Id)   REFERENCES dbo.Ui_Form(Form_Id),
        CONSTRAINT FK_Ui_View_Detail   FOREIGN KEY (Detail_View_Id) REFERENCES dbo.Ui_View(View_Id),
        CONSTRAINT FK_Ui_View_Tenant   FOREIGN KEY (Tenant_Id)      REFERENCES dbo.Sys_Tenant(Tenant_Id)
    );
END;
GO

-- Unique View_Code: global (Tenant_Id NULL) tách khỏi per-tenant
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Ui_View_Code_Global' AND object_id = OBJECT_ID('dbo.Ui_View'))
    CREATE UNIQUE INDEX UQ_Ui_View_Code_Global ON dbo.Ui_View(View_Code) WHERE Tenant_Id IS NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Ui_View_Code_Tenant' AND object_id = OBJECT_ID('dbo.Ui_View'))
    CREATE UNIQUE INDEX UQ_Ui_View_Code_Tenant ON dbo.Ui_View(View_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ui_View_Table' AND object_id = OBJECT_ID('dbo.Ui_View'))
    CREATE INDEX IX_Ui_View_Table ON dbo.Ui_View(Table_Id, Is_Active);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Ui_View_Column — cột + thuộc tính (render + export + format)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_View_Column', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_View_Column
    (
        View_Column_Id    INT            IDENTITY(1,1) NOT NULL,
        View_Id           INT            NOT NULL,
        Column_Id         INT            NULL,                       -- map Sys_Column (null = unbound/computed)
        Field_Name        NVARCHAR(100)  NOT NULL,                   -- FieldName trên control
        Caption_Key       NVARCHAR(150)  NULL,                       -- i18n (null = fallback Label_Key field → Field_Name)
        Column_Kind       NVARCHAR(30)   NOT NULL DEFAULT 'Data',    -- Data | Selection | Command | TreeSpin

        -- Hiển thị
        Width             NVARCHAR(20)   NULL,
        Min_Width         INT            NULL,
        Text_Align        NVARCHAR(10)   NULL,                       -- left | center | right
        Display_Format    NVARCHAR(50)   NULL,                       -- n0 | dd/MM/yyyy ...
        Render_Mode       NVARCHAR(20)   NOT NULL DEFAULT 'Text',    -- Text|Html|Image|Link|Badge|Boolean|Template|Date|DateTime|Time
        Cell_Template_Key NVARCHAR(150)  NULL,                       -- template/i18n cho Html/Badge/Link
        Is_Visible        BIT            NOT NULL DEFAULT 1,
        Order_No          INT            NOT NULL DEFAULT 0,
        Fixed_Position    NVARCHAR(10)   NULL,                       -- none | left | right (frozen)

        -- Hành vi cột
        Allow_Sort        BIT            NOT NULL DEFAULT 1,
        Sort_Order        NVARCHAR(4)    NULL,                       -- asc | desc (sort mặc định)
        Sort_Index        INT            NULL,                       -- thứ tự khi sort nhiều cột
        Allow_Filter      BIT            NOT NULL DEFAULT 1,
        Allow_Group       BIT            NOT NULL DEFAULT 0,
        Group_Index       INT            NULL,                       -- nhóm sẵn theo cột
        Summary_Type      NVARCHAR(20)   NULL,                       -- count|sum|avg|min|max

        -- Export (giá trị thuần — KHÔNG xuất HTML)
        Allow_Export      BIT            NOT NULL DEFAULT 1,         -- HTML-only/command/selection → 0
        Export_Format     NVARCHAR(50)   NULL,                       -- format khi xuất (≠ Display_Format)
        Export_Caption_Key NVARCHAR(150) NULL,                       -- tiêu đề cột khi xuất (≠ Caption_Key)

        -- Conditional formatting
        Style_Rule_Json   NVARCHAR(MAX)  NULL,                       -- điều kiện (Grammar V1 AST) → style ô

        Props_Json        NVARCHAR(MAX)  NULL,
        Is_Active         BIT            NOT NULL DEFAULT 1,

        CONSTRAINT PK_Ui_View_Column PRIMARY KEY (View_Column_Id),
        CONSTRAINT FK_Ui_View_Column_View   FOREIGN KEY (View_Id)   REFERENCES dbo.Ui_View(View_Id),
        CONSTRAINT FK_Ui_View_Column_Column FOREIGN KEY (Column_Id) REFERENCES dbo.Sys_Column(Column_Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ui_View_Column_View' AND object_id = OBJECT_ID('dbo.Ui_View_Column'))
    CREATE INDEX IX_Ui_View_Column_View ON dbo.Ui_View_Column(View_Id, Is_Visible, Order_No);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Ui_View_Action — nút toolbar / row (CRUD mở rộng, in, xuất file, custom)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_View_Action', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_View_Action
    (
        Action_Id         INT            IDENTITY(1,1) NOT NULL,
        View_Id           INT            NOT NULL,
        Action_Code       NVARCHAR(50)   NOT NULL,                   -- add|edit|delete|export|print|refresh|column-chooser|<custom>
        Action_Type       NVARCHAR(30)   NOT NULL,                   -- BuiltIn|Export|Print|Navigate|Event|Api
        Scope             NVARCHAR(20)   NOT NULL DEFAULT 'Toolbar', -- Toolbar | Row | Both
        Label_Key         NVARCHAR(150)  NULL,                       -- i18n nhãn
        Tooltip_Key       NVARCHAR(150)  NULL,                       -- i18n tooltip
        Confirm_Key       NVARCHAR(150)  NULL,                       -- i18n xác nhận (vd Xóa)
        Icon              NVARCHAR(50)   NULL,                       -- unicode/tên icon (không phải text dịch)
        Export_Format     NVARCHAR(20)   NULL,                       -- xlsx|xls|csv|pdf|docx (Action_Type='Export')
        Export_Engine     NVARCHAR(20)   NULL,                       -- Grid (client) | Server (template)
        Target            NVARCHAR(MAX)  NULL,                       -- url|event_code|api endpoint|report template
        Require_Selection BIT            NOT NULL DEFAULT 0,
        Order_No          INT            NOT NULL DEFAULT 0,
        Props_Json        NVARCHAR(MAX)  NULL,
        Is_Active         BIT            NOT NULL DEFAULT 1,

        CONSTRAINT PK_Ui_View_Action PRIMARY KEY (Action_Id),
        CONSTRAINT FK_Ui_View_Action_View FOREIGN KEY (View_Id) REFERENCES dbo.Ui_View(View_Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ui_View_Action_View' AND object_id = OBJECT_ID('dbo.Ui_View_Action'))
    CREATE INDEX IX_Ui_View_Action_View ON dbo.Ui_View_Action(View_Id, Is_Active, Order_No);
GO

PRINT N'Migration 031 completed — Ui_View + Ui_View_Column + Ui_View_Action created.';
GO
