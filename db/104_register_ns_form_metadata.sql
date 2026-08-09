-- =============================================================================
-- File    : 104_register_ns_form_metadata.sql
-- Database: ICare247_Config  (Config DB — metadata Form Engine)
-- Purpose : Đăng ký metadata cho Form Engine đợt NS_ (hồ sơ nhân viên):
--             - Sys_Table + Sys_Column cho NS_NhanVien + 6 bảng con
--             - Sys_Lookup tĩnh cho enum GioiTinh / TinhTrangHonNhan / LoaiDiaChi /
--               LoaiGiayToNN (Item_Code = '0/1/2…' để khớp cột tinyint qua ép kiểu ngầm)
-- Spec    : docs/spec/11_DATA_DB_SCHEMA.md §7.3 · db/097, db/098 (bảng Data DB).
-- Pattern : theo db/047_seed_ui_form_ht_vaitro.sql. Idempotent (NOT EXISTS theo code).
-- Note    : Bảng đích nằm ở Data DB per-tenant; đây chỉ khai metadata ở Config DB.
--           Form + field + lookup ở file kế tiếp (db/105).
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 1. Sys_Table — 7 bảng đích
-- ═══════════════════════════════════════════════════════════════════════════════
INSERT INTO dbo.Sys_Table (Table_Code, Table_Name, Schema_Name)
SELECT v.Code, v.Name, N'dbo'
FROM (VALUES
        (N'NS_NhanVien',                N'Nhân viên'),
        (N'NS_NhanVien_DiaChi',         N'Địa chỉ nhân viên'),
        (N'NS_NhanVien_HocVan',         N'Học vấn nhân viên'),
        (N'NS_NhanVien_NgoaiNgu',       N'Ngoại ngữ nhân viên'),
        (N'NS_NhanVien_ChungChi',       N'Chứng chỉ nhân viên'),
        (N'NS_NhanVien_ThanNhan',       N'Thân nhân nhân viên'),
        (N'NS_NhanVien_GiayToNuocNgoai',N'Giấy tờ người nước ngoài')
     ) v(Code, Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Table t WHERE t.Table_Code = v.Code);
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 2. Sys_Column
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── NS_NhanVien ────────────────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',                 N'bigint',   N'Int64',    0, 1, 1),
        (N'MaNhanVien',         N'nvarchar', N'String',   0, 0, 0),
        (N'HoTen',              N'nvarchar', N'String',   0, 0, 0),
        (N'TenThuongDung',      N'nvarchar', N'String',   1, 0, 0),
        (N'AnhDaiDien_Id',      N'bigint',   N'Int64',    1, 0, 0),
        (N'NgaySinh',           N'date',     N'DateTime', 0, 0, 0),
        (N'GioiTinh',           N'tinyint',  N'Byte',     0, 0, 0),
        (N'NoiSinh',            N'nvarchar', N'String',   1, 0, 0),
        (N'NoiSinh_PhuongXa_Id',N'bigint',   N'Int64',    1, 0, 0),
        (N'QuocTich_Id',        N'bigint',   N'Int64',    0, 0, 0),
        (N'DanToc_Id',          N'bigint',   N'Int64',    1, 0, 0),
        (N'TonGiao_Id',         N'bigint',   N'Int64',    1, 0, 0),
        (N'TinhTrangHonNhan',   N'tinyint',  N'Byte',     1, 0, 0),
        (N'NhomMau',            N'nvarchar', N'String',   1, 0, 0),
        (N'ChieuCao',           N'smallint', N'Int16',    1, 0, 0),
        (N'CanNang',            N'smallint', N'Int16',    1, 0, 0),
        (N'SoCCCD',             N'nvarchar', N'String',   1, 0, 0),
        (N'NgayCapCCCD',        N'date',     N'DateTime', 1, 0, 0),
        (N'NoiCapCCCD',         N'nvarchar', N'String',   1, 0, 0),
        (N'SoCMND',             N'nvarchar', N'String',   1, 0, 0),
        (N'MaSoThue',           N'nvarchar', N'String',   1, 0, 0),
        (N'DienThoai',          N'nvarchar', N'String',   1, 0, 0),
        (N'Email',              N'nvarchar', N'String',   1, 0, 0),
        (N'EmailCaNhan',        N'nvarchar', N'String',   1, 0, 0),
        (N'NguoiLienHeKhan',    N'nvarchar', N'String',   1, 0, 0),
        (N'DienThoaiKhan',      N'nvarchar', N'String',   1, 0, 0),
        (N'QuanHeLienHeKhan',   N'nvarchar', N'String',   1, 0, 0),
        (N'SoBHXH',             N'nvarchar', N'String',   1, 0, 0),
        (N'SoBHYT',             N'nvarchar', N'String',   1, 0, 0),
        (N'NoiKhamChuaBenh_Id', N'bigint',   N'Int64',    1, 0, 0),
        (N'NganHang_Id',        N'bigint',   N'Int64',    1, 0, 0),
        (N'ChiNhanhNganHang',   N'nvarchar', N'String',   1, 0, 0),
        (N'SoTaiKhoan',         N'nvarchar', N'String',   1, 0, 0),
        (N'TenChuTaiKhoan',     N'nvarchar', N'String',   1, 0, 0),
        (N'TrinhDoHocVan_Id',   N'bigint',   N'Int64',    1, 0, 0),
        (N'NgayBatDauLamViec',  N'date',     N'DateTime', 0, 0, 0),
        (N'MaNVCu',             N'nvarchar', N'String',   1, 0, 0),
        (N'GhiChu',             N'nvarchar', N'String',   1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_DiaChi ─────────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_DiaChi');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',          N'bigint',   N'Int64',   0, 1, 1),
        (N'NhanVien_Id', N'bigint',   N'Int64',   0, 0, 0),
        (N'LoaiDiaChi',  N'tinyint',  N'Byte',    0, 0, 0),
        (N'SoNha',       N'nvarchar', N'String',  1, 0, 0),
        (N'PhuongXa_Id', N'bigint',   N'Int64',   1, 0, 0),
        (N'LaChuHo',     N'bit',      N'Boolean', 0, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_HocVan ─────────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_HocVan');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',               N'bigint',   N'Int64',    0, 1, 1),
        (N'NhanVien_Id',      N'bigint',   N'Int64',    0, 0, 0),
        (N'TruongDaoTao',     N'nvarchar', N'String',   1, 0, 0),
        (N'ChuyenNganh',      N'nvarchar', N'String',   1, 0, 0),
        (N'TrinhDoHocVan_Id', N'bigint',   N'Int64',    1, 0, 0),
        (N'HeDaoTao',         N'nvarchar', N'String',   1, 0, 0),
        (N'XepLoai',          N'nvarchar', N'String',   1, 0, 0),
        (N'NamTotNghiep',     N'smallint', N'Int16',    1, 0, 0),
        (N'VanBang_File_Id',  N'bigint',   N'Int64',    1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_NgoaiNgu ───────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_NgoaiNgu');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',          N'bigint',   N'Int64',    0, 1, 1),
        (N'NhanVien_Id', N'bigint',   N'Int64',    0, 0, 0),
        (N'NgoaiNgu_Id', N'bigint',   N'Int64',    0, 0, 0),
        (N'TrinhDo',     N'nvarchar', N'String',   1, 0, 0),
        (N'XepLoai',     N'nvarchar', N'String',   1, 0, 0),
        (N'TenChungChi', N'nvarchar', N'String',   1, 0, 0),
        (N'NgayCap',     N'date',     N'DateTime', 1, 0, 0),
        (N'NgayHetHan',  N'date',     N'DateTime', 1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_ChungChi ───────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ChungChi');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',          N'bigint',   N'Int64',    0, 1, 1),
        (N'NhanVien_Id', N'bigint',   N'Int64',    0, 0, 0),
        (N'TenChungChi', N'nvarchar', N'String',   0, 0, 0),
        (N'NoiCap',      N'nvarchar', N'String',   1, 0, 0),
        (N'NgayCap',     N'date',     N'DateTime', 1, 0, 0),
        (N'NgayHetHan',  N'date',     N'DateTime', 1, 0, 0),
        (N'File_Id',     N'bigint',   N'Int64',    1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_ThanNhan ───────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ThanNhan');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',               N'bigint',   N'Int64',   0, 1, 1),
        (N'NhanVien_Id',      N'bigint',   N'Int64',   0, 0, 0),
        (N'HoTen',            N'nvarchar', N'String',  0, 0, 0),
        (N'QuanHe_Id',        N'bigint',   N'Int64',   1, 0, 0),
        (N'NamSinh',          N'smallint', N'Int16',   1, 0, 0),
        (N'NgheNghiep',       N'nvarchar', N'String',  1, 0, 0),
        (N'LaGiamTruThue',    N'bit',      N'Boolean', 0, 0, 0),
        (N'MaSoThuePhuThuoc', N'nvarchar', N'String',  1, 0, 0),
        (N'DienThoai',        N'nvarchar', N'String',  1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ── NS_NhanVien_GiayToNuocNgoai ────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_GiayToNuocNgoai');
INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @T, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',          N'bigint',   N'Int64',    0, 1, 1),
        (N'NhanVien_Id', N'bigint',   N'Int64',    0, 0, 0),
        (N'LoaiGiayTo',  N'tinyint',  N'Byte',     0, 0, 0),
        (N'SoGiayTo',    N'nvarchar', N'String',   0, 0, 0),
        (N'NgayCap',     N'date',     N'DateTime', 1, 0, 0),
        (N'NgayHetHan',  N'date',     N'DateTime', 1, 0, 0),
        (N'NoiCap',      N'nvarchar', N'String',   1, 0, 0),
        (N'TrangThai',   N'nvarchar', N'String',   1, 0, 0),
        (N'File_Id',     N'bigint',   N'Int64',    1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @T AND c.Column_Code = v.Code);
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- 3. Sys_Lookup tĩnh cho enum (Item_Code = số để khớp cột tinyint qua ép kiểu ngầm)
-- ═══════════════════════════════════════════════════════════════════════════════
-- Tenant_Id đã bị DROP khỏi Sys_Lookup (db/078 · ADR-035 — DB-per-tenant). KHÔNG dùng cột này.
MERGE dbo.Sys_Lookup AS tgt
USING (VALUES
        -- GioiTinh: 0=Nữ, 1=Nam, 2=Khác
        (N'NS_GIOITINH', N'0', N'ns.gioitinh.nu',    1),
        (N'NS_GIOITINH', N'1', N'ns.gioitinh.nam',   2),
        (N'NS_GIOITINH', N'2', N'ns.gioitinh.khac',  3),
        -- TinhTrangHonNhan: 0=Độc thân,1=Kết hôn,2=Ly hôn,3=Góa
        (N'NS_HONNHAN',  N'0', N'ns.honnhan.docthan',1),
        (N'NS_HONNHAN',  N'1', N'ns.honnhan.kethon', 2),
        (N'NS_HONNHAN',  N'2', N'ns.honnhan.lyhon',  3),
        (N'NS_HONNHAN',  N'3', N'ns.honnhan.goa',    4),
        -- LoaiDiaChi: 1=Thường trú,2=Tạm trú,3=Quê quán
        (N'NS_LOAIDIACHI', N'1', N'ns.diachi.thuongtru', 1),
        (N'NS_LOAIDIACHI', N'2', N'ns.diachi.tamtru',    2),
        (N'NS_LOAIDIACHI', N'3', N'ns.diachi.quequan',   3),
        -- LoaiGiayTo NN: 1=Hộ chiếu,2=Visa,3=Giấy phép LĐ
        (N'NS_LOAIGTNN', N'1', N'ns.gtnn.hochieu', 1),
        (N'NS_LOAIGTNN', N'2', N'ns.gtnn.visa',    2),
        (N'NS_LOAIGTNN', N'3', N'ns.gtnn.gpld',    3)
     ) AS src (Lookup_Code, Item_Code, Label_Key, Sort_Order)
ON  tgt.Lookup_Code = src.Lookup_Code AND tgt.Item_Code = src.Item_Code
WHEN NOT MATCHED THEN
    INSERT (Lookup_Code, Item_Code, Label_Key, Sort_Order)
    VALUES (src.Lookup_Code, src.Item_Code, src.Label_Key, src.Sort_Order);
GO

PRINT N'099 done — Sys_Table(7) + Sys_Column + Sys_Lookup(enum NS_) registered.';
GO
