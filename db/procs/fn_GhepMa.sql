-- =============================================================================
-- File    : fn_GhepMa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : MA-2 (spec 32 / ADR-036) — ghép mã hoàn chỉnh từ 3 mảnh:
--             tiền tố  +  số thứ tự (đệm 0 cho đủ độ rộng)  +  hậu tố
--           Dùng CHUNG bởi sp_SinhMa (cấp thật) và sp_XemTruocMa (xem trước) ⇒ hai
--           đường KHÔNG THỂ ghép lệch nhau. Đó là lý do tách hàm riêng thay vì
--           lặp mấy dòng nối chuỗi ở mỗi proc.
-- Vào     : @TienTo/@HauTo do C# dựng sẵn từ các ĐOẠN của quy tắc (LITERAL/DATE/
--           FIELD/LOOKUP — xem Sys_Ma_Rule_Segment, db/089). Proc SQL không tự tra
--           payload/bảng khác nên không tự dựng được 2 mảnh này.
-- Tràn    : @DoRongSeq = 3 mà số đã tới 1000 → trả SỐ ĐẦY ĐỦ ('1000'), KHÔNG cắt.
--           Cắt chữ số sẽ sinh mã TRÙNG — sai nặng hơn nhiều so với mã dài hơn dự kiến.
-- Perf    : scalar UDF — gọi ĐÚNG 1 LẦN mỗi lần sinh mã, KHÔNG bao giờ đặt trong
--           SELECT/WHERE duyệt nhiều dòng (đó mới là smell scalar UDF).
-- Spec    : docs/spec/32_SINH_MA_TU_DONG_SPEC.md §5 · ADR-036.
-- Idempotent: CREATE OR ALTER.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER FUNCTION dbo.fn_GhepMa
(
    @TienTo    NVARCHAR(100),   -- phần đứng trước số, vd 'CT01-PHONG-'
    @So        BIGINT,          -- số thứ tự
    @DoRongSeq INT,             -- số chữ số (0/NULL = độ dài tự nhiên, không đệm)
    @HauTo     NVARCHAR(100)    -- phần đứng sau số (thường rỗng)
)
RETURNS NVARCHAR(100)
AS
BEGIN
    DECLARE @soStr NVARCHAR(20) = CONVERT(NVARCHAR(20), ISNULL(@So, 0));

    IF @DoRongSeq IS NOT NULL AND @DoRongSeq > LEN(@soStr)
        SET @soStr = RIGHT(REPLICATE(N'0', @DoRongSeq) + @soStr, @DoRongSeq);

    RETURN CONVERT(NVARCHAR(100), ISNULL(@TienTo, N'') + @soStr + ISNULL(@HauTo, N''));
END;
GO

PRINT N'fn_GhepMa created/updated.';
GO
