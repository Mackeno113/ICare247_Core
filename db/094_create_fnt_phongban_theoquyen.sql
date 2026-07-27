-- =============================================================================
-- File    : 094_create_fnt_phongban_theoquyen.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy SAU db/092, db/093)
-- Purpose : fnt_PhongBanTheoQuyen(@NguoiDungID) — cây phòng ban user được truy cập =
--           gán riêng (HT_NguoiDung_PhongBan) ∪ theo vai trò (HT_VaiTro_PhongBan ⨝
--           HT_NguoiDung_VaiTro); chưa được phân công gì → mọi phòng ban active
--           (default-open, cùng semantics fnt_CongTyTheoQuyen db/084). Đúng node được
--           gán — KHÔNG tự gộp phòng ban con (khác cây công ty, chốt với user 2026-07-25).
-- Naming  : TVF tiền tố fnt_, scalar function tiền tố fns_ (quy tắc 2026-07-18).
-- Note    : CREATE OR ALTER — idempotent. Đọc-only.
-- =============================================================================

USE [ICare247_Solution];
GO

CREATE OR ALTER FUNCTION dbo.fnt_PhongBanTheoQuyen (@NguoiDungID BIGINT)
RETURNS TABLE
AS
RETURN
    SELECT p.Id, p.Ma, p.Ten, p.TenVietTat, p.PhongBan_Cha_Id, p.CongTy_Id
    FROM dbo.TC_PhongBan p
    WHERE p.IsDeleted = 0
      AND (
            -- Chưa được phân công ở cả 2 nguồn → default-open (mọi phòng ban active)
            (
                NOT EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_PhongBan up
                            WHERE up.NguoiDung_Id = @NguoiDungID AND up.IsDeleted = 0)
                AND NOT EXISTS (SELECT 1 FROM dbo.HT_VaiTro_PhongBan vp
                                JOIN dbo.HT_NguoiDung_VaiTro uv
                                     ON uv.VaiTro_Id = vp.VaiTro_Id AND uv.IsDeleted = 0
                                WHERE uv.NguoiDung_Id = @NguoiDungID AND vp.IsDeleted = 0)
            )
            -- Gán riêng trực tiếp
            OR EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_PhongBan up
                       WHERE up.NguoiDung_Id = @NguoiDungID AND up.PhongBan_Id = p.Id AND up.IsDeleted = 0)
            -- Kế thừa động theo vai trò
            OR EXISTS (SELECT 1 FROM dbo.HT_VaiTro_PhongBan vp
                       JOIN dbo.HT_NguoiDung_VaiTro uv
                            ON uv.VaiTro_Id = vp.VaiTro_Id AND uv.NguoiDung_Id = @NguoiDungID AND uv.IsDeleted = 0
                       WHERE vp.PhongBan_Id = p.Id AND vp.IsDeleted = 0)
          );
GO

PRINT N'Migration 094 completed — fnt_PhongBanTheoQuyen (phạm vi phòng ban hiệu lực của user).';
GO
