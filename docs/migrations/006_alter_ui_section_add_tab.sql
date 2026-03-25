-- ============================================================
-- Migration 006: Thêm Tab_Id vào Ui_Section
-- Mục đích  : Gắn section vào tab. NULL = section chưa thuộc
--             tab nào (backward compat với data cũ).
--             FormRunner: nếu Tab_Id NULL hoặc form chỉ có ≤1 tab
--             → render phẳng như trước.
-- Phụ thuộc : Migration 005 (Ui_Tab phải tồn tại trước)
-- Ngày      : 2026-03-25
-- ============================================================

-- ── 1. Thêm cột Tab_Id (nullable) vào Ui_Section ─────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Ui_Section')
      AND name = N'Tab_Id'
)
BEGIN
    ALTER TABLE dbo.Ui_Section
        ADD Tab_Id int NULL
            CONSTRAINT FK_Ui_Section_Tab
            FOREIGN KEY REFERENCES dbo.Ui_Tab (Tab_Id);
END
GO

-- ── 2. Index hỗ trợ query sections theo tab ──────────────────
-- Dùng khi FormRunner load: WHERE Tab_Id = ? AND Is_Active = 1
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Ui_Section')
      AND name = N'IX_Ui_Section_Tab'
)
BEGIN
    CREATE INDEX IX_Ui_Section_Tab
        ON dbo.Ui_Section (Tab_Id, Is_Active, Order_No)
        WHERE Tab_Id IS NOT NULL;
END
GO
