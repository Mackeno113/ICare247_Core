-- =============================================================================
-- File    : 004_seed_reference_data.sql
-- Purpose : Seed đúng dữ liệu tham chiếu bị thiếu/sai từ 001:
--           - Gram_Function  (001 dùng sai tên cột)
--           - Gram_Operator  (001 không seed)
--           - Sys_Resource   (i18n keys mẫu cho GENDER lookup)
-- Note    : Idempotent — dùng MERGE.
-- =============================================================================

USE [ICare247_Config];
GO

-- ── Gram_Function — seed với đúng schema ────────────────────────────────────
-- 001_seed_lookup_data.sql dùng cột (Func_Name, Category, Min_Params, Max_Params)
-- Thực tế schema dùng (Function_Code, Return_Net_Type, Param_Count_Min, Param_Count_Max)
IF OBJECT_ID('dbo.Gram_Function', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Gram_Function AS target
    USING (VALUES
        -- String functions
        ('len',        'string',     1,  1,  N'Độ dài chuỗi'),
        ('trim',       'string',     1,  1,  N'Xóa khoảng trắng đầu cuối'),
        ('upper',      'string',     1,  1,  N'Chuyển HOA'),
        ('lower',      'string',     1,  1,  N'Chuyển thường'),
        ('concat',     'string',     2,  0,  N'Nối chuỗi (variadic)'),
        ('substring',  'string',     2,  3,  N'Cắt chuỗi con'),
        ('contains',   'string',     2,  2,  N'Kiểm tra chứa chuỗi con'),
        ('startsWith', 'string',     2,  2,  N'Kiểm tra bắt đầu bằng'),
        ('endsWith',   'string',     2,  2,  N'Kiểm tra kết thúc bằng'),
        -- Math functions
        ('round',      'decimal',    1,  2,  N'Làm tròn số'),
        ('floor',      'decimal',    1,  1,  N'Làm tròn xuống'),
        ('ceil',       'decimal',    1,  1,  N'Làm tròn lên'),
        ('abs',        'decimal',    1,  1,  N'Giá trị tuyệt đối'),
        ('min',        'decimal',    2,  2,  N'Giá trị nhỏ nhất'),
        ('max',        'decimal',    2,  2,  N'Giá trị lớn nhất'),
        -- Logic functions
        ('iif',        'object',     3,  3,  N'If-then-else'),
        ('isNull',     'bool',       1,  1,  N'Kiểm tra null'),
        ('coalesce',   'object',     2,  0,  N'Giá trị đầu tiên không null'),
        -- Date functions
        ('today',      'datetime',   0,  0,  N'Ngày hôm nay (không có giờ)'),
        ('now',        'datetime',   0,  0,  N'Thời gian hiện tại'),
        ('toDate',     'datetime',   1,  1,  N'Chuyển chuỗi → DateTime'),
        ('dateDiff',   'int',        3,  3,  N'Khoảng cách giữa 2 ngày theo đơn vị'),
        -- Conversion functions
        ('toNumber',   'decimal',    1,  1,  N'Chuyển sang số'),
        ('toString',   'string',     1,  1,  N'Chuyển sang chuỗi'),
        ('toBool',     'bool',       1,  1,  N'Chuyển sang boolean')
    ) AS source (Function_Code, Return_Net_Type, Param_Count_Min, Param_Count_Max, Description)
    ON target.Function_Code = source.Function_Code
    WHEN NOT MATCHED THEN
        INSERT (Function_Code, Return_Net_Type, Param_Count_Min, Param_Count_Max, Description, Is_System, Is_Active)
        VALUES (source.Function_Code, source.Return_Net_Type, source.Param_Count_Min,
                source.Param_Count_Max, source.Description, 1, 1)
    WHEN MATCHED THEN
        UPDATE SET
            Return_Net_Type = source.Return_Net_Type,
            Param_Count_Min = source.Param_Count_Min,
            Param_Count_Max = source.Param_Count_Max,
            Description     = source.Description;
END;
GO

-- ── Gram_Operator — seed ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Gram_Operator', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Gram_Operator AS target
    USING (VALUES
        -- Arithmetic
        ('+',  'Arithmetic',  10, N'Cộng / nối chuỗi'),
        ('-',  'Arithmetic',  10, N'Trừ'),
        ('*',  'Arithmetic',  20, N'Nhân'),
        ('/',  'Arithmetic',  20, N'Chia'),
        ('%',  'Arithmetic',  20, N'Chia lấy dư'),
        -- Comparison
        ('==', 'Comparison',   5, N'Bằng'),
        ('!=', 'Comparison',   5, N'Khác'),
        ('>',  'Comparison',   5, N'Lớn hơn'),
        ('>=', 'Comparison',   5, N'Lớn hơn hoặc bằng'),
        ('<',  'Comparison',   5, N'Nhỏ hơn'),
        ('<=', 'Comparison',   5, N'Nhỏ hơn hoặc bằng'),
        -- Logical
        ('&&', 'Logical',      3, N'AND'),
        ('||', 'Logical',      2, N'OR'),
        ('!',  'Logical',     30, N'NOT (unary)')
    ) AS source (Operator_Symbol, Operator_Type, Precedence, Description)
    ON target.Operator_Symbol = source.Operator_Symbol
    WHEN NOT MATCHED THEN
        INSERT (Operator_Symbol, Operator_Type, Precedence, Description, Is_Active)
        VALUES (source.Operator_Symbol, source.Operator_Type, source.Precedence, source.Description, 1)
    WHEN MATCHED THEN
        UPDATE SET
            Operator_Type = source.Operator_Type,
            Precedence    = source.Precedence,
            Description   = source.Description;
END;
GO

-- ── Sys_Resource — i18n keys cho GENDER lookup ───────────────────────────────
IF OBJECT_ID('dbo.Sys_Resource', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Sys_Language', 'U') IS NOT NULL
BEGIN
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
END;
GO

PRINT N'Migration 004 completed — Gram_Function, Gram_Operator, Sys_Resource seeded.';
GO
