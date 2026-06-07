-- =============================================================================
-- File    : 025_migrate_section_title_key_add_title_suffix.sql
-- Purpose : Chuẩn hóa Section Title_Key sang convention mới có hậu tố `.title`.
--           Cũ : {table_code}.section.{section_code}
--           Mới: {table_code}.section.{section_code}.title
--           (Xem docs/spec/10_RESOURCE_KEY_CONVENTION.md §1c)
--
-- Phạm vi : - dbo.Ui_Section.Title_Key
--           - dbo.Sys_Resource.Resource_Key (các bản dịch vi/en của section)
--
-- An toàn : - Idempotent: key đã có `.title` (đã migrate) bị loại trừ.
--           - Chỉ chạm section key (có `.section.` và không còn dot phía sau);
--             KHÔNG đụng tab (`.tab.`), field (`.field.`), validation (`.val.`).
--           - Guard NOT EXISTS tránh đụng UNIQUE khi bản `.title` đã tồn tại.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
BEGIN TRAN;

-- ── 1. Sys_Resource: rename Resource_Key của section keys ──────────────────
-- Điều kiện nhận diện section key cũ:
--   - chứa '.section.'
--   - sau '.section.' KHÔNG còn dấu chấm  → '%.section.%.%' = false
--     (section_code chỉ gồm [a-z0-9_], không có dot; bản đã `.title` sẽ có dot)
UPDATE r
SET    r.Resource_Key = r.Resource_Key + '.title',
       r.Updated_At   = GETDATE()
FROM   dbo.Sys_Resource r
WHERE  r.Resource_Key LIKE '%.section.%'
  AND  r.Resource_Key NOT LIKE '%.section.%.%'
  AND  NOT EXISTS (
           SELECT 1 FROM dbo.Sys_Resource r2
           WHERE  r2.Resource_Key = r.Resource_Key + '.title'
             AND  r2.Lang_Code    = r.Lang_Code
       );

PRINT N'  Sys_Resource section keys migrated: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- ── 2. Ui_Section: rename cột Title_Key tương ứng ─────────────────────────
UPDATE s
SET    s.Title_Key = s.Title_Key + '.title'
FROM   dbo.Ui_Section s
WHERE  s.Title_Key LIKE '%.section.%'
  AND  s.Title_Key NOT LIKE '%.section.%.%';

PRINT N'  Ui_Section.Title_Key migrated: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRAN;
GO

PRINT N'Migration 025 completed — Section Title_Key chuẩn hóa hậu tố .title.';
GO
