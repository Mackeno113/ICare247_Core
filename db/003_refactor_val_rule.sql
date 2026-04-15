-- =============================================================================
-- File    : 003_refactor_val_rule.sql
-- Purpose : Refactor Val_Rule — bỏ bảng junction Val_Rule_Field,
--           gộp Field_Id trực tiếp vào Val_Rule.
--           Thêm cột Severity, Order_No. UNIQUE Error_Key.
-- Note    : Idempotent. Migrate data trước khi drop junction table.
-- ADR-011 : Required deprecated — Is_Required là cột DB trên Ui_Field.
-- =============================================================================

USE [ICare247_Config];
GO

-- BƯỚC 1: Thêm Field_Id (nullable tạm thời để migrate data)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Field_Id'
)
BEGIN
    ALTER TABLE dbo.Val_Rule ADD Field_Id INT NULL;
END;
GO

-- BƯỚC 2: Migrate data từ Val_Rule_Field → Val_Rule.Field_Id
IF OBJECT_ID('dbo.Val_Rule_Field', 'U') IS NOT NULL
BEGIN
    -- Lấy Field_Id đầu tiên (vì junction là many-to-many, ta lấy 1 field per rule)
    UPDATE vr
    SET    vr.Field_Id = vrf.Field_Id
    FROM   dbo.Val_Rule    vr
    JOIN   (
        SELECT Rule_Id, MIN(Field_Id) AS Field_Id
        FROM   dbo.Val_Rule_Field
        GROUP  BY Rule_Id
    ) vrf ON vrf.Rule_Id = vr.Rule_Id
    WHERE  vr.Field_Id IS NULL;
END;
GO

-- BƯỚC 3: Xóa rules không có field nào (orphaned)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Field_Id')
BEGIN
    -- Xóa FK từ Evt_Action trỏ vào Val_Rule nếu có (safety check)
    DELETE FROM dbo.Val_Rule WHERE Field_Id IS NULL;
END;
GO

-- BƯỚC 4: Đổi Field_Id thành NOT NULL + thêm FK
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Field_Id')
    AND NOT EXISTS (SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Field_Id'
          AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.Val_Rule ALTER COLUMN Field_Id INT NOT NULL;
END;
GO

-- FK Val_Rule → Ui_Field
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Val_Rule_Field' AND parent_object_id = OBJECT_ID('dbo.Val_Rule')
)
BEGIN
    ALTER TABLE dbo.Val_Rule
        ADD CONSTRAINT FK_Val_Rule_Field
            FOREIGN KEY (Field_Id) REFERENCES dbo.Ui_Field (Field_Id);
END;
GO

-- BƯỚC 5: Thêm cột Severity
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Severity')
BEGIN
    ALTER TABLE dbo.Val_Rule ADD Severity NVARCHAR(20) NOT NULL DEFAULT 'Error';
END;
GO

-- BƯỚC 6: Thêm cột Order_No vào Val_Rule
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Val_Rule') AND name = 'Order_No')
BEGIN
    ALTER TABLE dbo.Val_Rule ADD Order_No INT NOT NULL DEFAULT 0;
END;
GO

-- BƯỚC 7: UNIQUE constraint trên Error_Key
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Val_Rule_ErrorKey' AND object_id = OBJECT_ID('dbo.Val_Rule')
)
BEGIN
    CREATE UNIQUE INDEX UX_Val_Rule_ErrorKey ON dbo.Val_Rule (Error_Key);
END;
GO

-- Index query nhanh theo Field_Id
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Val_Rule_Field_Id' AND object_id = OBJECT_ID('dbo.Val_Rule')
)
BEGIN
    CREATE INDEX IX_Val_Rule_Field_Id ON dbo.Val_Rule (Field_Id, Order_No);
END;
GO

-- BƯỚC 8: Cập nhật CHECK constraint CHK_Val_Rule_HasExpression
--         ADR-011: Required deprecated, mọi rule đều cần Expression_Json
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CHK_Val_Rule_HasExpression' AND parent_object_id = OBJECT_ID('dbo.Val_Rule')
)
BEGIN
    ALTER TABLE dbo.Val_Rule DROP CONSTRAINT CHK_Val_Rule_HasExpression;
END;
GO

ALTER TABLE dbo.Val_Rule
    ADD CONSTRAINT CHK_Val_Rule_HasExpression
        CHECK (Expression_Json IS NOT NULL);
GO

-- BƯỚC 9: Drop bảng junction Val_Rule_Field
IF OBJECT_ID('dbo.Val_Rule_Field', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Val_Rule_Field;
END;
GO

PRINT N'Migration 003 completed — Val_Rule refactored (Field_Id direct, junction dropped).';
GO
