-- =============================================================================
-- File    : 008_add_ui_field_lookup.sql
-- Purpose : Tạo bảng Ui_Field_Lookup — cấu hình FK lookup cho field dynamic.
--           Quan hệ 1-1 với Ui_Field. Chỉ tồn tại khi Lookup_Source='dynamic'.
-- Note    : Các cột Reload_Trigger_Field, EditBox_Mode, Code_Field,
--           DropDown_Width, DropDown_Height sẽ được thêm trong migration 014.
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Ui_Field_Lookup', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Field_Lookup
    (
        Lookup_Cfg_Id       INT             IDENTITY(1,1) NOT NULL,
        Field_Id            INT             NOT NULL,
        Query_Mode          NVARCHAR(20)    NOT NULL DEFAULT 'table',
        Source_Name         NVARCHAR(500)   NOT NULL,
        Value_Column        NVARCHAR(100)   NOT NULL,
        Display_Column      NVARCHAR(100)   NOT NULL,
        Filter_Sql          NVARCHAR(MAX)   NULL,
        Order_By            NVARCHAR(200)   NULL,
        Search_Enabled      BIT             NOT NULL DEFAULT 1,
        Popup_Columns_Json  NVARCHAR(MAX)   NULL,
        Updated_At          DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Ui_Field_Lookup PRIMARY KEY (Lookup_Cfg_Id),
        -- 1-1 với Ui_Field
        CONSTRAINT UQ_Ui_Field_Lookup_Field UNIQUE (Field_Id),
        CONSTRAINT FK_Ui_Field_Lookup_Field FOREIGN KEY (Field_Id)
            REFERENCES dbo.Ui_Field (Field_Id),
        CONSTRAINT CHK_Ui_Field_Lookup_QueryMode
            CHECK (Query_Mode IN ('table', 'tvf', 'custom_sql'))
    );
END;
GO

PRINT N'Migration 008 completed — Ui_Field_Lookup created.';
GO
