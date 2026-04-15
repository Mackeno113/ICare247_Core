-- =============================================================================
-- File    : 011_add_rule_types.sql
-- Purpose : Thêm 2 rule type mới vào Val_Rule_Type:
--           - Length  : kiểm tra độ dài chuỗi trong khoảng min..max
--           - Compare : so sánh với field khác (value >= {OtherField})
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Val_Rule_Type', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Val_Rule_Type AS target
    USING (VALUES
        (
            'Length',
            N'{"type":"object","properties":{"min":{"type":"integer","minimum":0},"max":{"type":"integer","minimum":0}},"required":["min","max"]}'
        ),
        (
            'Compare',
            N'{"type":"object","properties":{"otherField":{"type":"string"},"operator":{"type":"string","enum":["==","!=",">",">=","<","<="]},"conditionExpression":{"type":"object"}},"required":["otherField","operator"]}'
        )
    ) AS source (Rule_Type_Code, Param_Schema)
    ON target.Rule_Type_Code = source.Rule_Type_Code
    WHEN NOT MATCHED THEN
        INSERT (Rule_Type_Code, Param_Schema) VALUES (source.Rule_Type_Code, source.Param_Schema)
    WHEN MATCHED THEN
        UPDATE SET Param_Schema = source.Param_Schema;
END;
GO

PRINT N'Migration 011 completed — Val_Rule_Type: Length, Compare added.';
GO
