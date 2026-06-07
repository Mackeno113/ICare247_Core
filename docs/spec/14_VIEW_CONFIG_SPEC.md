# View Config Spec — Ui_View / Ui_View_Column / Ui_View_Action

**Spec:** 14_VIEW_CONFIG_SPEC
**Phiên bản:** 1.0
**Ngày:** 2026-06-07
**ADR liên quan:** ADR-015 (architecture_decisions.md)
**i18n:** xem `10_RESOURCE_KEY_CONVENTION.md` §1d (View Keys)

---

## 1. Mục tiêu

Cấu hình **hiển thị danh sách** (Grid / TreeList / sau này Cards) hoàn toàn metadata-driven,
**tách khỏi form sửa** (`Ui_Form`/`Ui_Field`). Một bảng dữ liệu → nhiều view. Mỗi view tự
mô tả: nguồn dữ liệu, cột + thuộc tính cột, nút toolbar/row (CRUD, in, xuất file), và toàn bộ
text qua i18n.

### Nguyên tắc
- **Display ≠ Edit:** `Ui_View*` mô tả lưới/cây; `Ui_Form`/`Ui_Field` mô tả form sửa. View trỏ
  `Edit_Form_Id` để mở popup/tab Thêm/Sửa.
- **Generic control:** Grid và TreeList dùng chung 3 bảng; khác nhau ở `View_Type` + nhóm cột
  hierarchy (`Key_Field`/`Parent_Field`/`Expand_Level`).
- **Mọi text là resource KEY** (`Sys_Resource`), không literal — scope theo `table_code`.
- **Render giàu ≠ dữ liệu xuất:** cột có thể render HTML; export luôn lấy **giá trị thuần**.

---

## 2. Schema (Config DB — `ICare247_Config`)

### 2.1 `Ui_View` — header (datasource + hành vi + export/print + TreeList)

```sql
CREATE TABLE dbo.Ui_View
(
    View_Id             INT            IDENTITY(1,1) NOT NULL,
    View_Code           NVARCHAR(100)  NOT NULL,                  -- định danh kỹ thuật (route /view/{code})
    View_Type           NVARCHAR(30)   NOT NULL DEFAULT 'Grid',   -- Grid | TreeList | Cards
    Table_Id            INT            NOT NULL,                  -- bảng nguồn (base)
    Source_Type         NVARCHAR(30)   NOT NULL DEFAULT 'Table',  -- Table | View | Sp | Api
    Source_Object       NVARCHAR(MAX)  NULL,                      -- tên view/SP/SQL/endpoint khi ≠ Table
    Title_Key           NVARCHAR(150)  NULL,                      -- i18n tiêu đề màn
    Edit_Form_Id        INT            NULL,                      -- Ui_Form Thêm/Sửa (null = chỉ đọc)

    -- Hành vi lưới
    Page_Size           INT            NOT NULL DEFAULT 20,
    Allow_Paging        BIT            NOT NULL DEFAULT 1,
    Virtual_Scroll      BIT            NOT NULL DEFAULT 0,
    Show_Filter_Row     BIT            NOT NULL DEFAULT 1,
    Show_Group_Panel    BIT            NOT NULL DEFAULT 0,
    Show_Search_Box     BIT            NOT NULL DEFAULT 1,
    Show_Column_Chooser BIT            NOT NULL DEFAULT 0,
    Selection_Mode      NVARCHAR(20)   NOT NULL DEFAULT 'none',   -- none | single | multiple
    Allow_Add           BIT            NOT NULL DEFAULT 1,
    Allow_Edit          BIT            NOT NULL DEFAULT 1,
    Allow_Delete        BIT            NOT NULL DEFAULT 1,

    -- Export / Print (cờ nhanh; nút chi tiết ở Ui_View_Action)
    Allow_Export        BIT            NOT NULL DEFAULT 1,
    Export_Formats      NVARCHAR(100)  NULL,                      -- 'xlsx,csv,pdf,docx'
    Export_File_Name_Key NVARCHAR(150) NULL,                      -- i18n tên file (null = View_Code)
    Allow_Print         BIT            NOT NULL DEFAULT 0,

    -- TreeList
    Key_Field           NVARCHAR(100)  NULL,                      -- cột khóa
    Parent_Field        NVARCHAR(100)  NULL,                      -- cột cha (hierarchy)
    Expand_Level        INT            NULL,                      -- mở sẵn tới cấp

    -- Master-detail / lọc mặc định
    Detail_View_Id      INT            NULL,                      -- view con (row detail)
    Default_Filter_Json NVARCHAR(MAX)  NULL,                      -- bộ lọc khởi tạo

    Options_Json        NVARCHAR(MAX)  NULL,                      -- thuộc tính phụ (thoát hiểm)
    Tenant_Id           INT            NULL,
    Version             INT            NOT NULL DEFAULT 1,
    Is_Active           BIT            NOT NULL DEFAULT 1,
    Created_At          DATETIME       NOT NULL DEFAULT GETDATE(),
    Updated_At          DATETIME       NOT NULL DEFAULT GETDATE(),
    Description         NVARCHAR(500)  NULL,

    CONSTRAINT PK_Ui_View PRIMARY KEY (View_Id),
    CONSTRAINT FK_Ui_View_Table   FOREIGN KEY (Table_Id)      REFERENCES dbo.Sys_Table(Table_Id),
    CONSTRAINT FK_Ui_View_EditForm FOREIGN KEY (Edit_Form_Id) REFERENCES dbo.Ui_Form(Form_Id),
    CONSTRAINT FK_Ui_View_Detail  FOREIGN KEY (Detail_View_Id) REFERENCES dbo.Ui_View(View_Id),
    CONSTRAINT FK_Ui_View_Tenant  FOREIGN KEY (Tenant_Id)     REFERENCES dbo.Sys_Tenant(Tenant_Id)
);

CREATE UNIQUE INDEX UQ_Ui_View_Code_Global ON dbo.Ui_View(View_Code) WHERE Tenant_Id IS NULL;
CREATE UNIQUE INDEX UQ_Ui_View_Code_Tenant ON dbo.Ui_View(View_Code, Tenant_Id) WHERE Tenant_Id IS NOT NULL;
CREATE INDEX IX_Ui_View_Table ON dbo.Ui_View(Table_Id, Is_Active);
```

### 2.2 `Ui_View_Column` — cột + thuộc tính cột (render + export + format)

```sql
CREATE TABLE dbo.Ui_View_Column
(
    View_Column_Id    INT            IDENTITY(1,1) NOT NULL,
    View_Id           INT            NOT NULL,
    Column_Id         INT            NULL,                       -- map Sys_Column (null = unbound/computed)
    Field_Name        NVARCHAR(100)  NOT NULL,                   -- FieldName trên control
    Caption_Key       NVARCHAR(150)  NULL,                       -- i18n (null = fallback Label_Key field → Field_Name)
    Column_Kind       NVARCHAR(30)   NOT NULL DEFAULT 'Data',    -- Data | Selection | Command | TreeSpin

    -- Hiển thị
    Width             NVARCHAR(20)   NULL,
    Min_Width         INT            NULL,
    Text_Align        NVARCHAR(10)   NULL,                       -- left | center | right
    Display_Format    NVARCHAR(50)   NULL,                       -- n0 | dd/MM/yyyy ...
    Render_Mode       NVARCHAR(20)   NOT NULL DEFAULT 'Text',    -- Text|Html|Image|Link|Badge|Boolean|Template
    Cell_Template_Key NVARCHAR(150)  NULL,                       -- template/i18n cho Html/Badge/Link
    Is_Visible        BIT            NOT NULL DEFAULT 1,
    Order_No          INT            NOT NULL DEFAULT 0,
    Fixed_Position    NVARCHAR(10)   NULL,                       -- none | left | right (frozen)

    -- Hành vi cột
    Allow_Sort        BIT            NOT NULL DEFAULT 1,
    Sort_Order        NVARCHAR(4)    NULL,                       -- asc | desc (sort mặc định)
    Sort_Index        INT            NULL,                       -- thứ tự khi sort nhiều cột
    Allow_Filter      BIT            NOT NULL DEFAULT 1,
    Allow_Group       BIT            NOT NULL DEFAULT 0,
    Group_Index       INT            NULL,                       -- nhóm sẵn theo cột
    Summary_Type      NVARCHAR(20)   NULL,                       -- count|sum|avg|min|max

    -- Export (giá trị thuần — KHÔNG xuất HTML)
    Allow_Export      BIT            NOT NULL DEFAULT 1,         -- HTML-only/command/selection → 0
    Export_Format     NVARCHAR(50)   NULL,                       -- format khi xuất (≠ Display_Format)
    Export_Caption_Key NVARCHAR(150) NULL,                       -- tiêu đề cột khi xuất (≠ Caption_Key)

    -- Conditional formatting
    Style_Rule_Json   NVARCHAR(MAX)  NULL,                       -- điều kiện (Grammar V1 AST) → style ô

    Props_Json        NVARCHAR(MAX)  NULL,
    Is_Active         BIT            NOT NULL DEFAULT 1,

    CONSTRAINT PK_Ui_View_Column PRIMARY KEY (View_Column_Id),
    CONSTRAINT FK_Ui_View_Column_View   FOREIGN KEY (View_Id)   REFERENCES dbo.Ui_View(View_Id),
    CONSTRAINT FK_Ui_View_Column_Column FOREIGN KEY (Column_Id) REFERENCES dbo.Sys_Column(Column_Id)
);

CREATE INDEX IX_Ui_View_Column_View ON dbo.Ui_View_Column(View_Id, Is_Visible, Order_No);
```

### 2.3 `Ui_View_Action` — nút toolbar / row (CRUD mở rộng, in, xuất file, custom)

```sql
CREATE TABLE dbo.Ui_View_Action
(
    Action_Id         INT            IDENTITY(1,1) NOT NULL,
    View_Id           INT            NOT NULL,
    Action_Code       NVARCHAR(50)   NOT NULL,                   -- add|edit|delete|export|print|refresh|column-chooser|<custom>
    Action_Type       NVARCHAR(30)   NOT NULL,                   -- BuiltIn|Export|Print|Navigate|Event|Api
    Scope             NVARCHAR(20)   NOT NULL DEFAULT 'Toolbar', -- Toolbar | Row | Both
    Label_Key         NVARCHAR(150)  NULL,                       -- i18n nhãn
    Tooltip_Key       NVARCHAR(150)  NULL,                       -- i18n tooltip
    Confirm_Key       NVARCHAR(150)  NULL,                       -- i18n xác nhận (vd Xóa)
    Icon              NVARCHAR(50)   NULL,                       -- unicode/tên icon (không phải text dịch)
    Export_Format     NVARCHAR(20)   NULL,                       -- xlsx|xls|csv|pdf|docx (Action_Type='Export')
    Export_Engine     NVARCHAR(20)   NULL,                       -- Grid (client) | Server (template)
    Target            NVARCHAR(MAX)  NULL,                       -- url|event_code|api endpoint|report template
    Require_Selection BIT            NOT NULL DEFAULT 0,
    Order_No          INT            NOT NULL DEFAULT 0,
    Props_Json        NVARCHAR(MAX)  NULL,
    Is_Active         BIT            NOT NULL DEFAULT 1,

    CONSTRAINT PK_Ui_View_Action PRIMARY KEY (Action_Id),
    CONSTRAINT FK_Ui_View_Action_View FOREIGN KEY (View_Id) REFERENCES dbo.Ui_View(View_Id)
);

CREATE INDEX IX_Ui_View_Action_View ON dbo.Ui_View_Action(View_Id, Is_Active, Order_No);
```

---

## 3. i18n — toàn bộ text qua key

Mọi text hiển thị là `_Key` → `Sys_Resource` theo `Lang_Code`. **Scope = `table_code`** (tái dùng
bản dịch khi nhiều view bind cùng bảng). Convention đầy đủ ở `10_RESOURCE_KEY_CONVENTION.md` §1d.

| Bảng | Cột là KEY (i18n) | Cột kỹ thuật (literal) |
|---|---|---|
| `Ui_View` | `Title_Key`, `Export_File_Name_Key` | `View_Code`, `View_Type`, `Source_*`, cờ hành vi |
| `Ui_View_Column` | `Caption_Key`, `Export_Caption_Key`, `Cell_Template_Key` | `Field_Name`, `Display_Format`, `Render_Mode`, `Fixed_Position` |
| `Ui_View_Action` | `Label_Key`, `Tooltip_Key`, `Confirm_Key` | `Action_Code`, `Action_Type`, `Icon`, `Export_Format` |

**Fallback tiêu đề cột:** `Caption_Key` → (nếu bound) `Ui_Field.Label_Key` của field cùng cột →
`Field_Name`/`Column_Code`. ⇒ Mặc định `Caption_Key = NULL` để **tái dùng label field**, chỉ set khi
muốn caption khác.

---

## 4. Engine rules (chốt)

1. **Render lưới:** mỗi ô render theo `Render_Mode` (`Html` → `MarkupString`; `Boolean` → ✓; `Text` →
   format `Display_Format`).
2. **Export file:** **luôn lấy giá trị thuần** của `Field_Name`, format bằng `Export_Format ?? Display_Format`,
   **bỏ qua `Render_Mode`** (không xuất thẻ HTML). Cột không có scalar (HTML trang trí, command,
   selection) → `Allow_Export = 0`.
3. **Header export đa ngôn ngữ:** resolve `Export_Caption_Key ?? Caption_Key` theo `langCode` đang chọn.
4. **Export engine:**
   - `xlsx|xls|csv` → `Export_Engine='Grid'` (DxGrid client-side).
   - `pdf|docx` → `Export_Engine='Server'` (service xuất theo template `Target`). DxGrid **không** xuất docx.
   - `Export_Scope` = All | Filtered | Selected. **Xuất toàn bộ** (vượt trang đã tải) ⇒ bắt buộc Server-side.
5. **Phân quyền:** export/print tôn trọng tenant + permission (CC-3); không xuất cột user không được xem.
6. **TreeList:** dựng cây từ `Key_Field`/`Parent_Field`; `Expand_Level` mở sẵn; cột spin ở `Column_Kind='TreeSpin'`.

---

## 5. Ánh xạ runtime (Blazor)

`MasterDataGridConfig` + `MasterDataColumnDto` (đã có) là **runtime model**; `Ui_View`/`Ui_View_Column`
map vào đó. Component `DataView` chọn render `<DxGrid>` hoặc `<DxTreeList>` theo `View_Type`.

| Config DB | DxGrid prop |
|---|---|
| `Page_Size` / `Allow_Paging` | `PageSize` / `PagerVisible` |
| `Virtual_Scroll` | `VirtualScrollingEnabled` |
| `Show_Filter_Row` / `Show_Group_Panel` | `ShowFilterRow` / `ShowGroupPanel` |
| `Selection_Mode` | `SelectionMode` + `DxGridSelectionColumn` |
| `Width`/`Text_Align`/`Display_Format` | `DxGridDataColumn.Width`/`TextAlignment` + format |
| `Allow_Sort`/`Allow_Filter`/`Allow_Group` | `AllowSort`/`FilterRowCellVisible`/`AllowGroup` |
| `Summary_Type` | `DxGridSummaryItem` |

---

## 6. Tầng & ownership

| Tầng | Việc | Owner |
|---|---|---|
| `db/` | Migration tạo 3 bảng + seed view mặc định từ `Ui_Form` | **Codex** |
| Domain/Application | `ViewMetadata`/`ViewColumn`/`ViewAction`, `IViewRepository`, `GetViewQuery`, `IConfigCache.GetViewAsync` | **Claude** |
| Infrastructure | `ViewRepository` (Dapper, Config DB) | **Claude** |
| Api | `ViewController` (metadata + data) + export endpoint server-side | **Claude** |
| Blazor | `DataView` (DxGrid/DxTreeList), toolbar actions, client export | **Claude** |
| ConfigStudio WPF | Màn "Quản lý View" (header + grid cột + actions) | **Codex** |

---

## 7. Tương thích & migration

- `Ui_Field.Show_In_List` → **bị thay** bởi `Ui_View_Column`. Migration **auto-sinh 1 Grid view mặc định**
  cho mỗi `Ui_Form` đang có (lấy field `Show_In_List`, `Edit_Form_Id` = chính form đó) để màn cũ không vỡ.
- Route `/master/{FormCode}` → `/view/{ViewCode}`; giữ alias `/master/*` map sang view mặc định trong giai
  đoạn chuyển tiếp.
- Cache: `IConfigCache.GetViewAsync(viewCode, tenant, lang)`; key gồm `{tenant}:{lang}:v{n}` (theo ADR-014).
  ResourceMap loader nạp thêm prefix `{tableCode}.view.%`.

---

## 8. Trạng thái

- **Thiết kế:** ✅ chốt (ADR-015, 2026-06-07).
- **Triển khai:** ⏳ chưa bắt đầu — cần handoff Codex (db + ConfigStudio) trước khi Claude wire backend.
