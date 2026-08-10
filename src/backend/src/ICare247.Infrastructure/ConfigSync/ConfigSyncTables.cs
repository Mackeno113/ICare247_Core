// File    : ConfigSyncTables.cs
// Module  : ConfigSync
// Layer   : Infrastructure
// Purpose : Danh sách mô tả bảng config theo ĐÚNG thứ tự phụ thuộc (spec §2) cho engine sync.
//           CFGSYNC-2 — phủ trọn phạm vi §2 + bảng config-con cho F2: 5 bảng lõi (Sys_Table →
//           Sys_Column → Ui_Form → Ui_Section → Ui_Field) + Ui_Tab, Val_Rule, Ui_Field_Lookup,
//           Sys_Resource, Sys_Lookup, Ui_View, Ui_View_Column, Ui_View_Action, Ui_View_Filter.
// Ghi chú phạm vi:
//   • Val_Rule_Field (junction) ĐÃ BỊ DROP ở migration 003 — Val_Rule giờ mang Field_Id trực tiếp
//     (1 rule/1 field), Error_Key unique toàn cục → dùng làm khóa nghiệp vụ.
//   • Sys_Resource KHÔNG có Id identity (PK ghép Resource_Key+Lang_Code) → IdColumn=null,
//     engine khớp/UPDATE theo khóa nghiệp vụ. Không có Is_Active → không tombstone.
//   • Ui_Field_Lookup là bảng mở rộng 1-1 với Ui_Field (UNIQUE Field_Id), không có cột mã riêng →
//     khóa CHỈ theo cha (KeyColumns rỗng). Cờ sync cấp ở db/062 (cùng Ui_View_Filter).
//   • Sys_Relation chưa có cờ sync → ngoài phạm vi. Master-detail engine-hóa qua Ui_Form_Detail
//     (db/106, mục 6b) — khai báo pane chi tiết theo form, KHÔNG suy từ Sys_Relation.
//   • Sys_Ma_Rule / Sys_Ma_Rule_Segment (db/089): quy tắc sinh mã. Rule khóa GHÉP Table_Code+Column_Code,
//     KHÔNG re-link FK (trỏ bảng đích bằng chuỗi Table_Code). Segment khóa theo Order_No trong quy tắc cha.
//     BỘ ĐẾM không tồn tại (số suy từ dữ liệu bảng đích — ADR-036) nên không có gì "số đã cấp" để sync.

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

        // 6) Ui_Tab — con của Ui_Form; mã unique trong (Form_Id + Tab_Code). Không có cột Version.
        new ConfigTableDescriptor
        {
            TableName = "Ui_Tab",
            IdColumn = "Tab_Id",
            LocalKeyColumn = "Tab_Code",
            ContextParent = new ParentLink("Form_Id", "Ui_Form"),
            RelinkParents = [new ParentLink("Form_Id", "Ui_Form")],
            VersionColumn = null,
        },

        // 6b) Ui_Form_Detail — khai báo pane chi tiết master-detail (Spec 30 / rail workspace, db/106).
        //     Con của Ui_Form (Detail_Code unique trong Form_Id). Re-link Detail_Form_Id (→ Ui_Form con,
        //     đã sync ở bước 3) + Section_Id (→ Ui_Section, nullable). Có Is_Active + Version.
        new ConfigTableDescriptor
        {
            TableName = "Ui_Form_Detail",
            IdColumn = "Detail_Id",
            LocalKeyColumn = "Detail_Code",
            ContextParent = new ParentLink("Form_Id", "Ui_Form"),
            RelinkParents =
            [
                new ParentLink("Form_Id", "Ui_Form"),
                new ParentLink("Detail_Form_Id", "Ui_Form"),
                new ParentLink("Section_Id", "Ui_Section"),
            ],
        },

        // 7) Val_Rule — sau Migration 003: Field_Id trực tiếp (1 rule/1 field), Error_Key unique TOÀN CỤC
        //    (UX_Val_Rule_ErrorKey) → dùng làm khóa. ContextParent Ui_Field để bỏ qua rule mồ côi êm.
        //    Không có cột Version. (Junction Val_Rule_Field đã bị drop.)
        new ConfigTableDescriptor
        {
            TableName = "Val_Rule",
            IdColumn = "Rule_Id",
            LocalKeyColumn = "Error_Key",
            ContextParent = new ParentLink("Field_Id", "Ui_Field"),
            RelinkParents = [new ParentLink("Field_Id", "Ui_Field")],
            VersionColumn = null,
        },

        // 7b) Ui_Lookup_Template — mẫu lookup dùng chung (PICKER-P4, db/083); Template_Code unique
        //     toàn cục. Đứng TRƯỚC Ui_Field_Lookup (field tham chiếu mẫu theo CODE chuỗi — không FK Id
        //     nên không cần re-link, chỉ cần mẫu có mặt trước cho dễ đọc). Cờ sync tạo sẵn trong 083.
        new ConfigTableDescriptor
        {
            TableName = "Ui_Lookup_Template",
            IdColumn = "Template_Id",
            LocalKeyColumn = "Template_Code",
        },

        // 8) Ui_Field_Lookup — cấu hình lookup field FK (con 1-1 của Ui_Field, UNIQUE Field_Id). KHÔNG có
        //    cột mã riêng → khóa CHỈ theo cha (KeyColumns rỗng). Không có Is_Active/Version. (db/062 cấp cờ.)
        new ConfigTableDescriptor
        {
            TableName = "Ui_Field_Lookup",
            IdColumn = "Lookup_Cfg_Id",
            ContextParent = new ParentLink("Field_Id", "Ui_Field"),
            RelinkParents = [new ParentLink("Field_Id", "Ui_Field")],
            ActiveColumn = null,
            VersionColumn = null,
        },

        // 9) Sys_Resource — i18n. PK GHÉP (Resource_Key + Lang_Code), KHÔNG có Id identity → IdColumn=null.
        //    Không có Is_Active → không tombstone. Có cột Version.
        new ConfigTableDescriptor
        {
            TableName = "Sys_Resource",
            IdColumn = null,
            LocalKeyColumns = ["Resource_Key", "Lang_Code"],
            ActiveColumn = null,
        },

        // 10) Sys_Lookup — danh mục tĩnh; mỗi item = 1 dòng, khóa GHÉP (Lookup_Code + Item_Code).
        //    Có Lookup_Id identity. Không có cột Version. (Cột copy dò động qua GetColumnsAsync.)
        new ConfigTableDescriptor
        {
            TableName = "Sys_Lookup",
            IdColumn = "Lookup_Id",
            LocalKeyColumns = ["Lookup_Code", "Item_Code"],
            VersionColumn = null,
        },

        // 11) Ui_View — header lưới; View_Code unique toàn cục. Re-link Table_Id, Edit_Form_Id, và
        //     Detail_View_Id (SELF-FK — hiếm dùng; dữ liệu hiện NULL nên an toàn, chỉ rủi ro nếu
        //     view tham chiếu view xử lý SAU trong cùng đợt). Có cột Version.
        new ConfigTableDescriptor
        {
            TableName = "Ui_View",
            IdColumn = "View_Id",
            LocalKeyColumn = "View_Code",
            RelinkParents =
            [
                new ParentLink("Table_Id", "Sys_Table"),
                new ParentLink("Edit_Form_Id", "Ui_Form"),
                new ParentLink("Detail_View_Id", "Ui_View"),
            ],
        },

        // 12) Ui_View_Column — con của Ui_View; định danh trong view = Field_Name. Re-link Column_Id
        //     (nullable → unbound). Không có cột Version.
        new ConfigTableDescriptor
        {
            TableName = "Ui_View_Column",
            IdColumn = "View_Column_Id",
            LocalKeyColumn = "Field_Name",
            ContextParent = new ParentLink("View_Id", "Ui_View"),
            RelinkParents =
            [
                new ParentLink("View_Id", "Ui_View"),
                new ParentLink("Column_Id", "Sys_Column"),
            ],
            VersionColumn = null,
        },

        // 13) Ui_View_Action — con của Ui_View; mã unique trong (View_Id + Action_Code). Không có Version.
        new ConfigTableDescriptor
        {
            TableName = "Ui_View_Action",
            IdColumn = "Action_Id",
            LocalKeyColumn = "Action_Code",
            ContextParent = new ParentLink("View_Id", "Ui_View"),
            RelinkParents = [new ParentLink("View_Id", "Ui_View")],
            VersionColumn = null,
        },

        // 14) Ui_View_Filter — panel lọc cascade/ADR-030 (con của Ui_View); mã unique trong (View_Id +
        //     Filter_Code). Không có cột Version. (db/062 cấp cờ.)
        new ConfigTableDescriptor
        {
            TableName = "Ui_View_Filter",
            IdColumn = "Filter_Id",
            LocalKeyColumn = "Filter_Code",
            ContextParent = new ParentLink("View_Id", "Ui_View"),
            RelinkParents = [new ParentLink("View_Id", "Ui_View")],
            VersionColumn = null,
        },

        // 15) Sys_Ma_Rule — quy tắc sinh mã (db/089, spec 32 / ADR-036). Khóa nghiệp vụ GHÉP
        //     (Table_Code + Column_Code) — 1 cặp bảng+cột chỉ 1 quy tắc. KHÔNG có FK Id nào cần
        //     re-link: bảng đích tham chiếu bằng CHUỖI Table_Code, không phải Table_Id ⇒ vị trí trong
        //     danh sách không ràng buộc (đặt cuối cho gọn). Không có cột Version.
        new ConfigTableDescriptor
        {
            TableName = "Sys_Ma_Rule",
            IdColumn = "Rule_Id",
            LocalKeyColumns = ["Table_Code", "Column_Code"],
            VersionColumn = null,
        },

        // 16) Sys_Ma_Rule_Segment — các đoạn ghép mã, con của Sys_Ma_Rule. Định danh trong quy tắc =
        //     Order_No (thứ tự ghép; engine so khóa bằng ToString nên cột int dùng được).
        //     PHẢI đứng SAU Sys_Ma_Rule để map khóa cha → Id tenant sẵn sàng.
        //     Có Is_Active để tombstone: master gỡ 1 đoạn → tenant đặt Is_Active = 0 thay vì để lại
        //     đoạn thừa (đoạn thừa nằm lại sẽ âm thầm sinh mã SAI). Engine sinh mã chỉ đọc Is_Active = 1.
        new ConfigTableDescriptor
        {
            TableName = "Sys_Ma_Rule_Segment",
            IdColumn = "Segment_Id",
            LocalKeyColumn = "Order_No",
            ContextParent = new ParentLink("Rule_Id", "Sys_Ma_Rule"),
            RelinkParents = [new ParentLink("Rule_Id", "Sys_Ma_Rule")],
            VersionColumn = null,
        },
    ];
}
