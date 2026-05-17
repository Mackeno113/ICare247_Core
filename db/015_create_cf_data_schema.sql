-- =============================================================================
-- File    : 015_create_cf_data_schema.sql
-- Purpose : Tạo schema cà phê (Cf_*) cho database ICare247_Data.
--           Phục vụ phiên bản web quản lý đại lý cà phê theo Modular Monolith,
--           tách 7 bounded contexts: Catalog, Purchasing, Processing,
--           PriceClosing, Lending, Sales, Inventory, Accounting.
-- Source  : Phân tích module CategoryNgocChuong (desktop) — chuẩn hoá lại.
-- Note    : Idempotent — IF NOT EXISTS trước mỗi DB/bảng/index.
--           Tenant_Id, Created_By, Updated_By KHÔNG FK sang Sys_Tenant/Sys_User
--           vì 2 bảng đó nằm ở DB ICare247_Config (cross-DB FK không khả thi).
--           Ràng buộc enforce ở tầng Application.
-- =============================================================================

-- ── 0. Tạo database nếu chưa tồn tại ───────────────────────────────────────
IF DB_ID(N'ICare247_Data') IS NULL
BEGIN
    CREATE DATABASE [ICare247_Data];
END;
GO

USE [ICare247_Data];
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: CATALOG — Danh mục dùng chung (Cf_Branch, Cf_Partner, Cf_Item, Cf_Stock,
-- Cf_Account, Cf_Employee, Cf_Humidity, Cf_DryingFacility, lookup groups...)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_Branch ──────────────────────────────────────────────────────────────
-- Chi nhánh đại lý. Một tenant có nhiều branch (vd: CN1, CN2).
IF OBJECT_ID(N'dbo.Cf_Branch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Branch
    (
        Branch_Id           INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Code         NVARCHAR(50)    NOT NULL,
        Branch_Name         NVARCHAR(200)   NOT NULL,
        Branch_Address      NVARCHAR(500)   NULL,
        Phone_Number        NVARCHAR(50)    NULL,
        Tax_Code            NVARCHAR(50)    NULL,
        Director_Name       NVARCHAR(200)   NULL,
        Chief_Accountant    NVARCHAR(200)   NULL,
        Bank_Name           NVARCHAR(200)   NULL,
        Bank_Branch         NVARCHAR(200)   NULL,
        Bank_Account_No     NVARCHAR(50)    NULL,
        Bank_Account_Owner  NVARCHAR(200)   NULL,
        Email               NVARCHAR(100)   NULL,
        Website             NVARCHAR(150)   NULL,
        Is_Main             BIT             NOT NULL DEFAULT 0,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Branch PRIMARY KEY (Branch_Id),
        CONSTRAINT UQ_Cf_Branch_Code UNIQUE (Tenant_Id, Branch_Code)
    );
    CREATE INDEX IX_Cf_Branch_Tenant ON dbo.Cf_Branch (Tenant_Id) WHERE Is_Active = 1;
END;
GO

-- ── Cf_Employee ────────────────────────────────────────────────────────────
-- Nhân viên đại lý. Tách khỏi Sys_User vì có thông tin nghiệp vụ riêng.
IF OBJECT_ID(N'dbo.Cf_Employee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Employee
    (
        Employee_Id         INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Employee_Code       NVARCHAR(50)    NOT NULL,
        Employee_Name       NVARCHAR(200)   NOT NULL,
        Birth_Day           DATE            NULL,
        Email               NVARCHAR(100)   NULL,
        Phone_Number        NVARCHAR(50)    NULL,
        Group_Code          NVARCHAR(50)    NULL,
        User_Id             INT             NULL,   -- map sang Sys_User (nullable)
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Employee PRIMARY KEY (Employee_Id),
        CONSTRAINT UQ_Cf_Employee_Code UNIQUE (Tenant_Id, Employee_Code),
        CONSTRAINT FK_Cf_Employee_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id)
    );
    CREATE INDEX IX_Cf_Employee_Branch ON dbo.Cf_Employee (Tenant_Id, Branch_Id) WHERE Is_Active = 1;
END;
GO

-- ── Cf_PartnerGroup ────────────────────────────────────────────────────────
-- Nhóm đối tác (Khách hàng VIP, NCC cấp 1, vận chuyển...).
IF OBJECT_ID(N'dbo.Cf_PartnerGroup', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PartnerGroup
    (
        Group_Id            INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Group_Code          NVARCHAR(50)    NOT NULL,
        Group_Name          NVARCHAR(200)   NOT NULL,
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PartnerGroup PRIMARY KEY (Group_Id),
        CONSTRAINT UQ_Cf_PartnerGroup_Code UNIQUE (Tenant_Id, Group_Code)
    );
END;
GO

-- ── Cf_Partner ─────────────────────────────────────────────────────────────
-- Đối tác: khách hàng / nhà cung cấp / vận chuyển. Phân biệt qua Partner_Type.
IF OBJECT_ID(N'dbo.Cf_Partner', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Partner
    (
        Partner_Id          INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NULL,
        Group_Id            INT             NULL,
        Partner_Code        NVARCHAR(50)    NOT NULL,
        Partner_Name        NVARCHAR(300)   NOT NULL,
        Second_Name         NVARCHAR(300)   NULL,
        Partner_Type        NVARCHAR(20)    NOT NULL,   -- 'Customer'/'Supplier'/'Transporter'/'Both'
        Address             NVARCHAR(500)   NULL,
        Delivery_Address    NVARCHAR(500)   NULL,
        Contact_Person      NVARCHAR(200)   NULL,
        Tax_Code            NVARCHAR(50)    NULL,
        Phone_Number        NVARCHAR(50)    NULL,
        Email               NVARCHAR(100)   NULL,
        Payment_Term_Days   INT             NULL,
        Form_Of_Payment     NVARCHAR(20)    NULL,       -- 'Cash'/'Bank'/'Mixed'
        Is_Can_Fix_Price    BIT             NOT NULL DEFAULT 0,
        Sales_Employee_Id   INT             NULL,
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Partner PRIMARY KEY (Partner_Id),
        CONSTRAINT UQ_Cf_Partner_Code UNIQUE (Tenant_Id, Partner_Code),
        CONSTRAINT FK_Cf_Partner_Group FOREIGN KEY (Group_Id) REFERENCES dbo.Cf_PartnerGroup (Group_Id),
        CONSTRAINT FK_Cf_Partner_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_Partner_SalesEmp FOREIGN KEY (Sales_Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_Partner_Type CHECK (Partner_Type IN (N'Customer', N'Supplier', N'Transporter', N'Both'))
    );
    CREATE INDEX IX_Cf_Partner_Tenant_Name ON dbo.Cf_Partner (Tenant_Id, Partner_Name) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_Partner_Type ON dbo.Cf_Partner (Tenant_Id, Partner_Type) WHERE Is_Active = 1;
END;
GO

-- ── Cf_ItemGroup ───────────────────────────────────────────────────────────
-- Nhóm hàng (3 tầng: Ngành → Loại → Mặt hàng).
IF OBJECT_ID(N'dbo.Cf_ItemGroup', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_ItemGroup
    (
        Group_Id            INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Parent_Group_Id     INT             NULL,
        Group_Code          NVARCHAR(50)    NOT NULL,
        Group_Name          NVARCHAR(200)   NOT NULL,
        Group_Level         TINYINT         NOT NULL DEFAULT 1,  -- 1=Industry, 2=Type, 3=Lever
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_ItemGroup PRIMARY KEY (Group_Id),
        CONSTRAINT UQ_Cf_ItemGroup_Code UNIQUE (Tenant_Id, Group_Code),
        CONSTRAINT FK_Cf_ItemGroup_Parent FOREIGN KEY (Parent_Group_Id) REFERENCES dbo.Cf_ItemGroup (Group_Id)
    );
END;
GO

-- ── Cf_Item ────────────────────────────────────────────────────────────────
-- Mặt hàng: cf tươi, cf khô, cf nhân, cf non, dụng cụ...
IF OBJECT_ID(N'dbo.Cf_Item', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Item
    (
        Item_Id             INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Group_Id            INT             NULL,
        Item_Code           NVARCHAR(50)    NOT NULL,
        Item_Name           NVARCHAR(300)   NOT NULL,
        Other_Item_Name     NVARCHAR(300)   NULL,
        Unit                NVARCHAR(20)    NOT NULL DEFAULT N'kg',
        Barcode             NVARCHAR(50)    NULL,
        Default_Cost_Price  DECIMAL(18,2)   NULL,
        Default_Sale_Price  DECIMAL(18,2)   NULL,
        Default_Account_Id  INT             NULL,    -- TK kho mặc định (1561, 151...)
        Inventory_Min       DECIMAL(18,4)   NULL,
        Position_In_Stock   NVARCHAR(100)   NULL,
        Color               NVARCHAR(50)    NULL,
        Size                NVARCHAR(100)   NULL,
        Weight              DECIMAL(18,4)   NULL,
        Lock_Buy            BIT             NOT NULL DEFAULT 0,
        Lock_Sale           BIT             NOT NULL DEFAULT 0,
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Item PRIMARY KEY (Item_Id),
        CONSTRAINT UQ_Cf_Item_Code UNIQUE (Tenant_Id, Item_Code),
        CONSTRAINT FK_Cf_Item_Group FOREIGN KEY (Group_Id) REFERENCES dbo.Cf_ItemGroup (Group_Id)
    );
    CREATE INDEX IX_Cf_Item_Tenant_Name ON dbo.Cf_Item (Tenant_Id, Item_Name) WHERE Is_Active = 1;
END;
GO

-- ── Cf_Stock ───────────────────────────────────────────────────────────────
-- Kho hàng (kho cf tươi, kho cf nhân, kho tạm...).
IF OBJECT_ID(N'dbo.Cf_Stock', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Stock
    (
        Stock_Id            INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Stock_Code          NVARCHAR(50)    NOT NULL,
        Stock_Name          NVARCHAR(200)   NOT NULL,
        Stock_Address       NVARCHAR(500)   NULL,
        Is_Main             BIT             NOT NULL DEFAULT 0,
        Sum_Quantity        BIT             NOT NULL DEFAULT 1,   -- tham gia tính tổng tồn
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Stock PRIMARY KEY (Stock_Id),
        CONSTRAINT UQ_Cf_Stock_Code UNIQUE (Tenant_Id, Stock_Code),
        CONSTRAINT FK_Cf_Stock_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id)
    );
END;
GO

-- ── Cf_Account ─────────────────────────────────────────────────────────────
-- Hệ thống tài khoản kế toán (chart of accounts).
-- 1561=Hàng nhập kho, 331=Phải trả NCC, 002=Ký gửi, 131=Phải thu KH, 511=Doanh thu, 1283=Phải thu vay, 151=CF non.
IF OBJECT_ID(N'dbo.Cf_Account', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Account
    (
        Account_Id          INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Parent_Account_Id   INT             NULL,
        Account_Code        NVARCHAR(25)    NOT NULL,   -- '1561', '331', '002', '131'...
        Account_Name        NVARCHAR(300)   NOT NULL,
        Account_Class       NVARCHAR(20)    NOT NULL,   -- 'Asset','Liability','Equity','Revenue','Expense','OffBalance'
        Normal_Balance      CHAR(1)         NOT NULL,   -- 'D'=Debit, 'C'=Credit
        Is_Detail           BIT             NOT NULL DEFAULT 1,   -- tài khoản chi tiết (post được)
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Account PRIMARY KEY (Account_Id),
        CONSTRAINT UQ_Cf_Account_Code UNIQUE (Tenant_Id, Account_Code),
        CONSTRAINT FK_Cf_Account_Parent FOREIGN KEY (Parent_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_Account_Class CHECK (Account_Class IN
            (N'Asset', N'Liability', N'Equity', N'Revenue', N'Expense', N'OffBalance')),
        CONSTRAINT CK_Cf_Account_Balance CHECK (Normal_Balance IN ('D','C'))
    );
END;
GO

-- ── Cf_Humidity ────────────────────────────────────────────────────────────
-- Bảng quy đổi ẩm độ → tỉ lệ giảm trừ khối lượng.
IF OBJECT_ID(N'dbo.Cf_Humidity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Humidity
    (
        Humidity_Id             INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id               INT             NOT NULL,
        Comparative_Value       DECIMAL(8,4)    NOT NULL,    -- % ẩm độ đầu vào
        Standard_Value          DECIMAL(8,4)    NOT NULL,    -- hệ số quy đổi
        Note                    NVARCHAR(200)   NULL,

        Is_Active               BIT             NOT NULL DEFAULT 1,
        Created_At              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By              INT             NOT NULL,
        Updated_At              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By              INT             NULL,
        Row_Version             ROWVERSION,

        CONSTRAINT PK_Cf_Humidity PRIMARY KEY (Humidity_Id),
        CONSTRAINT UQ_Cf_Humidity_Value UNIQUE (Tenant_Id, Comparative_Value)
    );
END;
GO

-- ── Cf_DryingFacility ──────────────────────────────────────────────────────
-- Lò sấy / sân phơi (gộp từ GeneralCatalog GeneralType='DryingYard'/'DryingOven').
IF OBJECT_ID(N'dbo.Cf_DryingFacility', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_DryingFacility
    (
        Facility_Id         INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NULL,
        Facility_Code       NVARCHAR(50)    NOT NULL,
        Facility_Name       NVARCHAR(200)   NOT NULL,
        Facility_Type       NVARCHAR(20)    NOT NULL,   -- 'Oven' (sấy) | 'Yard' (phơi)
        Address             NVARCHAR(500)   NULL,
        Capacity_Kg         DECIMAL(18,2)   NULL,
        Owner_Name          NVARCHAR(200)   NULL,
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_DryingFacility PRIMARY KEY (Facility_Id),
        CONSTRAINT UQ_Cf_DryingFacility_Code UNIQUE (Tenant_Id, Facility_Code),
        CONSTRAINT FK_Cf_DryingFacility_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT CK_Cf_DryingFacility_Type CHECK (Facility_Type IN (N'Oven', N'Yard'))
    );
END;
GO

-- Bổ sung FK Cf_Item.Default_Account_Id (sau khi Cf_Account đã tạo)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Cf_Item_Account')
BEGIN
    ALTER TABLE dbo.Cf_Item
        ADD CONSTRAINT FK_Cf_Item_Account FOREIGN KEY (Default_Account_Id)
            REFERENCES dbo.Cf_Account (Account_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: PURCHASING — Mua cà phê (tươi, khô, non, nhân/xát) + thanh toán mua.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_PurchaseOrder ───────────────────────────────────────────────────────
-- Phiếu mua. Phân biệt 4 loại qua Purchase_Type.
IF OBJECT_ID(N'dbo.Cf_PurchaseOrder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PurchaseOrder
    (
        Order_Id            BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Order_Code          NVARCHAR(50)    NOT NULL,
        Order_Date          DATE            NOT NULL,
        Purchase_Type       NVARCHAR(20)    NOT NULL,    -- 'Fresh','Dry','Young','Bean'
        Partner_Id          INT             NOT NULL,
        Import_Stock_Id     INT             NULL,
        Employee_Id         INT             NOT NULL,
        Transporter_Name    NVARCHAR(200)   NULL,
        Contact_Person      NVARCHAR(200)   NULL,
        Total_Quantity      DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Total_Amount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Paid_Amount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',  -- Draft/Confirmed/Closed/Cancelled
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PurchaseOrder PRIMARY KEY (Order_Id),
        CONSTRAINT UQ_Cf_PurchaseOrder_Code UNIQUE (Tenant_Id, Order_Code),
        CONSTRAINT FK_Cf_PurchaseOrder_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_PurchaseOrder_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_PurchaseOrder_Stock FOREIGN KEY (Import_Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_PurchaseOrder_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_PurchaseOrder_Type CHECK (Purchase_Type IN (N'Fresh', N'Dry', N'Young', N'Bean')),
        CONSTRAINT CK_Cf_PurchaseOrder_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_PurchaseOrder_Tenant_Date ON dbo.Cf_PurchaseOrder (Tenant_Id, Order_Date) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_PurchaseOrder_Partner ON dbo.Cf_PurchaseOrder (Tenant_Id, Partner_Id) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_PurchaseOrder_Type_Date ON dbo.Cf_PurchaseOrder (Tenant_Id, Purchase_Type, Order_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_PurchaseOrderLine ───────────────────────────────────────────────────
-- Chi tiết phiếu mua. Mỗi line = 1 mặt hàng.
-- Quantity / Standard_Quantity tách bạch:
--   - Order_Quantity: số lượng thực tế giao (vd 1000kg cf tươi)
--   - Standard_Quantity: số lượng quy chuẩn sau khi trừ ẩm độ + bao bì
--   - Deposit_Quantity: số kg nhân khách muốn ký gửi (chỉ cho Purchase_Type='Fresh')
IF OBJECT_ID(N'dbo.Cf_PurchaseOrderLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PurchaseOrderLine
    (
        Line_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Order_Id            BIGINT          NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Row_No              INT             NOT NULL DEFAULT 1,
        Item_Id             INT             NOT NULL,
        Unit                NVARCHAR(20)    NOT NULL DEFAULT N'kg',

        Order_Quantity      DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Standard_Quantity   DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Deposit_Quantity    DECIMAL(18,4)   NOT NULL DEFAULT 0,

        Bag_Count           INT             NULL,            -- số bao
        Bag_Tare_Kg         DECIMAL(18,4)   NULL,            -- kg bao bì trừ
        Net_Deduction       DECIMAL(18,4)   NOT NULL DEFAULT 0,  -- tổng kg trừ
        Humidity_Percent    DECIMAL(8,4)    NULL,            -- ẩm độ %
        Impurity_Percent    DECIMAL(8,4)    NULL,            -- độ tạp %
        Conversion_Rate     DECIMAL(10,6)   NULL,            -- hệ số quy nhân (cf tươi)

        Unit_Price          DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Amount              DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Vat_Rate            DECIMAL(6,4)    NULL,
        Vat_Amount          DECIMAL(18,2)   NULL,
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PurchaseOrderLine PRIMARY KEY (Line_Id),
        CONSTRAINT FK_Cf_PurchaseOrderLine_Order FOREIGN KEY (Order_Id)
            REFERENCES dbo.Cf_PurchaseOrder (Order_Id) ON DELETE CASCADE,
        CONSTRAINT FK_Cf_PurchaseOrderLine_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT CK_Cf_PurchaseOrderLine_Qty CHECK (Order_Quantity >= 0 AND Standard_Quantity >= 0)
    );
    CREATE INDEX IX_Cf_PurchaseOrderLine_Order ON dbo.Cf_PurchaseOrderLine (Order_Id);
    CREATE INDEX IX_Cf_PurchaseOrderLine_Item ON dbo.Cf_PurchaseOrderLine (Tenant_Id, Item_Id);
END;
GO

-- ── Cf_PurchasePayment ─────────────────────────────────────────────────────
-- Thanh toán cho phiếu mua. 1 PurchaseOrder có thể có N Payment.
IF OBJECT_ID(N'dbo.Cf_PurchasePayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PurchasePayment
    (
        Payment_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Payment_Code        NVARCHAR(50)    NOT NULL,
        Payment_Date        DATE            NOT NULL,
        Order_Id            BIGINT          NULL,   -- tham chiếu PurchaseOrder (NULL = thanh toán tổng hợp)
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Cash_Account_Id     INT             NOT NULL,    -- TK tiền chi (111/112)
        Amount              DECIMAL(18,2)   NOT NULL,
        Payment_Method      NVARCHAR(20)    NOT NULL DEFAULT N'Cash',  -- Cash/Bank
        Reference_No        NVARCHAR(50)    NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PurchasePayment PRIMARY KEY (Payment_Id),
        CONSTRAINT UQ_Cf_PurchasePayment_Code UNIQUE (Tenant_Id, Payment_Code),
        CONSTRAINT FK_Cf_PurchasePayment_Order FOREIGN KEY (Order_Id) REFERENCES dbo.Cf_PurchaseOrder (Order_Id),
        CONSTRAINT FK_Cf_PurchasePayment_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_PurchasePayment_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_PurchasePayment_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_PurchasePayment_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_PurchasePayment_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled')),
        CONSTRAINT CK_Cf_PurchasePayment_Method CHECK (Payment_Method IN (N'Cash', N'Bank', N'Mixed'))
    );
    CREATE INDEX IX_Cf_PurchasePayment_Partner_Date ON dbo.Cf_PurchasePayment (Tenant_Id, Partner_Id, Payment_Date) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_PurchasePayment_Order ON dbo.Cf_PurchasePayment (Order_Id) WHERE Order_Id IS NOT NULL;
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: PROCESSING — Sấy / phơi cà phê.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_ProcessingExport ────────────────────────────────────────────────────
-- Phiếu xuất kho đi gia công (sấy hoặc phơi).
IF OBJECT_ID(N'dbo.Cf_ProcessingExport', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_ProcessingExport
    (
        Export_Id           BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Export_Code         NVARCHAR(50)    NOT NULL,
        Export_Date         DATE            NOT NULL,
        Process_Type        NVARCHAR(20)    NOT NULL,    -- 'Oven' (sấy) | 'Yard' (phơi)
        Source_Order_Id     BIGINT          NULL,        -- PurchaseOrder gốc (NULL nếu gộp nhiều phiếu)
        Export_Stock_Id     INT             NOT NULL,
        Facility_Id         INT             NOT NULL,    -- lò sấy / sân phơi
        Partner_Id          INT             NULL,        -- chủ lò/sân nếu thuê ngoài
        Employee_Id         INT             NOT NULL,
        Expected_Return_Date DATE           NULL,
        Total_Quantity      DECIMAL(18,4)   NOT NULL DEFAULT 0,   -- tổng kg xuất đi
        Processing_Cost     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_ProcessingExport PRIMARY KEY (Export_Id),
        CONSTRAINT UQ_Cf_ProcessingExport_Code UNIQUE (Tenant_Id, Export_Code),
        CONSTRAINT FK_Cf_ProcessingExport_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_ProcessingExport_Source FOREIGN KEY (Source_Order_Id) REFERENCES dbo.Cf_PurchaseOrder (Order_Id),
        CONSTRAINT FK_Cf_ProcessingExport_Stock FOREIGN KEY (Export_Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_ProcessingExport_Facility FOREIGN KEY (Facility_Id) REFERENCES dbo.Cf_DryingFacility (Facility_Id),
        CONSTRAINT FK_Cf_ProcessingExport_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_ProcessingExport_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_ProcessingExport_Type CHECK (Process_Type IN (N'Oven', N'Yard')),
        CONSTRAINT CK_Cf_ProcessingExport_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_ProcessingExport_Tenant_Date ON dbo.Cf_ProcessingExport (Tenant_Id, Export_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_ProcessingExportLine ────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Cf_ProcessingExportLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_ProcessingExportLine
    (
        Line_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Export_Id           BIGINT          NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Row_No              INT             NOT NULL DEFAULT 1,
        Item_Id             INT             NOT NULL,
        Source_Line_Id      BIGINT          NULL,    -- PurchaseOrderLine gốc nếu có
        Unit                NVARCHAR(20)    NOT NULL DEFAULT N'kg',
        Quantity            DECIMAL(18,4)   NOT NULL,
        Unit_Cost           DECIMAL(18,2)   NULL,
        Amount              DECIMAL(18,2)   NULL,
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_ProcessingExportLine PRIMARY KEY (Line_Id),
        CONSTRAINT FK_Cf_ProcessingExportLine_Export FOREIGN KEY (Export_Id)
            REFERENCES dbo.Cf_ProcessingExport (Export_Id) ON DELETE CASCADE,
        CONSTRAINT FK_Cf_ProcessingExportLine_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_ProcessingExportLine_SrcLine FOREIGN KEY (Source_Line_Id) REFERENCES dbo.Cf_PurchaseOrderLine (Line_Id)
    );
    CREATE INDEX IX_Cf_ProcessingExportLine_Export ON dbo.Cf_ProcessingExportLine (Export_Id);
END;
GO

-- ── Cf_ProcessingReturn ────────────────────────────────────────────────────
-- Nhập kho hàng sau khi sấy/phơi xong (cf nhân từ sấy, cf khô từ phơi).
IF OBJECT_ID(N'dbo.Cf_ProcessingReturn', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_ProcessingReturn
    (
        Return_Id           BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Return_Code         NVARCHAR(50)    NOT NULL,
        Return_Date         DATE            NOT NULL,
        Export_Id           BIGINT          NOT NULL,    -- ProcessingExport gốc
        Import_Stock_Id     INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Total_Quantity_In   DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Loss_Quantity       DECIMAL(18,4)   NOT NULL DEFAULT 0,  -- hao hụt
        Loss_Percent        DECIMAL(8,4)    NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_ProcessingReturn PRIMARY KEY (Return_Id),
        CONSTRAINT UQ_Cf_ProcessingReturn_Code UNIQUE (Tenant_Id, Return_Code),
        CONSTRAINT FK_Cf_ProcessingReturn_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_ProcessingReturn_Export FOREIGN KEY (Export_Id) REFERENCES dbo.Cf_ProcessingExport (Export_Id),
        CONSTRAINT FK_Cf_ProcessingReturn_Stock FOREIGN KEY (Import_Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_ProcessingReturn_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_ProcessingReturn_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_ProcessingReturn_Tenant_Date ON dbo.Cf_ProcessingReturn (Tenant_Id, Return_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_ProcessingReturnLine ────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Cf_ProcessingReturnLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_ProcessingReturnLine
    (
        Line_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Return_Id           BIGINT          NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Row_No              INT             NOT NULL DEFAULT 1,
        Item_Id             INT             NOT NULL,
        Unit                NVARCHAR(20)    NOT NULL DEFAULT N'kg',
        Quantity            DECIMAL(18,4)   NOT NULL,
        Humidity_Percent    DECIMAL(8,4)    NULL,
        Unit_Cost           DECIMAL(18,2)   NULL,
        Amount              DECIMAL(18,2)   NULL,
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_ProcessingReturnLine PRIMARY KEY (Line_Id),
        CONSTRAINT FK_Cf_ProcessingReturnLine_Return FOREIGN KEY (Return_Id)
            REFERENCES dbo.Cf_ProcessingReturn (Return_Id) ON DELETE CASCADE,
        CONSTRAINT FK_Cf_ProcessingReturnLine_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id)
    );
    CREATE INDEX IX_Cf_ProcessingReturnLine_Return ON dbo.Cf_ProcessingReturnLine (Return_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: PRICE CLOSING — Ký gửi - chốt giá - rút chốt.
-- Khách gửi cf nhân vào kho, chốt giá tại một thời điểm, rồi rút tiền.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_PriceCloseContract ──────────────────────────────────────────────────
-- Hợp đồng chốt giá. 1 lần chốt cho 1 lượng cụ thể.
IF OBJECT_ID(N'dbo.Cf_PriceCloseContract', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PriceCloseContract
    (
        Contract_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Contract_Code       NVARCHAR(50)    NOT NULL,
        Contract_Date       DATE            NOT NULL,
        Partner_Id          INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Closed_Quantity     DECIMAL(18,4)   NOT NULL,    -- số kg chốt
        Closed_Price        DECIMAL(18,2)   NOT NULL,    -- đơn giá chốt
        Closed_Amount       DECIMAL(18,2)   NOT NULL,
        Withdrawn_Amount    DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Remaining_Quantity  DECIMAL(18,4)   NOT NULL,    -- còn lại trong kho
        Borrowing_Interest_Rate DECIMAL(8,4) NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',  -- Draft/Confirmed/PartiallyWithdrawn/FullyWithdrawn/Cancelled
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PriceCloseContract PRIMARY KEY (Contract_Id),
        CONSTRAINT UQ_Cf_PriceCloseContract_Code UNIQUE (Tenant_Id, Contract_Code),
        CONSTRAINT FK_Cf_PriceCloseContract_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_PriceCloseContract_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_PriceCloseContract_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_PriceCloseContract_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_PriceCloseContract_Status CHECK (Status IN
            (N'Draft', N'Confirmed', N'PartiallyWithdrawn', N'FullyWithdrawn', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_PriceCloseContract_Tenant_Date ON dbo.Cf_PriceCloseContract (Tenant_Id, Contract_Date) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_PriceCloseContract_Partner ON dbo.Cf_PriceCloseContract (Tenant_Id, Partner_Id) WHERE Is_Active = 1;
END;
GO

-- ── Cf_PriceCloseWithdrawal ────────────────────────────────────────────────
-- Lần rút tiền dựa trên hợp đồng chốt.
IF OBJECT_ID(N'dbo.Cf_PriceCloseWithdrawal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PriceCloseWithdrawal
    (
        Withdrawal_Id       BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Withdrawal_Code     NVARCHAR(50)    NOT NULL,
        Withdrawal_Date     DATE            NOT NULL,
        Contract_Id         BIGINT          NOT NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Interest_Amount     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Net_Amount          DECIMAL(18,2)   NOT NULL,
        Cash_Account_Id     INT             NOT NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PriceCloseWithdrawal PRIMARY KEY (Withdrawal_Id),
        CONSTRAINT UQ_Cf_PriceCloseWithdrawal_Code UNIQUE (Tenant_Id, Withdrawal_Code),
        CONSTRAINT FK_Cf_PriceCloseWithdrawal_Contract FOREIGN KEY (Contract_Id) REFERENCES dbo.Cf_PriceCloseContract (Contract_Id),
        CONSTRAINT FK_Cf_PriceCloseWithdrawal_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_PriceCloseWithdrawal_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_PriceCloseWithdrawal_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_PriceCloseWithdrawal_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_PriceCloseWithdrawal_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_PriceCloseWithdrawal_Contract ON dbo.Cf_PriceCloseWithdrawal (Contract_Id);
END;
GO

-- ── Cf_PriceClosePayment ───────────────────────────────────────────────────
-- Thanh toán nốt phần chốt giá còn lại (BUYCLOSE trong desktop).
IF OBJECT_ID(N'dbo.Cf_PriceClosePayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_PriceClosePayment
    (
        Payment_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Payment_Code        NVARCHAR(50)    NOT NULL,
        Payment_Date        DATE            NOT NULL,
        Contract_Id         BIGINT          NOT NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Quantity            DECIMAL(18,4)   NOT NULL,    -- số kg trả chốt
        Unit_Price          DECIMAL(18,2)   NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Cash_Account_Id     INT             NOT NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_PriceClosePayment PRIMARY KEY (Payment_Id),
        CONSTRAINT UQ_Cf_PriceClosePayment_Code UNIQUE (Tenant_Id, Payment_Code),
        CONSTRAINT FK_Cf_PriceClosePayment_Contract FOREIGN KEY (Contract_Id) REFERENCES dbo.Cf_PriceCloseContract (Contract_Id),
        CONSTRAINT FK_Cf_PriceClosePayment_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_PriceClosePayment_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_PriceClosePayment_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_PriceClosePayment_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_PriceClosePayment_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: LENDING — Vay / Cho vay tiền.
-- Direction='Out' = mình cho khách vay (tài sản, TK 1283 Debit).
-- Direction='In'  = mình vay từ khách (nợ phải trả, TK 341/331 Credit).
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_Loan ────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Cf_Loan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Loan
    (
        Loan_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Loan_Code           NVARCHAR(50)    NOT NULL,
        Loan_Date           DATE            NOT NULL,
        Loan_Direction      NVARCHAR(10)    NOT NULL,    -- 'Out' (cho vay) | 'In' (đi vay)
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Principal           DECIMAL(18,2)   NOT NULL,
        Interest_Rate_Year  DECIMAL(8,4)    NULL,
        Maturity_Date       DATE            NULL,
        Cash_Account_Id     INT             NOT NULL,
        Outstanding_Principal DECIMAL(18,2) NOT NULL,   -- gốc còn lại
        Outstanding_Interest  DECIMAL(18,2) NOT NULL DEFAULT 0, -- lãi tích lũy chưa thu
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft', -- Draft/Active/Closed/Cancelled
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,
        Closed_At           DATETIME2       NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Loan PRIMARY KEY (Loan_Id),
        CONSTRAINT UQ_Cf_Loan_Code UNIQUE (Tenant_Id, Loan_Code),
        CONSTRAINT FK_Cf_Loan_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_Loan_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_Loan_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_Loan_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_Loan_Direction CHECK (Loan_Direction IN (N'Out', N'In')),
        CONSTRAINT CK_Cf_Loan_Status CHECK (Status IN (N'Draft', N'Active', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_Loan_Partner ON dbo.Cf_Loan (Tenant_Id, Partner_Id) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_Loan_Date ON dbo.Cf_Loan (Tenant_Id, Loan_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_LoanRepayment ───────────────────────────────────────────────────────
-- Lần trả nợ (gốc + lãi).
IF OBJECT_ID(N'dbo.Cf_LoanRepayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_LoanRepayment
    (
        Repayment_Id        BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Repayment_Code      NVARCHAR(50)    NOT NULL,
        Repayment_Date      DATE            NOT NULL,
        Loan_Id             BIGINT          NOT NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Principal_Amount    DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Interest_Amount     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Total_Amount        DECIMAL(18,2)   NOT NULL,
        Cash_Account_Id     INT             NOT NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_LoanRepayment PRIMARY KEY (Repayment_Id),
        CONSTRAINT UQ_Cf_LoanRepayment_Code UNIQUE (Tenant_Id, Repayment_Code),
        CONSTRAINT FK_Cf_LoanRepayment_Loan FOREIGN KEY (Loan_Id) REFERENCES dbo.Cf_Loan (Loan_Id),
        CONSTRAINT FK_Cf_LoanRepayment_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_LoanRepayment_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_LoanRepayment_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_LoanRepayment_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_LoanRepayment_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled')),
        CONSTRAINT CK_Cf_LoanRepayment_Total CHECK (Total_Amount = Principal_Amount + Interest_Amount)
    );
    CREATE INDEX IX_Cf_LoanRepayment_Loan ON dbo.Cf_LoanRepayment (Loan_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: SALES — Bán hàng.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_SalesContract ───────────────────────────────────────────────────────
-- Hợp đồng kinh tế (INFOR_ECONOMI). Header — chứa cam kết tổng.
IF OBJECT_ID(N'dbo.Cf_SalesContract', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_SalesContract
    (
        Contract_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Contract_Code       NVARCHAR(50)    NOT NULL,
        Contract_Date       DATE            NOT NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Contract_Quantity   DECIMAL(18,4)   NOT NULL,
        Contract_Price      DECIMAL(18,2)   NOT NULL,
        Contract_Amount     DECIMAL(18,2)   NOT NULL,
        Delivery_Address    NVARCHAR(500)   NULL,
        Deadline_Date       DATE            NULL,
        Has_Vat             BIT             NOT NULL DEFAULT 0,
        Vat_Rate            DECIMAL(6,4)    NULL,
        Note                NVARCHAR(500)   NULL,

        Delivered_Quantity  DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Received_Amount     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Advance_Amount      DECIMAL(18,2)   NOT NULL DEFAULT 0,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft', -- Draft/Active/PartiallyDelivered/Closed/Cancelled
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,
        Closed_At           DATETIME2       NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_SalesContract PRIMARY KEY (Contract_Id),
        CONSTRAINT UQ_Cf_SalesContract_Code UNIQUE (Tenant_Id, Contract_Code),
        CONSTRAINT FK_Cf_SalesContract_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_SalesContract_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_SalesContract_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_SalesContract_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_SalesContract_Status CHECK (Status IN
            (N'Draft', N'Active', N'PartiallyDelivered', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_SalesContract_Tenant_Date ON dbo.Cf_SalesContract (Tenant_Id, Contract_Date) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_SalesContract_Partner ON dbo.Cf_SalesContract (Tenant_Id, Partner_Id) WHERE Is_Active = 1;
END;
GO

-- ── Cf_SalesDelivery ───────────────────────────────────────────────────────
-- Phiếu giao hàng (INFOR_DELIVERY_CONTRACTS). 1 contract có N delivery.
IF OBJECT_ID(N'dbo.Cf_SalesDelivery', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_SalesDelivery
    (
        Delivery_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Delivery_Code       NVARCHAR(50)    NOT NULL,
        Delivery_Date       DATE            NOT NULL,
        Contract_Id         BIGINT          NULL,    -- NULL nếu giao theo bán trực tiếp
        Partner_Id          INT             NOT NULL,
        Export_Stock_Id     INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Export_Quantity     DECIMAL(18,4)   NOT NULL,    -- xuất kho
        Receive_Quantity    DECIMAL(18,4)   NULL,        -- nhận tại điểm bán
        Net_Deduction       DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Humidity_Percent    DECIMAL(8,4)    NULL,
        Impurity_Percent    DECIMAL(8,4)    NULL,
        Standard_Quantity   DECIMAL(18,4)   NOT NULL,
        Sold_Quantity       DECIMAL(18,4)   NOT NULL DEFAULT 0,   -- số đã bán
        Deposit_Quantity    DECIMAL(18,4)   NOT NULL DEFAULT 0,   -- số ký gửi tại điểm bán
        Unit_Price          DECIMAL(18,2)   NULL,
        Amount              DECIMAL(18,2)   NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_SalesDelivery PRIMARY KEY (Delivery_Id),
        CONSTRAINT UQ_Cf_SalesDelivery_Code UNIQUE (Tenant_Id, Delivery_Code),
        CONSTRAINT FK_Cf_SalesDelivery_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_SalesDelivery_Contract FOREIGN KEY (Contract_Id) REFERENCES dbo.Cf_SalesContract (Contract_Id),
        CONSTRAINT FK_Cf_SalesDelivery_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_SalesDelivery_Stock FOREIGN KEY (Export_Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_SalesDelivery_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_SalesDelivery_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_SalesDelivery_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_SalesDelivery_Contract ON dbo.Cf_SalesDelivery (Contract_Id) WHERE Contract_Id IS NOT NULL;
    CREATE INDEX IX_Cf_SalesDelivery_Tenant_Date ON dbo.Cf_SalesDelivery (Tenant_Id, Delivery_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_SalesDirect ─────────────────────────────────────────────────────────
-- Bán trực tiếp không qua HĐ (INFOR_SELLDIRECT).
IF OBJECT_ID(N'dbo.Cf_SalesDirect', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_SalesDirect
    (
        Sale_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Sale_Code           NVARCHAR(50)    NOT NULL,
        Sale_Date           DATE            NOT NULL,
        Partner_Id          INT             NOT NULL,
        Export_Stock_Id     INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Quantity            DECIMAL(18,4)   NOT NULL,
        Unit_Price          DECIMAL(18,2)   NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Has_Vat             BIT             NOT NULL DEFAULT 0,
        Vat_Rate            DECIMAL(6,4)    NULL,
        Vat_Amount          DECIMAL(18,2)   NULL,
        Total_Amount        DECIMAL(18,2)   NOT NULL,
        Cash_Account_Id     INT             NULL,    -- nếu thu tiền ngay
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_SalesDirect PRIMARY KEY (Sale_Id),
        CONSTRAINT UQ_Cf_SalesDirect_Code UNIQUE (Tenant_Id, Sale_Code),
        CONSTRAINT FK_Cf_SalesDirect_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_SalesDirect_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_SalesDirect_Stock FOREIGN KEY (Export_Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_SalesDirect_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_SalesDirect_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_SalesDirect_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_SalesDirect_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_SalesDirect_Tenant_Date ON dbo.Cf_SalesDirect (Tenant_Id, Sale_Date) WHERE Is_Active = 1;
END;
GO

-- ── Cf_SalesAdvance ────────────────────────────────────────────────────────
-- Ứng tiền trước giao (ADVANCE_MONEY).
IF OBJECT_ID(N'dbo.Cf_SalesAdvance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_SalesAdvance
    (
        Advance_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Advance_Code        NVARCHAR(50)    NOT NULL,
        Advance_Date        DATE            NOT NULL,
        Contract_Id         BIGINT          NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Borrowing_Interest_Rate DECIMAL(8,4) NULL,
        Cash_Account_Id     INT             NOT NULL,
        Settled_Amount      DECIMAL(18,2)   NOT NULL DEFAULT 0,   -- đã hoàn ứng
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_SalesAdvance PRIMARY KEY (Advance_Id),
        CONSTRAINT UQ_Cf_SalesAdvance_Code UNIQUE (Tenant_Id, Advance_Code),
        CONSTRAINT FK_Cf_SalesAdvance_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_SalesAdvance_Contract FOREIGN KEY (Contract_Id) REFERENCES dbo.Cf_SalesContract (Contract_Id),
        CONSTRAINT FK_Cf_SalesAdvance_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_SalesAdvance_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_SalesAdvance_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_SalesAdvance_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Settled', N'Cancelled'))
    );
END;
GO

-- ── Cf_SalesReceipt ────────────────────────────────────────────────────────
-- Nhận tiền bán (RECEIVE_MONEY) — có thể có hoặc không gắn contract.
IF OBJECT_ID(N'dbo.Cf_SalesReceipt', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_SalesReceipt
    (
        Receipt_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Receipt_Code        NVARCHAR(50)    NOT NULL,
        Receipt_Date        DATE            NOT NULL,
        Contract_Id         BIGINT          NULL,
        Delivery_Id         BIGINT          NULL,
        Partner_Id          INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Vat_Amount          DECIMAL(18,2)   NULL,
        Cash_Account_Id     INT             NOT NULL,
        Invoice_No          NVARCHAR(50)    NULL,
        Invoice_Date        DATE            NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_SalesReceipt PRIMARY KEY (Receipt_Id),
        CONSTRAINT UQ_Cf_SalesReceipt_Code UNIQUE (Tenant_Id, Receipt_Code),
        CONSTRAINT FK_Cf_SalesReceipt_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_SalesReceipt_Contract FOREIGN KEY (Contract_Id) REFERENCES dbo.Cf_SalesContract (Contract_Id),
        CONSTRAINT FK_Cf_SalesReceipt_Delivery FOREIGN KEY (Delivery_Id) REFERENCES dbo.Cf_SalesDelivery (Delivery_Id),
        CONSTRAINT FK_Cf_SalesReceipt_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_SalesReceipt_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT FK_Cf_SalesReceipt_Cash FOREIGN KEY (Cash_Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_SalesReceipt_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
    CREATE INDEX IX_Cf_SalesReceipt_Tenant_Date ON dbo.Cf_SalesReceipt (Tenant_Id, Receipt_Date) WHERE Is_Active = 1;
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: INVENTORY — Thẻ kho, số dư tồn, kiểm kê chênh lệch.
-- Cf_StockMovement = thẻ kho phẳng (1 record / 1 lần nhập-xuất / 1 line nghiệp vụ).
-- Cf_StockBalance = cache số dư theo (Stock × Item × Period) — cập nhật bằng job/event.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_StockMovement ───────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Cf_StockMovement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_StockMovement
    (
        Movement_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Movement_Date       DATE            NOT NULL,
        Movement_Type       NVARCHAR(20)    NOT NULL,    -- 'IN'/'OUT'/'TRANSFER'/'ADJUST'
        Source_Module       NVARCHAR(30)    NOT NULL,    -- 'Purchasing','Sales','Processing','StockCount','Manual'
        Source_Ref_Id       BIGINT          NULL,        -- Id của phiếu gốc
        Source_Ref_Code     NVARCHAR(50)    NULL,
        Stock_Id            INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Unit                NVARCHAR(20)    NOT NULL DEFAULT N'kg',
        Quantity            DECIMAL(18,4)   NOT NULL,    -- + = nhập, - = xuất
        Unit_Cost           DECIMAL(18,2)   NULL,
        Amount              DECIMAL(18,2)   NULL,
        Partner_Id          INT             NULL,
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_StockMovement PRIMARY KEY (Movement_Id),
        CONSTRAINT FK_Cf_StockMovement_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_StockMovement_Stock FOREIGN KEY (Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_StockMovement_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_StockMovement_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT CK_Cf_StockMovement_Type CHECK (Movement_Type IN (N'IN', N'OUT', N'TRANSFER', N'ADJUST'))
    );
    CREATE INDEX IX_Cf_StockMovement_StockItem_Date ON dbo.Cf_StockMovement (Tenant_Id, Stock_Id, Item_Id, Movement_Date);
    CREATE INDEX IX_Cf_StockMovement_Source ON dbo.Cf_StockMovement (Source_Module, Source_Ref_Id) WHERE Source_Ref_Id IS NOT NULL;
END;
GO

-- ── Cf_StockBalance ────────────────────────────────────────────────────────
-- Số dư tồn kho theo kỳ (cache). Cập nhật qua job/handler khi confirm movement.
IF OBJECT_ID(N'dbo.Cf_StockBalance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_StockBalance
    (
        Balance_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Period_Year         SMALLINT        NOT NULL,
        Period_Month        TINYINT         NOT NULL,
        Stock_Id            INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        Opening_Quantity    DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Opening_Amount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        In_Quantity         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        In_Amount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Out_Quantity        DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Out_Amount          DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Closing_Quantity    DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Closing_Amount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Calculated_At       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Cf_StockBalance PRIMARY KEY (Balance_Id),
        CONSTRAINT UQ_Cf_StockBalance UNIQUE (Tenant_Id, Period_Year, Period_Month, Stock_Id, Item_Id),
        CONSTRAINT FK_Cf_StockBalance_Stock FOREIGN KEY (Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_StockBalance_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT CK_Cf_StockBalance_Month CHECK (Period_Month BETWEEN 1 AND 12)
    );
END;
GO

-- ── Cf_StockCount ──────────────────────────────────────────────────────────
-- Phiếu kiểm kê. Lưu chênh lệch giữa số sổ và số thực tế.
IF OBJECT_ID(N'dbo.Cf_StockCount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_StockCount
    (
        Count_Id            BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Count_Code          NVARCHAR(50)    NOT NULL,
        Count_Date          DATE            NOT NULL,
        Stock_Id            INT             NOT NULL,
        Employee_Id         INT             NOT NULL,
        Note                NVARCHAR(500)   NULL,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Draft',
        Confirmed_At        DATETIME2       NULL,
        Confirmed_By        INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Deleted_At          DATETIME2       NULL,
        Deleted_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_StockCount PRIMARY KEY (Count_Id),
        CONSTRAINT UQ_Cf_StockCount_Code UNIQUE (Tenant_Id, Count_Code),
        CONSTRAINT FK_Cf_StockCount_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_StockCount_Stock FOREIGN KEY (Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        CONSTRAINT FK_Cf_StockCount_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_StockCount_Status CHECK (Status IN (N'Draft', N'Confirmed', N'Closed', N'Cancelled'))
    );
END;
GO

-- ── Cf_StockCountLine ──────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Cf_StockCountLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_StockCountLine
    (
        Line_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Count_Id            BIGINT          NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Item_Id             INT             NOT NULL,
        System_Quantity     DECIMAL(18,4)   NOT NULL,
        Actual_Quantity     DECIMAL(18,4)   NOT NULL,
        Difference_Quantity AS (Actual_Quantity - System_Quantity) PERSISTED,
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_StockCountLine PRIMARY KEY (Line_Id),
        CONSTRAINT FK_Cf_StockCountLine_Count FOREIGN KEY (Count_Id)
            REFERENCES dbo.Cf_StockCount (Count_Id) ON DELETE CASCADE,
        CONSTRAINT FK_Cf_StockCountLine_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id)
    );
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- BC: ACCOUNTING — Sổ cái kế toán kép, số dư đầu kỳ, rule auto-post.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Cf_Journal ─────────────────────────────────────────────────────────────
-- Mỗi record = 1 chứng từ kế toán. Tổng Debit = tổng Credit (enforce trong handler).
IF OBJECT_ID(N'dbo.Cf_Journal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_Journal
    (
        Journal_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Journal_Code        NVARCHAR(50)    NOT NULL,
        Journal_Date        DATE            NOT NULL,
        Journal_Type        NVARCHAR(30)    NOT NULL,    -- 'PurchaseFresh','PurchasePay','Sale','PriceClose','Loan','Opening','Manual'...
        Source_Module       NVARCHAR(30)    NULL,        -- BC name
        Source_Ref_Id       BIGINT          NULL,
        Source_Ref_Code     NVARCHAR(50)    NULL,
        Partner_Id          INT             NULL,
        Employee_Id         INT             NULL,
        Description         NVARCHAR(500)   NULL,
        Total_Debit         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Total_Credit        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Currency_Code       NVARCHAR(10)    NOT NULL DEFAULT N'VND',
        Exchange_Rate       DECIMAL(18,6)   NOT NULL DEFAULT 1,

        Status              NVARCHAR(20)    NOT NULL DEFAULT N'Posted',  -- Draft/Posted/Reversed
        Posted_At           DATETIME2       NULL,
        Posted_By           INT             NULL,
        Reversed_At         DATETIME2       NULL,
        Reversed_By         INT             NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_Journal PRIMARY KEY (Journal_Id),
        CONSTRAINT UQ_Cf_Journal_Code UNIQUE (Tenant_Id, Journal_Code),
        CONSTRAINT FK_Cf_Journal_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_Journal_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_Journal_Employee FOREIGN KEY (Employee_Id) REFERENCES dbo.Cf_Employee (Employee_Id),
        CONSTRAINT CK_Cf_Journal_Status CHECK (Status IN (N'Draft', N'Posted', N'Reversed')),
        CONSTRAINT CK_Cf_Journal_Balance CHECK (Total_Debit = Total_Credit)
    );
    CREATE INDEX IX_Cf_Journal_Tenant_Date ON dbo.Cf_Journal (Tenant_Id, Journal_Date) WHERE Is_Active = 1;
    CREATE INDEX IX_Cf_Journal_Source ON dbo.Cf_Journal (Source_Module, Source_Ref_Id) WHERE Source_Ref_Id IS NOT NULL;
END;
GO

-- ── Cf_JournalLine ─────────────────────────────────────────────────────────
-- 1 dòng = 1 chiều (Debit HOẶC Credit), KHÔNG cả 2 — chuẩn double-entry.
IF OBJECT_ID(N'dbo.Cf_JournalLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_JournalLine
    (
        Line_Id             BIGINT          IDENTITY(1,1) NOT NULL,
        Journal_Id          BIGINT          NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Row_No              INT             NOT NULL DEFAULT 1,
        Account_Id          INT             NOT NULL,
        Partner_Id          INT             NULL,
        Item_Id             INT             NULL,
        Stock_Id            INT             NULL,
        Quantity            DECIMAL(18,4)   NULL,
        Unit_Price          DECIMAL(18,2)   NULL,
        Debit_Amount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Credit_Amount       DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Description         NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Cf_JournalLine PRIMARY KEY (Line_Id),
        CONSTRAINT FK_Cf_JournalLine_Journal FOREIGN KEY (Journal_Id)
            REFERENCES dbo.Cf_Journal (Journal_Id) ON DELETE CASCADE,
        CONSTRAINT FK_Cf_JournalLine_Account FOREIGN KEY (Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT FK_Cf_JournalLine_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_JournalLine_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_JournalLine_Stock FOREIGN KEY (Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id),
        -- 1 dòng chỉ là Debit HOẶC Credit, không cả hai
        CONSTRAINT CK_Cf_JournalLine_DC CHECK (
            (Debit_Amount > 0 AND Credit_Amount = 0)
            OR (Debit_Amount = 0 AND Credit_Amount > 0)
        )
    );
    CREATE INDEX IX_Cf_JournalLine_Journal ON dbo.Cf_JournalLine (Journal_Id);
    CREATE INDEX IX_Cf_JournalLine_Account ON dbo.Cf_JournalLine (Tenant_Id, Account_Id);
    CREATE INDEX IX_Cf_JournalLine_Partner ON dbo.Cf_JournalLine (Tenant_Id, Partner_Id) WHERE Partner_Id IS NOT NULL;
END;
GO

-- ── Cf_OpeningBalance ──────────────────────────────────────────────────────
-- Số dư đầu kỳ — đầu năm tài chính, mở sổ.
IF OBJECT_ID(N'dbo.Cf_OpeningBalance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_OpeningBalance
    (
        Opening_Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Branch_Id           INT             NOT NULL,
        Period_Year         SMALLINT        NOT NULL,
        Account_Id          INT             NOT NULL,
        Partner_Id          INT             NULL,
        Item_Id             INT             NULL,
        Stock_Id            INT             NULL,
        Debit_Amount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Credit_Amount       DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Quantity            DECIMAL(18,4)   NULL,        -- với TK hàng tồn (1561, 151...)
        Note                NVARCHAR(500)   NULL,

        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_OpeningBalance PRIMARY KEY (Opening_Id),
        CONSTRAINT FK_Cf_OpeningBalance_Branch FOREIGN KEY (Branch_Id) REFERENCES dbo.Cf_Branch (Branch_Id),
        CONSTRAINT FK_Cf_OpeningBalance_Account FOREIGN KEY (Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT FK_Cf_OpeningBalance_Partner FOREIGN KEY (Partner_Id) REFERENCES dbo.Cf_Partner (Partner_Id),
        CONSTRAINT FK_Cf_OpeningBalance_Item FOREIGN KEY (Item_Id) REFERENCES dbo.Cf_Item (Item_Id),
        CONSTRAINT FK_Cf_OpeningBalance_Stock FOREIGN KEY (Stock_Id) REFERENCES dbo.Cf_Stock (Stock_Id)
    );
    CREATE INDEX IX_Cf_OpeningBalance_PeriodAcct ON dbo.Cf_OpeningBalance (Tenant_Id, Period_Year, Account_Id);
END;
GO

-- ── Cf_AccountMapping ──────────────────────────────────────────────────────
-- Quy tắc auto-post journal theo từng nghiệp vụ.
-- Khi handler raise event "PurchaseOrderConfirmed" với type='Fresh' →
-- truy vấn Cf_AccountMapping, lấy ra danh sách dòng Debit/Credit cần tạo.
-- Giúp chuyển logic kế toán từ code sang config (low-code).
IF OBJECT_ID(N'dbo.Cf_AccountMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cf_AccountMapping
    (
        Mapping_Id          INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id           INT             NOT NULL,
        Event_Code          NVARCHAR(50)    NOT NULL,    -- 'Purchase.Fresh.Confirmed','Sale.Direct.Confirmed'...
        Row_No              INT             NOT NULL,
        Side                CHAR(1)         NOT NULL,    -- 'D' = Debit, 'C' = Credit
        Account_Id          INT             NOT NULL,
        Amount_Source       NVARCHAR(50)    NOT NULL,    -- 'TotalAmount','OrderQuantity*UnitPrice','DepositQuantity*1'...
        Item_Source         NVARCHAR(50)    NULL,        -- 'Line.Item_Id', 'Header.Default_Item' — null nếu không cần
        Description_Tpl     NVARCHAR(500)   NULL,        -- template với {Partner.Name}, {Order.Code}
        Note                NVARCHAR(500)   NULL,

        Is_Active           BIT             NOT NULL DEFAULT 1,
        Created_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Created_By          INT             NOT NULL,
        Updated_At          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Updated_By          INT             NULL,
        Row_Version         ROWVERSION,

        CONSTRAINT PK_Cf_AccountMapping PRIMARY KEY (Mapping_Id),
        CONSTRAINT UQ_Cf_AccountMapping UNIQUE (Tenant_Id, Event_Code, Row_No),
        CONSTRAINT FK_Cf_AccountMapping_Account FOREIGN KEY (Account_Id) REFERENCES dbo.Cf_Account (Account_Id),
        CONSTRAINT CK_Cf_AccountMapping_Side CHECK (Side IN ('D','C'))
    );
    CREATE INDEX IX_Cf_AccountMapping_Event ON dbo.Cf_AccountMapping (Tenant_Id, Event_Code) WHERE Is_Active = 1;
END;
GO

-- =============================================================================
-- END — 27 bảng đã tạo:
--   Catalog (10): Branch, Employee, PartnerGroup, Partner, ItemGroup, Item,
--                 Stock, Account, Humidity, DryingFacility
--   Purchasing (3): PurchaseOrder, PurchaseOrderLine, PurchasePayment
--   Processing (4): ProcessingExport, ProcessingExportLine,
--                   ProcessingReturn, ProcessingReturnLine
--   PriceClosing (3): PriceCloseContract, PriceCloseWithdrawal, PriceClosePayment
--   Lending (2): Loan, LoanRepayment
--   Sales (5): SalesContract, SalesDelivery, SalesDirect, SalesAdvance, SalesReceipt
--   Inventory (4): StockMovement, StockBalance, StockCount, StockCountLine
--   Accounting (4): Journal, JournalLine, OpeningBalance, AccountMapping
-- =============================================================================
