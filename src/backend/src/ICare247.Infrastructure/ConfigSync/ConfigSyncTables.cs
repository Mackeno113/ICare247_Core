// File    : ConfigSyncTables.cs
// Module  : ConfigSync
// Layer   : Infrastructure
// Purpose : Danh sách mô tả bảng config theo ĐÚNG thứ tự phụ thuộc (spec §2) cho engine sync.
//           CFGSYNC-2 — VERTICAL SLICE: 5 bảng lõi (Sys_Table → Sys_Column → Ui_Form →
//           Ui_Section → Ui_Field) để verify pattern. Mở rộng các bảng còn lại (Sys_Resource,
//           Sys_Lookup, Ui_Tab, Ui_View*, Val_Rule...) ở đợt sau theo cùng khuôn.

namespace ICare247.Infrastructure.ConfigSync;

/// <summary>
/// Khai báo tĩnh thứ tự + mô tả các bảng đồng bộ. Cha luôn đứng trước con để map Code→Id sẵn sàng.
/// </summary>
internal static class ConfigSyncTables
{
    /// <summary>Thứ tự đồng bộ (vertical slice). Thêm bảng mới = nối vào đúng vị trí phụ thuộc.</summary>
    public static readonly IReadOnlyList<ConfigTableDescriptor> Order =
    [
        // 1) Sys_Table — gốc, mã unique toàn cục.
        new ConfigTableDescriptor
        {
            TableName = "Sys_Table",
            IdColumn = "Table_Id",
            LocalKeyColumn = "Table_Code",
        },

        // 2) Sys_Column — con của Sys_Table; mã unique trong (Table_Id + Column_Code).
        new ConfigTableDescriptor
        {
            TableName = "Sys_Column",
            IdColumn = "Column_Id",
            LocalKeyColumn = "Column_Code",
            ContextParent = new ParentLink("Table_Id", "Sys_Table"),
            RelinkParents = [new ParentLink("Table_Id", "Sys_Table")],
        },

        // 3) Ui_Form — mã unique toàn cục; FK Table_Id chỉ để re-link (không tạo ngữ cảnh khóa).
        new ConfigTableDescriptor
        {
            TableName = "Ui_Form",
            IdColumn = "Form_Id",
            LocalKeyColumn = "Form_Code",
            RelinkParents = [new ParentLink("Table_Id", "Sys_Table")],
        },

        // 4) Ui_Section — con của Ui_Form; mã unique trong (Form_Id + Section_Code).
        new ConfigTableDescriptor
        {
            TableName = "Ui_Section",
            IdColumn = "Section_Id",
            LocalKeyColumn = "Section_Code",
            ContextParent = new ParentLink("Form_Id", "Ui_Form"),
            RelinkParents = [new ParentLink("Form_Id", "Ui_Form")],
        },

        // 5) Ui_Field — con của Ui_Form (khóa = Form + Field_Code); re-link thêm Section_Id (nullable)
        //    + Column_Id. KHÔNG có Is_Active (dùng Is_Visible) → không tombstone.
        new ConfigTableDescriptor
        {
            TableName = "Ui_Field",
            IdColumn = "Field_Id",
            LocalKeyColumn = "Field_Code",
            ContextParent = new ParentLink("Form_Id", "Ui_Form"),
            RelinkParents =
            [
                new ParentLink("Form_Id", "Ui_Form"),
                new ParentLink("Section_Id", "Ui_Section"),
                new ParentLink("Column_Id", "Sys_Column"),
            ],
            ActiveColumn = null,
        },
    ];
}
