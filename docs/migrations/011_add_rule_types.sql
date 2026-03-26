-- ============================================================
-- Migration 011: Thêm rule type Length và Compare
-- ============================================================
-- Ngày  : 2026-03-26
-- Lý do : ADR-011 — bổ sung 2 rule type phổ biến còn thiếu.
--
--  Length  — kiểm tra len(value) nằm trong khoảng [min, max]
--            Expression_Json tự sinh:
--            len({field}) >= {min} && len({field}) <= {max}
--
--  Compare — so sánh giá trị field này với field khác trong form
--            Expression_Json tự sinh:
--            {field} {op} {otherField}
--            VD: NgayNghiViec >= NgayVaoLam
--
-- Ghi chú:
--   Required vẫn giữ để backward compat với dữ liệu cũ.
--   ConfigStudio mới sẽ không tạo rule Required nữa (dùng
--   Ui_Field.Is_Required = 1 thay thế).
-- ============================================================

BEGIN TRANSACTION;

-- 1. Length rule type
IF NOT EXISTS (
    SELECT 1 FROM Val_Rule_Type WHERE Rule_Type_Code = 'Length'
)
BEGIN
    INSERT INTO Val_Rule_Type (Rule_Type_Code, Description, Requires_Expression, Is_Active)
    VALUES (
        'Length',
        N'Kiểm tra độ dài ký tự nằm trong khoảng [min, max]. Dùng cho nvarchar/varchar.',
        1,
        1
    );
    PRINT 'Val_Rule_Type: Length inserted';
END
ELSE
    PRINT 'Val_Rule_Type: Length already exists — skipped';

-- 2. Compare rule type
IF NOT EXISTS (
    SELECT 1 FROM Val_Rule_Type WHERE Rule_Type_Code = 'Compare'
)
BEGIN
    INSERT INTO Val_Rule_Type (Rule_Type_Code, Description, Requires_Expression, Is_Active)
    VALUES (
        'Compare',
        N'So sánh giá trị field này với giá trị của field khác trong cùng form. VD: NgayKetThuc >= NgayBatDau.',
        1,
        1
    );
    PRINT 'Val_Rule_Type: Compare inserted';
END
ELSE
    PRINT 'Val_Rule_Type: Compare already exists — skipped';

-- 3. Đánh dấu Required là deprecated (vẫn giữ — không xóa)
--    ConfigStudio sẽ hiển thị "(deprecated)" trong danh sách nếu cần
--    Không thay đổi dữ liệu hiện có

-- 4. Kiểm tra kết quả
SELECT Rule_Type_Code, Description, Requires_Expression, Is_Active
FROM Val_Rule_Type
ORDER BY Rule_Type_Code;

COMMIT TRANSACTION;
