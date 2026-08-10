-- =============================================================================
-- File    : 106_create_ui_form_detail.sql
-- Database: ICare247_Config  (Config DB — Form Engine)
-- Purpose : Năng lực nền tảng MASTER-DETAIL cho Form Engine (Spec 30 §2.1, mở rộng cho
--           "rail workspace"). Khai báo n lưới/timeline chi tiết gắn vào 1 form master.
--             • Bảng MỚI  dbo.Ui_Form_Detail — mỗi dòng = 1 pane (Grid | Timeline) trên rail.
--             • Cột  MỚI  dbo.Ui_Form.Detail_Layout — cách form hiển thị chi tiết (Inline | Rail).
-- Depends : db/000 (Ui_Form/Ui_Section), db/104-105 (form NS_ + 6 form con làm Detail_Form_Id).
-- Config  : Đây CHỈ là schema. Cấu hình rail (mục nào lên, thứ tự, nhãn, icon, save mode)
--           nhập qua ConfigStudio WPF (Pha 3) — KHÔNG seed giao diện bằng SQL (quy tắc dự án).
-- Sync    : Bảng có đủ cờ ConfigSync (Is_System/Is_Customized/Synced_At/Source_Ver) — khai báo
--           descriptor ở ConfigSyncTables.cs. Con của Ui_Form; re-link Detail_Form_Id/Section_Id.
-- Note    : Idempotent (guard OBJECT_ID / COL_LENGTH). ADR-022 cột audit tối thiểu như bảng Ui_.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- A. Ui_Form_Detail — khai báo pane chi tiết (Grid | Timeline)
-- ═══════════════════════════════════════════════════════════════════════════════
IF OBJECT_ID('dbo.Ui_Form_Detail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Form_Detail
    (
        Detail_Id         INT            IDENTITY(1,1) NOT NULL,
        Form_Id           INT            NOT NULL,       -- form master (Ui_Form)
        Detail_Code       NVARCHAR(50)   NOT NULL,       -- định danh pane trong form (vd 'HocVan')

        -- Kiểu & nguồn nội dung pane -------------------------------------------------
        Pane_Type         NVARCHAR(20)   NOT NULL CONSTRAINT DF_UFD_PaneType DEFAULT 'Grid',
                                                          -- 'Grid' = lưới CRUD bảng con · 'Timeline' = dòng thời gian (đọc view/query)
        Detail_Form_Id    INT            NULL,           -- Ui_Form CON định nghĩa cột lưới (BẮT BUỘC khi Pane_Type='Grid'; NULL cho Timeline)
        Parent_Key_Column NVARCHAR(100)  NULL,           -- cột FK bảng con trỏ về master (vd 'NhanVien_Id'); NULL cho Timeline dùng Options_Json
        Save_Mode         NVARCHAR(20)   NOT NULL CONSTRAINT DF_UFD_SaveMode DEFAULT 'WithMaster',
                                                          -- 'Immediate' = mỗi dòng lưu ngay (hồ sơ) · 'WithMaster' = lưu gộp 1 transaction (chứng từ, Spec 30)

        -- Trình bày trên rail --------------------------------------------------------
        Section_Id        INT            NULL,           -- (Inline) section đặt lưới; NULL = pane riêng
        Title_Key         NVARCHAR(150)  NULL,           -- i18n nhãn pane / tiêu đề lưới
        Icon              NVARCHAR(50)    NULL,           -- icon mục rail
        Group_Key         NVARCHAR(50)    NULL,           -- nhóm rail (vd 'RELATED' / 'HISTORY')

        -- Hành vi lưới ---------------------------------------------------------------
        Edit_Mode         NVARCHAR(20)   NOT NULL CONSTRAINT DF_UFD_EditMode DEFAULT 'EntryPanel',
                                                          -- EntryPanel | CellInline | RowPopup (Spec 30 §3.1)
        Allow_Add         BIT            NOT NULL CONSTRAINT DF_UFD_Add    DEFAULT 1,
        Allow_Delete      BIT            NOT NULL CONSTRAINT DF_UFD_Del    DEFAULT 1,
        Allow_Reorder     BIT            NOT NULL CONSTRAINT DF_UFD_Reord  DEFAULT 0,
        Min_Rows          INT            NOT NULL CONSTRAINT DF_UFD_MinRow DEFAULT 0,
        Summary_Json      NVARCHAR(MAX)  NULL,           -- footer tổng: [{"field":"ThanhTien","func":"SUM"}]
        Options_Json      NVARCHAR(MAX)  NULL,           -- cấu hình phụ (Timeline: map cột date/title/body/tag → view)

        -- Thứ tự & trạng thái --------------------------------------------------------
        Order_No          INT            NOT NULL CONSTRAINT DF_UFD_Order  DEFAULT 0,
        Version           INT            NOT NULL CONSTRAINT DF_UFD_Ver    DEFAULT 1,
        Is_Active         BIT            NOT NULL CONSTRAINT DF_UFD_Active DEFAULT 1,

        -- Cờ ConfigSync (master→tenant) ---------------------------------------------
        Is_System         BIT            NOT NULL CONSTRAINT DF_UFD_Sys    DEFAULT 0,
        Is_Customized     BIT            NOT NULL CONSTRAINT DF_UFD_Cust   DEFAULT 0,
        Synced_At         DATETIME       NULL,
        Source_Ver        INT            NULL,

        -- Audit ----------------------------------------------------------------------
        Created_At        DATETIME       NOT NULL CONSTRAINT DF_UFD_Created DEFAULT GETDATE(),
        Updated_At        DATETIME       NOT NULL CONSTRAINT DF_UFD_Updated DEFAULT GETDATE(),

        CONSTRAINT PK_Ui_Form_Detail PRIMARY KEY (Detail_Id),
        CONSTRAINT UQ_Ui_Form_Detail UNIQUE (Form_Id, Detail_Code),
        CONSTRAINT FK_UFD_Form       FOREIGN KEY (Form_Id)        REFERENCES dbo.Ui_Form (Form_Id),
        CONSTRAINT FK_UFD_DetailForm FOREIGN KEY (Detail_Form_Id) REFERENCES dbo.Ui_Form (Form_Id),
        CONSTRAINT FK_UFD_Section    FOREIGN KEY (Section_Id)     REFERENCES dbo.Ui_Section (Section_Id),
        CONSTRAINT CK_UFD_PaneType   CHECK (Pane_Type IN (N'Grid', N'Timeline')),
        CONSTRAINT CK_UFD_SaveMode   CHECK (Save_Mode IN (N'Immediate', N'WithMaster'))
    );

    -- Nạp rail theo form master, đúng thứ tự, chỉ pane còn hiệu lực.
    CREATE INDEX IX_Ui_Form_Detail_Form
        ON dbo.Ui_Form_Detail (Form_Id, Is_Active, Order_No);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- B. Ui_Form.Detail_Layout — cách form master hiển thị các pane chi tiết
-- ═══════════════════════════════════════════════════════════════════════════════
--   'Inline' (mặc định, tương thích ngược) = lưới chi tiết chèn trong thân form (Spec 30 chứng từ).
--   'Rail'   = workspace có rail điều hướng con: form vô hướng + mục rail cho từng pane (hồ sơ NS_).
IF COL_LENGTH('dbo.Ui_Form', 'Detail_Layout') IS NULL
BEGIN
    ALTER TABLE dbo.Ui_Form
        ADD Detail_Layout NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Ui_Form_DetailLayout DEFAULT 'Inline';
END;
GO
