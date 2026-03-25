-- ============================================================
-- Migration 007: Thêm Col_Span, Lookup_Source, Lookup_Code
--                vào Ui_Field
-- Mục đích  :
--   Col_Span     : Độ rộng field trong grid (1=1/3, 2=2/3, 3=full)
--                  Cần là column riêng vì FormRunner dùng trực tiếp
--                  để build CSS grid — không thể để trong JSON.
--   Lookup_Source: Phân biệt field thường / static lookup / dynamic FK
--                  'static'  → đọc Sys_Lookup theo Lookup_Code
--                  'dynamic' → đọc Ui_Field_Lookup (migration 008)
--                  NULL      → field thường, không phải lookup
--   Lookup_Code  : Tham chiếu logic đến Sys_Lookup.Lookup_Code
--                  (không dùng FK vật lý để tránh constraint khi
--                  thêm lookup code mới theo tenant)
-- Ngày      : 2026-03-25
-- ============================================================

-- ── 1. Thêm Col_Span ─────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND name = N'Col_Span'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD Col_Span tinyint NOT NULL
            CONSTRAINT DF_Ui_Field_ColSpan DEFAULT 1;
    -- Giá trị hợp lệ: 1 | 2 | 3
    -- 1 = 1/3 width (default, tương đương behavior hiện tại)
    -- 2 = 2/3 width
    -- 3 = full width (textarea, subgrid, rich editor,...)
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_ColSpan
            CHECK (Col_Span BETWEEN 1 AND 3);
END
GO

-- ── 2. Thêm Lookup_Source ────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND name = N'Lookup_Source'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD Lookup_Source nvarchar(20) NULL;
    -- NULL     = field thường (TextBox, DateEdit,...)
    -- 'static' = dùng Sys_Lookup, đọc qua Lookup_Code bên dưới
    -- 'dynamic'= có bản ghi Ui_Field_Lookup tương ứng (migration 008)
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_LookupSource
            CHECK (Lookup_Source IN (N'static', N'dynamic') OR Lookup_Source IS NULL);
END
GO

-- ── 3. Thêm Lookup_Code ──────────────────────────────────────
-- Chỉ có ý nghĩa khi Lookup_Source = 'static'
-- Không dùng FK vật lý → cho phép thêm Lookup_Code mới theo
-- tenant mà không cần sửa constraint
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND name = N'Lookup_Code'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD Lookup_Code nvarchar(50) NULL;
END
GO

-- ── 4. Constraint đảm bảo nhất quán giữa 2 cột ──────────────
-- Nếu Lookup_Source = 'static' → Lookup_Code phải có giá trị
-- Nếu Lookup_Source = 'dynamic' hoặc NULL → Lookup_Code phải NULL
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Ui_Field')
      AND name = N'CHK_Ui_Field_LookupConsistency'
)
BEGIN
    ALTER TABLE dbo.Ui_Field
        ADD CONSTRAINT CHK_Ui_Field_LookupConsistency CHECK (
            -- static: Lookup_Code bắt buộc
            (Lookup_Source = N'static'  AND Lookup_Code IS NOT NULL)
            -- dynamic: Lookup_Code không dùng (config trong Ui_Field_Lookup)
         OR (Lookup_Source = N'dynamic' AND Lookup_Code IS NULL)
            -- field thường: cả 2 đều NULL
         OR (Lookup_Source IS NULL      AND Lookup_Code IS NULL)
        );
END
GO
