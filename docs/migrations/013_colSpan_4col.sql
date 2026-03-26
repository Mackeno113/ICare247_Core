-- ============================================================
-- Migration 013: Đổi Col_Span từ 3-column sang 4-column grid
-- ============================================================
-- Ngày     : 2026-03-26
-- Lý do    : ADR-013 — Full HD layout 4-column cho form y tế.
--            Màn hình hiện tại full HD → 4 cột tận dụng tốt hơn.
--            Options: 1/4, 2/4(half), 3/4, 4/4(full)
--            Mapping từ 3-col sang 4-col:
--              Col_Span = 1 → 1  (narrow ~1/3 → ~1/4, giữ nguyên)
--              Col_Span = 2 → 2  (2/3 → half, gần tương đương)
--              Col_Span = 3 → 4  (full → full, đổi từ 3 thành 4)
-- ============================================================

BEGIN TRANSACTION;

-- ── 1. Migrate data trước khi đổi constraint ──────────────
-- Col_Span = 3 (full width cũ) → 4 (full width mới)
UPDATE dbo.Ui_Field
SET    Col_Span = 4
WHERE  Col_Span = 3;

PRINT CONCAT('Migrated ', @@ROWCOUNT, ' rows: Col_Span 3 → 4 (full width)');

-- ── 2. Drop constraint cũ ─────────────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  parent_object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND  name             = N'CHK_Ui_Field_ColSpan'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        DROP CONSTRAINT CHK_Ui_Field_ColSpan;
    PRINT 'CHK_Ui_Field_ColSpan dropped';
END

-- ── 3. Thêm constraint mới ────────────────────────────────
-- 1 = 1/4 width  (narrow, field ngắn: mã, số, ngày)
-- 2 = 2/4 width  (half, field trung bình: tên, email, SĐT)
-- 3 = 3/4 width  (mới: field rộng: địa chỉ, ghi chú ngắn)
-- 4 = 4/4 width  (full: textarea, rich editor, subgrid)
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  parent_object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND  name             = N'CHK_Ui_Field_ColSpan'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_ColSpan
            CHECK (Col_Span BETWEEN 1 AND 4);
    PRINT 'CHK_Ui_Field_ColSpan added (BETWEEN 1 AND 4)';
END

-- ── 4. Verify ─────────────────────────────────────────────
SELECT
    Col_Span,
    COUNT(*) AS Field_Count
FROM dbo.Ui_Field
GROUP BY Col_Span
ORDER BY Col_Span;

COMMIT TRANSACTION;
