-- =============================================================================
-- File    : 083_create_ui_lookup_template.sql
-- Database: ICare247_Config  (Config DB — master canonical + mỗi tenant 1 Config DB)
-- Purpose : PICKER-P4 (spec 31 §5) — Lookup Template cho màn engine no-code:
--           1) Bảng Ui_Lookup_Template — mẫu lookup đóng gói sẵn (Query_Mode/Source/cột/Filter),
--              khai tham số canonical qua Canonical_Params (JSON).
--           2) ALTER Ui_Field_Lookup thêm Template_Code (chọn mẫu; NULL = tự cấu hình như cũ)
--              + Param_Map (JSON map tham số canonical ← Field_Code trên form / @token / hằng số).
--           3) Seed 3 mẫu nền: TPL_CONG_TY (theo quyền — fn_CongTyTheoQuyen db/084, token
--              @NguoiDungID tự resolve) · TPL_TINH_THANH · TPL_PHUONG_XA (tham số TinhId).
--              TPL_NHAN_VIEN_TAI_THOI_DIEM để đợt NS_ (bảng NS_NhanVien chưa có).
-- Note    : Idempotent (OBJECT_ID / COL_LENGTH / NOT EXISTS guard). Mẫu mang 4 cờ sync CFGSYNC-1
--           (db/050) để đồng bộ master→tenant qua descriptor ConfigSyncTables.
--           Filter_Sql/custom_sql của mẫu KHÔNG được chứa chuỗi con DDL/DML (kể cả "IsDeleted"
--           chứa "DELETE") — vì vậy nguồn trỏ VIEW vw_* (db/051/052) hoặc inline TVF (db/084).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) Ui_Lookup_Template — mỗi dòng = 1 mẫu lookup dùng lại được ở nhiều field/form
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Lookup_Template', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Lookup_Template
    (
        Template_Id        INT            IDENTITY(1,1) NOT NULL,
        Template_Code      NVARCHAR(100)  NOT NULL,                 -- khóa nghiệp vụ (sync theo mã)
        Ten                NVARCHAR(200)  NOT NULL,                 -- tên hiển thị trong ConfigStudio
        Mo_Ta              NVARCHAR(500)  NULL,

        -- Định nghĩa truy vấn (cùng ngữ nghĩa các cột Ui_Field_Lookup — template thắng khi field chọn mẫu)
        Query_Mode         NVARCHAR(20)   NOT NULL DEFAULT 'table', -- table | tvf | custom_sql
        Source_Name        NVARCHAR(MAX)  NOT NULL,                 -- bảng/view, hoặc câu SQL (custom_sql)
        Value_Column       NVARCHAR(100)  NOT NULL,
        Display_Column     NVARCHAR(300)  NOT NULL,
        Code_Field         NVARCHAR(100)  NULL,
        Filter_Sql         NVARCHAR(MAX)  NULL,                     -- parameterized, dùng @CanonicalParam / @token
        Order_By           NVARCHAR(200)  NULL,
        Popup_Columns_Json NVARCHAR(MAX)  NULL,
        Parent_Column      NVARCHAR(100)  NULL,                     -- cho TreeLookupBox

        -- Tham số canonical NGƯỜI CẤU HÌNH phải map ở từng field (JSON array).
        -- Schema: [{"name":"TinhId","type":"bigint","required":true,"moTa":"Field Tỉnh/Thành trên form"}]
        -- Token đăng ký ở Sys_Context_Param (@NguoiDungID, @CongTyID_Active…) engine TỰ resolve — KHÔNG khai ở đây.
        Canonical_Params   NVARCHAR(MAX)  NULL,

        Is_Active          BIT            NOT NULL DEFAULT 1,

        -- 4 cờ sync CFGSYNC-1 (db/050) — đồng bộ master→tenant
        Is_System          BIT            NOT NULL DEFAULT 0,
        Is_Customized      BIT            NOT NULL DEFAULT 0,
        Synced_At          DATETIME       NULL,
        Source_Ver         INT            NULL,

        CONSTRAINT PK_Ui_Lookup_Template PRIMARY KEY (Template_Id),
        CONSTRAINT CHK_Ui_Lookup_Template_QueryMode CHECK (Query_Mode IN ('table', 'tvf', 'custom_sql'))
    );
    CREATE UNIQUE INDEX UQ_Ui_Lookup_Template_Code ON dbo.Ui_Lookup_Template (Template_Code);
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) Ui_Field_Lookup — thêm 2 cột chọn mẫu + map tham số
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.Ui_Field_Lookup', 'Template_Code') IS NULL
    ALTER TABLE dbo.Ui_Field_Lookup ADD Template_Code NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Ui_Field_Lookup', 'Param_Map') IS NULL
    -- JSON map tham số canonical ← nguồn giá trị. VD: {"TinhId": "TinhThanhPho_Id"}
    -- Giá trị: "FieldCode" (field trên form) | "@TokenName" (Sys_Context_Param) | hằng số (number/bool).
    ALTER TABLE dbo.Ui_Field_Lookup ADD Param_Map NVARCHAR(MAX) NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3) Seed 3 mẫu nền (idempotent theo Template_Code; Is_System=1 — bản gốc master)
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Lookup_Template WHERE Template_Code = N'TPL_CONG_TY')
    INSERT INTO dbo.Ui_Lookup_Template
        (Template_Code, Ten, Mo_Ta, Query_Mode, Source_Name, Value_Column, Display_Column,
         Code_Field, Filter_Sql, Order_By, Canonical_Params, Is_Active, Is_System)
    VALUES
        (N'TPL_CONG_TY', N'Công ty (theo quyền)',
         N'Cây công ty user được truy cập: gán riêng (HT_NguoiDung_CongTy) hợp theo vai trò (HT_VaiTro_CongTy). Token @NguoiDungID engine tự resolve — không cần map. Cần Data DB đã chạy db/082 + db/084.',
         N'custom_sql',
         N'SELECT Id, Ma, Ten FROM dbo.fn_CongTyTheoQuyen(@NguoiDungID) ORDER BY Ten',
         N'Id', N'Ten', N'Ma', NULL, NULL, NULL, 1, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Lookup_Template WHERE Template_Code = N'TPL_TINH_THANH')
    INSERT INTO dbo.Ui_Lookup_Template
        (Template_Code, Ten, Mo_Ta, Query_Mode, Source_Name, Value_Column, Display_Column,
         Code_Field, Filter_Sql, Order_By, Canonical_Params, Is_Active, Is_System)
    VALUES
        (N'TPL_TINH_THANH', N'Tỉnh/Thành phố',
         N'Danh mục Tỉnh/Thành phố active (view vw_DM_TinhThanhPho đã lọc xóa mềm — db/052).',
         N'table', N'vw_DM_TinhThanhPho', N'Id', N'Ten', N'Ma', NULL, N'Ten', NULL, 1, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Lookup_Template WHERE Template_Code = N'TPL_PHUONG_XA')
    INSERT INTO dbo.Ui_Lookup_Template
        (Template_Code, Ten, Mo_Ta, Query_Mode, Source_Name, Value_Column, Display_Column,
         Code_Field, Filter_Sql, Order_By, Canonical_Params, Is_Active, Is_System)
    VALUES
        (N'TPL_PHUONG_XA', N'Xã/Phường (theo Tỉnh)',
         N'Xã/Phường thuộc tỉnh được chọn (view vw_DM_PhuongXa — db/052). Map tham số TinhId vào field Tỉnh/Thành trên form; field đó tự thành reload-trigger.',
         N'table', N'vw_DM_PhuongXa', N'Id', N'Ten', N'Ma',
         N'TinhThanhPho_Id = @TinhId', N'Ten',
         N'[{"name":"TinhId","type":"bigint","required":true,"moTa":"Field Tỉnh/Thành trên form"}]', 1, 1);
GO

PRINT N'Migration 083 completed — Ui_Lookup_Template + Ui_Field_Lookup.Template_Code/Param_Map + seed 3 mẫu.';
GO
