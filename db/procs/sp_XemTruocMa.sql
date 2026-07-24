-- =============================================================================
-- File    : sp_XemTruocMa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : MA-2 (spec 32 / ADR-036) — XEM TRƯỚC mã dự kiến khi mở form Thêm mới.
--           Cùng cách tính MAX như sp_SinhMa, nhưng KHÔNG khóa, KHÔNG giữ chỗ,
--           KHÔNG ghi gì cả — mở form rồi hủy thì không để lại dấu vết nào.
-- Vì sao  : mở form rồi hủy là hành vi thường xuyên. Nếu giữ chỗ số lúc mở form thì
--           dãy mã thủng lỗ chỗ — kế toán không chấp nhận.
--           Đánh đổi ĐÃ CHỐT: hai người mở form cùng lúc thấy CÙNG một mã dự kiến;
--           người lưu sau nhận mã kế tiếp (sp_SinhMa mới là nơi cấp thật).
--           ⇒ UI phải ghi rõ "mã dự kiến — cấp chính thức khi lưu" (spec §7).
-- Ghi chú : Không cần bảng bộ đếm ⇒ mã dự kiến ở đây luôn khớp thực tế trong bảng
--           tại thời điểm xem, kể cả sau khi ai đó import/sửa tay/xóa bản ghi.
-- Spec    : docs/spec/32_SINH_MA_TU_DONG_SPEC.md §2 · ADR-036.
-- Idempotent: CREATE OR ALTER.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER PROCEDURE dbo.sp_XemTruocMa
    @Bang       NVARCHAR(128),
    @Cot        NVARCHAR(128) = N'Ma',
    @TienTo     NVARCHAR(100),
    @HauTo      NVARCHAR(100) = N'',
    @DoRongSeq  INT           = 0,
    @Buoc       INT           = 1,
    @SchemaName SYSNAME       = N'dbo',
    @MaDuKien   NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @TienTo    = ISNULL(@TienTo, N'');
    SET @HauTo     = ISNULL(@HauTo,  N'');
    SET @Buoc      = CASE WHEN ISNULL(@Buoc, 1) < 1 THEN 1 ELSE @Buoc END;
    SET @DoRongSeq = ISNULL(@DoRongSeq, 0);

    -- ── Validate identifier (dynamic SQL — cùng mức chặt như sp_SinhMa) ──────
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables  t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @Bang AND s.name = @SchemaName AND c.name = @Cot)
    BEGIN
        SET @MaDuKien = NULL;      -- cấu hình trỏ sai bảng/cột → để ô trống, KHÔNG đoán bừa
        RETURN;
    END;

    IF @HauTo <> N'' AND @DoRongSeq <= 0
    BEGIN
        SET @MaDuKien = NULL;
        RETURN;
    END;

    DECLARE @TienToEsc NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(REPLACE(@TienTo, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[');
    DECLARE @HauToEsc  NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(REPLACE(@HauTo,  N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[');

    DECLARE @Start INT = LEN(@TienTo) + 1;
    DECLARE @Mask  NVARCHAR(400);
    DECLARE @CatSo NVARCHAR(200);

    IF @HauTo = N''
    BEGIN
        SET @Mask  = @TienToEsc + N'[0-9]%';
        SET @CatSo = N'SUBSTRING(' + QUOTENAME(@Cot) + N', @Start, 4000)';
    END
    ELSE
    BEGIN
        SET @Mask  = @TienToEsc + REPLICATE(N'[0-9]', @DoRongSeq) + @HauToEsc;
        SET @CatSo = N'SUBSTRING(' + QUOTENAME(@Cot) + N', @Start, @Rong)';
    END;

    -- Đọc thuần — KHÔNG hint khóa (đây là đường CHỈ XEM, không được chặn ai).
    -- Vẫn KHÔNG lọc IsDeleted: cùng tập dữ liệu với sp_SinhMa thì số xem trước mới khớp.
    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT @SoOut = ISNULL(MAX(TRY_CONVERT(BIGINT, ' + @CatSo + N')), 0) ' +
        N'FROM ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@Bang) + N' ' +
        N'WHERE ' + QUOTENAME(@Cot) + N' LIKE @Mask ESCAPE ''\'';';

    DECLARE @So BIGINT;
    EXEC sp_executesql @Sql,
         N'@Mask NVARCHAR(400), @Start INT, @Rong INT, @SoOut BIGINT OUTPUT',
         @Mask = @Mask, @Start = @Start, @Rong = @DoRongSeq, @SoOut = @So OUTPUT;

    SET @MaDuKien = dbo.fn_GhepMa(@TienTo, ISNULL(@So, 0) + @Buoc, @DoRongSeq, @HauTo);
END;
GO

PRINT N'sp_XemTruocMa created/updated.';
GO
