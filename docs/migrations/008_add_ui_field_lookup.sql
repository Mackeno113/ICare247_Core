-- ============================================================
-- Migration 008: Thêm bảng Ui_Field_Lookup
-- Mục đích  : Lưu cấu hình FK lookup cho field dynamic
--             (Ui_Field.Lookup_Source = 'dynamic').
--             Quan hệ 1-1 với Ui_Field (mỗi field chỉ có
--             tối đa 1 bản ghi lookup config).
-- Phụ thuộc : Migration 007 (Lookup_Source đã có trên Ui_Field)
-- Ngày      : 2026-03-25
-- ============================================================

-- ── 1. Tạo bảng Ui_Field_Lookup ──────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Ui_Field_Lookup')
BEGIN
    CREATE TABLE dbo.Ui_Field_Lookup (
        Lookup_Cfg_Id   int            IDENTITY(1,1) NOT NULL,
        -- FK 1-1 với Ui_Field (UNIQUE đảm bảo quan hệ 1-1)
        Field_Id        int            NOT NULL,

        -- Chế độ truy vấn dữ liệu nguồn
        -- 'table'      : SELECT từ bảng hoặc view (phổ biến nhất)
        -- 'tvf'        : Table-Valued Function — truyền tham số phức tạp
        -- 'custom_sql' : SQL tùy chỉnh do người cấu hình nhập
        Query_Mode      nvarchar(20)   NOT NULL CONSTRAINT DF_UiFL_QueryMode DEFAULT N'table',

        -- Tên bảng, view, TVF hoặc câu SQL tùy chỉnh tùy theo Query_Mode
        Source_Name     nvarchar(500)  NOT NULL,

        -- Cột lưu vào DB khi user chọn (VD: N'PhongBanID')
        Value_Column    nvarchar(100)  NOT NULL,

        -- Cột hiển thị trong dropdown (VD: N'TenPhongBan')
        Display_Column  nvarchar(100)  NOT NULL,

        -- Mệnh đề WHERE tùy chọn (parameterized, KHÔNG string interpolation)
        -- Hỗ trợ system params: @TenantId, @Today, @CurrentUser
        -- Hỗ trợ field params: @FieldCode (cascading — reload qua Event)
        -- VD: N'Is_Active = 1 AND Tenant_Id = @TenantId'
        Filter_Sql      nvarchar(max)  NULL,

        -- Mệnh đề ORDER BY (VD: N'Ten_PhongBan ASC')
        Order_By        nvarchar(200)  NULL,

        -- Cho phép tìm kiếm trong dropdown
        Search_Enabled  bit            NOT NULL CONSTRAINT DF_UiFL_Search DEFAULT 1,

        -- Danh sách cột hiển thị trong popup grid khi dropdown mở rộng
        -- JSON array: [{"column":"MaPhongBan","title":"Mã","width":80}, ...]
        -- NULL = chỉ hiển thị Display_Column, không dùng popup grid
        Popup_Columns_Json nvarchar(max) NULL,

        Updated_At      datetime       NOT NULL CONSTRAINT DF_UiFL_UpdatedAt DEFAULT GETDATE(),

        CONSTRAINT PK_Ui_Field_Lookup PRIMARY KEY (Lookup_Cfg_Id),

        -- 1-1 với Ui_Field: mỗi field chỉ có tối đa 1 lookup config
        CONSTRAINT UQ_Ui_Field_Lookup_Field UNIQUE (Field_Id),

        CONSTRAINT FK_Ui_Field_Lookup_Field FOREIGN KEY (Field_Id)
            REFERENCES dbo.Ui_Field (Field_Id),

        CONSTRAINT CHK_Ui_Field_Lookup_QueryMode
            CHECK (Query_Mode IN (N'table', N'tvf', N'custom_sql'))
    );
END
GO
