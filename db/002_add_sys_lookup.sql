-- =============================================================================
-- File    : 002_add_sys_lookup.sql
-- Purpose : Thêm bảng Sys_Lookup — danh mục dùng chung cho các list tĩnh
--           (Gender, MaritalStatus, BloodType,...).
--           Cũng seed dữ liệu mẫu và fix Val_Rule_Type (001 dùng sai cột).
-- Note    : Idempotent — kiểm tra IF NOT EXISTS trước.
-- =============================================================================

USE [ICare247_Config];
GO

-- ── Sys_Lookup ───────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Lookup', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Lookup
    (
        Lookup_Id       INT             IDENTITY(1,1) NOT NULL,
        Tenant_Id       INT             NULL,
        Lookup_Code     NVARCHAR(50)    NOT NULL,
        Item_Code       NVARCHAR(50)    NOT NULL,
        Label_Key       NVARCHAR(200)   NOT NULL,
        Sort_Order      INT             NOT NULL DEFAULT 0,
        Is_Active       BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Lookup PRIMARY KEY (Lookup_Id),
        CONSTRAINT FK_Sys_Lookup_Tenant FOREIGN KEY (Tenant_Id)
            REFERENCES dbo.Sys_Tenant (Tenant_Id)
    );

    -- Unique: global lookups (Tenant_Id IS NULL)
    CREATE UNIQUE INDEX UQ_Sys_Lookup_Global
        ON dbo.Sys_Lookup (Lookup_Code, Item_Code) WHERE Tenant_Id IS NULL;

    -- Unique: per-tenant lookups
    CREATE UNIQUE INDEX UQ_Sys_Lookup_Tenant
        ON dbo.Sys_Lookup (Lookup_Code, Item_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;

    -- Query nhanh theo code
    CREATE INDEX IX_Sys_Lookup_Code
        ON dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Is_Active);
END;
GO

-- ── Seed: Sys_Language ────────────────────────────────────────────────────────
-- Hai ngôn ngữ mặc định: tiếng Việt (default) + tiếng Anh
IF OBJECT_ID('dbo.Sys_Language', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Sys_Language AS target
    USING (VALUES
        ('vi', N'Tiếng Việt', 1),
        ('en', N'English',    0)
    ) AS source (Lang_Code, Lang_Name, Is_Default)
    ON target.Lang_Code = source.Lang_Code
    WHEN NOT MATCHED THEN
        INSERT (Lang_Code, Lang_Name, Is_Default)
        VALUES (source.Lang_Code, source.Lang_Name, source.Is_Default)
    WHEN MATCHED THEN
        UPDATE SET Lang_Name = source.Lang_Name;
END;
GO

-- ── Fix Val_Rule_Type seed (001 dùng cột Description không tồn tại) ──────────
-- 001_seed_lookup_data.sql gọi INSERT (Rule_Type_Code, Description)
-- nhưng bảng chỉ có (Rule_Type_Code, Param_Schema) → seed đó fail silently.
-- Migration này seed lại đúng với Param_Schema.
IF OBJECT_ID('dbo.Val_Rule_Type', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Val_Rule_Type AS target
    USING (VALUES
        ('Required', N'{"type":"object","properties":{},"required":[]}'),
        ('Regex',    N'{"type":"object","properties":{"pattern":{"type":"string"}},"required":["pattern"]}'),
        ('Range',    N'{"type":"object","properties":{"min":{},"max":{}},"required":["min","max"]}'),
        ('Custom',   N'{"type":"object","properties":{"expression":{"type":"object"}},"required":["expression"]}')
    ) AS source (Rule_Type_Code, Param_Schema)
    ON target.Rule_Type_Code = source.Rule_Type_Code
    WHEN NOT MATCHED THEN
        INSERT (Rule_Type_Code, Param_Schema) VALUES (source.Rule_Type_Code, source.Param_Schema)
    WHEN MATCHED THEN
        UPDATE SET Param_Schema = source.Param_Schema;
END;
GO

-- ── Seed: Sys_Lookup — GENDER ────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Lookup', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Sys_Lookup AS target
    USING (VALUES
        (NULL, 'GENDER', 'NAM', 'common.gender.male',    1),
        (NULL, 'GENDER', 'NU',  'common.gender.female',  2),
        (NULL, 'GENDER', 'KXD', 'common.gender.unknown', 3)
    ) AS source (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order)
    ON  target.Lookup_Code = source.Lookup_Code
    AND target.Item_Code   = source.Item_Code
    AND target.Tenant_Id   IS NULL
    WHEN NOT MATCHED THEN
        INSERT (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order)
        VALUES (source.Tenant_Id, source.Lookup_Code, source.Item_Code,
                source.Label_Key, source.Sort_Order);
END;
GO

PRINT N'Migration 002 completed — Sys_Lookup created, Sys_Language seeded.';
GO
