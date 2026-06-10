-- =============================================================================
-- File    : 033_seed_common_ui_resources.sql
-- Purpose : Seed resource i18n cho chuỗi UI dùng chung (nút Lưu/Hủy/Thêm/Xóa…).
--           Scope key 'common.*' — client load qua GET /api/v1/resources.
-- Note    : Idempotent — chỉ insert key/lang chưa có (NOT EXISTS). Chạy lại an toàn.
-- =============================================================================

USE [ICare247_Config];
GO

-- Bảng tạm chứa cặp (key, lang, value) cần seed.
DECLARE @seed TABLE (Resource_Key NVARCHAR(150), Lang_Code NVARCHAR(10), Resource_Value NVARCHAR(MAX));

INSERT INTO @seed (Resource_Key, Lang_Code, Resource_Value) VALUES
    (N'common.btn.save',      N'vi', N'Lưu'),
    (N'common.btn.save',      N'en', N'Save'),
    (N'common.btn.cancel',    N'vi', N'Hủy'),
    (N'common.btn.cancel',    N'en', N'Cancel'),
    (N'common.btn.saving',    N'vi', N'Đang lưu…'),
    (N'common.btn.saving',    N'en', N'Saving…'),
    (N'common.btn.add',       N'vi', N'Thêm mới'),
    (N'common.btn.add',       N'en', N'Add new'),
    (N'common.btn.delete',    N'vi', N'Xóa'),
    (N'common.btn.delete',    N'en', N'Delete'),
    (N'common.action.create', N'vi', N'Thêm mới'),
    (N'common.action.create', N'en', N'Add new'),
    (N'common.action.update', N'vi', N'Cập nhật'),
    (N'common.action.update', N'en', N'Update');

INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value)
SELECT s.Resource_Key, s.Lang_Code, s.Resource_Value
FROM   @seed s
WHERE  NOT EXISTS (
        SELECT 1 FROM dbo.Sys_Resource r
        WHERE  r.Resource_Key = s.Resource_Key AND r.Lang_Code = s.Lang_Code);
GO

PRINT N'Migration 033 completed — seed common UI resources (common.*).';
GO
