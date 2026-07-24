-- =============================================================================
-- File    : sp_SinhMa.sql
-- Database: ICare247_Solution (Data DB per-tenant)
-- Purpose : MA-2 (spec 32 / ADR-036) — CẤP THẬT một mã mới cho bảng đích.
--           Gọi NGAY TRƯỚC khi INSERT, TRONG CÙNG transaction ghi.
--
-- ⛔ KHÔNG CÓ BỘ ĐẾM LƯU SẴN. Số kế tiếp = quét CHÍNH BẢNG ĐÍCH lấy MAX theo tiền tố:
--        SELECT MAX(<phần số của Ma>) FROM <bảng> WHERE <cột mã> LIKE '<tiền tố>%'
--     Vì sao bắt buộc như vậy: bộ đếm lưu sẵn sẽ LỆCH thực tế ngay khi có import dữ
--     liệu cũ, sửa mã bằng tay, xóa/khôi phục bản ghi, hoặc restore DB từng phần —
--     rồi cấp trùng mã. Sự thật duy nhất về "mã lớn nhất" là DỮ LIỆU TRONG BẢNG.
--
-- Phạm vi đánh số = TIỀN TỐ. Mã chứa năm → tự đánh lại theo năm; chứa mã công ty →
--     tự đánh riêng từng công ty. Không có cột cấu hình reset nào cả.
--
-- Tham số: quy tắc do CALLER (C#) dựng sẵn thành @TienTo/@HauTo/@DoRongSeq — proc ở
--     Data DB không đọc được Sys_Ma_Rule (bảng ở CONFIG DB) và cũng không thấy payload.
--     Truy vấn 3 phần tên ([TenConfigDb].dbo.…) sẽ đóng cứng tên database vào proc,
--     vỡ mô hình 1-DB-1-tenant (ADR-018/025).
--
-- An toàn: @Bang/@Cot ghép vào DYNAMIC SQL ⇒ BẮT BUỘC kiểm tra tồn tại thật trong
--     sys.tables/sys.columns rồi QUOTENAME (cùng cách sp_RecomputeTreeOrder db/085).
--     @TienTo/@HauTo chỉ đi qua tham số sp_executesql, không nối chuỗi.
--
-- Spec    : docs/spec/32_SINH_MA_TU_DONG_SPEC.md §5 · ADR-036.
-- Idempotent: CREATE OR ALTER.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhMa
    @Bang       NVARCHAR(128),                  -- 'TC_PhongBan'
    @Cot        NVARCHAR(128) = N'Ma',
    @TienTo     NVARCHAR(100),                  -- 'CT01-PHONG-'  (C# đã dựng từ các đoạn)
    @HauTo      NVARCHAR(100) = N'',            -- phần sau số (thường rỗng)
    @DoRongSeq  INT           = 0,              -- số chữ số; 0 = không đệm
    @Buoc       INT           = 1,
    @SchemaName SYSNAME       = N'dbo',
    @MaMoi      NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- ⚠️ XACT_ABORT TẮT (khác các proc khác của dự án): proc chạy BÊN TRONG transaction
    -- lưu của engine. Với XACT_ABORT ON, bất kỳ lỗi nào cũng đẩy transaction ngoài sang
    -- trạng thái uncommittable (XACT_STATE = -1). Ở đây ta chống đua bằng KHÓA, không
    -- bằng bắt lỗi trùng khóa. Đặt trong proc chỉ ảnh hưởng phạm vi proc.
    SET XACT_ABORT OFF;

    SET @TienTo    = ISNULL(@TienTo, N'');
    SET @HauTo     = ISNULL(@HauTo,  N'');
    SET @Buoc      = CASE WHEN ISNULL(@Buoc, 1) < 1 THEN 1 ELSE @Buoc END;
    SET @DoRongSeq = ISNULL(@DoRongSeq, 0);

    -- ── 1) Bắt buộc chạy trong transaction ───────────────────────────────────
    -- Khóa phạm vi (sp_getapplock @LockOwner='Transaction') giữ tới COMMIT. Không có
    -- transaction thì khóa nhả ngay sau proc ⇒ hai phiên cùng đọc MAX ⇒ CẤP TRÙNG MÃ.
    -- Caller PHẢI mở transaction (đường lưu của engine đã có sẵn).
    IF @@TRANCOUNT = 0
    BEGIN
        RAISERROR(N'sp_SinhMa: phải gọi BÊN TRONG transaction (@@TRANCOUNT = 0) — nếu không sẽ cấp trùng mã.', 16, 1);
        RETURN;
    END;

    -- ── 2) Validate identifier (chống injection qua dynamic SQL) ─────────────
    IF NOT EXISTS (
        SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @Bang AND s.name = @SchemaName)
    BEGIN
        RAISERROR(N'sp_SinhMa: bảng %s.%s không tồn tại.', 16, 1, @SchemaName, @Bang);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables  t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @Bang AND s.name = @SchemaName AND c.name = @Cot)
    BEGIN
        RAISERROR(N'sp_SinhMa: cột %s không tồn tại trên %s.%s.', 16, 1, @Cot, @SchemaName, @Bang);
        RETURN;
    END;

    IF @HauTo <> N'' AND @DoRongSeq <= 0
    BEGIN
        -- Số nằm GIỮA mã thì phải biết nó rộng bao nhiêu ký tự mới cắt ra được.
        RAISERROR(N'sp_SinhMa: có hậu tố thì @DoRongSeq bắt buộc > 0 (số nằm giữa mã).', 16, 1);
        RETURN;
    END;

    -- ── 3) Khóa phạm vi đánh số ──────────────────────────────────────────────
    -- Chỉ nối tiếp các lần sinh mã CÙNG TIỀN TỐ (cùng công ty / cùng năm…), không chặn
    -- toàn bảng. Nhả tự động khi COMMIT/ROLLBACK. Đây là thứ thay cho bộ đếm: đọc MAX
    -- rồi ghi mã mới trở thành một thao tác không thể chen ngang.
    DECLARE @Res NVARCHAR(255) = N'SinhMa|' + @SchemaName + N'|' + @Bang + N'|' + @Cot + N'|' + @TienTo;
    DECLARE @Lock INT;
    EXEC @Lock = sp_getapplock @Resource = @Res, @LockMode = N'Exclusive',
                               @LockOwner = N'Transaction', @LockTimeout = 5000;
    IF @Lock < 0
    BEGIN
        RAISERROR(N'sp_SinhMa: không lấy được khóa phạm vi ''%s'' (mã lỗi %d) — thử lại.', 16, 1, @Res, @Lock);
        RETURN;
    END;

    -- ── 4) Dựng mặt nạ LIKE + biểu thức cắt phần số ──────────────────────────
    -- Escape ký tự đặc biệt của LIKE trong tiền tố/hậu tố (\ % _ [) — dùng ESCAPE '\'.
    -- Phần '[0-9]' của mặt nạ là CÚ PHÁP, ghép sau khi escape nên không bị escape nhầm.
    DECLARE @TienToEsc NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(REPLACE(@TienTo, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[');
    DECLARE @HauToEsc  NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(REPLACE(@HauTo,  N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[');

    DECLARE @Start INT = LEN(@TienTo) + 1;      -- vị trí ký tự số đầu tiên trong mã
    DECLARE @Mask  NVARCHAR(400);
    DECLARE @CatSo NVARCHAR(200);               -- biểu thức SQL cắt phần số ra khỏi mã

    IF @HauTo = N''
    BEGIN
        -- Số nằm CUỐI mã: lấy hết phần đuôi rồi TRY_CONVERT. Mã có đuôi không phải số
        -- (dữ liệu tay kiểu 'CT01-PHONG-ABC') → NULL → MAX tự bỏ qua.
        -- Cách này còn xử lý đúng cả mã đã TRÀN độ rộng ('...-1000' khi @DoRongSeq = 3),
        -- thứ mà mặt nạ cố định số chữ số sẽ bỏ sót ⇒ cấp trùng.
        SET @Mask  = @TienToEsc + N'[0-9]%';
        SET @CatSo = N'SUBSTRING(' + QUOTENAME(@Cot) + N', @Start, 4000)';
    END
    ELSE
    BEGIN
        SET @Mask  = @TienToEsc + REPLICATE(N'[0-9]', @DoRongSeq) + @HauToEsc;
        SET @CatSo = N'SUBSTRING(' + QUOTENAME(@Cot) + N', @Start, @Rong)';
    END;

    -- ── 5) Quét MAX trên bảng đích ───────────────────────────────────────────
    -- UPDLOCK/HOLDLOCK: khóa khoảng khóa trên vùng mã đang xét (lớp phòng vệ thứ hai
    -- cạnh applock, phòng khi caller quên khóa ở đường ghi khác).
    -- KHÔNG lọc IsDeleted: mã của bản ghi đã xóa mềm VẪN tính vào MAX ⇒ không tái dùng
    -- mã cũ. Tái dùng mã của bản ghi đã xóa là thứ kế toán/kiểm toán không chấp nhận.
    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT @SoOut = ISNULL(MAX(TRY_CONVERT(BIGINT, ' + @CatSo + N')), 0) ' +
        N'FROM ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@Bang) + N' WITH (UPDLOCK, HOLDLOCK) ' +
        N'WHERE ' + QUOTENAME(@Cot) + N' LIKE @Mask ESCAPE ''\'';';

    DECLARE @So BIGINT;
    EXEC sp_executesql @Sql,
         N'@Mask NVARCHAR(400), @Start INT, @Rong INT, @SoOut BIGINT OUTPUT',
         @Mask = @Mask, @Start = @Start, @Rong = @DoRongSeq, @SoOut = @So OUTPUT;

    SET @So = ISNULL(@So, 0) + @Buoc;
    SET @MaMoi = dbo.fn_GhepMa(@TienTo, @So, @DoRongSeq, @HauTo);

    -- ── 6) Chốt cuối: mã vừa dựng đã có ai chiếm chưa ────────────────────────
    -- MAX chỉ bảo đảm SỐ chưa dùng, không bảo đảm MÃ chưa bị chiếm: dữ liệu nhập tay
    -- có thể đệm số khác quy tắc ('CT-7' vs 'CT-007') nên không lọt mặt nạ nhưng lại
    -- đụng đúng chuỗi mã ta sắp ghi. Nhảy tiếp tới khi trống (chặn trên 100 vòng để
    -- cấu hình sai không thành vòng lặp vô tận).
    DECLARE @Ton INT = 1, @Vong INT = 0;
    DECLARE @SqlTon NVARCHAR(MAX) =
        N'SELECT @TonOut = CASE WHEN EXISTS (SELECT 1 FROM ' +
        QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@Bang) + N' WITH (UPDLOCK, HOLDLOCK) ' +
        N'WHERE ' + QUOTENAME(@Cot) + N' = @Ma) THEN 1 ELSE 0 END;';

    WHILE @Vong < 100
    BEGIN
        EXEC sp_executesql @SqlTon, N'@Ma NVARCHAR(100), @TonOut INT OUTPUT',
             @Ma = @MaMoi, @TonOut = @Ton OUTPUT;

        IF @Ton = 0 BREAK;

        SET @So    = @So + @Buoc;
        SET @MaMoi = dbo.fn_GhepMa(@TienTo, @So, @DoRongSeq, @HauTo);
        SET @Vong  = @Vong + 1;
    END;

    IF @Ton = 1
    BEGIN
        RAISERROR(N'sp_SinhMa: không tìm được mã trống cho %s.%s sau 100 lần thử (tiền tố ''%s'') — xem lại quy tắc.',
                  16, 1, @SchemaName, @Bang, @TienTo);
        RETURN;
    END;
END;
GO

PRINT N'sp_SinhMa created/updated.';
GO
