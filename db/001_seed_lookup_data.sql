-- =============================================================================
-- File    : 001_seed_lookup_data.sql
-- Purpose : Seed dữ liệu lookup tables cho ICare247_Config database.
-- Note    : Chạy sau khi tạo schema. Idempotent — dùng MERGE để tránh duplicate.
-- =============================================================================

-- ── Tenant mặc định ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Tenant WHERE Tenant_Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Sys_Tenant ON;
    INSERT INTO dbo.Sys_Tenant (Tenant_Id, Tenant_Code, Tenant_Name, Is_Active)
    VALUES (1, 'DEFAULT', N'Tenant mặc định', 1);
    SET IDENTITY_INSERT dbo.Sys_Tenant OFF;
END;

-- ── Event Trigger Types ─────────────────────────────────────────────────────
MERGE dbo.Evt_Trigger_Type AS target
USING (VALUES
    ('OnChange'),
    ('OnBlur'),
    ('OnLoad'),
    ('OnSubmit'),
    ('OnSectionToggle')
) AS source (Trigger_Code)
ON target.Trigger_Code = source.Trigger_Code
WHEN NOT MATCHED THEN
    INSERT (Trigger_Code) VALUES (source.Trigger_Code);

-- ── Event Action Types ──────────────────────────────────────────────────────
MERGE dbo.Evt_Action_Type AS target
USING (VALUES
    ('SET_VALUE',           N'{"type":"object","properties":{"targetField":{"type":"string"},"valueExpression":{"type":"object"}},"required":["targetField","valueExpression"]}'),
    ('SET_VISIBLE',         N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('SET_REQUIRED',        N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('SET_READONLY',        N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'),
    ('RELOAD_OPTIONS',      N'{"type":"object","properties":{"targetField":{"type":"string"},"apiEndpoint":{"type":"string"},"dependsOn":{"type":"array","items":{"type":"string"}}},"required":["targetField","apiEndpoint"]}'),
    ('TRIGGER_VALIDATION',  N'{"type":"object","properties":{"targetFields":{"type":"array","items":{"type":"string"}}},"required":["targetFields"]}')
) AS source (Action_Code, Param_Schema)
ON target.Action_Code = source.Action_Code
WHEN NOT MATCHED THEN
    INSERT (Action_Code, Param_Schema) VALUES (source.Action_Code, source.Param_Schema)
WHEN MATCHED THEN
    UPDATE SET Param_Schema = source.Param_Schema;

-- ── Validation Rule Types (lookup) ──────────────────────────────────────────
-- Nếu có bảng Val_Rule_Type
IF OBJECT_ID('dbo.Val_Rule_Type', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Val_Rule_Type AS target
    USING (VALUES
        ('Required',    N'Bắt buộc nhập'),
        ('Regex',       N'Kiểm tra regex pattern'),
        ('Range',       N'Kiểm tra khoảng giá trị'),
        ('Custom',      N'Expression tùy chỉnh')
    ) AS source (Rule_Type_Code, Description)
    ON target.Rule_Type_Code = source.Rule_Type_Code
    WHEN NOT MATCHED THEN
        INSERT (Rule_Type_Code, Description) VALUES (source.Rule_Type_Code, source.Description);
END;

-- ── Grammar Functions (whitelist) ───────────────────────────────────────────
IF OBJECT_ID('dbo.Gram_Function', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Gram_Function AS target
    USING (VALUES
        -- String functions
        ('len',         'string',       1, 1, N'Độ dài chuỗi'),
        ('trim',        'string',       1, 1, N'Xóa khoảng trắng đầu cuối'),
        ('upper',       'string',       1, 1, N'Chuyển HOA'),
        ('lower',       'string',       1, 1, N'Chuyển thường'),
        ('concat',      'string',       2, 99, N'Nối chuỗi'),
        ('substring',   'string',       2, 3, N'Cắt chuỗi con'),
        ('contains',    'string',       2, 2, N'Kiểm tra chứa chuỗi con'),
        ('startsWith',  'string',       2, 2, N'Kiểm tra bắt đầu bằng'),
        ('endsWith',    'string',       2, 2, N'Kiểm tra kết thúc bằng'),
        -- Math functions
        ('round',       'math',         1, 2, N'Làm tròn'),
        ('floor',       'math',         1, 1, N'Làm tròn xuống'),
        ('ceil',        'math',         1, 1, N'Làm tròn lên'),
        ('abs',         'math',         1, 1, N'Giá trị tuyệt đối'),
        ('min',         'math',         2, 2, N'Giá trị nhỏ nhất'),
        ('max',         'math',         2, 2, N'Giá trị lớn nhất'),
        -- Logic functions
        ('iif',         'logic',        3, 3, N'If-then-else'),
        ('isNull',      'logic',        1, 1, N'Kiểm tra null'),
        ('coalesce',    'logic',        2, 99, N'Giá trị đầu tiên không null'),
        -- Date functions
        ('today',       'date',         0, 0, N'Ngày hôm nay'),
        ('now',         'date',         0, 0, N'Thời gian hiện tại'),
        ('toDate',      'date',         1, 1, N'Chuyển sang DateTime'),
        ('dateDiff',    'date',         3, 3, N'Khoảng cách giữa 2 ngày'),
        -- Conversion functions
        ('toNumber',    'conversion',   1, 1, N'Chuyển sang số'),
        ('toString',    'conversion',   1, 1, N'Chuyển sang chuỗi'),
        ('toBool',      'conversion',   1, 1, N'Chuyển sang boolean')
    ) AS source (Func_Name, Category, Min_Params, Max_Params, Description)
    ON target.Func_Name = source.Func_Name
    WHEN NOT MATCHED THEN
        INSERT (Func_Name, Category, Min_Params, Max_Params, Description)
        VALUES (source.Func_Name, source.Category, source.Min_Params, source.Max_Params, source.Description)
    WHEN MATCHED THEN
        UPDATE SET Category = source.Category,
                   Min_Params = source.Min_Params,
                   Max_Params = source.Max_Params,
                   Description = source.Description;
END;

PRINT N'Seed lookup data completed successfully.';
GO
