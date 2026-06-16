-- =============================================================================
-- File    : 053_update_sys_val_templates.sql
-- Database: ICare247_Config
-- Purpose : Chuẩn hóa template thông báo validation theo convention TOKEN mới:
--             {0} = giá trị người dùng nhập   ·   {1} = nhãn field
--           (trước đây sys.val.Unique dùng {0} = nhãn → đổi sang {1} cho nhất quán
--            với message per-field + block check unique ở handler).
-- Liên quan: SaveMasterDataCommandHandler / InsertLookupCommandHandler (unique),
--            ResourceResolver.ResolveRequired/ResolveUnique (replace {0}/{1}).
-- Note    : Idempotent (MERGE — update nếu có, insert nếu chưa). Chạy trên Config DB.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

MERGE dbo.Sys_Resource AS t
USING (VALUES
    -- Unique: {1}=nhãn, {0}=giá trị nhập.
    (N'sys.val.Unique', N'vi', N'{1} "{0}" đã được sử dụng, vui lòng dùng giá trị khác'),
    (N'sys.val.Unique', N'en', N'{1} "{0}" is already in use, please use another value'),
    -- Required: {1}=nhãn (giá trị rỗng nên không dùng {0}).
    (N'sys.val.Required', N'vi', N'{1} không được để trống'),
    (N'sys.val.Required', N'en', N'{1} is required')
) AS s (Resource_Key, Lang_Code, Resource_Value)
ON  t.Resource_Key = s.Resource_Key AND t.Lang_Code = s.Lang_Code
WHEN MATCHED THEN
    UPDATE SET Resource_Value = s.Resource_Value, Updated_At = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Resource_Key, Lang_Code, Resource_Value)
    VALUES (s.Resource_Key, s.Lang_Code, s.Resource_Value);
GO

PRINT N'Migration 053 completed — chuẩn hóa template sys.val.Unique/Required ({0}=giá trị, {1}=nhãn).';
GO
