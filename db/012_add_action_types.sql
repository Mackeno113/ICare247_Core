-- =============================================================================
-- File    : 012_add_action_types.sql
-- Purpose : Thêm 3 action type mới vào Evt_Action_Type:
--           - SET_ENABLED  : enable/disable field theo condition
--           - CLEAR_VALUE  : xóa giá trị field
--           - SHOW_MESSAGE : hiển thị thông báo (info/warn/error)
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Evt_Action_Type', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Evt_Action_Type AS target
    USING (VALUES
        (
            'SET_ENABLED',
            N'{"type":"object","properties":{"targetField":{"type":"string"},"conditionExpression":{"type":"object"}},"required":["targetField","conditionExpression"]}'
        ),
        (
            'CLEAR_VALUE',
            N'{"type":"object","properties":{"targetField":{"type":"string"}},"required":["targetField"]}'
        ),
        (
            'SHOW_MESSAGE',
            N'{"type":"object","properties":{"messageKey":{"type":"string"},"severity":{"type":"string","enum":["info","warn","error"]}},"required":["messageKey","severity"]}'
        )
    ) AS source (Action_Code, Param_Schema)
    ON target.Action_Code = source.Action_Code
    WHEN NOT MATCHED THEN
        INSERT (Action_Code, Param_Schema) VALUES (source.Action_Code, source.Param_Schema)
    WHEN MATCHED THEN
        UPDATE SET Param_Schema = source.Param_Schema;
END;
GO

PRINT N'Migration 012 completed — Evt_Action_Type: SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE added.';
GO
