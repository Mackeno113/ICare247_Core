-- =============================================================================
-- File    : 089_create_sys_ma_rule.sql
-- Database: ICare247_Config  (Config DB — master canonical + mỗi tenant 1 Config DB)
-- Purpose : MA-1 (spec 32 / ADR-036) — quy tắc sinh mã tự động, 2 bảng:
--             1) Sys_Ma_Rule         — 1 dòng / 1 cặp (bảng, cột) cần sinh mã.
--             2) Sys_Ma_Rule_Segment — các ĐOẠN ghép nên mã, có thứ tự.
--           DEV khai qua ConfigStudio WPF ở master → ConfigSync đẩy xuống tenant.
--
-- Vì sao tách bảng con (KHÔNG dùng 1 chuỗi "mẫu mã"):
--           Mã thực tế ghép từ NHIỀU NGUỒN, có bảng cần 3 nguồn trở lên, và nguồn
--           thường phải TRA SANG BẢNG KHÁC (payload có CongTy_Id = 5, mã cần 'CT01').
--           Nhồi hết vào một chuỗi token sẽ thành ngôn ngữ mini không đọc nổi và
--           không dựng được UI cấu hình. Mỗi đoạn 1 dòng ⇒ WPF hiện lưới kéo-thả.
--           VD mã phòng ban 'CT01-PHONG-007' = LOOKUP(CongTy_Id→TC_CongTy.Ma)
--           + LITERAL('-') + LOOKUP(Cap_Id→TC_CapPhongBan.Ma) + LITERAL('-') + SEQ(3).
--
-- ⛔ KHÔNG CÓ BẢNG BỘ ĐẾM. Số lớn nhất TUYỆT ĐỐI không được lưu ở đâu — mỗi lần sinh
--           phải quét chính bảng đích lấy MAX theo tiền tố (sp_SinhMa). Bộ đếm lưu sẵn
--           sẽ lệch thực tế (import, sửa tay, xóa, restore) rồi cấp trùng mã.
--
-- Hệ quả : PHẠM VI ĐÁNH SỐ = TIỀN TỐ CỦA MÃ. Mã chứa {yy} thì tự đánh lại theo năm;
--           chứa mã công ty thì tự đánh riêng từng công ty. ⇒ KHÔNG có cột Reset_Scope
--           / Scope_Column: mọi phạm vi đều suy ra từ chính các đoạn đứng trước SEQ.
--
-- Note    : Idempotent (OBJECT_ID guard). KHÔNG seed quy tắc nào — đợt này chỉ dựng
--           hạ tầng (spec 32 §10: mã chuẩn ISO/hành chính TUYỆT ĐỐI không tự sinh).
-- Quy ước : Bảng Config DB → cột tiếng Anh, tiền tố `Sys_`, mang 4 cờ sync CFGSYNC-1
--           (db/050). KHÔNG có khối cột auto ADR-022 (khối đó của Data DB) — cùng dạng
--           Sys_Context_Param (db/060) và Ui_Lookup_Template (db/083).
-- Spec    : docs/spec/32_SINH_MA_TU_DONG_SPEC.md §3 · ADR-036.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) Sys_Ma_Rule — quy tắc (1 dòng / 1 cặp bảng+cột)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Sys_Ma_Rule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Ma_Rule
    (
        Rule_Id       INT            IDENTITY(1,1) NOT NULL,

        -- Đích áp dụng. Table_Code khớp Sys_Table.Table_Code; Column_Code thường 'Ma'
        -- (để riêng cột → sau mở cho cột mã thứ 2, vd SoChungTu, không phải đổi schema).
        Table_Code    NVARCHAR(128)  NOT NULL,
        Column_Code   NVARCHAR(128)  NOT NULL,

        Step          INT            NOT NULL DEFAULT 1,   -- bước nhảy (thường 1)
        Allow_Manual  BIT            NOT NULL DEFAULT 0,   -- 1 = user được gõ đè mã
        Is_Active     BIT            NOT NULL DEFAULT 1,
        Description   NVARCHAR(500)  NULL,

        -- 4 cờ sync CFGSYNC-1 (db/050) — đồng bộ master→tenant, tôn trọng tùy biến tenant
        Is_System     BIT            NOT NULL DEFAULT 0,
        Is_Customized BIT            NOT NULL DEFAULT 0,
        Synced_At     DATETIME       NULL,
        Source_Ver    INT            NULL,

        CONSTRAINT PK_Sys_Ma_Rule PRIMARY KEY (Rule_Id),
        CONSTRAINT CHK_Sys_Ma_Rule_Step CHECK (Step >= 1)
    );

    -- 1 cặp (bảng, cột) chỉ có ĐÚNG 1 quy tắc — engine không phải chọn giữa nhiều bản.
    CREATE UNIQUE INDEX UQ_Sys_Ma_Rule_Target
        ON dbo.Sys_Ma_Rule (Table_Code, Column_Code);
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) Sys_Ma_Rule_Segment — các đoạn ghép nên mã (thứ tự Order_No tăng dần)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Sys_Ma_Rule_Segment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Ma_Rule_Segment
    (
        Segment_Id      INT            IDENTITY(1,1) NOT NULL,
        Rule_Id         INT            NOT NULL,
        Order_No        INT            NOT NULL,        -- thứ tự ghép, 1..n

        -- LITERAL: chữ cố định  · DATE: mốc thời gian  · FIELD: giá trị thô trong payload
        -- LOOKUP : field → tra bảng khác lấy cột khác   · SEQ  : số thứ tự
        Segment_Type    NVARCHAR(20)   NOT NULL,

        -- LITERAL → chuỗi cố định ('CT', '-', '/')
        -- DATE    → định dạng: 'yyyy' | 'yy' | 'MM' | 'dd' | ghép ('yyyyMM')
        Text_Value      NVARCHAR(100)  NULL,

        -- FIELD / LOOKUP → mã cột trong payload đang lưu (vd 'CongTy_Id')
        Field_Code      NVARCHAR(128)  NULL,

        -- LOOKUP → tra: SELECT [Lookup_Val_Col] FROM [Lookup_Table] WHERE [Lookup_Key_Col] = <giá trị field>
        Lookup_Table    NVARCHAR(128)  NULL,            -- 'TC_CongTy'
        Lookup_Key_Col  NVARCHAR(128)  NULL,            -- 'Id'
        Lookup_Val_Col  NVARCHAR(128)  NULL,            -- 'Ma'

        -- Chuẩn hóa giá trị đoạn (áp cho DATE/FIELD/LOOKUP; SEQ chỉ dùng Length = số chữ số)
        Substring_Start INT            NULL,            -- cắt từ ký tự thứ n (1-based), NULL = 1
        Length          INT            NULL,            -- độ rộng CỐ ĐỊNH; NULL/0 = độ dài tự nhiên
        Pad_Char        NCHAR(1)       NULL,            -- ký tự đệm cho đủ Length (SEQ thường '0')
        Pad_Side        CHAR(1)        NULL,            -- 'L' (đệm trái) | 'R' (đệm phải)
        Text_Transform  NVARCHAR(10)   NULL,            -- NONE | UPPER | LOWER

        -- Is_Active: BẮT BUỘC cho ConfigSync tombstone. Master gỡ 1 đoạn → tenant đặt Is_Active = 0
        -- (không hard-delete). Thiếu cột này thì đoạn bị gỡ ở master sẽ NẰM LẠI trên tenant và âm thầm
        -- sinh mã sai. Engine sinh mã CHỈ đọc đoạn Is_Active = 1.
        Is_Active       BIT            NOT NULL DEFAULT 1,

        -- 4 cờ sync CFGSYNC-1 (db/050) — engine ConfigSync ném lỗi nếu bảng đồng bộ thiếu bất kỳ cột nào.
        Is_System       BIT            NOT NULL DEFAULT 0,
        Is_Customized   BIT            NOT NULL DEFAULT 0,
        Synced_At       DATETIME       NULL,
        Source_Ver      INT            NULL,

        CONSTRAINT PK_Sys_Ma_Rule_Segment PRIMARY KEY (Segment_Id),
        CONSTRAINT FK_Sys_Ma_Rule_Segment_Rule FOREIGN KEY (Rule_Id)
            REFERENCES dbo.Sys_Ma_Rule (Rule_Id),

        CONSTRAINT CHK_Sys_Ma_Seg_Type
            CHECK (Segment_Type IN (N'LITERAL', N'DATE', N'FIELD', N'LOOKUP', N'SEQ')),
        CONSTRAINT CHK_Sys_Ma_Seg_Pad
            CHECK (Pad_Side IS NULL OR Pad_Side IN ('L', 'R')),
        CONSTRAINT CHK_Sys_Ma_Seg_Transform
            CHECK (Text_Transform IS NULL OR Text_Transform IN (N'NONE', N'UPPER', N'LOWER')),
        CONSTRAINT CHK_Sys_Ma_Seg_Length
            CHECK (Length IS NULL OR Length BETWEEN 0 AND 100),

        -- Đủ dữ liệu theo từng loại đoạn (chặn cấu hình khuyết ngay ở DB, không đợi lúc lưu)
        CONSTRAINT CHK_Sys_Ma_Seg_Required CHECK (
                (Segment_Type = N'LITERAL' AND Text_Value IS NOT NULL)
             OR (Segment_Type = N'DATE'    AND Text_Value IS NOT NULL)
             OR (Segment_Type = N'FIELD'   AND Field_Code IS NOT NULL)
             OR (Segment_Type = N'LOOKUP'  AND Field_Code   IS NOT NULL
                                          AND Lookup_Table  IS NOT NULL
                                          AND Lookup_Key_Col IS NOT NULL
                                          AND Lookup_Val_Col IS NOT NULL)
             OR (Segment_Type = N'SEQ')
        )
    );

    CREATE UNIQUE INDEX UQ_Sys_Ma_Rule_Segment_Order
        ON dbo.Sys_Ma_Rule_Segment (Rule_Id, Order_No);

    -- ĐÚNG 1 đoạn SEQ mỗi quy tắc: 0 đoạn → mọi bản ghi ra cùng một mã (vỡ unique cột Ma
    -- ngay dòng thứ hai); 2 đoạn → không xác định được đoạn nào mang số khi quét MAX.
    -- Filtered unique index chặn trường hợp 2; trường hợp 0 do WPF + C# chặn (DB không
    -- diễn tả được "phải có ít nhất 1 dòng con" bằng constraint).
    -- Kèm Is_Active = 1: đoạn SEQ đã tombstone (sync gỡ) không được chặn đoạn SEQ mới thay thế.
    CREATE UNIQUE INDEX UQ_Sys_Ma_Rule_Segment_Seq
        ON dbo.Sys_Ma_Rule_Segment (Rule_Id)
        WHERE Segment_Type = N'SEQ' AND Is_Active = 1;
END;
GO

PRINT N'Migration 089 completed — Sys_Ma_Rule + Sys_Ma_Rule_Segment (Config DB). Chưa seed quy tắc nào.';
PRINT N'Nhắc: KHÔNG có bảng bộ đếm — số lớn nhất quét trực tiếp từ bảng đích (sp_SinhMa).';
GO
