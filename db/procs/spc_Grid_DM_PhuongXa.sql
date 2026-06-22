-- =============================================================================
-- File    : spc_Grid_DM_PhuongXa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : SVHOOK-6 — VALIDATE trước khi ghi cho màn Xã/Phường (engine-driven).
--           Nhận toàn bộ field màn qua @PayloadJson + người thực hiện + @Id (0=thêm mới).
--           Trả result set lỗi: error_key, args_json, field_name, severity (RỖNG = hợp lệ).
--           Token args (theo vị trí): {0}=giá trị · {1}=nhãn. Handler resolve i18n server-side.
-- Lưu ý   : Required/Unique cũng được ValidationEngine + unique-check (C#) chặn TRƯỚC store
--           (nếu field cấu hình Is_Required/Is_Unique) → ở đây là lớp "belt-and-suspenders".
--           Check FK tồn tại (Tỉnh/Thành phố) là phần store LÀM ĐƯỢC mà engine không.
-- Spec    : docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md · ADR-029.
-- Idempotent: CREATE OR ALTER (chạy lại = cập nhật).
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER PROCEDURE dbo.spc_Grid_DM_PhuongXa
    @Id            BIGINT,
    @TenantId      INT,
    @NguoiThucHien BIGINT,
    @LangCode      NVARCHAR(10),
    @PayloadJson   NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Tách field động từ payload ──────────────────────────────────────────
    DECLARE @Ma   NVARCHAR(20)  = NULLIF(LTRIM(RTRIM(JSON_VALUE(@PayloadJson, '$.Ma'))),  N'');
    DECLARE @Ten  NVARCHAR(150) = NULLIF(LTRIM(RTRIM(JSON_VALUE(@PayloadJson, '$.Ten'))), N'');
    DECLARE @Tinh BIGINT        = TRY_CAST(JSON_VALUE(@PayloadJson, '$.TinhThanhPho_Id') AS BIGINT);

    DECLARE @err TABLE (
        error_key  NVARCHAR(200),
        args_json  NVARCHAR(MAX),
        field_name NVARCHAR(128),
        severity   NVARCHAR(20)
    );

    -- ── Required ({1}=nhãn; {0} rỗng vì giá trị trống) ──────────────────────
    IF @Ma IS NULL
        INSERT @err VALUES (N'sys.val.Required', N'["","Mã Xã/Phường"]',   N'Ma',              N'error');
    IF @Ten IS NULL
        INSERT @err VALUES (N'sys.val.Required', N'["","Tên Xã/Phường"]',  N'Ten',             N'error');
    IF @Tinh IS NULL
        INSERT @err VALUES (N'sys.val.Required', N'["","Tỉnh/Thành phố"]', N'TinhThanhPho_Id', N'error');

    -- ── Unique Ma (toàn bảng, IsDeleted=0, loại trừ chính bản ghi khi sửa) ──
    --    args = [giá trị, nhãn] khớp template sys.val.Unique = "{1} ""{0}"" đã được sử dụng".
    IF @Ma IS NOT NULL AND EXISTS (
            SELECT 1 FROM dbo.DM_PhuongXa
            WHERE Ma = @Ma AND IsDeleted = 0 AND Id <> @Id)
        INSERT @err VALUES (
            N'sys.val.Unique',
            N'["' + STRING_ESCAPE(@Ma, 'json') + N'","Mã Xã/Phường"]',
            N'Ma', N'error');

    -- ── Referential: Tỉnh/Thành phố phải tồn tại & chưa xóa (store-only) ────
    IF @Tinh IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.DM_TinhThanhPho WHERE Id = @Tinh AND IsDeleted = 0)
        INSERT @err VALUES (N'sys.val.NotFound', NULL, N'TinhThanhPho_Id', N'error');

    -- Rỗng = hợp lệ → cho qua; có dòng = lỗi → handler rollback + trả 422.
    SELECT error_key, args_json, field_name, severity FROM @err;
END;
GO

PRINT N'Created/Altered dbo.spc_Grid_DM_PhuongXa.';
GO
