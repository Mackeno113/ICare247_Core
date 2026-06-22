-- =============================================================================
-- File    : 060_create_sys_context_param.sql
-- Database: ICare247_Config
-- Purpose : Registry tham số ngữ cảnh (ADR-030) — danh mục token mà engine bind
--           SERVER-SIDE cho mọi SQL admin tự viết (Lookup_Sql bộ lọc, Source SP/SQL
--           của View). Thêm token mới = thêm 1 dòng (no-code), không phải sửa code.
-- Model   : Whitelist khi chạy = (Sys_Context_Param Is_Active) ∪ (param khai trong
--           Ui_View_Filter). Ngoài danh sách → chặn (chống injection).
--           Giá trị resolve theo Source_Kind:
--             • Claim       — đọc JWT claim Source_Key (bất biến, client không sửa).
--             • Header       — đọc HTTP header Source_Key.
--             • ActiveScope  — đọc header Source_Key rồi chạy Validate_Sql(@NguoiDungID,@val)
--                              trả 1/0; sai/rỗng → ép về Default_Value (vd 0 = bỏ thu hẹp).
-- Quy ước : @__xxx = nội bộ engine (CẤM trong SQL config); @<tên registry> = công khai;
--           hậu tố _Active = phạm vi do UI chọn (server-validate).
-- Note    : Idempotent — IF OBJECT_ID / IF NOT EXISTS. Chạy lại an toàn.
-- Spec    : docs/spec/19_CONTEXT_PARAM_SPEC.md · ADR-030.
-- =============================================================================

USE [ICare247_Config];
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Bảng Sys_Context_Param
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Sys_Context_Param', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Context_Param
    (
        Param_Id       INT            IDENTITY(1,1) NOT NULL,
        Param_Name     NVARCHAR(100)  NOT NULL,                 -- token KHÔNG có '@', vd 'CongTyID_Active'
        Sql_Type       NVARCHAR(20)   NOT NULL,                 -- bigint|int|string|decimal|date|bool
        Source_Kind    NVARCHAR(20)   NOT NULL,                 -- Claim | Header | ActiveScope
        Source_Key     NVARCHAR(100)  NOT NULL,                 -- tên claim / tên header để đọc
        Validate_Sql   NVARCHAR(MAX)  NULL,                     -- chỉ ActiveScope — trả 1/0 (bind @NguoiDungID,@val)
        Default_Value  NVARCHAR(255)  NULL,                     -- giá trị mặc định khi rỗng/không hợp lệ
        Description    NVARCHAR(300)  NULL,
        Is_System      BIT            NOT NULL DEFAULT 0,        -- token lõi nền tảng (đồng bộ master→tenant)
        Is_Active      BIT            NOT NULL DEFAULT 1,

        CONSTRAINT PK_Sys_Context_Param PRIMARY KEY (Param_Id),
        CONSTRAINT CHK_Sys_Context_Param_Source
            CHECK (Source_Kind IN ('Claim', 'Header', 'ActiveScope')),
        CONSTRAINT CHK_Sys_Context_Param_SqlType
            CHECK (Sql_Type IN ('bigint', 'int', 'string', 'decimal', 'date', 'bool')),
        -- ActiveScope BẮT BUỘC có Validate_Sql (giá trị từ client → phải kiểm theo quyền)
        CONSTRAINT CHK_Sys_Context_Param_ActiveScope
            CHECK (Source_Kind <> 'ActiveScope' OR Validate_Sql IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Sys_Context_Param_Name' AND object_id = OBJECT_ID('dbo.Sys_Context_Param'))
    CREATE UNIQUE INDEX UQ_Sys_Context_Param_Name ON dbo.Sys_Context_Param(Param_Name);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Seed 4 token lõi
--    LƯU Ý: Validate_Sql của CongTyID_Active là MẪU — admin phải sửa khớp bảng
--    phân công user↔công ty THẬT của tenant (tên bảng/cột chưa chốt — xem TASKS).
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @seed TABLE (
    Param_Name    NVARCHAR(100),
    Sql_Type      NVARCHAR(20),
    Source_Kind   NVARCHAR(20),
    Source_Key    NVARCHAR(100),
    Validate_Sql  NVARCHAR(MAX),
    Default_Value NVARCHAR(255),
    Description   NVARCHAR(300)
);

INSERT INTO @seed VALUES
    (N'NguoiDungID', N'bigint', N'Claim', N'sub', NULL, NULL,
     N'NguoiDung_Id của user đăng nhập (ranh giới bảo mật cứng). Dùng JOIN bảng quyền.'),
    (N'TenantId', N'int', N'Claim', N'tenant', NULL, NULL,
     N'Tenant hiện tại (claim tenant trong JWT).'),
    (N'LangCode', N'string', N'Header', N'X-Lang', NULL, N'vi',
     N'Ngôn ngữ giao diện (header X-Lang; rỗng → vi).'),
    (N'CongTyID_Active', N'bigint', N'ActiveScope', N'X-Active-CongTy',
     -- MẪU: thay bảng/cột theo schema phân công user↔công ty thật của tenant.
     N'SELECT 1 FROM dbo.HT_NguoiDung_CongTy WHERE NguoiDung_Id = @NguoiDungID AND CongTy_Id = @val AND IsDeleted = 0',
     N'0',
     N'Công ty đang chọn ở company-switcher (0 = mọi công ty được phân quyền). Thu hẹp MỀM trong ranh giới @NguoiDungID.');

INSERT INTO dbo.Sys_Context_Param (Param_Name, Sql_Type, Source_Kind, Source_Key, Validate_Sql, Default_Value, Description, Is_System)
SELECT s.Param_Name, s.Sql_Type, s.Source_Kind, s.Source_Key, s.Validate_Sql, s.Default_Value, s.Description, 1
FROM   @seed s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.Sys_Context_Param t WHERE t.Param_Name = s.Param_Name);
GO

PRINT N'Migration 060 completed — Sys_Context_Param + seed 4 token (NguoiDungID/TenantId/LangCode/CongTyID_Active).';
GO
