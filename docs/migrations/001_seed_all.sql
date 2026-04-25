-- =============================================================================
-- File    : 001_seed_all.sql
-- Purpose : Seed toàn bộ dữ liệu tham chiếu — trạng thái HIỆN TẠI (canonical).
-- Version : 2026-04-25 (tổng hợp từ seed migrations 001–004, 011–012)
-- Note    : Idempotent — dùng MERGE. Chạy sau 000_create_schema.sql.
-- =============================================================================

USE [ICare247_Config];
GO

-- ── Tenant mặc định ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Tenant WHERE Tenant_Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Sys_Tenant ON;
    INSERT INTO dbo.Sys_Tenant (Tenant_Id, Tenant_Code, Tenant_Name, Is_Active)
    VALUES (1, 'DEFAULT', N'Tenant mặc định', 1);
    SET IDENTITY_INSERT dbo.Sys_Tenant OFF;
END;
GO

-- ── Ngôn ngữ ─────────────────────────────────────────────────────────────────
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
GO

-- ── Event Trigger Types ───────────────────────────────────────────────────────
MERGE dbo.Evt_Trigger_Type AS target
USING (VALUES
    ('OnChange'),
    ('OnBlur'),
    ('OnLoad'),
    ('OnSubmit'),
    ('OnSectionToggle')
) AS source (Trigger_Code)
ON target.Trigger_Code = source.Trigger_Code
WHEN NOT MATCHED THEN INSERT (Trigger_Code) VALUES (source.Trigger_Code);
GO

-- ── Event Action Types ────────────────────────────────────────────────────────
-- Bao gồm 3 types mới từ migration 012: SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE
MERGE dbo.Evt_Action_Type AS target
USING (VALUES
    ('SET_VALUE',           N'{"type":"object","properties":{"targetField":{"type":"string"},"valueExpression":{"type":"object"}},"required":["targetField","valueExpression"]}'),
    ('SET_VISIBLE',         N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('SET_REQUIRED',        N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('SET_READONLY',        N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('SET_ENABLED',         N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('CLEAR_VALUE',         N'{"type":"object","properties":{"targetField":{"type":"string"}},"required":["targetField"]}'),
    ('SHOW_MESSAGE',        N'{"type":"object","properties":{"messageKey":{"type":"string"},"severity":{"type":"string","enum":["info","warn","error"]}},"required":["messageKey","severity"]}'),
    ('RELOAD_OPTIONS',      N'{"type":"object","properties":{"targetField":{"type":"string"},"apiEndpoint":{"type":"string"},"dependsOn":{"type":"array","items":{"type":"string"}}},"required":["targetField","apiEndpoint"]}'),
    ('TRIGGER_VALIDATION',  N'{"type":"object","properties":{"targetFields":{"type":"array","items":{"type":"string"}}},"required":["targetFields"]}')
) AS source (Action_Code, Param_Schema)
ON target.Action_Code = source.Action_Code
WHEN NOT MATCHED THEN
    INSERT (Action_Code, Param_Schema) VALUES (source.Action_Code, source.Param_Schema)
WHEN MATCHED THEN
    UPDATE SET Param_Schema = source.Param_Schema;
GO

-- ── Validation Rule Types ─────────────────────────────────────────────────────
-- Bao gồm Length và Compare từ migration 011
-- Required deprecated (ADR-011) — giữ lại để backward compat với data cũ
MERGE dbo.Val_Rule_Type AS target
USING (VALUES
    ('Required', N'{"type":"object","properties":{},"required":[]}'),
    ('Regex',    N'{"type":"object","properties":{"pattern":{"type":"string"}},"required":["pattern"]}'),
    ('Range',    N'{"type":"object","properties":{"min":{},"max":{}},"required":["min","max"]}'),
    ('Custom',   N'{"type":"object","properties":{"expression":{"type":"object"}},"required":["expression"]}'),
    ('Length',   N'{"type":"object","properties":{"min":{"type":"integer","minimum":0},"max":{"type":"integer","minimum":0}},"required":["min","max"]}'),
    ('Compare',  N'{"type":"object","properties":{"otherField":{"type":"string"},"operator":{"type":"string","enum":["==","!=",">",">=","<","<="]},"conditionExpression":{"type":"object"}},"required":["otherField","operator"]}')
) AS source (Rule_Type_Code, Param_Schema)
ON target.Rule_Type_Code = source.Rule_Type_Code
WHEN NOT MATCHED THEN
    INSERT (Rule_Type_Code, Param_Schema) VALUES (source.Rule_Type_Code, source.Param_Schema)
WHEN MATCHED THEN
    UPDATE SET Param_Schema = source.Param_Schema;
GO

-- ── Grammar Functions (25 functions) ─────────────────────────────────────────
MERGE dbo.Gram_Function AS target
USING (VALUES
    -- String
    ('len',        'string',   1, 1,  N'Độ dài chuỗi'),
    ('trim',       'string',   1, 1,  N'Xóa khoảng trắng đầu cuối'),
    ('upper',      'string',   1, 1,  N'Chuyển HOA'),
    ('lower',      'string',   1, 1,  N'Chuyển thường'),
    ('concat',     'string',   2, 0,  N'Nối chuỗi (variadic)'),
    ('substring',  'string',   2, 3,  N'Cắt chuỗi con'),
    ('contains',   'string',   2, 2,  N'Kiểm tra chứa chuỗi con'),
    ('startsWith', 'string',   2, 2,  N'Kiểm tra bắt đầu bằng'),
    ('endsWith',   'string',   2, 2,  N'Kiểm tra kết thúc bằng'),
    -- Math
    ('round',      'decimal',  1, 2,  N'Làm tròn số'),
    ('floor',      'decimal',  1, 1,  N'Làm tròn xuống'),
    ('ceil',       'decimal',  1, 1,  N'Làm tròn lên'),
    ('abs',        'decimal',  1, 1,  N'Giá trị tuyệt đối'),
    ('min',        'decimal',  2, 2,  N'Giá trị nhỏ nhất'),
    ('max',        'decimal',  2, 2,  N'Giá trị lớn nhất'),
    -- Logic
    ('iif',        'object',   3, 3,  N'If-then-else'),
    ('isNull',     'bool',     1, 1,  N'Kiểm tra null'),
    ('coalesce',   'object',   2, 0,  N'Giá trị đầu tiên không null'),
    -- Date
    ('today',      'datetime', 0, 0,  N'Ngày hôm nay'),
    ('now',        'datetime', 0, 0,  N'Thời gian hiện tại'),
    ('toDate',     'datetime', 1, 1,  N'Chuyển chuỗi → DateTime'),
    ('dateDiff',   'int',      3, 3,  N'Khoảng cách giữa 2 ngày theo đơn vị'),
    -- Conversion
    ('toNumber',   'decimal',  1, 1,  N'Chuyển sang số'),
    ('toString',   'string',   1, 1,  N'Chuyển sang chuỗi'),
    ('toBool',     'bool',     1, 1,  N'Chuyển sang boolean')
) AS source (Function_Code, Return_Net_Type, Param_Count_Min, Param_Count_Max, Description)
ON target.Function_Code = source.Function_Code
WHEN NOT MATCHED THEN
    INSERT (Function_Code, Return_Net_Type, Param_Count_Min, Param_Count_Max, Description, Is_System, Is_Active)
    VALUES (source.Function_Code, source.Return_Net_Type, source.Param_Count_Min,
            source.Param_Count_Max, source.Description, 1, 1)
WHEN MATCHED THEN
    UPDATE SET Return_Net_Type = source.Return_Net_Type,
               Param_Count_Min = source.Param_Count_Min,
               Param_Count_Max = source.Param_Count_Max,
               Description     = source.Description;
GO

-- ── Grammar Operators ─────────────────────────────────────────────────────────
MERGE dbo.Gram_Operator AS target
USING (VALUES
    ('+',  'Arithmetic', 10, N'Cộng / nối chuỗi'),
    ('-',  'Arithmetic', 10, N'Trừ'),
    ('*',  'Arithmetic', 20, N'Nhân'),
    ('/',  'Arithmetic', 20, N'Chia'),
    ('%',  'Arithmetic', 20, N'Chia lấy dư'),
    ('==', 'Comparison',  5, N'Bằng'),
    ('!=', 'Comparison',  5, N'Khác'),
    ('>',  'Comparison',  5, N'Lớn hơn'),
    ('>=', 'Comparison',  5, N'Lớn hơn hoặc bằng'),
    ('<',  'Comparison',  5, N'Nhỏ hơn'),
    ('<=', 'Comparison',  5, N'Nhỏ hơn hoặc bằng'),
    ('&&', 'Logical',     3, N'AND'),
    ('||', 'Logical',     2, N'OR'),
    ('!',  'Logical',    30, N'NOT (unary)')
) AS source (Operator_Symbol, Operator_Type, Precedence, Description)
ON target.Operator_Symbol = source.Operator_Symbol
WHEN NOT MATCHED THEN
    INSERT (Operator_Symbol, Operator_Type, Precedence, Description, Is_Active)
    VALUES (source.Operator_Symbol, source.Operator_Type, source.Precedence, source.Description, 1)
WHEN MATCHED THEN
    UPDATE SET Operator_Type = source.Operator_Type,
               Precedence    = source.Precedence,
               Description   = source.Description;
GO

-- ── Sys_Lookup — GENDER ───────────────────────────────────────────────────────
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
GO

-- ── Sys_Resource — i18n keys ──────────────────────────────────────────────────
MERGE dbo.Sys_Resource AS target
USING (VALUES
    ('common.gender.male',    'vi', N'Nam'),
    ('common.gender.male',    'en', N'Male'),
    ('common.gender.female',  'vi', N'Nữ'),
    ('common.gender.female',  'en', N'Female'),
    ('common.gender.unknown', 'vi', N'Không xác định'),
    ('common.gender.unknown', 'en', N'Unknown')
) AS source (Resource_Key, Lang_Code, Resource_Value)
ON  target.Resource_Key = source.Resource_Key
AND target.Lang_Code    = source.Lang_Code
WHEN NOT MATCHED THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value)
    VALUES (source.Resource_Key, source.Lang_Code, source.Resource_Value)
WHEN MATCHED THEN
    UPDATE SET Resource_Value = source.Resource_Value;
GO

PRINT N'Seed completed — Tenant, Language, TriggerTypes, ActionTypes(9), RuleTypes(6), Functions(25), Operators(14), Lookup GENDER, Resource i18n.';
GO
