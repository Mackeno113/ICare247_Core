-- =============================================================================
-- File    : 084_create_fnt_congty_theoquyen.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy SAU db/082)
-- Purpose : PICKER-P4 — inline TVF fnt_CongTyTheoQuyen(@NguoiDungID): cây công ty user được
--           truy cập = gán riêng (HT_NguoiDung_CongTy) ∪ theo vai trò (HT_VaiTro_CongTy ⨝
--           HT_NguoiDung_VaiTro); chưa được phân công gì → mọi công ty active (default-open,
--           cùng semantics MeCompanyRepository). Dùng cho mẫu lookup TPL_CONG_TY (db/083):
--           engine cấm chuỗi con DDL/DML trong custom_sql (kể cả "IsDeleted") nên logic lọc
--           phải nằm trong hàm này, câu SQL của mẫu chỉ còn SELECT ... FROM fnt_...(@NguoiDungID).
-- Naming  : TVF tiền tố fnt_, scalar function tiền tố fns_ (quy tắc 2026-07-18).
-- Note    : CREATE OR ALTER — idempotent. Đọc-only. Có bước dọn tên cũ fn_CongTyTheoQuyen
--           (bản đầu đã chạy trước khi chốt quy tắc fnt_/fns_).
-- =============================================================================

USE [ICare247_Solution];
GO

-- Dọn tên cũ (bản đầu dùng fn_ — đổi sang fnt_ theo quy tắc đặt tên)
IF OBJECT_ID('dbo.fn_CongTyTheoQuyen', 'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_CongTyTheoQuyen;
GO

CREATE OR ALTER FUNCTION dbo.fnt_CongTyTheoQuyen (@NguoiDungID BIGINT)
RETURNS TABLE
AS
RETURN
    SELECT c.Id, c.Ma, c.Ten, c.TenVietTat, c.CongTy_Cha_Id
    FROM dbo.TC_CongTy c
    WHERE c.IsDeleted = 0
      AND (
            -- Chưa được phân công ở cả 2 nguồn → default-open (mọi công ty active)
            (
                NOT EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_CongTy uc
                            WHERE uc.NguoiDung_Id = @NguoiDungID AND uc.IsDeleted = 0)
                AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_CongTy vc
                                JOIN dbo.HT_NguoiDung_VaiTro uv
                                     ON uv.VaiTro_Id = vc.VaiTro_Id AND uv.IsDeleted = 0
                                WHERE uv.NguoiDung_Id = @NguoiDungID AND vc.IsDeleted = 0)
            )
            -- Gán riêng trực tiếp
            OR EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_CongTy uc
                       WHERE uc.NguoiDung_Id = @NguoiDungID AND uc.CongTy_Id = c.Id AND uc.IsDeleted = 0)
            -- Kế thừa động theo vai trò
            OR EXISTS (SELECT 1 FROM dbo.HT_VaiTro_CongTy vc
                       JOIN dbo.HT_NguoiDung_VaiTro uv
                            ON uv.VaiTro_Id = vc.VaiTro_Id AND uv.NguoiDung_Id = @NguoiDungID AND uv.IsDeleted = 0
                       WHERE vc.CongTy_Id = c.Id AND vc.IsDeleted = 0)
          );
GO

PRINT N'Migration 084 completed — fnt_CongTyTheoQuyen (nguồn mẫu lookup TPL_CONG_TY).';
GO
