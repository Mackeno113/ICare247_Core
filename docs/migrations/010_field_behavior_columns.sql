-- ============================================================
-- Migration 010: Thêm Is_Required và Is_Enabled vào Ui_Field
-- ============================================================
-- Ngày     : 2026-03-26
-- Lý do    : ADR-010 — Is_Required và Is_Enabled phải là cột tĩnh
--            trong Ui_Field, nhất quán với Is_Visible và Is_ReadOnly.
--
--  Is_Visible  (đã có) — field có hiển thị không
--  Is_ReadOnly (đã có) — hiển thị nhưng không sửa được; vẫn submit
--  Is_Required (MỚI)  — không cho phép để trống khi submit
--  Is_Enabled  (MỚI)  — field có được tương tác không;
--                        disabled = grayout + KHÔNG submit
--
-- Hành vi ĐỘNG (theo điều kiện) → Evt_Action:
--   SET_VISIBLE, SET_READONLY, SET_REQUIRED, SET_ENABLED
--
-- Lưu ý:
--   Val_Rule type 'Required' vẫn giữ trong schema để backward compat
--   nhưng ConfigStudio sẽ không tạo mới loại rule này nữa.
--   Dùng Is_Required = 1 cho "luôn bắt buộc";
--   dùng SET_REQUIRED event cho "bắt buộc theo điều kiện".
-- ============================================================

BEGIN TRANSACTION;

-- 1. Thêm Is_Required
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('Ui_Field')
      AND  name      = 'Is_Required'
)
BEGIN
    ALTER TABLE Ui_Field
        ADD Is_Required bit NOT NULL DEFAULT 0;

    PRINT 'Ui_Field.Is_Required added (bit NOT NULL DEFAULT 0)';
END
ELSE
    PRINT 'Ui_Field.Is_Required already exists — skipped';

-- 2. Thêm Is_Enabled
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('Ui_Field')
      AND  name      = 'Is_Enabled'
)
BEGIN
    ALTER TABLE Ui_Field
        ADD Is_Enabled bit NOT NULL DEFAULT 1;

    PRINT 'Ui_Field.Is_Enabled added (bit NOT NULL DEFAULT 1)';
END
ELSE
    PRINT 'Ui_Field.Is_Enabled already exists — skipped';

-- 3. Kiểm tra kết quả
SELECT
    name,
    TYPE_NAME(user_type_id) AS data_type,
    is_nullable,
    OBJECT_DEFINITION(default_object_id) AS default_value
FROM sys.columns
WHERE object_id = OBJECT_ID('Ui_Field')
  AND name IN ('Is_Visible', 'Is_ReadOnly', 'Is_Required', 'Is_Enabled')
ORDER BY column_id;

COMMIT TRANSACTION;
