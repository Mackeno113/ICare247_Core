-- =============================================================================
-- File    : 047_seed_ui_form_ht_vaitro.sql
-- Purpose : Khai báo form danh mục cho HT_VaiTro (Vai trò) — để engine MasterData phục vụ
--           /master/HT_VaiTro (CRUD no-code). Tương đương việc dựng form trong ConfigStudio.
-- Spec    : docs/spec/15_AUTHZ_NAVIGATION_SPEC.md (AUTHZ-UI-2) · ADR-023.
-- Context : Config DB (ICare247_Config). Idempotent (NOT EXISTS theo code).
-- Note    : Engine tự bơm CreatedBy/CreatedAt (insert) + UpdatedBy/UpdatedAt (update) nên KHÔNG
--           khai field audit. Label_Key đặt thẳng tiếng Việt (engine fallback key khi thiếu resource).
--           Người dùng (HT_NguoiDung) KHÔNG dựng kiểu này (field nhạy cảm) — để bespoke sau.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

-- ── 1. Sys_Table: bảng đích HT_VaiTro (Data DB) ─────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Table WHERE Table_Code = N'HT_VaiTro' AND Tenant_Id IS NULL)
    INSERT INTO dbo.Sys_Table (Table_Code, Table_Name, Schema_Name)
    VALUES (N'HT_VaiTro', N'Vai trò', N'dbo');
GO

-- ── 2. Sys_Column: cột phơi cho form (Id PK + Ma/Ten/MoTa) ──────────────────
DECLARE @TableId INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'HT_VaiTro' AND Tenant_Id IS NULL);

INSERT INTO dbo.Sys_Column (Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable, Is_PK, Is_Identity)
SELECT @TableId, v.Code, v.Dt, v.Nt, v.Nullable, v.Pk, v.Ident
FROM (VALUES
        (N'Id',   N'bigint',   N'Int64',  0, 1, 1),
        (N'Ma',   N'nvarchar', N'String', 0, 0, 0),
        (N'Ten',  N'nvarchar', N'String', 0, 0, 0),
        (N'MoTa', N'nvarchar', N'String', 1, 0, 0)
     ) v(Code, Dt, Nt, Nullable, Pk, Ident)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sys_Column c WHERE c.Table_Id = @TableId AND c.Column_Code = v.Code);
GO

-- ── 3. Ui_Form ──────────────────────────────────────────────────────────────
DECLARE @TableId INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'HT_VaiTro' AND Tenant_Id IS NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Ui_Form WHERE Form_Code = N'HT_VaiTro')
    INSERT INTO dbo.Ui_Form (Form_Code, Table_Id, Platform, Display_Mode)
    VALUES (N'HT_VaiTro', @TableId, N'Web', N'Popup');
GO

-- ── 4. Ui_Field (Ma/Ten/MoTa) — Id là identity PK, không phơi nhập ───────────
DECLARE @TableId INT = (SELECT Table_Id FROM dbo.Sys_Table WHERE Table_Code = N'HT_VaiTro' AND Tenant_Id IS NULL);
DECLARE @FormId  INT = (SELECT Form_Id  FROM dbo.Ui_Form  WHERE Form_Code  = N'HT_VaiTro');

INSERT INTO dbo.Ui_Field (Form_Id, Column_Id, Editor_Type, Label_Key, Order_No, Show_In_List, Is_Unique)
SELECT @FormId, sc.Column_Id, v.Editor, v.Label, v.OrderNo, v.ShowList, v.Uniq
FROM (VALUES
        (N'Ma',   N'TextBox', N'Mã vai trò',  1, 1, 1),
        (N'Ten',  N'TextBox', N'Tên vai trò', 2, 1, 0),
        (N'MoTa', N'TextBox', N'Mô tả',       3, 0, 0)
     ) v(Col, Editor, Label, OrderNo, ShowList, Uniq)
JOIN dbo.Sys_Column sc ON sc.Table_Id = @TableId AND sc.Column_Code = v.Col
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ui_Field uf WHERE uf.Form_Id = @FormId AND uf.Column_Id = sc.Column_Id);
GO
