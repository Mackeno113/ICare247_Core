-- =============================================================================
-- File    : 039_seed_config_lookup_foundation.sql
-- Purpose : Seed Sys_Lookup + Sys_Resource (CONFIG DB) cho các mã trạng thái/loại
--           dùng bởi nền tảng Data DB (HT_/TC_): TRANGTHAI_NGUOIDUNG, TRANGTHAI_DONVI,
--           LOAI_TAIKHOAN, HINHTHUC_2FA.
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §6.1 — Item_Code là HẰNG SỐ bất biến
--           (code logic phụ thuộc); tenant chỉ sửa LABEL qua Sys_Resource.
-- Target  : ICare247_Config (per-tenant). Data DB chỉ lưu chuỗi Item_Code.
-- Note    : Idempotent — MERGE; Tenant_Id = NULL (global, dùng chung mọi tenant).
-- =============================================================================

USE [ICare247_Config];
GO

-- ── 1. Sys_Lookup — danh sách mã (registry) ────────────────────────────────
IF OBJECT_ID('dbo.Sys_Lookup', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Sys_Lookup AS target
    USING (VALUES
        -- Trạng thái người dùng
        (NULL, 'TRANGTHAI_NGUOIDUNG', 'HoatDong',      'common.status.active',      1),
        (NULL, 'TRANGTHAI_NGUOIDUNG', 'TamKhoa',       'common.status.locked',      2),
        (NULL, 'TRANGTHAI_NGUOIDUNG', 'NgungHoatDong', 'common.status.inactive',    3),
        -- Trạng thái đơn vị (công ty / phòng ban)
        (NULL, 'TRANGTHAI_DONVI',     'HoatDong',      'common.status.active',      1),
        (NULL, 'TRANGTHAI_DONVI',     'NgungHoatDong', 'common.status.inactive',    2),
        -- Loại tài khoản
        (NULL, 'LOAI_TAIKHOAN',       'Local',         'lookup.loai_taikhoan.local',  1),
        (NULL, 'LOAI_TAIKHOAN',       'AD',            'lookup.loai_taikhoan.ad',     2),
        (NULL, 'LOAI_TAIKHOAN',       'SSO',           'lookup.loai_taikhoan.sso',    3),
        (NULL, 'LOAI_TAIKHOAN',       'Portal',        'lookup.loai_taikhoan.portal', 4),
        -- Hình thức 2FA
        (NULL, 'HINHTHUC_2FA',        'None',          'lookup.hinhthuc_2fa.none',  1),
        (NULL, 'HINHTHUC_2FA',        'App',           'lookup.hinhthuc_2fa.app',   2),
        (NULL, 'HINHTHUC_2FA',        'Email',         'lookup.hinhthuc_2fa.email', 3),
        (NULL, 'HINHTHUC_2FA',        'SMS',           'lookup.hinhthuc_2fa.sms',   4)
    ) AS source (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order)
    ON  target.Lookup_Code = source.Lookup_Code
    AND target.Item_Code   = source.Item_Code
    AND target.Tenant_Id   IS NULL
    WHEN NOT MATCHED THEN
        INSERT (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order)
        VALUES (source.Tenant_Id, source.Lookup_Code, source.Item_Code,
                source.Label_Key, source.Sort_Order)
    WHEN MATCHED THEN
        UPDATE SET Label_Key = source.Label_Key, Sort_Order = source.Sort_Order;
END;
GO

-- ── 2. Sys_Resource — label i18n (vi + en) ─────────────────────────────────
IF OBJECT_ID('dbo.Sys_Resource', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Sys_Language', 'U') IS NOT NULL
BEGIN
    MERGE dbo.Sys_Resource AS target
    USING (VALUES
        ('common.status.active',        'vi', N'Hoạt động'),
        ('common.status.active',        'en', N'Active'),
        ('common.status.locked',        'vi', N'Tạm khóa'),
        ('common.status.locked',        'en', N'Locked'),
        ('common.status.inactive',      'vi', N'Ngừng hoạt động'),
        ('common.status.inactive',      'en', N'Inactive'),

        ('lookup.loai_taikhoan.local',  'vi', N'Nội bộ'),
        ('lookup.loai_taikhoan.local',  'en', N'Local'),
        ('lookup.loai_taikhoan.ad',     'vi', N'Active Directory'),
        ('lookup.loai_taikhoan.ad',     'en', N'Active Directory'),
        ('lookup.loai_taikhoan.sso',    'vi', N'Đăng nhập một lần (SSO)'),
        ('lookup.loai_taikhoan.sso',    'en', N'Single Sign-On (SSO)'),
        ('lookup.loai_taikhoan.portal', 'vi', N'Cổng (Portal)'),
        ('lookup.loai_taikhoan.portal', 'en', N'Portal'),

        ('lookup.hinhthuc_2fa.none',    'vi', N'Không'),
        ('lookup.hinhthuc_2fa.none',    'en', N'None'),
        ('lookup.hinhthuc_2fa.app',     'vi', N'Ứng dụng (TOTP)'),
        ('lookup.hinhthuc_2fa.app',     'en', N'Authenticator app (TOTP)'),
        ('lookup.hinhthuc_2fa.email',   'vi', N'Email'),
        ('lookup.hinhthuc_2fa.email',   'en', N'Email'),
        ('lookup.hinhthuc_2fa.sms',     'vi', N'SMS'),
        ('lookup.hinhthuc_2fa.sms',     'en', N'SMS')
    ) AS source (Resource_Key, Lang_Code, Resource_Value)
    ON  target.Resource_Key = source.Resource_Key
    AND target.Lang_Code    = source.Lang_Code
    WHEN NOT MATCHED THEN
        INSERT (Resource_Key, Lang_Code, Resource_Value)
        VALUES (source.Resource_Key, source.Lang_Code, source.Resource_Value)
    WHEN MATCHED THEN
        UPDATE SET Resource_Value = source.Resource_Value;
END;
GO

PRINT N'Migration 039 completed — Sys_Lookup + Sys_Resource seeded (foundation status/type codes).';
GO
