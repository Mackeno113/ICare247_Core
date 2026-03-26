-- ============================================================
-- Migration 012: Thêm action type SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE
-- ============================================================
-- Ngày  : 2026-03-26
-- Lý do : ADR-012 — bổ sung 3 action type còn thiếu trong Event Engine.
--
--  SET_ENABLED   — bật/tắt Is_Enabled của field theo điều kiện.
--                  Disabled = grayout + không submit.
--                  Param: { "targetField": "string",
--                           "conditionExpression": <ast> }
--
--  CLEAR_VALUE   — xóa giá trị field khi field phụ thuộc thay đổi.
--                  VD: đổi Tỉnh → xóa Huyện.
--                  Param: { "targetField": "string" }
--
--  SHOW_MESSAGE  — hiển thị toast/popup thông báo cho user.
--                  Param: { "messageKey": "string",
--                           "severity": "info|warn|error",
--                           "conditionExpression": <ast> }   ← optional
--
-- Action đã có: SET_VALUE, SET_VISIBLE, SET_REQUIRED, SET_READONLY,
--               RELOAD_OPTIONS, TRIGGER_VALIDATION
-- ============================================================

BEGIN TRANSACTION;

-- 1. SET_ENABLED
IF NOT EXISTS (
    SELECT 1 FROM Evt_Action_Type WHERE Action_Code = 'SET_ENABLED'
)
BEGIN
    INSERT INTO Evt_Action_Type (Action_Code, Description, Param_Schema_Json, Is_Active)
    VALUES (
        'SET_ENABLED',
        N'Bật/tắt trạng thái enabled của field. Disabled = grayout + không tính vào submit.',
        N'{"targetField":"string","conditionExpression":"ast"}',
        1
    );
    PRINT 'Evt_Action_Type: SET_ENABLED inserted';
END
ELSE
    PRINT 'Evt_Action_Type: SET_ENABLED already exists — skipped';

-- 2. CLEAR_VALUE
IF NOT EXISTS (
    SELECT 1 FROM Evt_Action_Type WHERE Action_Code = 'CLEAR_VALUE'
)
BEGIN
    INSERT INTO Evt_Action_Type (Action_Code, Description, Param_Schema_Json, Is_Active)
    VALUES (
        'CLEAR_VALUE',
        N'Xóa giá trị của field khi field phụ thuộc thay đổi. VD: clear Huyện khi đổi Tỉnh.',
        N'{"targetField":"string"}',
        1
    );
    PRINT 'Evt_Action_Type: CLEAR_VALUE inserted';
END
ELSE
    PRINT 'Evt_Action_Type: CLEAR_VALUE already exists — skipped';

-- 3. SHOW_MESSAGE
IF NOT EXISTS (
    SELECT 1 FROM Evt_Action_Type WHERE Action_Code = 'SHOW_MESSAGE'
)
BEGIN
    INSERT INTO Evt_Action_Type (Action_Code, Description, Param_Schema_Json, Is_Active)
    VALUES (
        'SHOW_MESSAGE',
        N'Hiển thị thông báo toast/popup cho user. Severity: info | warn | error.',
        N'{"messageKey":"string","severity":"info|warn|error","conditionExpression":"ast|optional"}',
        1
    );
    PRINT 'Evt_Action_Type: SHOW_MESSAGE inserted';
END
ELSE
    PRINT 'Evt_Action_Type: SHOW_MESSAGE already exists — skipped';

-- 4. Kiểm tra kết quả
SELECT Action_Code, Description, Is_Active
FROM Evt_Action_Type
ORDER BY Action_Code;

COMMIT TRANSACTION;
