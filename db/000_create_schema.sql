-- =============================================================================
-- File    : 000_create_schema.sql
-- Purpose : Tạo toàn bộ schema cho ICare247_Config database — 30 bảng.
-- Source  : docs/spec/02_DATABASE_SCHEMA.md (ngày 2026-03-18)
-- Note    : Chạy trên SQL Server. Idempotent — kiểm tra IF NOT EXISTS trước mỗi bảng.
--           Thứ tự: lookup tables trước, FK-dependent tables sau.
-- =============================================================================

USE [ICare247_Config];
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: System (Sys_*)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Sys_Tenant ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Tenant', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Tenant
    (
        Tenant_Id       INT             IDENTITY(1,1) NOT NULL,
        Tenant_Code     NVARCHAR(100)   NOT NULL,
        Tenant_Name     NVARCHAR(255)   NOT NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Tenant PRIMARY KEY (Tenant_Id),
        CONSTRAINT UQ_Sys_Tenant_Code UNIQUE (Tenant_Code)
    );
END;
GO

-- ── Sys_Language ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Language', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Language
    (
        Lang_Code       NVARCHAR(10)    NOT NULL,
        Lang_Name       NVARCHAR(100)   NOT NULL DEFAULT '',
        Is_Default      BIT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_Sys_Language PRIMARY KEY (Lang_Code)
    );

    CREATE UNIQUE INDEX UQ_Sys_Language_Default
        ON dbo.Sys_Language (Is_Default) WHERE Is_Default = 1;
END;
GO

-- ── Sys_Resource (i18n) ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Resource', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Resource
    (
        Resource_Key    NVARCHAR(150)   NOT NULL,
        Lang_Code       NVARCHAR(10)    NOT NULL,
        Resource_Value  NVARCHAR(MAX)   NOT NULL,
        Version         INT             NOT NULL DEFAULT 1,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Resource PRIMARY KEY (Resource_Key, Lang_Code),
        CONSTRAINT FK_Sys_Resource_Lang FOREIGN KEY (Lang_Code)
            REFERENCES dbo.Sys_Language (Lang_Code)
    );
END;
GO

-- ── Sys_Table ───────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Table', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Table
    (
        Table_Id        INT             IDENTITY(1,1) NOT NULL,
        Table_Code      NVARCHAR(100)   NOT NULL,
        Table_Name      NVARCHAR(255)   NOT NULL DEFAULT '',
        Schema_Name     NVARCHAR(50)    NOT NULL DEFAULT 'dbo',
        Is_Tenant       BIT             NOT NULL DEFAULT 0,
        Tenant_Id       INT             NULL,
        Version         INT             NOT NULL DEFAULT 1,
        Checksum        NVARCHAR(64)    NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Description     NVARCHAR(500)   NULL,

        CONSTRAINT PK_Sys_Table PRIMARY KEY (Table_Id),
        CONSTRAINT FK_Sys_Table_Tenant FOREIGN KEY (Tenant_Id)
            REFERENCES dbo.Sys_Tenant (Tenant_Id)
    );

    CREATE UNIQUE INDEX UQ_Sys_Table_Code_Global
        ON dbo.Sys_Table (Table_Code) WHERE Tenant_Id IS NULL;
    CREATE UNIQUE INDEX UQ_Sys_Table_Code_Tenant
        ON dbo.Sys_Table (Table_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
END;
GO

-- ── Sys_Column ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Column', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Column
    (
        Column_Id       INT             IDENTITY(1,1) NOT NULL,
        Table_Id        INT             NOT NULL,
        Column_Code     NVARCHAR(100)   NOT NULL,
        Data_Type       NVARCHAR(50)    NOT NULL,
        Net_Type        NVARCHAR(50)    NOT NULL,
        Max_Length      INT             NULL,
        [Precision]     INT             NULL,
        Scale           INT             NULL,
        Is_Nullable     BIT             NOT NULL DEFAULT 1,
        Is_PK           BIT             NOT NULL DEFAULT 0,
        Is_Identity     BIT             NOT NULL DEFAULT 0,
        Default_Value   NVARCHAR(255)   NULL,
        Version         INT             NOT NULL DEFAULT 1,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Column PRIMARY KEY (Column_Id),
        CONSTRAINT FK_Sys_Column_Table FOREIGN KEY (Table_Id)
            REFERENCES dbo.Sys_Table (Table_Id),
        CONSTRAINT UQ_Sys_Column_Code UNIQUE (Table_Id, Column_Code)
    );

    CREATE INDEX IX_Sys_Column_Table
        ON dbo.Sys_Column (Table_Id, Is_Active);
END;
GO

-- ── Sys_Relation ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Relation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Relation
    (
        Relation_Id     INT             IDENTITY(1,1) NOT NULL,
        Master_Table_Id INT             NOT NULL,
        Detail_Table_Id INT             NOT NULL,
        Relation_Type   NVARCHAR(50)    NOT NULL,
        Display_Column  NVARCHAR(100)   NULL,
        Value_Column    NVARCHAR(100)   NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Relation PRIMARY KEY (Relation_Id),
        CONSTRAINT FK_Sys_Relation_Master FOREIGN KEY (Master_Table_Id)
            REFERENCES dbo.Sys_Table (Table_Id),
        CONSTRAINT FK_Sys_Relation_Detail FOREIGN KEY (Detail_Table_Id)
            REFERENCES dbo.Sys_Table (Table_Id)
    );
END;
GO

-- ── Sys_Role ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Role', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Role
    (
        Role_Id         INT             IDENTITY(1,1) NOT NULL,
        Role_Code       NVARCHAR(100)   NOT NULL,
        Role_Name       NVARCHAR(255)   NOT NULL,
        Tenant_Id       INT             NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Role PRIMARY KEY (Role_Id),
        CONSTRAINT FK_Sys_Role_Tenant FOREIGN KEY (Tenant_Id)
            REFERENCES dbo.Sys_Tenant (Tenant_Id)
    );

    CREATE UNIQUE INDEX UQ_Sys_Role_Code_Global
        ON dbo.Sys_Role (Role_Code) WHERE Tenant_Id IS NULL;
    CREATE UNIQUE INDEX UQ_Sys_Role_Code_Tenant
        ON dbo.Sys_Role (Role_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
END;
GO

-- ── Sys_Config ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Config', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Config
    (
        Config_Id       INT             IDENTITY(1,1) NOT NULL,
        Config_Key      NVARCHAR(150)   NOT NULL,
        Config_Value    NVARCHAR(MAX)   NOT NULL,
        Scope           NVARCHAR(50)    NOT NULL DEFAULT 'Global',
        Tenant_Id       INT             NULL,
        Version         INT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Config PRIMARY KEY (Config_Id),
        CONSTRAINT FK_Sys_Config_Tenant FOREIGN KEY (Tenant_Id)
            REFERENCES dbo.Sys_Tenant (Tenant_Id)
    );

    CREATE UNIQUE INDEX UQ_Sys_Config_Global
        ON dbo.Sys_Config (Config_Key, Scope) WHERE Tenant_Id IS NULL;
    CREATE UNIQUE INDEX UQ_Sys_Config_Tenant
        ON dbo.Sys_Config (Config_Key, Scope, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
END;
GO

-- ── Sys_Version ─────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Version', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Version
    (
        Object_Type     NVARCHAR(50)    NOT NULL,
        Object_Id       INT             NOT NULL,
        Version         INT             NOT NULL,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Version PRIMARY KEY (Object_Type, Object_Id)
    );
END;
GO

-- ── Sys_Cache_Invalidation ──────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Cache_Invalidation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Cache_Invalidation
    (
        Invalidation_Id BIGINT          IDENTITY(1,1) NOT NULL,
        Object_Type     NVARCHAR(50)    NOT NULL,
        Object_Id       INT             NOT NULL,
        Invalidated_At  DATETIME        NOT NULL DEFAULT GETDATE(),
        Is_Published    BIT             NOT NULL DEFAULT 0,
        Published_At    DATETIME        NULL,

        CONSTRAINT PK_Sys_Cache_Invalidation PRIMARY KEY (Invalidation_Id)
    );

    CREATE INDEX IX_Sys_Cache_Invalidation_Pending
        ON dbo.Sys_Cache_Invalidation (Is_Published, Invalidated_At)
        WHERE Is_Published = 0;
END;
GO

-- ── Sys_Audit_Log ───────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Audit_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Audit_Log
    (
        Audit_Id        BIGINT          IDENTITY(1,1) NOT NULL,
        Object_Type     NVARCHAR(50)    NOT NULL,
        Object_Id       INT             NOT NULL,
        [Action]        NVARCHAR(20)    NOT NULL,
        Changed_By      NVARCHAR(150)   NOT NULL,
        Changed_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Old_Value_Json  NVARCHAR(MAX)   NULL,
        New_Value_Json  NVARCHAR(MAX)   NULL,
        Correlation_Id  NVARCHAR(64)    NULL,

        CONSTRAINT PK_Sys_Audit_Log PRIMARY KEY (Audit_Id)
    );

    CREATE INDEX IX_Sys_Audit_Log_Created
        ON dbo.Sys_Audit_Log (Changed_At);
    CREATE INDEX IX_Sys_Audit_Log_Object
        ON dbo.Sys_Audit_Log (Object_Type, Object_Id);
END;
GO

-- ── Sys_Perf_Log ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Perf_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Perf_Log
    (
        Perf_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Metric_Type     NVARCHAR(50)    NOT NULL,
        Reference_Code  NVARCHAR(150)   NULL,
        Duration_MS     BIGINT          NOT NULL,
        Is_Cache_Hit    BIT             NULL,
        Correlation_Id  NVARCHAR(64)    NULL,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Perf_Log PRIMARY KEY (Perf_Id)
    );

    CREATE INDEX IX_Sys_Perf_Log_Created
        ON dbo.Sys_Perf_Log (Created_At);
    CREATE INDEX IX_Sys_Perf_Log_Type
        ON dbo.Sys_Perf_Log (Metric_Type, Created_At);
END;
GO

-- ── Sys_Error_Log ───────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Error_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Error_Log
    (
        Error_Id        BIGINT          IDENTITY(1,1) NOT NULL,
        Source          NVARCHAR(150)   NULL,
        [Message]       NVARCHAR(MAX)   NULL,
        Stack           NVARCHAR(MAX)   NULL,
        Correlation_Id  NVARCHAR(64)    NULL,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Sys_Error_Log PRIMARY KEY (Error_Id)
    );

    CREATE INDEX IX_Sys_Error_Log_Created
        ON dbo.Sys_Error_Log (Created_At);
END;
GO

-- ── Sys_Permission ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Permission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Permission
    (
        Permission_Id   INT             IDENTITY(1,1) NOT NULL,
        Role_Id         INT             NOT NULL,
        Object_Type     NVARCHAR(50)    NOT NULL,
        Object_Id       INT             NOT NULL,
        Can_Read        BIT             NOT NULL DEFAULT 0,
        Can_Write       BIT             NOT NULL DEFAULT 0,
        Can_Submit      BIT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_Sys_Permission PRIMARY KEY (Permission_Id),
        CONSTRAINT FK_Sys_Permission_Role FOREIGN KEY (Role_Id)
            REFERENCES dbo.Sys_Role (Role_Id),
        CONSTRAINT UQ_Sys_Permission UNIQUE (Role_Id, Object_Type, Object_Id)
    );

    CREATE INDEX IX_Sys_Permission_Object
        ON dbo.Sys_Permission (Object_Type, Object_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: UI Form Engine (Ui_*)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Ui_Form ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Form', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Form
    (
        Form_Id         INT             IDENTITY(1,1) NOT NULL,
        Form_Code       NVARCHAR(100)   NOT NULL,
        Table_Id        INT             NOT NULL,
        Platform        NVARCHAR(50)    NOT NULL,
        Layout_Engine   NVARCHAR(50)    NOT NULL DEFAULT 'Grid',
        Version         INT             NOT NULL DEFAULT 1,
        Checksum        NVARCHAR(64)    NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),
        Description     NVARCHAR(500)   NULL,

        CONSTRAINT PK_Ui_Form PRIMARY KEY (Form_Id),
        CONSTRAINT UQ_Ui_Form_Code UNIQUE (Form_Code),
        CONSTRAINT FK_Ui_Form_Table FOREIGN KEY (Table_Id)
            REFERENCES dbo.Sys_Table (Table_Id)
    );

    CREATE INDEX IX_Ui_Form_Table
        ON dbo.Ui_Form (Table_Id, Is_Active);
END;
GO

-- ── Ui_Section ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Section', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Section
    (
        Section_Id      INT             IDENTITY(1,1) NOT NULL,
        Form_Id         INT             NOT NULL,
        Section_Code    NVARCHAR(100)   NOT NULL,
        Title_Key       NVARCHAR(150)   NULL,
        Order_No        INT             NOT NULL DEFAULT 0,
        Layout_Json     NVARCHAR(MAX)   NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Description     NVARCHAR(500)   NULL,

        CONSTRAINT PK_Ui_Section PRIMARY KEY (Section_Id),
        CONSTRAINT FK_Ui_Section_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id)
    );

    CREATE INDEX IX_Ui_Section_Form
        ON dbo.Ui_Section (Form_Id, Is_Active, Order_No);
    CREATE UNIQUE INDEX UQ_Ui_Section_Code
        ON dbo.Ui_Section (Form_Id, Section_Code) WHERE Is_Active = 1;
END;
GO

-- ── Ui_Field ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Field', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Field
    (
        Field_Id            INT             IDENTITY(1,1) NOT NULL,
        Form_Id             INT             NOT NULL,
        Section_Id          INT             NULL,
        Column_Id           INT             NOT NULL,
        Editor_Type         NVARCHAR(50)    NOT NULL,
        Label_Key           NVARCHAR(150)   NOT NULL,
        Placeholder_Key     NVARCHAR(150)   NULL,
        Tooltip_Key         NVARCHAR(150)   NULL,
        Is_Visible          BIT             NOT NULL DEFAULT 1,
        Is_ReadOnly         BIT             NOT NULL DEFAULT 0,
        Order_No            INT             NOT NULL DEFAULT 0,
        Control_Props_Json  NVARCHAR(MAX)   NULL,
        Version             INT             NOT NULL DEFAULT 1,
        Updated_At          DATETIME        NOT NULL DEFAULT GETDATE(),
        Description         NVARCHAR(500)   NULL,

        CONSTRAINT PK_Ui_Field PRIMARY KEY (Field_Id),
        CONSTRAINT FK_Ui_Field_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id),
        CONSTRAINT FK_Ui_Field_Section FOREIGN KEY (Section_Id)
            REFERENCES dbo.Ui_Section (Section_Id),
        CONSTRAINT FK_Ui_Field_Column FOREIGN KEY (Column_Id)
            REFERENCES dbo.Sys_Column (Column_Id)
    );

    CREATE INDEX IX_Ui_Field_Form
        ON dbo.Ui_Field (Form_Id, Is_Visible, Order_No);
END;
GO

-- ── Ui_Control_Map ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Ui_Control_Map', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ui_Control_Map
    (
        Editor_Type         NVARCHAR(50)    NOT NULL,
        Platform            NVARCHAR(50)    NOT NULL,
        Control_Name        NVARCHAR(100)   NOT NULL,
        Default_Props_Json  NVARCHAR(MAX)   NULL,

        CONSTRAINT PK_Ui_Control_Map PRIMARY KEY (Editor_Type, Platform)
    );
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: Validation (Val_*)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Val_Rule_Type ───────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Val_Rule_Type', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Val_Rule_Type
    (
        Rule_Type_Code  NVARCHAR(50)    NOT NULL,
        Param_Schema    NVARCHAR(MAX)   NULL,

        CONSTRAINT PK_Val_Rule_Type PRIMARY KEY (Rule_Type_Code)
    );
END;
GO

-- ── Val_Rule ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Val_Rule', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Val_Rule
    (
        Rule_Id         INT             IDENTITY(1,1) NOT NULL,
        Rule_Type_Code  NVARCHAR(50)    NOT NULL,
        Error_Key       NVARCHAR(150)   NOT NULL,
        Expression_Json NVARCHAR(MAX)   NULL,
        Condition_Expr  NVARCHAR(MAX)   NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Val_Rule PRIMARY KEY (Rule_Id),
        CONSTRAINT FK_Val_Rule_Type FOREIGN KEY (Rule_Type_Code)
            REFERENCES dbo.Val_Rule_Type (Rule_Type_Code),
        CONSTRAINT CHK_Val_Rule_HasExpression
            CHECK (Expression_Json IS NOT NULL OR Rule_Type_Code = 'Required')
    );
END;
GO

-- ── Val_Rule_Field ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Val_Rule_Field', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Val_Rule_Field
    (
        Rule_Field_Id   INT             IDENTITY(1,1) NOT NULL,
        Field_Id        INT             NOT NULL,
        Rule_Id         INT             NOT NULL,
        Order_No        INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_Val_Rule_Field PRIMARY KEY (Rule_Field_Id),
        CONSTRAINT FK_Val_Rule_Field_Field FOREIGN KEY (Field_Id)
            REFERENCES dbo.Ui_Field (Field_Id),
        CONSTRAINT FK_Val_Rule_Field_Rule FOREIGN KEY (Rule_Id)
            REFERENCES dbo.Val_Rule (Rule_Id),
        CONSTRAINT UQ_Val_Rule_Field UNIQUE (Field_Id, Rule_Id)
    );

    CREATE INDEX IX_Val_Rule_Field
        ON dbo.Val_Rule_Field (Field_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: Grammar / AST Engine (Gram_*)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Gram_Operator ───────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Gram_Operator', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gram_Operator
    (
        Operator_Symbol NVARCHAR(20)    NOT NULL,
        Operator_Type   NVARCHAR(50)    NOT NULL,
        Precedence      INT             NOT NULL DEFAULT 0,
        Description     NVARCHAR(255)   NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Gram_Operator PRIMARY KEY (Operator_Symbol)
    );
END;
GO

-- ── Gram_Function ───────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Gram_Function', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gram_Function
    (
        Function_Id     INT             IDENTITY(1,1) NOT NULL,
        Function_Code   NVARCHAR(100)   NOT NULL,
        Description     NVARCHAR(500)   NULL,
        Return_Net_Type NVARCHAR(50)    NOT NULL,
        Param_Count_Min INT             NOT NULL DEFAULT 0,
        Param_Count_Max INT             NOT NULL DEFAULT 0,
        Is_System       BIT             NOT NULL DEFAULT 1,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Gram_Function PRIMARY KEY (Function_Id),
        CONSTRAINT UQ_Gram_Function_Code UNIQUE (Function_Code)
    );
END;
GO

-- ── Gram_Function_Param ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Gram_Function_Param', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gram_Function_Param
    (
        Param_Id        INT             IDENTITY(1,1) NOT NULL,
        Function_Id     INT             NOT NULL,
        Param_Index     INT             NOT NULL,
        Param_Name      NVARCHAR(100)   NOT NULL,
        Net_Type        NVARCHAR(50)    NOT NULL,
        Is_Optional     BIT             NOT NULL DEFAULT 0,
        Default_Value   NVARCHAR(255)   NULL,

        CONSTRAINT PK_Gram_Function_Param PRIMARY KEY (Param_Id),
        CONSTRAINT FK_Gram_Function_Param FOREIGN KEY (Function_Id)
            REFERENCES dbo.Gram_Function (Function_Id),
        CONSTRAINT UQ_Gram_Function_Param UNIQUE (Function_Id, Param_Index)
    );

    CREATE INDEX IX_Gram_Function_Param
        ON dbo.Gram_Function_Param (Function_Id);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: Event Engine (Evt_*)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Evt_Trigger_Type ────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Evt_Trigger_Type', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evt_Trigger_Type
    (
        Trigger_Code    NVARCHAR(50)    NOT NULL,

        CONSTRAINT PK_Evt_Trigger_Type PRIMARY KEY (Trigger_Code)
    );
END;
GO

-- ── Evt_Action_Type ─────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Evt_Action_Type', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evt_Action_Type
    (
        Action_Code     NVARCHAR(50)    NOT NULL,
        Param_Schema    NVARCHAR(MAX)   NULL,

        CONSTRAINT PK_Evt_Action_Type PRIMARY KEY (Action_Code)
    );
END;
GO

-- ── Evt_Definition ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Evt_Definition', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evt_Definition
    (
        Event_Id        INT             IDENTITY(1,1) NOT NULL,
        Form_Id         INT             NOT NULL,
        Field_Id        INT             NULL,
        Trigger_Code    NVARCHAR(50)    NOT NULL,
        Condition_Expr  NVARCHAR(MAX)   NULL,
        Order_No        INT             NOT NULL DEFAULT 0,
        Is_Active       BIT             NOT NULL DEFAULT 1,
        Updated_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Evt_Definition PRIMARY KEY (Event_Id),
        CONSTRAINT FK_Evt_Definition_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id),
        CONSTRAINT FK_Evt_Definition_Field FOREIGN KEY (Field_Id)
            REFERENCES dbo.Ui_Field (Field_Id),
        CONSTRAINT FK_Evt_Definition_Trigger FOREIGN KEY (Trigger_Code)
            REFERENCES dbo.Evt_Trigger_Type (Trigger_Code)
    );

    CREATE INDEX IX_Evt_Definition_Field
        ON dbo.Evt_Definition (Field_Id, Trigger_Code, Is_Active)
        WHERE Field_Id IS NOT NULL;
    CREATE INDEX IX_Evt_Definition_Form
        ON dbo.Evt_Definition (Form_Id, Is_Active);
END;
GO

-- ── Evt_Action ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Evt_Action', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evt_Action
    (
        Action_Id           INT             IDENTITY(1,1) NOT NULL,
        Event_Id            INT             NOT NULL,
        Action_Code         NVARCHAR(50)    NOT NULL,
        Action_Param_Json   NVARCHAR(MAX)   NULL,
        Order_No            INT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_Evt_Action PRIMARY KEY (Action_Id),
        CONSTRAINT FK_Evt_Action_Event FOREIGN KEY (Event_Id)
            REFERENCES dbo.Evt_Definition (Event_Id),
        CONSTRAINT FK_Evt_Action_Type FOREIGN KEY (Action_Code)
            REFERENCES dbo.Evt_Action_Type (Action_Code)
    );

    CREATE INDEX IX_Evt_Action_Event
        ON dbo.Evt_Action (Event_Id, Order_No);
END;
GO

-- ── Evt_Execution_Log ───────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Evt_Execution_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evt_Execution_Log
    (
        Exec_Id         BIGINT          IDENTITY(1,1) NOT NULL,
        Event_Id        INT             NOT NULL,
        Form_Code       NVARCHAR(100)   NOT NULL,
        Trigger_Code    NVARCHAR(50)    NOT NULL,
        Condition_Result NVARCHAR(10)   NULL,
        Actions_Json    NVARCHAR(MAX)   NULL,
        Result_Json     NVARCHAR(MAX)   NULL,
        Is_Success      BIT             NOT NULL,
        Error_Message   NVARCHAR(MAX)   NULL,
        Duration_MS     BIGINT          NOT NULL DEFAULT 0,
        Correlation_Id  NVARCHAR(64)    NULL,
        Created_At      DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Evt_Execution_Log PRIMARY KEY (Exec_Id)
    );

    CREATE INDEX IX_Evt_Exec_Log_Corr
        ON dbo.Evt_Execution_Log (Correlation_Id);
    CREATE INDEX IX_Evt_Exec_Log_Created
        ON dbo.Evt_Execution_Log (Created_At);
    CREATE INDEX IX_Evt_Exec_Log_Event
        ON dbo.Evt_Execution_Log (Event_Id, Created_At);
END;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- MODULE: System - Dependency (depends on Ui_Form)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Sys_Dependency ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Dependency', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Dependency
    (
        Dependency_Id   INT             IDENTITY(1,1) NOT NULL,
        Source_Type     NVARCHAR(50)    NOT NULL,
        Source_Id       INT             NOT NULL,
        Target_Type     NVARCHAR(50)    NOT NULL,
        Target_Id       INT             NOT NULL,
        Form_Id         INT             NOT NULL,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Dependency PRIMARY KEY (Dependency_Id),
        CONSTRAINT FK_Sys_Dependency_Form FOREIGN KEY (Form_Id)
            REFERENCES dbo.Ui_Form (Form_Id),
        CONSTRAINT UQ_Sys_Dependency UNIQUE (Source_Type, Source_Id, Target_Type, Target_Id, Form_Id)
    );

    CREATE INDEX IX_Sys_Dependency_Source
        ON dbo.Sys_Dependency (Source_Type, Source_Id, Is_Active);
    CREATE INDEX IX_Sys_Dependency_Target
        ON dbo.Sys_Dependency (Target_Type, Target_Id);
END;
GO

PRINT N'Schema creation completed — 30 tables.';
GO
