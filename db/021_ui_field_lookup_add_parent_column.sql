-- Migration 021: Thêm Parent_Column vào Ui_Field_Lookup cho TreeLookupBox
-- Database  : ICare247_Config
-- Parent_Column: tên cột chứa Parent Id trong bảng nguồn (VD: Parent_Id).
-- NULL = không phải tree (LookupBox thông thường).
-- TreeLookupBox: EditorType = 'TreeLookupBox', Parent_Column NOT NULL.

ALTER TABLE dbo.Ui_Field_Lookup
    ADD Parent_Column nvarchar(100) NULL;
GO

-- Seed action type mới (nếu chưa có) cho TreeLookupBox reload
IF NOT EXISTS (SELECT 1 FROM dbo.Evt_Action_Type WHERE Action_Code = 'RELOAD_TREE')
BEGIN
    INSERT INTO dbo.Evt_Action_Type (Action_Code, Param_Schema, Description, Is_Active)
    VALUES (
        'RELOAD_TREE',
        N'{"targetField":"string","dependsOn":["string"]}',
        N'Reload cây dữ liệu của TreeLookupBox khi field filter thay đổi.',
        1
    );
END
GO
