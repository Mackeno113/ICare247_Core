-- ============================================================
-- Migration 005: Thêm bảng Ui_Tab cho multi-tab form layout
-- Mục đích  : Hỗ trợ form có nhiều tab (Ui_Form → Ui_Tab →
--             Ui_Section → Ui_Field).
--             Nếu form chỉ có 0 hoặc 1 tab → FormRunner bỏ
--             qua tab bar, render phẳng như cũ (backward compat).
-- Ngày      : 2026-03-25
-- Quy tắc   : Mọi string literal tiếng Việt dùng N'...' (Unicode)
-- ============================================================

-- ── 1. Tạo bảng Ui_Tab ───────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Ui_Tab')
BEGIN
    CREATE TABLE dbo.Ui_Tab (
        Tab_Id      int            IDENTITY(1,1) NOT NULL,
        Form_Id     int            NOT NULL,
        -- Mã định danh tab, unique trong 1 form
        Tab_Code    nvarchar(100)  NOT NULL,
        -- Resource key → Sys_Resource (NULL = không hiện tab label)
        Title_Key   nvarchar(150)  NULL,
        -- Icon tùy chọn (VD: N'person', N'work', N'star')
        Icon_Key    nvarchar(100)  NULL,
        -- Thứ tự hiển thị tab trên form
        Order_No    int            NOT NULL CONSTRAINT DF_Ui_Tab_OrderNo   DEFAULT 0,
        -- Tab nào mở đầu tiên khi form load
        Is_Default  bit            NOT NULL CONSTRAINT DF_Ui_Tab_IsDefault DEFAULT 0,
        Is_Active   bit            NOT NULL CONSTRAINT DF_Ui_Tab_IsActive  DEFAULT 1,

        CONSTRAINT PK_Ui_Tab PRIMARY KEY (Tab_Id),
        CONSTRAINT FK_Ui_Tab_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id)
    );

    -- Query tabs theo form: load tất cả tab active theo thứ tự
    CREATE INDEX IX_Ui_Tab_Form
        ON dbo.Ui_Tab (Form_Id, Is_Active, Order_No);

    -- Tab_Code phải unique trong cùng 1 form (chỉ kiểm tra active)
    CREATE UNIQUE INDEX UQ_Ui_Tab_Code
        ON dbo.Ui_Tab (Form_Id, Tab_Code)
        WHERE Is_Active = 1;

    -- Đảm bảo chỉ có đúng 1 tab Is_Default = 1 trong 1 form
    CREATE UNIQUE INDEX UQ_Ui_Tab_Default
        ON dbo.Ui_Tab (Form_Id)
        WHERE Is_Default = 1 AND Is_Active = 1;
END
GO
