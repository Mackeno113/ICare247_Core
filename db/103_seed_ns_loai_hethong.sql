-- =============================================================================
-- File    : 103_seed_ns_loai_hethong.sql
-- Database: ICare247_Solution  (Data DB per-tenant — chạy riêng cho mỗi tenant)
-- Purpose : Seed danh mục HỆ THỐNG (LaHeThong = 1) đợt NS_:
--             - NS_LoaiBienDong  (8 loại biến động chuẩn + cờ hành vi)
--             - NS_LoaiQuyetDinh (các loại quyết định nền)
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.3, §7.5.
-- Convention: CreatedBy tường minh = tài khoản admin (không dựa DEFAULT). Idempotent
--            (NOT EXISTS theo Ma). Cờ hành vi theo bảng thiết kế §7.3.
-- Prereq  : 099 (bảng danh mục), 038 (tài khoản admin bootstrap).
-- =============================================================================

SET XACT_ABORT ON;
GO

DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Chưa có tài khoản admin — chạy 038_seed_data_db_bootstrap.sql trước.', 16, 1);
    RETURN;
END;

-- ── NS_LoaiBienDong (8 loại chuẩn) ─────────────────────────────────────────
INSERT INTO dbo.NS_LoaiBienDong
    (Ma, Ten, MoGheMoi, DongGheCu, DongTatCaGhe, LaKiemNhiem, YeuCauQuyetDinh, LaHeThong, ThuTu, CreatedBy, CreatedAt)
SELECT v.Ma, v.Ten, v.MoGheMoi, v.DongGheCu, v.DongTatCaGhe, v.LaKiemNhiem, v.YeuCauQuyetDinh, 1, v.ThuTu, @AdminId, SYSUTCDATETIME()
FROM (VALUES
    (N'TIEP_NHAN',   N'Tiếp nhận / Tuyển mới',        1, 0, 0, 0, 1, 1),
    (N'BO_NHIEM',    N'Bổ nhiệm',                     1, 1, 0, 0, 1, 2),
    (N'DIEU_DONG',   N'Điều động / Thuyên chuyển',    1, 1, 0, 0, 1, 3),
    (N'LUAN_CHUYEN', N'Luân chuyển',                  1, 1, 0, 0, 1, 4),
    (N'BIET_PHAI',   N'Biệt phái',                    1, 0, 0, 0, 1, 5),
    (N'KIEM_NHIEM',  N'Kiêm nhiệm',                   1, 0, 0, 1, 1, 6),
    (N'MIEN_NHIEM',  N'Miễn nhiệm / Thôi giữ chức',   0, 1, 0, 0, 1, 7),
    (N'NGHI_VIEC',   N'Nghỉ việc / Chấm dứt HĐLĐ',    0, 1, 1, 0, 1, 8)
) AS v(Ma, Ten, MoGheMoi, DongGheCu, DongTatCaGhe, LaKiemNhiem, YeuCauQuyetDinh, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NS_LoaiBienDong t WHERE t.Ma = v.Ma AND t.IsDeleted = 0);
GO

-- ── NS_LoaiQuyetDinh (các loại nền — tenant bổ sung thêm tùy ý) ─────────────
DECLARE @AdminId BIGINT =
    (SELECT Id FROM dbo.HT_NguoiDung WHERE TenDangNhap = N'admin' AND IsDeleted = 0);

INSERT INTO dbo.NS_LoaiQuyetDinh (Ma, Ten, Nhom, LaHeThong, ThuTu, CreatedBy, CreatedAt)
SELECT v.Ma, v.Ten, v.Nhom, 1, v.ThuTu, @AdminId, SYSUTCDATETIME()
FROM (VALUES
    (N'BIEN_DONG_NHAN_SU', N'Biến động nhân sự',  N'BienDong',   1),  -- cả cụm biến động = 1 loại QĐ
    (N'HOP_DONG',          N'Hợp đồng lao động',  N'HopDong',    2),
    (N'DIEN_BIEN_LUONG',   N'Diễn biến lương',    N'Luong',      3),
    (N'KHEN_THUONG',       N'Khen thưởng',        N'KhenThuong', 4),
    (N'KY_LUAT',           N'Kỷ luật',            N'KyLuat',     5),
    (N'CHAM_DUT_HDLD',     N'Chấm dứt HĐLĐ',      N'HopDong',    6)
) AS v(Ma, Ten, Nhom, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NS_LoaiQuyetDinh t WHERE t.Ma = v.Ma AND t.IsDeleted = 0);
GO
