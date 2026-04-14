-- =============================================================================
-- File    : 005_add_ui_tab.sql
-- Purpose : Tạo bảng Ui_Tab — hỗ trợ multi-tab form.
--           Nếu form có 0 hoặc 1 tab, FormRunner render phẳng (backward compat).
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Ui_Tab', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Tab
    (
        Tab_Id      INT             IDENTITY(1,1) NOT NULL,
        Form_Id     INT             NOT NULL,
        Tab_Code    NVARCHAR(100)   NOT NULL,
        Title_Key   NVARCHAR(150)   NULL,
        Icon_Key    NVARCHAR(100)   NULL,
        Order_No    INT             NOT NULL DEFAULT 0,
        Is_Default  BIT             NOT NULL DEFAULT 0,
        Is_Active   BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Ui_Tab PRIMARY KEY (Tab_Id),
        CONSTRAINT FK_Ui_Tab_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id)
    );

    -- Query theo form
    CREATE INDEX IX_Ui_Tab_Form
        ON dbo.Ui_Tab (Form_Id, Is_Active, Order_No);

    -- Unique Tab_Code trong form (active only)
    CREATE UNIQUE INDEX UQ_Ui_Tab_Code
        ON dbo.Ui_Tab (Form_Id, Tab_Code) WHERE Is_Active = 1;

    -- Tối đa 1 tab Is_Default per form (active only)
    CREATE UNIQUE INDEX UQ_Ui_Tab_Default
        ON dbo.Ui_Tab (Form_Id) WHERE Is_Default = 1 AND Is_Active = 1;
END;
GO

PRINT N'Migration 005 completed — Ui_Tab created.';
GO
