-- =============================================================================
-- File    : 032_seed_default_views.sql
-- Purpose : VIEW-1b — Seed 1 View Grid mặc định cho mỗi Ui_Form đang active, để màn
--           danh sách cũ (/master/*) chuyển sang /view/* không vỡ.
--           View_Code = 'Grid_' + Form_Code (theo convention {View_Type}_{suffix} của WPF).
--           Cột lấy từ field Show_In_List = 1 (bỏ field ảo); Edit_Form = chính form đó.
-- Note    : Idempotent — chỉ tạo view/cột khi chưa có (NOT EXISTS). Chạy lại an toàn.
--           Yêu cầu db/031_create_ui_view.sql đã chạy.
-- =============================================================================

USE [ICare247_Config];
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Header: 1 Grid view / form active (chưa có view trùng code)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO dbo.Ui_View
    (View_Code, View_Type, Table_Id, Source_Type, Title_Key, Edit_Form_Id,
     Description)
SELECT 'Grid_' + f.Form_Code,
       'Grid',
       f.Table_Id,
       'Table',
       f.Title_Key,
       f.Form_Id,
       N'View Grid mặc định (seed từ Ui_Form — VIEW-1b)'
FROM   dbo.Ui_Form  f
JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
WHERE  f.Is_Active = 1
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.Ui_View v
        WHERE  v.View_Code = 'Grid_' + f.Form_Code
  );
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Cột: từ field Show_In_List = 1 (bỏ field ảo) — chỉ seed view chưa có cột nào
--    Field_Name = Sys_Column.Column_Code (fallback Field_Code); Caption_Key = Label_Key.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO dbo.Ui_View_Column
    (View_Id, Column_Id, Field_Name, Caption_Key, Column_Kind, Render_Mode,
     Is_Visible, Order_No)
SELECT v.View_Id,
       uf.Column_Id,
       COALESCE(sc.Column_Code, uf.Field_Code),
       uf.Label_Key,
       'Data',
       'Text',
       1,
       uf.Order_No
FROM   dbo.Ui_View   v
JOIN   dbo.Ui_Form   f  ON f.Form_Id = v.Edit_Form_Id
                       AND v.View_Code = 'Grid_' + f.Form_Code
JOIN   dbo.Ui_Field  uf ON uf.Form_Id = f.Form_Id
                       AND uf.Show_In_List = 1
                       AND ISNULL(uf.Is_Virtual, 0) = 0
LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = uf.Column_Id
WHERE  COALESCE(sc.Column_Code, uf.Field_Code) IS NOT NULL
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.Ui_View_Column c WHERE c.View_Id = v.View_Id
  );
GO

PRINT N'Migration 032 completed — seed Grid view mặc định cho các Ui_Form active.';
GO
