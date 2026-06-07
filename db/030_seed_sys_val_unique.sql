-- =============================================================================
-- File    : 030_seed_sys_val_unique.sql
-- Purpose : Seed template thông báo trùng (field Is_Unique) — đa ngôn ngữ.
--           Key: sys.val.Unique. {0} = nhãn field. Có thể override per-field bằng
--           {form_code}.val.{field_code}.Unique (xem ResourceResolver.ResolveUnique).
-- An toàn : NCHAR(<code point>) cho ký tự có dấu → file ASCII, chạy kiểu gì cũng đúng.
--           đã = U+0111(đ) a U+0303(huyền?) ... dùng N'...' chuẩn vì file lưu UTF-8.
-- =============================================================================

USE [ICare247_Config];
GO

IF OBJECT_ID('dbo.Sys_Resource', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Sys_Resource AS target
    USING (VALUES
        ('sys.val.Unique', 'vi', N'{0} đã tồn tại'),
        ('sys.val.Unique', 'en', N'{0} already exists')
    ) AS source (Resource_Key, Lang_Code, Resource_Value)
    ON  target.Resource_Key = source.Resource_Key
    AND target.Lang_Code    = source.Lang_Code
    WHEN NOT MATCHED THEN
        INSERT (Resource_Key, Lang_Code, Resource_Value)
        VALUES (source.Resource_Key, source.Lang_Code, source.Resource_Value);
END;
GO

PRINT N'Migration 030 completed — sys.val.Unique seeded (vi/en).';
GO
