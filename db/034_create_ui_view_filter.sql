-- =============================================================================
-- File    : 034_create_ui_view_filter.sql
-- Purpose : Lưới nâng cao — panel lọc trái cho View nguồn SP/SQL (Hướng A, ADR-016).
--           1) Bảng Ui_View_Filter (per-View, MỖI THAM SỐ = 1 DÒNG).
--           2) ALTER Ui_View thêm cờ panel lọc (Filter_Panel_*).
--           3) Seed resource i18n chung: nút Tìm/Đặt lại + thông báo thiếu tham số.
-- Note    : Idempotent — guard IF OBJECT_ID / IF NOT EXISTS / COL_LENGTH. Chạy lại an toàn.
--           Xem spec 14_VIEW_CONFIG_SPEC §9 + ADR-016. Mọi text UI là resource KEY → Sys_Resource.
-- =============================================================================

USE [ICare247_Config];
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Ui_View_Filter — mỗi control lọc trên panel trái = 1 dòng = 1 tham số
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_View_Filter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_View_Filter
    (
        Filter_Id        INT            IDENTITY(1,1) NOT NULL,
        View_Id          INT            NOT NULL,                   -- thuộc View nào (per-View)
        Filter_Code      NVARCHAR(50)   NOT NULL,                   -- định danh kỹ thuật (unique/View)

        -- Control hiển thị trên panel
        Control_Type     NVARCHAR(30)   NOT NULL,                   -- Text|Number|Date|Combo|MultiSelect|Checkbox|Radio
        Label_Key        NVARCHAR(150)  NOT NULL,                   -- ★ i18n nhãn control
        Placeholder_Key  NVARCHAR(150)  NULL,                       -- ★ i18n placeholder
        Tooltip_Key      NVARCHAR(150)  NULL,                       -- ★ i18n tooltip

        -- Ánh xạ tham số (whitelist — chống injection). MỖI DÒNG = 1 tham số.
        Param_Name       NVARCHAR(100)  NOT NULL,                   -- @MaBN, @TuNgay, @DenNgay... khớp tham số SP/SQL
        Param_Type       NVARCHAR(30)   NOT NULL,                   -- string|int|decimal|date|bool
        Operator         NVARCHAR(20)   NOT NULL DEFAULT '=',       -- = | LIKE | >= | <= | IN

        -- Hành vi
        Default_Value    NVARCHAR(255)  NULL,                       -- giá trị/Item_Code mặc định (literal, KHÔNG i18n)
        Is_Required      BIT            NOT NULL DEFAULT 0,         -- bắt buộc nhập mới cho Tìm
        Is_Visible       BIT            NOT NULL DEFAULT 1,
        Order_No         INT            NOT NULL DEFAULT 0,         -- thứ tự trên panel
        Col_Span         TINYINT        NOT NULL DEFAULT 1,         -- bố cục panel (giống Ui_Field 4-col)

        -- Nguồn cho Combo/MultiSelect/Radio (tái dùng pattern Sys_Lookup / Ui_Field_Lookup)
        Lookup_Source    NVARCHAR(20)   NULL,                       -- NULL | static | dynamic
        Lookup_Code      NVARCHAR(50)   NULL,                       -- Sys_Lookup.Lookup_Code (khi static)
        Lookup_Sql       NVARCHAR(MAX)  NULL,                       -- SELECT value,display (khi dynamic)

        Props_Json       NVARCHAR(MAX)  NULL,                       -- thoát hiểm (preset ngày, min/max, group bố cục…)
        Is_Active        BIT            NOT NULL DEFAULT 1,

        CONSTRAINT PK_Ui_View_Filter PRIMARY KEY (Filter_Id),
        CONSTRAINT FK_Ui_View_Filter_View FOREIGN KEY (View_Id) REFERENCES dbo.Ui_View(View_Id),
        CONSTRAINT CHK_Ui_View_Filter_Operator
            CHECK (Operator IN ('=', 'LIKE', '>=', '<=', 'IN')),
        CONSTRAINT CHK_Ui_View_Filter_LookupSource
            CHECK (Lookup_Source IN ('static', 'dynamic') OR Lookup_Source IS NULL),
        CONSTRAINT CHK_Ui_View_Filter_LookupConsistency
            CHECK ((Lookup_Source = 'static'  AND Lookup_Code IS NOT NULL)
                OR (Lookup_Source = 'dynamic' AND Lookup_Sql  IS NOT NULL)
                OR (Lookup_Source IS NULL))
    );
END;
GO

-- Unique Filter_Code trong 1 View (chỉ tính dòng active)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Ui_View_Filter_Code' AND object_id = OBJECT_ID('dbo.Ui_View_Filter'))
    CREATE UNIQUE INDEX UQ_Ui_View_Filter_Code ON dbo.Ui_View_Filter(View_Id, Filter_Code) WHERE Is_Active = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ui_View_Filter_View' AND object_id = OBJECT_ID('dbo.Ui_View_Filter'))
    CREATE INDEX IX_Ui_View_Filter_View ON dbo.Ui_View_Filter(View_Id, Is_Visible, Order_No);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Ui_View — cờ panel lọc trái (chỉ kích hoạt khi Source_Type ∈ {Sp, Sql})
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.Ui_View', 'Filter_Panel_Enabled') IS NULL
    ALTER TABLE dbo.Ui_View ADD Filter_Panel_Enabled  BIT           NOT NULL DEFAULT 0;
GO
IF COL_LENGTH('dbo.Ui_View', 'Filter_Panel_Position') IS NULL
    ALTER TABLE dbo.Ui_View ADD Filter_Panel_Position NVARCHAR(10)  NOT NULL DEFAULT 'left';  -- left | top
GO
IF COL_LENGTH('dbo.Ui_View', 'Filter_Collapsible') IS NULL
    ALTER TABLE dbo.Ui_View ADD Filter_Collapsible    BIT           NOT NULL DEFAULT 1;
GO
IF COL_LENGTH('dbo.Ui_View', 'Auto_Search_On_Load') IS NULL
    ALTER TABLE dbo.Ui_View ADD Auto_Search_On_Load   BIT           NOT NULL DEFAULT 0;  -- mặc định CHỜ bấm Tìm
GO
IF COL_LENGTH('dbo.Ui_View', 'Search_Label_Key') IS NULL
    ALTER TABLE dbo.Ui_View ADD Search_Label_Key      NVARCHAR(150) NULL;                -- ★ i18n nút Tìm
GO
IF COL_LENGTH('dbo.Ui_View', 'Reset_Label_Key') IS NULL
    ALTER TABLE dbo.Ui_View ADD Reset_Label_Key       NVARCHAR(150) NULL;                -- ★ i18n nút Đặt lại
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Seed resource i18n chung cho panel lọc (nút + thông báo thiếu tham số)
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @seed TABLE (Resource_Key NVARCHAR(150), Lang_Code NVARCHAR(10), Resource_Value NVARCHAR(MAX));

INSERT INTO @seed (Resource_Key, Lang_Code, Resource_Value) VALUES
    (N'common.filter.search',    N'vi', N'Tìm kiếm'),
    (N'common.filter.search',    N'en', N'Search'),
    (N'common.filter.reset',     N'vi', N'Đặt lại'),
    (N'common.filter.reset',     N'en', N'Reset'),
    (N'common.filter.searching', N'vi', N'Đang tìm…'),
    (N'common.filter.searching', N'en', N'Searching…'),
    -- Thông báo validation: {0} = nhãn control (đã i18n). Engine format chuỗi này.
    (N'common.validation.required', N'vi', N'{0} là bắt buộc'),
    (N'common.validation.required', N'en', N'{0} is required');

INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value)
SELECT s.Resource_Key, s.Lang_Code, s.Resource_Value
FROM   @seed s
WHERE  NOT EXISTS (
        SELECT 1 FROM dbo.Sys_Resource r
        WHERE  r.Resource_Key = s.Resource_Key AND r.Lang_Code = s.Lang_Code);
GO

PRINT N'Migration 034 completed — Ui_View_Filter + cờ panel lọc trên Ui_View + seed resource.';
GO
