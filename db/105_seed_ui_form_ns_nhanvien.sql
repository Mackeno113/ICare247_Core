-- =============================================================================
-- File    : 105_seed_ui_form_ns_nhanvien.sql
-- Database: ICare247_Config  (Config DB — Form Engine)
-- Purpose : Sinh form cho đợt NS_ (Position Management — Phương án 1, master-detail sau):
--             - NS_NhanVien : form lõi full-page (Display_Mode='Tab'), 8 section, ~37 field.
--             - 6 form CRUD ĐỘC LẬP cho bảng con (địa chỉ/học vấn/ngoại ngữ/chứng chỉ/
--               thân nhân/giấy tờ NN) — mỗi form chọn nhân viên qua NhanVien_Id.
-- Depends : db/104 (Sys_Table + Sys_Column + Sys_Lookup enum).
-- Engine  : Editor_Type khớp FieldRenderer.razor (text/textarea/number/date/bool/select/
--           combobox/attachment). FK động = combobox + Lookup_Source='dynamic' + Ui_Field_Lookup.
--           Enum tinyint = select tĩnh + Lookup_Source='static' + Lookup_Code (Item_Code số).
-- Note    : Không dùng Ui_Tab (0 tab → render phẳng). Địa chỉ dùng combobox DM_PhuongXa
--           (composite 'address' để đợt sau khi chốt ControlProps). Idempotent.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- A. FORM LÕI — NS_NhanVien
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── A1. Ui_Form ─────────────────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien', @T, N'Web', N'Tab');   -- 'Tab' = trang đầy đủ (routed)
GO

-- ── A2. Ui_Section (8 nhóm) ─────────────────────────────────────────────────
DECLARE @F INT = (SELECT Form_Id FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien');
INSERT INTO dbo.Ui_Section (Form_Id, Section_Code, Title_Key, Order_No)
SELECT @F, v.Code, v.Title, v.OrderNo
FROM (VALUES
        (N'DINHDANH', N'Định danh',            1),
        (N'CANHAN',   N'Thông tin cá nhân',    2),
        (N'GIAYTO',   N'Giấy tờ & thuế',       3),
        (N'LIENHE',   N'Liên hệ',              4),
        (N'BAOHIEM',  N'Bảo hiểm',             5),
        (N'NGANHANG', N'Ngân hàng',            6),
        (N'HOCVAN',   N'Học vấn',              7),
        (N'MOCGOC',   N'Mốc & ghi chú',        8)
     ) v(Code, Title, OrderNo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Section s WHERE s.Form_Id = @F AND s.Section_Code = v.Code);
GO

-- ── A3. Ui_Field (~37 field) ────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien');
INSERT INTO dbo.Ui_Field (Form_Id, Section_Id, Column_Id, Editor_Type, Label_Key,
                          Order_No, Col_Span, Is_Required, Show_In_List, Is_Unique,
                          Lookup_Source, Lookup_Code)
SELECT @F, sec.Section_Id, sc.Column_Id, v.Editor, v.Label,
       v.OrderNo, v.ColSpan, v.Req, v.ShowList, v.Uniq, v.LkSrc, v.LkCode
FROM (VALUES
    --  Col                   SecCode      Editor      Label                    Ord Span Req Show Uniq LkSrc       LkCode
        (N'MaNhanVien',        N'DINHDANH', N'text',    N'Mã nhân viên',           1, 1, 1, 1, 1, NULL,        NULL),
        (N'HoTen',             N'DINHDANH', N'text',    N'Họ và tên',              2, 2, 1, 1, 0, NULL,        NULL),
        (N'TenThuongDung',     N'DINHDANH', N'text',    N'Tên thường dùng',        3, 1, 0, 0, 0, NULL,        NULL),
        (N'AnhDaiDien_Id',     N'DINHDANH', N'attachment', N'Ảnh đại diện',        4, 1, 0, 0, 0, NULL,        NULL),

        (N'NgaySinh',          N'CANHAN',   N'date',    N'Ngày sinh',              5, 1, 1, 1, 0, NULL,        NULL),
        (N'GioiTinh',          N'CANHAN',   N'select',  N'Giới tính',              6, 1, 1, 1, 0, N'static',   N'NS_GIOITINH'),
        (N'QuocTich_Id',       N'CANHAN',   N'combobox',N'Quốc tịch',              7, 1, 1, 0, 0, N'dynamic',  NULL),
        (N'DanToc_Id',         N'CANHAN',   N'combobox',N'Dân tộc',                8, 1, 0, 0, 0, N'dynamic',  NULL),
        (N'TonGiao_Id',        N'CANHAN',   N'combobox',N'Tôn giáo',               9, 1, 0, 0, 0, N'dynamic',  NULL),
        (N'TinhTrangHonNhan',  N'CANHAN',   N'select',  N'Tình trạng hôn nhân',   10, 1, 0, 0, 0, N'static',   N'NS_HONNHAN'),
        (N'NoiSinh',           N'CANHAN',   N'text',    N'Nơi sinh',              11, 2, 0, 0, 0, NULL,        NULL),
        (N'NoiSinh_PhuongXa_Id',N'CANHAN',  N'combobox',N'Phường/Xã nơi sinh',    12, 1, 0, 0, 0, N'dynamic',  NULL),
        (N'NhomMau',           N'CANHAN',   N'text',    N'Nhóm máu',              13, 1, 0, 0, 0, NULL,        NULL),
        (N'ChieuCao',          N'CANHAN',   N'number',  N'Chiều cao (cm)',        14, 1, 0, 0, 0, NULL,        NULL),
        (N'CanNang',           N'CANHAN',   N'number',  N'Cân nặng (kg)',         15, 1, 0, 0, 0, NULL,        NULL),

        (N'SoCCCD',            N'GIAYTO',   N'text',    N'Số CCCD',               16, 1, 0, 1, 1, NULL,        NULL),
        (N'NgayCapCCCD',       N'GIAYTO',   N'date',    N'Ngày cấp CCCD',         17, 1, 0, 0, 0, NULL,        NULL),
        (N'NoiCapCCCD',        N'GIAYTO',   N'text',    N'Nơi cấp CCCD',          18, 2, 0, 0, 0, NULL,        NULL),
        (N'SoCMND',            N'GIAYTO',   N'text',    N'Số CMND (cũ)',          19, 1, 0, 0, 0, NULL,        NULL),
        (N'MaSoThue',          N'GIAYTO',   N'text',    N'Mã số thuế',            20, 1, 0, 0, 0, NULL,        NULL),

        (N'DienThoai',         N'LIENHE',   N'text',    N'Điện thoại',            21, 1, 0, 1, 0, NULL,        NULL),
        (N'Email',             N'LIENHE',   N'text',    N'Email công việc',       22, 1, 0, 0, 0, NULL,        NULL),
        (N'EmailCaNhan',       N'LIENHE',   N'text',    N'Email cá nhân',         23, 1, 0, 0, 0, NULL,        NULL),
        (N'NguoiLienHeKhan',   N'LIENHE',   N'text',    N'Người liên hệ khẩn',    24, 1, 0, 0, 0, NULL,        NULL),
        (N'DienThoaiKhan',     N'LIENHE',   N'text',    N'ĐT liên hệ khẩn',       25, 1, 0, 0, 0, NULL,        NULL),
        (N'QuanHeLienHeKhan',  N'LIENHE',   N'text',    N'Quan hệ (khẩn)',        26, 1, 0, 0, 0, NULL,        NULL),

        (N'SoBHXH',            N'BAOHIEM',  N'text',    N'Số BHXH',               27, 1, 0, 0, 0, NULL,        NULL),
        (N'SoBHYT',            N'BAOHIEM',  N'text',    N'Số BHYT',               28, 1, 0, 0, 0, NULL,        NULL),
        (N'NoiKhamChuaBenh_Id',N'BAOHIEM',  N'combobox',N'Nơi KCB ban đầu',       29, 2, 0, 0, 0, N'dynamic',  NULL),

        (N'NganHang_Id',       N'NGANHANG', N'combobox',N'Ngân hàng',             30, 1, 0, 0, 0, N'dynamic',  NULL),
        (N'ChiNhanhNganHang',  N'NGANHANG', N'text',    N'Chi nhánh',             31, 1, 0, 0, 0, NULL,        NULL),
        (N'SoTaiKhoan',        N'NGANHANG', N'text',    N'Số tài khoản',          32, 1, 0, 0, 0, NULL,        NULL),
        (N'TenChuTaiKhoan',    N'NGANHANG', N'text',    N'Tên chủ tài khoản',     33, 1, 0, 0, 0, NULL,        NULL),

        (N'TrinhDoHocVan_Id',  N'HOCVAN',   N'combobox',N'Trình độ học vấn',      34, 1, 0, 0, 0, N'dynamic',  NULL),

        (N'NgayBatDauLamViec', N'MOCGOC',   N'date',    N'Ngày bắt đầu làm việc', 35, 1, 1, 1, 0, NULL,        NULL),
        (N'MaNVCu',            N'MOCGOC',   N'text',    N'Mã NV cũ',              36, 1, 0, 0, 0, NULL,        NULL),
        (N'GhiChu',            N'MOCGOC',   N'textarea',N'Ghi chú',               37, 3, 0, 0, 0, NULL,        NULL)
     ) v(Col, SecCode, Editor, Label, OrderNo, ColSpan, Req, ShowList, Uniq, LkSrc, LkCode)
JOIN      dbo.Sys_Column  sc  ON sc.Table_Id = @T AND sc.Column_Code = v.Col
LEFT JOIN dbo.Ui_Section  sec ON sec.Form_Id = @F AND sec.Section_Code = v.SecCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO

-- ── A4. Ui_Field_Lookup (7 FK động của form lõi) ────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', v.Src, N'Id', N'Ten', N'Ten', 1
FROM (VALUES
        (N'QuocTich_Id',        N'DM_QuocGia'),
        (N'DanToc_Id',          N'DM_DanToc'),
        (N'TonGiao_Id',         N'DM_TonGiao'),
        (N'NoiSinh_PhuongXa_Id',N'DM_PhuongXa'),
        (N'NoiKhamChuaBenh_Id', N'DM_NoiKCB'),
        (N'NganHang_Id',        N'DM_NganHang'),
        (N'TrinhDoHocVan_Id',   N'DM_TrinhDoHocVan')
     ) v(Col, Src)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
JOIN dbo.Ui_Field   uf ON uf.Form_Id  = @F AND uf.Column_Id  = sc.Column_Id
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- B. 6 FORM CRUD ĐỘC LẬP cho bảng con (mỗi form chọn nhân viên qua NhanVien_Id)
--    Dùng lại tới khi engine có master-detail (Phương án 3).
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── B1. NS_NhanVien_DiaChi ──────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_DiaChi');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_DiaChi')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_DiaChi', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_DiaChi');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_DiaChi');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id', N'combobox', N'Nhân viên',   1, 1, 1, N'dynamic', NULL),
        (N'LoaiDiaChi',  N'select',   N'Loại địa chỉ',2, 1, 1, N'static',  N'NS_LOAIDIACHI'),
        (N'SoNha',       N'text',     N'Số nhà/đường',3, 0, 1, NULL,       NULL),
        (N'PhuongXa_Id', N'combobox', N'Phường/Xã',   4, 0, 1, N'dynamic', NULL),
        (N'LaChuHo',     N'bool',     N'Là chủ hộ',   5, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_DiaChi');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_DiaChi');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', v.Src, N'Id', v.Disp, v.Disp, 1
FROM (VALUES (N'NhanVien_Id', N'NS_NhanVien', N'HoTen'), (N'PhuongXa_Id', N'DM_PhuongXa', N'Ten')) v(Col, Src, Disp)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
JOIN dbo.Ui_Field   uf ON uf.Form_Id  = @F AND uf.Column_Id  = sc.Column_Id
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ── B2. NS_NhanVien_HocVan ──────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_HocVan');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_HocVan')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_HocVan', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_HocVan');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_HocVan');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id',      N'combobox',  N'Nhân viên',       1, 1, 1, N'dynamic', NULL),
        (N'TruongDaoTao',     N'text',      N'Trường đào tạo',  2, 0, 1, NULL,       NULL),
        (N'ChuyenNganh',      N'text',      N'Chuyên ngành',    3, 0, 1, NULL,       NULL),
        (N'TrinhDoHocVan_Id', N'combobox',  N'Trình độ',        4, 0, 1, N'dynamic', NULL),
        (N'HeDaoTao',         N'text',      N'Hệ đào tạo',      5, 0, 0, NULL,       NULL),
        (N'XepLoai',          N'text',      N'Xếp loại',        6, 0, 0, NULL,       NULL),
        (N'NamTotNghiep',     N'number',    N'Năm tốt nghiệp',  7, 0, 1, NULL,       NULL),
        (N'VanBang_File_Id',  N'attachment',N'Văn bằng',        8, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_HocVan');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_HocVan');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', v.Src, N'Id', v.Disp, v.Disp, 1
FROM (VALUES (N'NhanVien_Id', N'NS_NhanVien', N'HoTen'), (N'TrinhDoHocVan_Id', N'DM_TrinhDoHocVan', N'Ten')) v(Col, Src, Disp)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
JOIN dbo.Ui_Field   uf ON uf.Form_Id  = @F AND uf.Column_Id  = sc.Column_Id
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ── B3. NS_NhanVien_NgoaiNgu ────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_NgoaiNgu');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_NgoaiNgu')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_NgoaiNgu', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_NgoaiNgu');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_NgoaiNgu');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id', N'combobox', N'Nhân viên',      1, 1, 1, N'dynamic', NULL),
        (N'NgoaiNgu_Id', N'combobox', N'Ngoại ngữ',      2, 1, 1, N'dynamic', NULL),
        (N'TrinhDo',     N'text',     N'Trình độ',       3, 0, 1, NULL,       NULL),
        (N'XepLoai',     N'text',     N'Xếp loại',       4, 0, 0, NULL,       NULL),
        (N'TenChungChi', N'text',     N'Tên chứng chỉ',  5, 0, 0, NULL,       NULL),
        (N'NgayCap',     N'date',     N'Ngày cấp',       6, 0, 0, NULL,       NULL),
        (N'NgayHetHan',  N'date',     N'Ngày hết hạn',   7, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_NgoaiNgu');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_NgoaiNgu');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', v.Src, N'Id', v.Disp, v.Disp, 1
FROM (VALUES (N'NhanVien_Id', N'NS_NhanVien', N'HoTen'), (N'NgoaiNgu_Id', N'DM_NgoaiNgu', N'Ten')) v(Col, Src, Disp)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
JOIN dbo.Ui_Field   uf ON uf.Form_Id  = @F AND uf.Column_Id  = sc.Column_Id
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ── B4. NS_NhanVien_ChungChi ────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ChungChi');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_ChungChi')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_ChungChi', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ChungChi');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_ChungChi');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id', N'combobox',  N'Nhân viên',     1, 1, 1, N'dynamic', NULL),
        (N'TenChungChi', N'text',      N'Tên chứng chỉ', 2, 1, 1, NULL,       NULL),
        (N'NoiCap',      N'text',      N'Nơi cấp',       3, 0, 1, NULL,       NULL),
        (N'NgayCap',     N'date',      N'Ngày cấp',      4, 0, 1, NULL,       NULL),
        (N'NgayHetHan',  N'date',      N'Ngày hết hạn',  5, 0, 0, NULL,       NULL),
        (N'File_Id',     N'attachment',N'Tệp đính kèm',  6, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ChungChi');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_ChungChi');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', N'NS_NhanVien', N'Id', N'HoTen', N'HoTen', 1
FROM dbo.Sys_Column sc
JOIN dbo.Ui_Field   uf ON uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id
WHERE sc.Table_Id = @T AND sc.Column_Code = N'NhanVien_Id'
  AND NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ── B5. NS_NhanVien_ThanNhan ────────────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ThanNhan');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_ThanNhan')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_ThanNhan', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ThanNhan');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_ThanNhan');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id',      N'combobox', N'Nhân viên',       1, 1, 1, N'dynamic', NULL),
        (N'HoTen',            N'text',     N'Họ tên thân nhân',2, 1, 1, NULL,       NULL),
        (N'QuanHe_Id',        N'combobox', N'Quan hệ',         3, 0, 1, N'dynamic', NULL),
        (N'NamSinh',          N'number',   N'Năm sinh',        4, 0, 1, NULL,       NULL),
        (N'NgheNghiep',       N'text',     N'Nghề nghiệp',     5, 0, 0, NULL,       NULL),
        (N'LaGiamTruThue',    N'bool',     N'Giảm trừ thuế',   6, 0, 1, NULL,       NULL),
        (N'MaSoThuePhuThuoc', N'text',     N'MST người PT',    7, 0, 0, NULL,       NULL),
        (N'DienThoai',        N'text',     N'Điện thoại',      8, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_ThanNhan');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_ThanNhan');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', v.Src, N'Id', v.Disp, v.Disp, 1
FROM (VALUES (N'NhanVien_Id', N'NS_NhanVien', N'HoTen'), (N'QuanHe_Id', N'DM_QuanHeThanNhan', N'Ten')) v(Col, Src, Disp)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
JOIN dbo.Ui_Field   uf ON uf.Form_Id  = @F AND uf.Column_Id  = sc.Column_Id
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

-- ── B6. NS_NhanVien_GiayToNuocNgoai ─────────────────────────────────────────
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_GiayToNuocNgoai');
IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'NS_NhanVien_GiayToNuocNgoai')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'NS_NhanVien_GiayToNuocNgoai', @T, N'Web', N'Popup');
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_GiayToNuocNgoai');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_GiayToNuocNgoai');
INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Is_Required, Show_In_List, Lookup_Source, Lookup_Code)
SELECT @F, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.Req, v.ShowList, v.LkSrc, v.LkCode
FROM (VALUES
        (N'NhanVien_Id', N'combobox',  N'Nhân viên',     1, 1, 1, N'dynamic', NULL),
        (N'LoaiGiayTo',  N'select',    N'Loại giấy tờ',  2, 1, 1, N'static',  N'NS_LOAIGTNN'),
        (N'SoGiayTo',    N'text',      N'Số giấy tờ',    3, 1, 1, NULL,       NULL),
        (N'NgayCap',     N'date',      N'Ngày cấp',      4, 0, 1, NULL,       NULL),
        (N'NgayHetHan',  N'date',      N'Ngày hết hạn',  5, 0, 1, NULL,       NULL),
        (N'NoiCap',      N'text',      N'Nơi cấp',       6, 0, 0, NULL,       NULL),
        (N'TrangThai',   N'text',      N'Trạng thái',    7, 0, 0, NULL,       NULL),
        (N'File_Id',     N'attachment',N'Tệp đính kèm',  8, 0, 0, NULL,       NULL)
     ) v(Col, Editor, Label, OrderNo, Req, ShowList, LkSrc, LkCode)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @T AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id);
GO
DECLARE @T INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'NS_NhanVien_GiayToNuocNgoai');
DECLARE @F INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'NS_NhanVien_GiayToNuocNgoai');
INSERT INTO dbo.Ui_Field_Lookup (Field_Id, Query_Mode, Source_Name, Value_Column, Display_Column, Order_By, Search_Enabled)
SELECT uf.Field_Id, N'table', N'NS_NhanVien', N'Id', N'HoTen', N'HoTen', 1
FROM dbo.Sys_Column sc
JOIN dbo.Ui_Field   uf ON uf.Form_Id = @F AND uf.Column_Id = sc.Column_Id
WHERE sc.Table_Id = @T AND sc.Column_Code = N'NhanVien_Id'
  AND NOT EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup fl WHERE fl.Field_Id = uf.Field_Id);
GO

PRINT N'100 done — Ui_Form NS_NhanVien (8 section, 37 field, 7 lookup) + 6 form CRUD con.';
GO
