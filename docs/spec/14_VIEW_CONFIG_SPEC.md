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
- **Triển khai:** ✅ backend + Blazor + ConfigStudio đã code (Ui_View cơ bản). Panel lọc nâng cao xem §9.

---

## 9. Lưới nâng cao — Panel lọc trái + tham số SP/SQL (ADR-016, 2026-06-11)

Mở rộng `Ui_View` để hỗ trợ **panel control lọc bên trái → nút Tìm (i18n) → đẩy tham số vào
Stored Procedure / SQL → đổ kết quả ra lưới**. **Không** tạo `View_Type` mới — panel lọc là tính năng
bật/tắt theo nguồn (`Source_Type ∈ {Sp, Sql}`), tái dùng nguyên `Ui_View_Column`/`Ui_View_Action`.

### 9.1 Cờ panel — thêm vào `Ui_View` (Migration 034)

| Cột | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `Filter_Panel_Enabled` | bit | 0 | Bật panel lọc trái |
| `Filter_Panel_Position` | nvarchar(10) | 'left' | left \| top |
| `Filter_Collapsible` | bit | 1 | Cho thu gọn panel |
| `Auto_Search_On_Load` | bit | 0 | Tự Tìm khi mở (mặc định **chờ bấm** — tránh SP nặng) |
| `Search_Label_Key` | nvarchar(150) | NULL | ★ i18n nút Tìm (null → `common.filter.search`) |
| `Reset_Label_Key` | nvarchar(150) | NULL | ★ i18n nút Đặt lại (null → `common.filter.reset`) |

### 9.2 `Ui_View_Filter` — mỗi control lọc = 1 dòng = **1 tham số**

```sql
CREATE TABLE dbo.Ui_View_Filter
(
    Filter_Id        INT            IDENTITY(1,1) NOT NULL,
    View_Id          INT            NOT NULL,                   -- per-View
    Filter_Code      NVARCHAR(50)   NOT NULL,                   -- unique/View; client gửi value theo code này
    Control_Type     NVARCHAR(30)   NOT NULL,                   -- Text|Number|Date|Combo|MultiSelect|Checkbox|Radio
    Label_Key        NVARCHAR(150)  NOT NULL,                   -- ★ i18n
    Placeholder_Key  NVARCHAR(150)  NULL,                       -- ★ i18n
    Tooltip_Key      NVARCHAR(150)  NULL,                       -- ★ i18n
    Param_Name       NVARCHAR(100)  NOT NULL,                   -- @MaBN, @TuNgay... (literal, whitelist)
    Param_Type       NVARCHAR(30)   NOT NULL,                   -- string|int|decimal|date|bool
    Operator         NVARCHAR(20)   NOT NULL DEFAULT '=',       -- = | LIKE | >= | <= | IN
    Default_Value    NVARCHAR(255)  NULL,                       -- literal — KHÔNG i18n
    Is_Required      BIT            NOT NULL DEFAULT 0,
    Is_Visible       BIT            NOT NULL DEFAULT 1,
    Order_No         INT            NOT NULL DEFAULT 0,
    Col_Span         TINYINT        NOT NULL DEFAULT 1,         -- bố cục panel (grid 4-col)
    Lookup_Source    NVARCHAR(20)   NULL,                       -- NULL|static|dynamic
    Lookup_Code      NVARCHAR(50)   NULL,                       -- Sys_Lookup.Lookup_Code (static)
    Lookup_Sql       NVARCHAR(MAX)  NULL,                       -- SELECT value,display (dynamic)
    Props_Json       NVARCHAR(MAX)  NULL,
    Is_Active        BIT            NOT NULL DEFAULT 1,
    CONSTRAINT PK_Ui_View_Filter PRIMARY KEY (Filter_Id),
    CONSTRAINT FK_Ui_View_Filter_View FOREIGN KEY (View_Id) REFERENCES dbo.Ui_View(View_Id)
);
-- + CHK Operator/Lookup; UQ (View_Id, Filter_Code) WHERE Is_Active=1; IX (View_Id, Is_Visible, Order_No)
```

> **Khoảng giá trị (DateRange/NumberRange):** tách **2 dòng** — vd `tu_ngay` (Operator '>=') +
> `den_ngay` (Operator '<='), mỗi dòng nhãn + `Is_Required` riêng → thông báo "Từ ngày là bắt buộc"
> và focus đúng ô. (Quyết định bỏ cột `Param_Name_To`, xem ADR-016.)

### 9.3 i18n

| Cột là KEY (★) | Cột literal (không dịch) |
|---|---|
| `Label_Key`, `Placeholder_Key`, `Tooltip_Key`, `Search_Label_Key`, `Reset_Label_Key` | `Filter_Code`, `Control_Type`, `Param_Name`, `Param_Type`, `Operator`, `Default_Value`, `Lookup_Code` |

Key convention (xem spec 10 §1d): `{table}.view.filter.{filter_code}.label/.placeholder/.tooltip`;
nút dùng chung `common.filter.search` / `common.filter.reset`; thiếu tham số `common.validation.required`
= `"{0} là bắt buộc"` ({0} = nhãn control đã i18n).

### 9.4 Engine rules

1. Panel chỉ render khi `Filter_Panel_Enabled=1` **và** `Source_Type ∈ {Sp,Sql}` **và** có ≥1 filter
   (`ViewMetadata.HasFilterPanel`). Nguồn `Table` → dùng filter row trong cột, không có panel.
2. Bấm **Tìm** → validate `Is_Required` rỗng → chặn + thông báo i18n + `FocusAsync()` ô lỗi đầu tiên.
3. Bind tham số: **chỉ** từ `Ui_View_Filter` (whitelist). Ép kiểu theo `Param_Type`; `LIKE` bọc `%...%`;
   rỗng → NULL (SP nên dùng `WHERE (@x IS NULL OR col=@x)`). Giá trị luôn parameterized (Dapper).
4. SP trả nguyên tập đã lọc → **client phân trang** (DxGrid). SP nặng nên tự giới hạn theo tham số.
5. `MultiSelect → IN`: đợt 2 (khung tách mảng theo dấu phẩy đã có cho `Source_Type='Sql'`).

### 9.5 API

`POST /api/v1/views/{code}/search` — body `{ "filters": { "{filter_code}": "value", ... } }`,
query `?lang=vi`. Trả `ViewDataResult` (rows + total). Tham số bắt buộc thiếu / sai định dạng → **400**.

### 9.6 Ánh xạ runtime (Blazor)

| Config DB | Blazor |
|---|---|
| `Filter_Panel_Enabled` + `Filter_Panel_Position` | render `<FilterPanel>` trái/trên `<DataView>` |
| `Ui_View_Filter.Control_Type` | chọn editor DevExpress (DxTextBox/DxDateEdit/DxComboBox/…) |
| `Is_Required` | validate trước khi gọi `/search` + thông báo + focus |
| `Auto_Search_On_Load` | gọi `/search` ngay khi mở (nếu true) |

---

## 10. Bộ lọc liên kết (cascade) + đổ giá trị sang Thêm mới (ADR-030, 2026-06-22)

Mở rộng §9 cho 3 nhu cầu: (a) control lọc **phụ thuộc nhau** (chọn Công ty → nạp Phòng ban →
chọn Năm → nạp Nhân viên); (b) **lọc theo tài khoản đăng nhập** (chỉ đơn vị user được phân quyền);
(c) **đổ giá trị filter sang form Thêm mới** (cho sửa lại hoặc khóa). Vẫn **Hướng A** — chỉ thêm cột
vào `Ui_View_Filter`, KHÔNG bảng song song.

### 10.1 `Ui_View_Filter` — thêm 3 cột (Migration 059)

| Cột | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `Depends_On` | nvarchar(255) | NULL | CSV `Filter_Code` **cha**; cha đổi giá trị → nạp lại options control con. NULL = độc lập. |
| `Default_To_Field` | nvarchar(100) | NULL | `Field_Code` trên form (`Edit_Form_Id`) nhận giá trị filter khi **Thêm mới**. NULL = không prefill. |
| `Default_Lock` | bit | 0 | `1` = khóa (đổ sẵn, read-only) · `0` = đổ sẵn cho sửa lại. |

### 10.2 Token ngữ cảnh trong `Lookup_Sql` (registry — spec 19)

Cascade + scope theo tài khoản viết bằng **SQL động** (`Lookup_Source='dynamic'`, `Lookup_Sql`).
Engine bind SERVER-SIDE các token đăng ký ở `Sys_Context_Param` + giá trị filter cha. **Whitelist** khi
chạy = (`Sys_Context_Param` Is_Active) ∪ (param khai trong `Ui_View_Filter` của View) — ngoài danh
sách → chặn (chống injection). Quy ước tên: `@__xxx` nội bộ engine (cấm) · `@NguoiDungID` định danh ·
hậu tố `_Active` = phạm vi UI chọn (server-validate). Chi tiết: **spec 19_CONTEXT_PARAM_SPEC**.

Token lõi: `@NguoiDungID` (NguoiDung_Id user — ranh giới bảo mật cứng), `@TenantId`, `@LangCode`,
`@CongTyID_Active` (công ty đang chọn ở switcher; `0` = mọi công ty được phân quyền, thu hẹp MỀM).

### 10.3 Cascade — hành vi engine

1. Filter độc lập (`Depends_On` NULL) nạp options khi mở panel (bind token ngữ cảnh).
2. Filter con nạp options khi **mọi cha** trong `Depends_On` đã có giá trị; chưa đủ → disable/trống.
3. Cha đổi giá trị → **nạp lại con + xóa giá trị con đang chọn** (lan truyền theo thứ tự topo).
4. `Lookup_Sql` của con tham chiếu `@<Param_Name cha>` (chỉ filter khai trong `Depends_On` được bind)
   + token ngữ cảnh. VD Phòng ban: `… WHERE CongTy_Id = @CongTyId AND <scope @NguoiDungID>`.

### 10.4 Prefill khi Thêm mới

Bấm **Thêm mới** trên View → ViewPage truyền giá trị panel hiện tại sang form. Với mỗi filter có
`Default_To_Field`: set field đó = giá trị filter; `Default_Lock=1` → render read-only (tái dùng
`Lock_On_Edit`/EffectiveReadOnly); `=0` → đổ sẵn cho sửa. Field không khai → bỏ qua.

### 10.5 Ví dụ — View "Danh sách nhân viên"

| Filter (`Filter_Code`) | `Param_Name` | `Depends_On` | `Lookup_Sql` (rút gọn) | `Default_To_Field` / `Default_Lock` |
|---|---|---|---|---|
| `cong_ty` | `@CongTyId` | — | `…TC_CongTy c JOIN <bảng quyền> q ON q.CongTy_Id=c.Id WHERE q.NguoiDung_Id=@NguoiDungID` | `CongTy_Id` / `1` |
| `phong_ban` | `@PhongBanId` | `cong_ty` | `…TC_PhongBan WHERE CongTy_Id=@CongTyId AND <scope @NguoiDungID>` | `PhongBan_Id` / `0` |
| `nam` | `@Nam` | — | (Number, không prefill) | — |

Lưới (`Source_Type=Sp/Sql`) nhận `@CongTyId/@PhongBanId/@Nam`, lọc `NgayBatDau` theo năm.

### 10.6 Tiền đề runtime

Cascade cần **load options thật** cho Combo/MultiSelect (hiện fallback text — "đợt 2" §9.4.5). Khi sang
giai đoạn code runtime phải làm phần load options trước. Bản ghi/giá trị bên trong màn = **dữ liệu thật
theo cấu hình**, không mock.
