-- =============================================================================
-- File    : 058_seed_sys_val_general_messages.sql
-- Database: ICare247_Config
-- Purpose : SVHOOK-1 — Seed bộ thông báo i18n CHUNG cho save-hook store
--           (spc_Grid_<T> / sp_AfterSave_Grid_<T>) trả error_key thay vì text.
--           Gồm: thông báo cấp form (sys.val.Invalid/Forbidden/Conflict/NotFound),
--           template field dùng chung (Integer/Numeric/Regex/Length/MinLength/Range/Compare)
--           và passthrough sys.msg.raw cho text tự do (mất đa ngôn ngữ — chỉ escape hatch).
-- Token   : {0} = giá trị người dùng nhập · {1} = nhãn field · {2}/{3} = tham số phụ
--           (đồng nhất ResourceResolver.ApplyTokens + db/053). KHÔNG đụng
--           sys.val.Required / sys.val.Unique (thuộc db/053).
-- Liên quan: docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029 · spec 10 (Resource Key).
-- Note    : Idempotent (MERGE — update nếu có, insert nếu chưa). Chạy trên Config DB.
--           Master = Config DB; sẽ đồng bộ xuống tenant qua config-sync (ADR-025).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

MERGE dbo.Sys_Resource AS t
USING (VALUES
    -- ── Thông báo cấp form (field_name = NULL → banner/toast, KHÔNG args) ──────
    (N'sys.val.Invalid',   N'vi', N'Dữ liệu của bạn không hợp lệ'),
    (N'sys.val.Invalid',   N'en', N'Your data is invalid'),
    (N'sys.val.Forbidden', N'vi', N'Bạn không có quyền thực hiện thao tác này'),
    (N'sys.val.Forbidden', N'en', N'You do not have permission to perform this action'),
    (N'sys.val.Conflict',  N'vi', N'Dữ liệu đã bị thay đổi, vui lòng tải lại và thử lại'),
    (N'sys.val.Conflict',  N'en', N'The data has changed, please reload and try again'),
    (N'sys.val.NotFound',  N'vi', N'Không tìm thấy dữ liệu'),
    (N'sys.val.NotFound',  N'en', N'Data not found'),

    -- ── Template field dùng chung ({1}=nhãn · {2}/{3}=giới hạn) ───────────────
    (N'sys.val.Integer',   N'vi', N'{1} chỉ được nhập số nguyên'),
    (N'sys.val.Integer',   N'en', N'{1} must be an integer'),
    (N'sys.val.Numeric',   N'vi', N'{1} chỉ được nhập số'),
    (N'sys.val.Numeric',   N'en', N'{1} must be a number'),
    (N'sys.val.Regex',     N'vi', N'{1} không đúng định dạng'),
    (N'sys.val.Regex',     N'en', N'{1} has an invalid format'),
    (N'sys.val.Length',    N'vi', N'{1} không được vượt quá {2} ký tự'),
    (N'sys.val.Length',    N'en', N'{1} must not exceed {2} characters'),
    (N'sys.val.MinLength', N'vi', N'{1} phải có ít nhất {2} ký tự'),
    (N'sys.val.MinLength', N'en', N'{1} must be at least {2} characters'),
    (N'sys.val.Range',     N'vi', N'{1} phải nằm trong khoảng {2} đến {3}'),
    (N'sys.val.Range',     N'en', N'{1} must be between {2} and {3}'),
    (N'sys.val.Compare',   N'vi', N'{1} phải lớn hơn hoặc bằng {2}'),
    (N'sys.val.Compare',   N'en', N'{1} must be greater than or equal to {2}'),

    -- ── Passthrough text tự do ({0} = nguyên câu store gửi; mất đa ngôn ngữ) ──
    (N'sys.msg.raw',       N'vi', N'{0}'),
    (N'sys.msg.raw',       N'en', N'{0}')
) AS s (Resource_Key, Lang_Code, Resource_Value)
ON  t.Resource_Key = s.Resource_Key AND t.Lang_Code = s.Lang_Code
WHEN MATCHED THEN
    UPDATE SET Resource_Value = s.Resource_Value, Updated_At = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value)
    VALUES (s.Resource_Key, s.Lang_Code, s.Resource_Value);
GO

PRINT N'Migration 058 completed — seed sys.val.* chung + sys.val.Invalid/Forbidden/Conflict/NotFound + sys.msg.raw (vi/en).';
GO
