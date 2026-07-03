# TASKS — Lưu trữ (Archive)

> Nhật ký các hạng mục **ĐÃ HOÀN THÀNH** tách khỏi [TASKS.md](TASKS.md) để giảm context mỗi
> session (Nhóm A dọn token, 2026-07-04). Nội dung nguyên văn, không chỉnh sửa. Việc đang mở
> + mọi roadmap còn open item vẫn ở `TASKS.md`. Thứ tự giữ nguyên như bản gốc.

---

## ✅ ConfigStudio: fix i18n preview field + bố cục Control Props + docs WPF (session 70 — 2026-06-28, đã push master)
- [x] **Fix i18n preview** ô Display dính giá trị field TRƯỚC: resolve LẠI từ key canonical (`NormalizeFieldKeysToCanonical`) + `preserveTypedText=!_isLoading` (`ResolveI18nPreviewAsync`) → load rỗng thì xóa trắng. VERIFY DB thật trước (DB đúng, lỗi UI). Commit `f9c2c6c`.
- [x] **On-blur default**: rời ô Nhãn → tự điền Gợi ý/Mô tả nếu trống. Commit `f9c2c6c`.
- [x] **Bố cục Control Props**: ghim hướng dẫn đầu tab (2 cột, ngoài vùng cuộn) + `LookupBoxPropsPanel` chia 2 cột (giảm ~½ chiều cao). Commit `bf19b1b`.
- [x] **Docs**: thêm `docs/huong-dan-wpf/cau-hinh-lookupbox.md` + gom guide WPF vào `docs/huong-dan-wpf/` (git mv + README index + sửa 6 link). Commit `0d29dc9`.
- [ ] (Verify) Rebuild + chạy app: ô Display đúng/trống khi bấm qua field; on-blur; Control Props gọn 1 khung nhìn.
- [ ] (Tùy) áp 2 cột cho `ComboBoxPropsPanel`; responsive gộp cột khi màn hẹp.

## ✅ Hạ tầng AI Templates (session 69 — 2026-06-28, đã push master)
- [x] Rà soát skill/rule/agent/command hiện có + đối chiếu aitmpl.com → báo cáo mâu thuẫn.
- [x] Vá domain drift (design-agent/product-analyst) + ownership path (BRAIN/AGENTS) + đổi 3 spec trùng số (09/10/11→22/23/24). Commit `735abbc`.
- [x] Governance: BRAIN §11 + `docs/ai/TEMPLATE_INTAKE.md` + `docs/ai/AI_TEMPLATE_INTEGRATION_PLAN.md`. Commit `af5bda1`.
- [x] 12 agent (5 nhập aitmpl đã customize + 7 engine tự viết). Commit `05dc695`.
- [x] 14 slash command nối agent. Commit `9847237`.
- [ ] (Sau) Nhập agent Solution Architect / Database Architect / Technical Writer → nối 3 command inline.
- [ ] (Sau) Chạy thử agent review trên diff thật để hiệu chỉnh.

## ✅ Done (session 61b — 2026-06-23: Ẩn Lookup_Sql khỏi /info + Observability correlationId/"Mã lỗi")

> Ad-hoc theo phản hồi user: rò `Lookup_Sql` ở `/views/{code}/info` + lỗi 500 chung chung khó truy khi vận hành.
> Build BE `ICare247.slnx` 0/0 · FE Shared + ICare247_UI 0/0.

- [x] **SEC-VIEW-INFO** — Ẩn `ViewFilter.LookupSql` (SQL admin: tên bảng quyền/cột/token `@NguoiDungID`/`@CongTyID_Active`)
      khỏi response `/info`. Tạo `ViewInfoResponse` + `ViewFilterInfo` (bỏ `LookupSql`, giữ `LookupCode`) ở Application;
      `GetViewByCodeQuery`/Handler trả DTO (`FromEntity`). **KHÔNG** `[JsonIgnore]` lên entity (cache L2 serialize cùng
      kiểu → mất `LookupSql` sau round-trip → vỡ `GetFilterOptionsAsync`). Cột/Action tái dùng entity (không chứa SQL).
- [x] **OBS-LOG-CORR** — Serilog `outputTemplate` thêm `[{CorrelationId}]` mỗi dòng (Console+File) → khách báo "Mã lỗi",
      `grep "<mã>" logs/icare247-*.log` ra request + thời điểm + stacktrace.
- [x] **OBS-CLIENT-CODE** — Helper chung `ApiErrorHelper` (UI.Shared) bóc `correlationId` (root body, `[JsonExtensionData]`)
      + nối "(Mã lỗi: 8 ký tự đầu)"; wire MasterData/View/Form/Runtime/ConfigSync ApiService.

**Decisions Log (session 61b):**
- **Lộ config nhạy cảm = chặn ở tầng serialize-ra-client bằng DTO riêng**, KHÔNG `[JsonIgnore]` entity bị cache (sẽ mất
  field server cần). Cache giữ entity đầy đủ; chỉ `/info` map sang DTO sạch. → [[project-error-correlation-observability]].
- **Lỗi 500 chung chung KHÔNG phải thiếu thông tin**: chi tiết ở log server theo `correlationId` (giờ in mỗi dòng). Câu
  chung chung là cố ý (không lộ exception/SQL). Client chỉ cần hiện "Mã lỗi" để nối về log.
- **Gotcha**: 500 Lưu masterdata = proc DB cũ lệch param (`@NguoiThucHien` vs `@NguoiDungID` đã đổi) → re-deploy
  `db/procs/spc_/sp_AfterSave_Grid_*.sql`. Sửa hook proc nhớ deploy lại DB, không chỉ sửa file. (User đã sửa store.)
- **CHƯA làm (tùy chọn)**: CORS expose `X-Correlation-Id`; gắn "Mã lỗi" cho MenuAdmin/AdminPermission ApiService.

## ✅ Done (session 59 — 2026-06-21: Lưới View xem hết dữ liệu + LookupBox popup teleport + cờ tắt cache + bề rộng modal/popup)

> Ad-hoc theo phản hồi user khi test màn danh mục Xã/Phường (engine-driven View). Build BE + FE 0/0
> (`src/backend/ICare247.slnx`, `src/frontend/ICare247_UI.slnx`).

- [x] **VIEW-LOADALL** — Lưới View kẹt ở 1 trang server (chỉ 20/3321 dòng, không xem tiếp được): `ViewPage.ReloadDataAsync`
      nạp **toàn bộ** (`pageSize: int.MaxValue`) → DxGrid tự phân trang/cuộn ảo client. Tương tự `MasterDataListPage`.
      Backend đã có OFFSET/FETCH, không cap pageSize. Hướng đã chốt với user = **tải hết 1 lần** (hợp danh mục cỡ vừa;
      bảng cực lớn về sau dùng server-side paging).
- [x] **VIEW-VSCROLL-H** — Cuộn ảo cần lưới có chiều cao giới hạn: `.dv-dxgrid { height: calc(100vh - 220px); min-height:320px }`
      (trước `overflow:visible` không set height → không virtualize).
- [x] **LOOKUP-TELEPORT** — Popup LookupBox trong `DraggableModal` bị cắt (modal kéo bằng `transform` = containing-block +
      `overflow:auto`). Fix: **teleport node popup ra `<body>`** + định vị `fixed` theo ô neo (`icare.teleportPopup`/
      `restorePopup` trong index.html); `LookupBoxRenderer` teleport ở `OnAfterRenderAsync`, restore ở `HidePopupAsync`
      TRƯỚC khi ẩn (Blazor remove đúng vị trí gốc, không vỡ diff). → xem [[project-draggablemodal-dropdown-clip]].
- [x] **LOOKUP-DISPLAY** — FK LookupBox hiện id thô ("2") thay vì tên: sau `LoadDataAsync` resolve lại `_inputText =
      GetSelectedDisplay()` (OnParametersSet chạy trước khi data về). Áp dụng mọi field LookupBox.
- [x] **CACHE-TOGGLE** — Cờ `Cache:Enabled` (section appsettings, mặc định true; **Development=false**) bind `CacheSettings`,
      chèn vào `HybridCacheService` (Get→miss/Set→no-op, phủ form/view/lookup/resource vì MetadataEngine dùng chung
      ICacheService) + `NavigationCache` (menu). Log cảnh báo khi tắt. Đọc lúc khởi động → đổi phải restart BE.
- [x] **MODAL-WIDTH** — Modal Thêm/Sửa thu hẹp theo số cột form (`MasterDataForm.OnWidthResolved` → ViewPage/
      MasterDataListPage truyền `DraggableModal.Width`): 1 cột=480px (trước 720), 2=680, 3=900, ≥4=1080.
- [x] **POPUP-WIDTH** — Popup combobox/lookup mặc định = **bề rộng control** (bỏ width cố định 600; teleport đặt
      `minWidth = bề rộng ô`; tại chỗ dùng CSS `min-width:100%`).

**Decisions Log (session 59):**
- **DraggableModal kéo bằng `transform`** → mọi dropdown custom trong modal bị clip (kể cả `fixed`); chuẩn xử lý =
  teleport ra body. DxComboBox/DevExpress tự render overlay cấp body nên miễn nhiễm. (memory mới: `project-draggablemodal-dropdown-clip`.)
- **Lookup "rỗng/không chọn được" thực chất là popup bị cắt**, KHÔNG phải lỗi data/query — không cần đụng `Ui_Field_Lookup`/BE.
- **Cache tắt theo môi trường, không theo build config**: Development (`dotnet run`/launchSettings) tự tắt; bản publish =
  Production → cache bật. An toàn mặc định cho prod.
- **Tải hết dữ liệu lưới (Cách B)** do user chọn — hợp danh mục cỡ vừa; cảnh báo bảng cực lớn nên chuyển server-side paging.

## ✅ Done (session 58 — 2026-06-20: NavMenu sidebar — thu nhỏ rail + bộ lọc i18n + icon sub-menu + đúng thứ tự cấu hình)

> Ad-hoc theo phản hồi user trên sidebar shell (`MainLayout`/`NavMenu`). Build FE 0/0 (`src/frontend/ICare247.UI.slnx`).

- [x] **NAV-RAIL** — Nút thu nhỏ/mở rộng sidebar về **rail icon** (~72px, token `--sidebar-collapsed`), đặt ở
      **topbar** (luôn bấm được), nhớ trạng thái qua **localStorage** `ic247.nav.rail`. Thu gọn = chỉ icon + tooltip
      (ẩn nhãn/caption/caret/ô lọc, giữ logo); bỏ overlay hover (gây "không ghim lại được"). Lớp `app-sidebar__inner`
      tách nền/viền để layout ổn định. Rail chỉ desktop (>768px); mobile vẫn off-canvas ☰.
- [x] **NAV-FILTER** — Ô lọc menu live đầu sidebar (placeholder i18n `nav.filter.placeholder`), lọc theo **nhãn đã
      dịch** (ngôn ngữ giao diện hiện tại — so khớp trên `Loc.L`), nhóm có kết quả tự bung, rỗng → `nav.filter.empty`,
      nút xóa "x".
- [x] **NAV-CSS-DEEP** — Fix GỐC "link thô/xanh gạch chân, icon sai màu": scoped CSS NavMenu KHÔNG ăn vào thẻ `<a>`
      do `<NavLink>` (component con) sinh → bọc **`::deep`** cho `.nav-item`/`.nav-subitem`/icon/label. Buttons
      (plain element) vẫn scoped OK nên trước đây chỉ NavLink bị lỗi.
- [x] **NAV-SUBICON** — Màn con (sub-item) có **icon riêng** (icon màn nếu DB set, mặc định `dot`); thêm icon
      `dot`/`languages`/`chevrons-left` vào `Icon.razor` (+ RegisteredNames). Bỏ đường rail dọc cũ.
- [x] **NAV-GROUPSCREEN** — Màn `Loai=ManHinh` treo THẲNG vào nhóm (vd Tỉnh/Thành phố, Quốc gia) trước bị
      `BuildFromApi` bỏ qua → render thô; nay dựng thành mục đơn có icon trong nhóm.
- [x] **NAV-ORDER** — Trộn phân hệ (`Menu`) + màn treo thẳng (`ManHinh`) của 1 nhóm thành **một danh sách sắp theo
      `ThuTu`** (`VmChild` = Module|Item) = đúng thứ tự cấu hình ở Menu Builder, thay vì luôn nhét màn xuống cuối.

**Decisions Log (session 58):**
- **Nút thu nhỏ đặt ở topbar, KHÔNG trong sidebar** + bỏ hover-expand overlay: tránh "không ghim lại được" khi nút
  bị overlay che / chỉ hiện lúc hover. Rail đơn giản, đoán được; bung = 1 nút luôn thấy.
- **`::deep` cho mọi rule nhắm `<NavLink>`** (NavMenu.razor.css): thẻ `<a>` của component con KHÔNG nhận thuộc tính
  scope `b-xxx` → rule scoped thường rớt (gốc lỗi link xanh/gạch chân + icon sai màu). Pattern bắt buộc khi style
  NavLink/component con bằng scoped CSS.
- **Con của nhóm = 1 sequence theo ThuTu** (`VmChild`): sidebar khớp thứ tự lưới Menu Builder.

> ⚠️ Vận hành (auto-memory `feedback-no-build-while-app-running`): KHÔNG `dotnet build` UI khi web đang chạy → race
> xóa/ghi `_framework/*.wasm|*.pdb` gây 404/SRI integrity (KHÔNG phải bug). Sửa = tắt server → run-ui.bat → hard reload.

---

## ✅ Done (session 56 — 2026-06-18: Menu Builder hoàn thiện + i18n write-store + cache flush + View render modes)

- [x] **MENU-UX** — Quản lý menu: thêm/sửa node qua **popup kéo-di-chuyển** (kéo tiêu đề); panel phải = **chi tiết
      read-only** (bỏ Lưu/Thêm); **Đường dẫn = link mở tab mới** (lưới + chi tiết) để test giao diện; nút **Xóa mỗi
      dòng + dialog xác nhận i18n** (node hệ thống không xóa); nút **▲▼ đổi thứ tự** (swap ThuTu, dùng lại Update).
- [x] **MENU-GRID** — DxTreeList: **filter row theo từng cột đúng kiểu** (text→Chứa, bool→checkbox, số→numeric) +
      **nút đổi toán tử lọc** (FilterMenuButtonDisplayMode=Always) + **resize/đổi vị trí cột** + nút thao tác dạng
      pill nền trắng (đọc được khi dòng đang chọn). Cột Phân hệ/Loại lọc theo **nhãn hiển thị** (ModuleText/LoaiText),
      không theo mã thô.
- [x] **MENU-COMBO** — Module dropdown đọc từ **`HT_PhanHe`** (`db/055` + `GET /admin/menu/modules`); Icon **chọn từ
      `Icon.razor`** (`RegisteredNames`, bỏ gõ tay); View/Node cha/Phân hệ = **DxComboBox bind nguyên object** (bỏ
      ValueField/TextField — kiểu private nested làm reflection fail → Module lưu null). Override `DuongDan` (server
      mặc định auto-derive, cho sửa khi sai).
- [x] **I18N-WRITESTORE** — Dịch tên menu **inline trong popup** theo từng ngôn ngữ, lưu vào **`Sys_Resource`**:
      `PUT /api/v1/resources` (upsert, `[Authorize]`+quyền), `GET .../overlay` + `.../translations`;
      `LocalizationService` **gộp overlay DB lên lớp JSON tĩnh** + `Save/GetTranslationsAsync`. Khóa i18n **ASCII-hóa**
      (`NavKeys.Slug`: bỏ dấu, bỏ prefix group./view.) — NavMenu + Menu Builder dùng chung NavKeys nên khóa khớp.
- [x] **I18N-LANGS** — Ngôn ngữ **động theo `Sys_Language`**: `GET /api/v1/languages`; `LocalizationService` nạp manifest
      từ API (fallback `languages.json`). Thêm ngôn ngữ = 1 dòng DB → popup dịch + bộ chuyển ngôn ngữ tự cập nhật (>2).
- [x] **CACHE-FLUSH** — Xóa cache từ web: nút "↻ Xóa cache" trên màn View/Form + màn quản trị `/m/administration/cache`
      (xóa theo mã View/Form/Lookup, xóa cache combobox client). **Cưỡng chế làm mới toàn bộ cache tenant**:
      `ICacheVersion` (version-stamp **in-memory theo tenant** = **CC-4a** mức 1 instance) gắn vào ConfigCache +
      MetadataEngine keys; `POST /api/v1/admin/cache/flush` bump version + `InvalidateTenant` nav; client tải lại cưỡng chế.
- [x] **VIEW-RENDER** — `DataView`: render mode **Date/DateTime/Time** (ép kiểu client → DxGrid hiện date-picker lọc +
      sort theo ngày) + **Template token** `{Field}` (vd "{Ma} - {Ten}"). `Render_Mode` không có CHECK → không cần migration.

**Decisions Log (session 56):**
- **DxComboBox PHẢI bind nguyên object** (Value/ValueChanged, KHÔNG ValueField/TextField) khi model là kiểu app —
  reflection theo tên-trường của DevExpress fail với kiểu `private`/`internal` nested (fallback ToString, value=null).
  Hiển thị qua `ToString()`; model là **public top-level** (`MenuBuilderModels.cs`). Đây là gốc lỗi "Module lưu null".
- **i18n write-store dùng lại `Sys_Resource`** (không bảng mới); `LocalizationService` = lớp tĩnh JSON (base build) **+**
  lớp DB overlay (sửa runtime, DB thắng); ngôn ngữ DB-driven (`Sys_Language`). Endpoint flush/upsert resources `[Authorize]`.
- **CC-4a version-stamp mức 1 instance (in-memory `ConcurrentDictionary` theo tenant)** — bump = vô hiệu toàn bộ cache
  config tenant không cần liệt kê key. Scale-out ≥2 instance → chuyển sang Redis `INCR cfgver:{tenant}` (CC-4a Redis).
- **Khóa i18n menu = ASCII** (NavKeys.Slug bỏ dấu/đ→d): tránh unicode tiếng Việt trong key; NavMenu & Menu Builder
  cùng đi qua `NavKeys.*` để khóa hiển thị = khóa tra cứu (nếu lệch thì dịch không ăn).

> `db/055` (HT_PhanHe) ✅ ĐÃ CHẠY 2026-06-21 (Data DB). ⏳ Cần vận hành: rebuild + restart API · hard-refresh WASM.
> Tùy chọn: seed `Sys_Language` thêm ngôn ngữ · seed node menu `administration.cache`.

---

## ✅ Done (session 55 — 2026-06-17: Menu Builder web + WPF grid layout persistence + guides)

- [x] **MENU-BUILDER** — Màn web "Quản lý menu" (`/m/administration/menu`) cấu hình cây `HT_ChucNang` bằng
      **chọn** thay vì gõ tay. Kiến trúc: **1 service / 2 factory** (KHÔNG tách microservice) — **ghi đơn-DB**
      `HT_ChucNang` (Data tenant) qua `/api/v1/admin/menu`, **picker đọc Config DB** qua `/api/v1/views` sẵn có.
      Backend: `MenuAdminController` + CQRS `Features/Admin/Menu/*` (GetMenuTree/UpsertMenuNode/DeleteMenuNode) +
      `MenuAdminRepository` (`IDataDbConnectionFactory`) + DI. Sau ghi → `INavigationCache.InvalidateTenant`.
      Web: `MenuBuilderPage.razor` (DxTreeList + form) + `MenuAdminApiService` + DI. `db/054` seed node
      `administration.menu` + grant SUPERADMIN. CRUD đầy đủ; chặn xóa node hệ thống/còn con; chống vòng lặp cha-con.
      Build BE 0/0, FE 0/0. `db/054` ✅ ĐÃ CHẠY 2026-06-21. ⏳ E2E (login admin). (ADR-026)
- [x] **WPF-GRID-LAYOUT** — Mọi `dxg:GridControl` trong ConfigStudio: cho **kéo chỉnh độ rộng cột** + **tự lưu/phục
      hồi layout ra file local** (`%LOCALAPPDATA%\ICare247\ConfigStudio\GridLayouts\*.xml`, KHÔNG vào DB). Gắn 1 lần
      qua implicit `TableView` style (`Themes/Controls.xaml`): `AutoWidth=False` + `AllowResizing=True` +
      attached behavior `GridLayoutBehavior` (key tự sinh từ tên UserControl + chữ ký cột; FNV-1a ổn định).
- [x] **DOC-GUIDES** — `docs/guide/cau-hinh-man-quan-ly-view.md` (7 tab màn Quản Lý View) +
      `docs/guide/cau-hinh-menu.md` (cẩm nang sử dụng Menu Builder, kiểu user-manual).

**Decisions Log (session 55):**
- **Menu Builder = monolith 2-factory, KHÔNG tách service** (phân tích: tách microservice tái tạo chi phí mạng mà
  cache in-process đã khử). Ghi đơn-DB (Data) + đọc chéo chỉ để picker (Config). (ADR-026)
- **`NodeKind` discriminator {Group, View, Form}**: server xử lý CẢ `Form` (whitelist route/đối-tượng). UI v1 = **LC1**
  (Group+View, Form ẩn). **Nâng LC1→LC3 = chỉ sửa web** (bỏ `disabled` option Form + thêm dropdown Form từ
  `FormApiService.GetFormsListAsync`) — KHÔNG đụng backend/DB/endpoint.
- **WPF grid `AutoWidth=False`** (user chốt pixel cố định + cuộn ngang): điều kiện để kéo giãn cột trên lưới nhiều
  cột (AutoWidth=True ép cột co vừa khung → không kéo được). Đánh đổi: cột `Width="*"` mất tự-lấp-đầy.

## ✅ Done (session 54 — 2026-06-16: ConfigStudio table picker + i18n token validation)

- [x] **CFGSTUDIO-SYSTABLE-PICKER** — Màn Sys Table thêm combobox chọn bảng/view thật từ Target DB
      (`ISchemaInspectorService` + `DbObjects`); chọn → tự điền Table_Code/Schema_Name (Table_Name nếu trống);
      guard chỉ fill khi khớp đúng 1 mục. (commit `96c0d9d`)
- [x] **VALIDATION-TOKEN** — Thông báo `required` + `unique` hỗ trợ token **`{0}`=giá trị nhập · `{1}`=nhãn field**
      (thay ở CẢ message per-field lẫn template): `ResourceResolver.ResolveRequired/ResolveUnique` (+helper `ApplyTokens`),
      `SaveMasterDataCommandHandler`/`InsertLookupCommandHandler` (block unique). `db/053` chuẩn hóa template
      `sys.val.Unique/Required` ({1}=nhãn). Build BE 0/0. `db/053` ✅ ĐÃ CHẠY 2026-06-21 (Config DB).
- [x] **DOC-GUIDE** — `docs/guide/cau-hinh-man-danh-muc.md`: hướng dẫn cấu hình màn engine-driven (ví dụ Cấp công ty)
      + biến thể danh mục phẳng/FK/cây + quy ước Form_Code/View_Code.

**Decisions Log (session 54):**
- **Convention token thông báo validation = `{0}` giá trị nhập · `{1}` nhãn field** (đồng nhất required + unique;
  với required `{0}` rỗng vì field trống). Dùng `.Replace` (không `string.Format`) để an toàn dấu ngoặc lẻ.
  Áp ở CẢ message per-field (trước đây trả nguyên văn) lẫn template `sys.val.*`. Rule message (`ResolveRuleMessage`)
  giữ nguyên `{0}`=nhãn — chuẩn hóa sau nếu cần.
- **Sys Table**: chọn bảng/view thật từ **Target DB** (= Live DB), KHÔNG phải Config DB. TargetDb trong
  ConfigStudio Settings phải trỏ `ICare247_Solution` (đã sửa file appsettings local của user).

## ✅ Done (session 2026-06-15b — baseline lưới + rule + pivot engine-driven)

- [x] **UI-GRID-BASELINE** — `DataView`/`MasterDataGrid`: `TextWrapEnabled=true`, `ColumnResizeMode=ColumnsContainer`
      (cuộn ngang, không ép vừa màn), reorder/hover/focus, **cột chọn ghim trái + đầu tiên**; TreeList +
      `DxTreeListSelectionColumn`. Fix `FilterRowCellVisible`→`FilterRowEditorVisible` (crash DX 25.2.3). (commit `41ce53a`)
- [x] **UI-RULE** — Skill `icare247-admin-ui`: tách rule **DxGrid** + **DxTreeList** thành 2 file độc lập
      (`references/`), thêm `references/i18n.md` (**cấm hardcode chuỗi**) + blueprint màn Công ty; ràng buộc i18n vào SKILL.md. (commit `41ce53a`)
- [x] **ORG-BESPOKE-REVERT** — Dựng màn Công ty bespoke full-stack (`d658ff8`) rồi **GỠ** (`0fae3f7`) sau khi chốt
      hướng engine-driven (ADR-024). History `d658ff8` giữ làm tham chiếu thiết kế.

## ✅ Done (session 2026-06-13b — Auth full-stack + bộ màn đăng nhập + audit log non-blocking)

> Đăng nhập **full-stack thật** (JWT + refresh, verify PBKDF2 với `HT_NguoiDung` ở Live DB) + 3 màn
> Auth Blazor theo design đã chốt + cơ chế **log hành vi non-blocking** (Channel→Redis Stream→SqlBulkCopy
> vào DB audit RIÊNG per-tenant). Build backend + frontend **0/0**.

- [x] **CFG-CONN** — Connstring ngoài repo: đổi `Data`→`Demo` (QLNS_Demo), thêm **`LiveData`** (ICare247_Solution,
      login đọc từ đây) + **`Audit`** (ICare247_Solution_Audit). Sửa `TenantConnectionResolver`/`ConnectionChecker`
      đọc `LiveData`/`Audit`. `Jwt:SecretKey` sinh thật. (`appsettings.local.json` ngoài git + placeholder repo.)
- [x] **AUTH-BE** — Backend auth: package `Microsoft.Extensions.Identity.Core` + `System.IdentityModel.Tokens.Jwt`;
      Domain `NguoiDung`; `Features/Auth` (Login verify PBKDF2 + lockout 5 lần/15', Refresh rotation, Logout,
      Forgot/Reset **stub**); Infra `AuthRepository`/`RefreshTokenRepository` (Dapper, Live DB) + `JwtTokenService` +
      `IdentityPasswordHasher`; `AuthController` `POST /api/v1/auth/{login,refresh,logout,forgot-password,reset-password}`.
- [x] **AUTH-FE** — `auth.css` (tách riêng), components `AuthShell/AuthInput/AuthButton/BrandPanel`, 3 màn
      `Login`(thật)/`ForgotPassword`/`ResetPassword`(UI+stub), `AuthService` thật + `TokenStore`(localStorage) +
      `JwtAuthenticationStateProvider` + guard `MainLayout` (chưa login→/login; đã login vào /login→/). Bộ chọn
      ngôn ngữ trong cột form. Package RCL thêm `Microsoft.AspNetCore.Components.Authorization`. **Đăng ký: bỏ** (cổng ứng viên — sau).
- [x] **AUDIT-1** — Log hành vi non-blocking: `IAuditWriter`/`IAuditQueue` (bounded, **drop khi đầy**, không chặn
      request) + `AuditBackgroundService` (có Redis→Redis Stream `ic247:audit`→consumer group; không→ghi thẳng DB) +
      `AuditNkWriter` (SqlBulkCopy theo tenant). Gắn enqueue: Auth (login success/failed/locked, logout, refresh),
      MasterData (create/update/delete). DB audit **RIÊNG per-tenant** (`db/040` bảng `NK_NhatKyHoatDong`; `db/041`
      thêm cột `Audit_Conn_Encrypted` cho catalog). ⏳ CẦN tạo `ICare247_Solution_Audit` + chạy `db/040`.
- [x] **DOCS-DEBUG** — `docs/backend-debug/`: README (bản đồ lớp + pipeline + breakpoint theo lớp + bảng lỗi) +
      trang debug từng tính năng (auth-login mẫu, refresh/logout, forgot/reset, forms-config, runtime-form,
      master-data, views) + `redis-setup.md` (hướng dẫn cài Redis).

**Decisions Log:**
- **Login chạy fallback `X-Tenant-Id`** (chưa dựng Catalog local) — hạ tầng đã sẵn sàng đa tenant; subdomain để sau.
- **Audit non-blocking, ưu tiên UX tuyệt đối**: request chỉ enqueue (~micro-giây); hàng đợi đầy → **drop** (không chặn);
  ghi DB dồn nền + gộp lô SqlBulkCopy. Mục tiêu "10k login không khựng giao diện".
- **DB audit tách RIÊNG, per-tenant**: tránh tranh transaction-log/RAM/I-O với DB nghiệp vụ. Event chỉ mang `TenantId`
  (KHÔNG nhét connstring vào Redis → tránh lộ secret); resolve audit-conn lúc ghi (catalog `Audit_Conn_Encrypted`
  hoặc `ConnectionStrings:Audit`, rỗng→fallback Live DB).
- **Forgot/Reset = stub** (chưa SMTP/kho token); **Đăng ký = bỏ** (để sau cho cổng ứng viên nộp CV).
- `db/` là vùng Codex — `db/040`+`041` Claude tạo thay, đã ghi `AI_HANDOFF.md` (AUDIT-1).

## ✅ Done (session 2026-06-13 — Data DB nền tảng: chốt tiền tố + spec HT_/DM_/TC_ + migration)

> Chốt **bộ tiền tố bảng Data DB theo module nghiệp vụ** + thiết kế **spec nền tảng** (người dùng,
> danh mục, tổ chức) + **sinh SQL migration** 037/038/039. User đã tạo DB `ICare247_Solution` và
> chạy đủ 3 script.

- [x] **DDB-PREFIX** — Chốt **10 tiền tố theo module** (qua hỏi-đáp): `HT_` (Hệ Thống) `TC_` (Tổ Chức)
      `DM_` (Danh Mục) `NS_` (Nhân Sự) `TL_` (Tiền Lương) `TM_` (Thương Mại — gộp hàng hóa/mua/bán/kho)
      `CN_` (Công Nợ) `BC_` (Báo Cáo) `NK_` (Nhật Ký) `TT_` (Tệp Tin). → **ADR-022** + update ADR-019.
- [x] **DDB-SPEC** — `docs/spec/11_DATA_DB_SCHEMA.md`: spec **16 bảng nền tảng** (`DM_` 5 + `TC_` 4 + `HT_` 7).
      Convention: không `Tenant_Id` (DB-per-tenant), khối auto `Id/CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver`,
      cột nghiệp vụ tiếng Việt, `Ma`/`Ten` generic (KHÔNG entity-suffix) — §0.1/§0.2. Tổ chức = **2 cây tree
      tự tham chiếu** (Công ty + Phòng ban, mỗi cây có cấp). Hành chính **2 cấp** (Tỉnh→Phường/Xã, bỏ Huyện
      theo mô hình VN 2025). `HT_NguoiDung` gọn (auth-only): 2FA/SSO/lockout/mobile/hạn dùng; thông tin cá nhân
      lấy qua `NhanVien_Id` (bắt buộc — siết NOT NULL ở đợt NS_).
- [x] **DDB-037** — `db/037_create_data_db_foundation.sql`: DDL 16 bảng (idempotent, FK, filtered-unique `Ma`,
      phá vòng lặp `TC_PhongBan↔HT_NguoiDung` bằng ALTER FK cuối file). Chạy trên Data DB.
- [x] **DDB-038** — `db/038_seed_data_db_bootstrap.sql`: super-admin `admin`/`Admin@12345` (PBKDF2 Identity v3,
      `NhanVien_Id` NULL — ngoại lệ bootstrap §6.7) + vai trò SUPERADMIN + danh mục cấp + Quốc gia VN.
- [~] **DDB-039 (đã thu hồi)** — Ban đầu seed `Sys_Lookup`/`Sys_Resource` cho trạng thái; sau **đổi hướng**:
      mã trạng thái hệ thống = **C# enum + i18n shell** (không Sys_Lookup) → file 039 **đã xóa** khỏi repo (spec §0.3/§6.1).
- [x] **DDB-RUN** — User tạo DB `ICare247_Solution` + chạy đủ 037/038/039 ✅.

**Decisions Log:**
- **Tiền tố theo module nghiệp vụ** (không theo bản chất dữ liệu DS_/GD_) → nhất quán Config DB + 8 module UI;
  Trade gộp `TM_`; bảng hạ tầng (DM_/HT_/NK_/TT_) tách riêng. (ADR-022)
- **Phân quyền toàn bộ ở Data DB** (`HT_`): VaiTro/ChucNang/VaiTro_Quyen; `Sys_Role/Permission` Config DB chỉ cho Engine.
- **Mã trạng thái hệ thống** (TrangThai/LoaiTaiKhoan/HinhThuc2FA) = **C# enum + frontend i18n shell** (đa số UI
  hardcode → không chạm Config DB); Data DB chỉ lưu chuỗi code. Sys_Lookup chỉ cho danh mục tenant tự cấu hình/màn Engine. (spec §0.3)
- **Data DB KHÔNG chứa cấu hình nền tảng** (IP): 2 DB tách (Config `ICare247_Config` + Data `ICare247_Solution`),
  không FK cross-DB; render = ghép trong RAM ở app (ConfigCache/enum+i18n), KHÔNG JOIN SQL. (spec §0.3)
- **Hash mật khẩu = PBKDF2** qua `PasswordHasher<T>` (.NET built-in, versioning + rehash khi login). Cột `nvarchar(256)`.
- **`Ma`/`Ten` generic** (không entity-suffix `MaDonViTinh`) để giữ engine generic; FK thì entity-suffix `{Bang}_Id`.
- **INSERT/seed set cột audit tường minh** (CreatedBy/CreatedAt), không dựa DEFAULT — memory `feedback-explicit-audit-columns`.
- **`015_create_cf_data_schema.sql` (Cf_*) giữ làm tham khảo** cho module mua bán cà phê nhân (`TM_` sau), KHÔNG migrate trực tiếp.
- **Hoãn** (dựa name-match fallback): đăng ký `Sys_Table` + `Sys_Relation` cho FK Data DB — làm khi đưa bảng vào metadata Engine.

## ✅ Done (session 2026-06-12 — Frontend ICare247_UI: dựng khung + menu + i18n)

- [x] **FE-MOVE** — Chuyển `ICare247.Blazor.RuntimeCheck` từ `src/backend/src/` → `src/frontend/` (user di chuyển folder). Sửa path ảnh hưởng: `run-blazor.bat`, `run-all.bat`, `.claude/launch.json`, `src/backend/ICare247.slnx` (`../frontend/...`), 2 docs spec (12/13). Build verify OK.
- [x] **FE-KHUNG** — Dựng khung end-user app `ICare247_UI` theo **modular monolith** (mỗi module 1 RCL): tạo RCL `ICare247.UI.Shared` (`ApiClientBase`, `IAuthService`+stub, `AppState`, `AddIcare247UiShared()`); host nối Shared; solution frontend `src/frontend/ICare247.UI.slnx`. Build 0/0.
- [x] **FE-SHELL** — Shell ERP responsive: `MainLayout` (sidebar + topbar + ☰ off-canvas mobile), `NavMenu` accordion **data-driven từ `AppNav`** (8 phân hệ + màn con), `AuthLayout`, `Dashboard` (`/`), `ScreenView` (placeholder `/m/{module}` lưới thẻ + `/m/{module}/{screen}` stub + breadcrumb). Trang dev cũ đổi route `/` → `/dev/forms`. Verify chạy thật (desktop + mobile) OK.
- [x] **FE-RUNUI** — `run-ui.bat` mở web (https://localhost:7027, tự bật trình duyệt). Sửa `launchSettings` cổng phụ `5040`→`5173` (5040 bị OS chặn).
- [x] **FE-I18N** — i18n shell (gói #1–#4 + #6): `LocalizationService` (lazy-load, key suy từ cấu trúc + base vi tại chỗ gọi, JSON = value overlay, localStorage, CultureInfo, tham số {0}, gộp overlay đa nguồn), `LocalizedComponentBase`, `LanguageSwitcher` (đổ từ manifest), `wwwroot/i18n/{languages.json,en.json}`. Bind NavMenu/MainLayout/Dashboard/ScreenView qua `Loc.L`. Pseudo-localization verify: không còn chuỗi hardcode; English fallback về vi OK. Build 0/0.

**Decisions Log:**
- **Modular monolith FE**: mỗi module nghiệp vụ = 1 RCL riêng (user chốt), host `ICare247_UI` + `ICare247.UI.Shared` (cross-cutting). Map 8 module: Organization, Hr, Payroll, Trade, Finance (riêng), Reporting, Administration, + Auth.
- **i18n shell (user chốt)**: KEY thuộc cấu trúc/code (suy từ slug AppNav), KHÔNG gõ key tay vào JSON; JSON chỉ là *value overlay*; base vi ở fallback luôn hiển thị → "key có trước, dịch sau". Tách khỏi `Sys_Resource` (chỉ cho nội dung động form/field/view). Thêm ngôn ngữ = thả 1 file `{lang}.json` + 1 dòng manifest.
- **DDD**: cân nhắc nhưng **chưa áp dụng**, giữ Clean Architecture hiện tại (chỉ thảo luận).
- Để sau (i18n): #5 xuất skeleton Excel + import, #7 trang `/dev/i18n` báo độ phủ/key thiếu, #8 tránh trùng `common.*` với Sys_Resource, #9 RTL, #10 `<html lang>`.

## ✅ Done (session 2026-06-10 — Test Grid + Grid UX + WPF config cột)

- [x] **VIEW-3f** — Endpoint `GET /api/v1/views` (CQRS `GetViewsList` + `IViewRepository.GetListAsync`, ROW_NUMBER khử trùng code ưu tiên tenant) + trang `TestGrid` (`/test-grid`, link ở `MainLayout`) chọn View từ danh sách → render `DataView`. Build 0/0.
- [x] **🐞 Bug fix DxGrid** — `DxGridDataColumn.FilterRowCellVisible` KHÔNG tồn tại ở DX 25.2.3 → ném `InvalidOperationException` khi set parameters → **rớt toàn bộ cột Data** (chỉ còn cột lệnh). Sửa thành `FilterRowEditorVisible`. (Ảnh hưởng cả `/view/{code}`.)
- [x] **VIEW-3f (grid UX)** — `DataView`: grid `ColumnResizeMode=NextColumn`/`AllowColumnReorder`/`HighlightRowOnHover`/`FocusedRowEnabled`/`KeyboardNavigationEnabled`; cột `MinWidth` + **ghim `FixedPosition`** + sort mặc định `SortIndex/SortOrder` (thêm 3 field vào `ViewColumnDto`).
- [x] **VIEW-3f.1** — Filter operator Mức 1: ô filter mặc định `Contains` (text) / `Equal` (số,boolean) + `FilterMenuButtonDisplayMode=Always` để user đổi operator.
- [x] **VIEW-4f** — ConfigStudio WPF tab "Cột" thêm 4 cột chỉnh (MinWidth, Ghim, SortMặc định, SortIdx) — model/`ViewDataService` đã lưu sẵn, chỉ bổ sung UI. Web không cần sửa thêm (đã consume). Build WPF 0/0.
- [x] **Tài liệu** — `docs/reference/DEVEXPRESS_{DXGRID,CONTROLS}_PROPERTIES.md` reflect từ DLL v25.2.3 (DxGrid + 32 control).

**Decisions Log:**
- Verify thuộc tính DevExpress qua **reflection DLL / Console** trước khi dùng — đừng đoán (đã ghi memory `feedback-devexpress-verify-api`). `DxPopover` không tồn tại → dùng `DxFlyout`.
- **NGUYÊN TẮC SỐ 1** ở CLAUDE.md: luôn hỏi trước khi quyết định; dữ liệu thật theo cấu hình, không mock.
- Hoãn: **VIEW-3g** (lưu layout/user — chốt localStorage vs bảng per-user+auth), **VIEW-3h** (filter operator Mức 2 metadata-driven — cần DB migration + cột WPF `Filter_Operator`).

## ✅ Done (session 2026-06-08 — NumericBox locale format)

- [x] **BZ-NumFmt** — `NumericBoxRenderer`: real-time thousand separator khi gõ (JS interop `icare.setupNumericInput`). `locale=""` → quốc tế `9,999.05`, `locale="vi"` → VN `9.999,05`. `DxSpinEdit.Culture` theo locale. Prop `locale` trong `Control_Props_Json`. TODO: đọc mặc định từ system config (CC-config-number-format). ✅ (2026-06-08)

---

## ✅ Done (Session 38 — Master Data CRUD full-stack — 2026-06-06)

> Màn List bản ghi danh mục + Thêm/Sửa/Xóa. Form Thêm/Sửa render **Popup** hoặc **Tab** (nội bộ SPA)
> theo cấu hình `Ui_Form.Display_Mode` do WPF quyết định. Lưới List hiện cột theo `Ui_Field.Show_In_List`.
> Xóa = **xóa cứng** nhưng **chặn nếu đang bị tham chiếu** (soft-check theo quy ước tên PK).

**Quyết định đã chốt:**
- `Display_Mode`: **cột mới** trên Ui_Form (Popup|Tab), không repurpose Layout_Engine.
- Xóa: **cứng** (DELETE row), KHÔNG soft-delete.
- Cột List: **cờ mới** `Ui_Field.Show_In_List`.
- "New Tab": **tab nội bộ SPA** (routed page), không browser tab.
- Soft-check FK: **theo quy ước tên** — PK `CongTyID` → cột tham chiếu `CongTyID` hoặc `%_CongTyID`.
  Quét `Sys_Column` **KHÔNG lọc Is_Active** (bắt cả dữ liệu cũ). Log rõ bảng.cột + số dòng bị khóa.

#### Tầng 0 — Database ✅
- [x] **DB-1** — `db/023_ui_form_display_mode.sql` (Display_Mode + CHK constraint)
- [x] **DB-2** — `db/024_ui_field_show_in_list.sql` (Show_In_List)
- [x] **DB-3** — Cập nhật `docs/spec/02_DATABASE_SCHEMA.md` (2 cột mới + constraint)
- [ ] **DB-RUN** — Chạy 023 + 024 trên DB thật ⏳ (manual step)

#### Tầng 1 — Backend (generic CRUD + soft-check) ✅ (build 0/0)
- [x] **BE-1** — `IMasterDataRepository` + `MasterDataRepository`: GetFormInfo/GetList/GetById/Insert/Update/Delete (tên bảng `[Schema_Name].[Table_Code]` đọc từ Ui_Form→Sys_Table ở server, parameterized, verify tenant, safe identifier regex)
- [x] **BE-1b** — `IReferenceCheckService` + `ReferenceCheckService`: soft-check quy ước tên PK (`CongTyID` hoặc `%[_]CongTyID`), quét Sys_Column **không lọc Is_Active**, try/catch từng candidate, trả `ReferenceUsage[]` {schema,table,column,rowCount,isLegacy}, log chi tiết nơi khóa
- [x] **BE-2** — CQRS `Features/MasterData/`: GetFormInfo/GetList/GetRecord/CheckUsage query; Save (Insert|Update, **chạy ValidationEngine server-side**); Delete (soft-check enforce). Result DTOs: MasterDataSaveResult/DeleteResult
- [x] **BE-3** — `MasterDataController`: GET info, GET list, GET {id}, GET {id}/usage, POST, PUT, DELETE. Validation fail → 422; bị tham chiếu → 409 + blockedBy[]
- [x] **BE-4** — DI (IMasterDataRepository + IReferenceCheckService scoped) + build verify ICare247.slnx 0/0

#### Tầng 2 — Blazor (List + CRUD + Popup/Tab) ✅ (build 0/0 + verify live DB thật)
- [x] **BZ-1** — `Services/MasterDataApiService.cs` (7 endpoint + DTOs; xử lý 422 validation, 409 blockedBy)
- [x] **BZ-2** — `Pages/MasterData/MasterDataListPage.razor` (`@page "/master/{FormCode}"`) — container, list/search/active filter, switch Popup↔Tab theo Display_Mode
- [x] **BZ-3** — `Components/MasterData/MasterDataGrid.razor` — lưới cột theo Show_In_List (fallback all) + nút Sửa/Xóa
- [x] **BZ-4** — Form host: logic switch Display_Mode nằm trong ListPage (Popup = modal inline; Tab = NavigateTo) — gộp, không tách component riêng
- [x] **BZ-5** — `MasterDataForm.razor` (tái dùng FieldRenderer + lưới responsive) + `MasterDataTabPage.razor` (`@page "/master/{FormCode}/edit/{Id?}"`)
- [x] **BZ-6** — `ConfirmDeleteDialog.razor` — soft-check khi mở, liệt kê schema.table.column + số dòng + nhãn "dữ liệu cũ", ẩn nút Xóa khi bị tham chiếu
- [x] **BE-fix** — PK resolve: `Sys_Column.Is_PK` không đáng tin (DB thật toàn False, PK không đăng ký field) → fallback đọc PK **vật lý** từ Data DB INFORMATION_SCHEMA (cả MasterDataRepository + ReferenceCheckService). Label resolve qua Sys_Resource.
- [x] **Verify live** (API↔DB QLNS_Demo): info+PK vật lý (NhanVienID/TrinhDoVanHoaID), list 7 bản ghi + label tiếng Việt, getById, soft-check usage; UI List grid + Popup form 12 field lưới 4 cột responsive.

> ⚠️ Live test phải tạm trỏ Blazor BaseUrl sang http://localhost:5215 (https 7130 không bind trong sandbox) — **đã revert** về https://localhost:7130.

#### Tầng 3 — WPF ConfigStudio (cấu hình) ✅ (build 0/0)
- [x] **WPF-1** — FormEditor: `Display_Mode` dropdown (Popup/Tab) thay `Layout_Engine`. `IFormDataService.UpdateFormMetadataAsync` + `CreateFormAsync` thêm `displayMode` param. `FormDataService` thêm SET clause `Display_Mode = @DisplayMode`. `FormDetailRecord` + `FormDetailDataService` thêm `DisplayMode`. ViewModel: `DisplayMode` property + `DisplayModeOptions`. XAML: "Chế độ mở form" ComboBox.
- [x] **WPF-2** — FieldConfig: `ShowInList` (bool). `FieldConfigRecord` thêm property. `FieldDataService` thêm `Show_In_List` vào SELECT/INSERT/UPDATE + `BuildFieldParam`. `FieldConfigViewModel` thêm property + load/save. `FieldConfigView.xaml` thêm ToggleSwitch "📋 Hiện trong danh sách" vào Behavior grid (Col 2, Row 4).
- [x] **WPF-3** — Build verify WPF ConfigStudio.WPF.UI.slnx: 0 Warning, 0 Error ✅

---

## ✅ Done (Session 37 — Responsive form grid + SysTable UX polish — 2026-06-06)

UI/UX fixes: lưới field FormRunner responsive theo thiết bị + dọn vài điểm UX màn Sys_Table.

- [x] **Blazor responsive grid** — `.fields-grid` đổi từ `repeat(4,1fr)` cứng (không media query) sang dùng biến `--cols` + media query: Desktop ≥992px = 4 cột, Tablet 768–991px = 2 cột, Mobile ≤767px = 1 cột. `FieldRenderer` đổi `grid-column: span {ColSpan}` cứng → `--col-span` để CSS clamp `span min(--col-span,--cols)`. Ngưỡng đồng bộ Bootstrap 5 (theme DevExpress bs5). Verified live 1280/768/375px.
- [x] **SysTableManager UX** — bỏ nút "+ Bản ghi mới" (trùng NewCommand với "Làm mới form nhập"); đổi `SaveButtonText` "Tạo mới" → "Lưu"; bọc `ScrollViewer MaxHeight=80` cho LoadErrorMessage; `AutoPopulateColumns="False"` (obsolete XLS1111) → `AutoGenerateColumns="None"`.

**Decisions Log:**
- Nguyên tắc responsive: **metadata (`Col_Span`) = ý đồ logic bố cục, render engine = reflow + clamp số cột theo breakpoint**. Metadata cố định cột KHÔNG tự responsive — trách nhiệm reflow thuộc engine, không thuộc metadata.
- Breakpoint canh theo Bootstrap 5 (md=768, lg=992) vì app nạp theme `blazing-berry.bs5.min.css` của DevExpress (xây trên Bootstrap 5).
- Phát hiện phụ: cột `Ui_Form.Layout_Engine` (Grid/Flex/Custom) hiện là **field chết** — không DTO/engine nào đọc; FormRunner luôn render cùng 1 layout. Ứng viên: repurpose thành Display_Mode (Popup/Tab) khi triển khai MasterDataTemplate.

---

## ✅ Done (Session 36 — LookupBox "thêm mới entity" full-stack — 2026-06-05)

Tính năng: thêm mới bản ghi danh mục ngay trên control LookupBox (mở dialog Ui_Form → insert → auto-select). Bật/tắt theo từng field.

- [x] **Tầng 1 — Config**: migration `022_ui_field_lookup_add_addnew.sql` (`Allow_Add_New` + `Add_Form_Code`); Domain `FieldLookupConfig`, RuntimeCheck `FieldLookupConfigDto`, `FormRepository.sqlLookupConfigs`.
- [x] **Tầng 2 — Backend**: `DynamicLookupRepository.InsertAsync` (parameterized + verify tenant + safe identifier); `InsertLookupCommand`/handler; `POST /api/v1/lookups/insert` + `InsertLookupRequest`.
- [x] **Tầng 3 — Frontend**: `ILookupQueryService.InsertAsync` (+`LookupInsertResult`); `LookupAddDialog.razor` (+css) tái dùng `FieldRenderer`; `LookupBoxRenderer` nút "➕ Thêm mới" + auto-select.
- [x] **WPF config**: `FieldLookupConfigRecord`, `FieldDataService` (read+upsert), `FieldConfigViewModel`, `LookupBoxPropsPanel.xaml`.
- [x] **Docs**: `docs/spec/13_LOOKUP_ADD_NEW_GUIDE.md`.

**Decisions Log:**
- Bảng đích đọc từ server (`Ui_Field_Lookup.Source_Name`) theo `fieldId`, **không** nhận từ client → bảo mật.
- FieldCode = tên cột DB (`COALESCE(Field_Code, Column_Code)`) → insert map trực tiếp, không cần bảng ánh xạ.
- Dialog tái dùng `FieldRenderer` → tự hỗ trợ mọi control + cascade lồng nhau.
- ⏳ Pending: insert **chưa** chạy ValidationEngine server-side (chỉ check required client); chưa test runtime (cần DB + Ui_Form đích).

---

## ✅ Done (Session 35 — Cascade LookupBox fix + keyboard nav — 2026-06-05)

- [x] **Fix cascade bug** — `DynamicLookupRepository`: unwrap `JsonElement` → primitive trước khi add vào Dapper. Lỗi cũ: `NotSupportedException: member ... of type JsonElement cannot be used as a parameter value` khi cascade truyền `@FieldCode`. Áp dụng cho cả `QueryAsync` + `QueryTreeAsync`.
- [x] **Keyboard nav LookupBox** — ↑/↓ di chuyển highlight, Enter chọn, Escape đóng. Dòng đầu tự highlight khi gõ.
- [x] **Keyboard nav TreeLookupBox** — ↑/↓/Enter/Escape trên danh sách node hiển thị.
- [x] **TreeLookupBox lọc trực tiếp trên control** — bỏ thanh search trong popup, EditBox `<div>` → `<input>` gõ thẳng (mirror LookupBox). Node/toggle dùng `onmousedown` + `preventDefault` để không nuốt click khi blur.
- [x] **Docs** — `docs/spec/12_CASCADE_LOOKUP_GUIDE.md`: hướng dẫn cấu hình Tỉnh→Xã (filterSql `@FieldCode` + ReloadTriggerField), verify đúng theo runtime.

**Decisions Log:**
- Cascade runtime: `@param` trong filterSql **phải trùng FieldCode** field cha (repo bind context key = FieldCode trực tiếp, không qua bảng ánh xạ). Reload do `ReloadTriggerField` (đơn) quyết định — `filterParams`/`reloadOnChange` trong Control_Props **không** được RuntimeCheck renderer tiêu thụ.

---

## ✅ Done (Wave UX ConfigStudio + Wave 017 — 2026-05-17)

### Wave A — Navigation Quick Wins ✅ (2026-05-17)
Commits: `0322cb2` (A.1) + `7e8b173` (A.2)
- [x] A1 — Double-click row mở Editor (FormManager, SysLookup, ValidationRule, EventEditor)
- [x] A2 — Right-click context menu trên grid (4 manager view, DevExpress pattern PlacementTarget chain)
- [x] A3 — Keyboard shortcuts global (Alt+1..9, Ctrl+B/N/S/F/Z/Y/F5/Esc) + per-view
- [x] A4 — `IRegionMemberLifetime.KeepAlive` cho 5 Manager VM → giữ filter khi navigate qua lại
- [x] A5 — Breadcrumb bar + Back/Forward + Alt+←/→ (INavigationHistoryService + chain hierarchical/root)

### Wave D — Power editing FormEditor ✅ (2026-05-17)
Commits: `8261a41` (D.1) + `d1ab936` (D.2) + `b79f074` (D.3)
- [x] D.1 — Quick Property Bar (QPB Row 3 dưới FormEditor, edit 6 prop nóng không cần FieldConfig đầy đủ)
- [x] D.2 — Multi-select Bulk Editor (tick N field → panel cam → IsThreeState toggle → Apply hàng loạt)
- [x] D.3 — Grid-edit Mode tab ("Bảng Fields", DevExpress Grid edit-mode group theo Section, lazy hydrate)

### Wave 017 — Cleanup Is_Enabled + thêm Lock_On_Edit ✅ (2026-05-17, ADR-017)
Commits: `dcbc5f0` (refactor 24 files) + `45fe1cc` (Effective ReadOnly Blazor)
- [x] Migration `017_lock_on_edit_replace_is_enabled.sql`
- [x] Backend: Domain `FieldMetadata`, `FieldRepository`, `FormRepository`, `ValidationEngine`
- [x] Blazor: `RuntimeModels.FieldState`, `FormMetadataDto`, `FormRunner.razor`, 8 renderer
- [x] WPF: `FieldConfigRecord`, `FormTreeNode`, `FieldDataService`, `FieldConfigViewModel`, `FormEditorViewModel`, QPB/Bulk/Grid XAML
- [x] Docs: `02_DATABASE_SCHEMA.md`, `architecture_decisions.md` (ADR-010 revised + ADR-017 added)
- [x] Effective ReadOnly logic: `FieldState.EffectiveReadOnly = IsReadOnly OR (LockOnEdit AND IsEditMode)`, FormRunner đọc `?recordId` query param → set IsEditMode, 8 renderer migrate sang `EffectiveReadOnly`

**Decisions Log:**
- ADR-017: bỏ `Is_Enabled` vì overlap với ReadOnly+Visible và % case dùng thực nhỏ. Thay bằng `Lock_On_Edit` phục vụ pattern key/code/audit field. `SET_ENABLED` action alias sang `SET_READONLY` (backward compat seed).
- D.1/D.2 hydrate cache lazy `Dictionary<int, FieldConfigRecord>` per FormEditor instance, fetch lần đầu khi user chọn/edit, dùng lại cho mọi save sau đó.
- D.3 same-instance binding: Grid + Tree + QPB cùng bind `FormTreeNode` reference → sửa 1 chỗ update 3 chỗ, không cần manual sync.

---

## ✅ Done (Session 34 — LookupBox UX + Cache API + Bug fixes — 2026-06-05)

### WPF Bug Fix
- [x] **BUG-FC1** — Fix Section dropdown mất khi navigate field trong Left Panel
  - Root cause: `ExecuteNavigateToField` hardcode `sectionId = 0` → `OnNavigatedTo` set SectionId=0 → restore tìm không ra → dropdown trắng
  - Fix: thêm `SectionId` vào `FieldNavGroup`, populate khi build navigator, tìm group chứa item → truyền đúng sectionId

### Backend — Cache Invalidate API
- [x] **BE-CACHE** — Thêm endpoint `POST /api/v1/config/forms/{code}/invalidate-cache`
  - `FormController` inject `IMetadataEngine`, gọi `InvalidateFormCacheAsync` (xóa L1 + L2)

### Blazor — LookupBox UX Redesign
- [x] **BLZ-LB1** — Redesign LookupBox thành searchable combobox
  - Bỏ popup grid có header → input trực tiếp + dropdown list đơn giản
  - `onmousedown` để SelectRow chạy trước `onblur` (tránh race)
  - Sync `_inputText` ↔ display value khi focus/blur/select
- [x] **BLZ-LB2** — Thêm nút "🗑 Clear Cache" trên FormRunner header
  - `FormApiService.InvalidateCacheAsync` → gọi API invalidate → reload form
- [x] **BLZ-LB3** — Redesign CSS dropdown list item
  - Header tiêu đề (State.Label, uppercase 11px)
  - Padding `9px 14px`, margin `1px 6px`, border-radius `6px`
  - Selected: màu tím + dấu `✓`; Hover: background xám nhạt

### Comment Rules
- [x] Cập nhật `.claude-rules/comment-rules.md` — bắt buộc XML doc tiếng Việt + `<remarks>` ghi sự kiện theo sau mỗi hàm

**Decisions Log:**
- LookupBox chuyển sang pattern "searchable combobox" — bỏ popup grid vì header không cần thiết, UX gõ thẳng tự nhiên hơn
- `onmousedown` thay `onclick` cho SelectRow để tránh blur đóng popup trước khi chọn được item
- Cache invalidate endpoint đặt trong `FormController` (không `RuntimeController`) vì đây là admin operation

---

## ✅ Done (Session 33 — Expression Builder + TreeLookupBox — 2026-06-04)

- [x] **BUG-EB1** — Fix Expression Builder: FIELD context trống (commit `89d368f`)
  - EventEditorViewModel inject IFormDetailDataService, load + cache formFields, truyền vào DialogParameters
  - ExpressionFieldInfo.cs thêm Core.Data (dùng chung Events + Grammar)
- [x] **BUG-EB2** — Fix Expression Builder: không nhập được Literal (commit `89d368f`)
  - Thêm Literal Editor panel (TextBox + NetType ComboBox + Áp dụng) hiện khi chọn node Literal
- [x] **FEAT-TLB** — Implement TreeLookupBox editor type (commit `bd157b6`)
  - Migration 021: Parent_Column vào Ui_Field_Lookup
  - Backend: QueryTreeAsync + POST /api/v1/lookups/query-tree
  - Blazor: TreeLookupBoxRenderer.razor (tree expand/collapse, search, cascade)
  - WPF: AvailableEditorTypes, ParentColumn prop, LookupBoxPropsPanel panel xanh
- [x] **BUG-AS1** — Fix NullReferenceException AutoSave StatusChanged lambda (commit `f230f97`)
  - Guard `if (_autoSave is null) return` trong lambda sau DisposeP0Services

**Decisions Log:**
- TreeLookupBox dùng flat list + `__parentId` key (inject bởi repository) — client build tree. Không thay đổi DB query pattern, tận dụng lại QueryDynamic infrastructure.
- FieldInfo.cs trong Grammar giữ lại dưới dạng global alias → backward compat (không breaking change).

---

## ✅ Done (Wave ComboBox/LookupBox System — 2026-03-28)

### Wave — ComboBox/LookupBox System (2026-03-28)

> **Bối cảnh:** Hệ thống có 2 dạng dropdown Blazor hoàn toàn khác nhau: DxComboBox (static/dynamic list) và DxDropDownBox (FK lookup với popup grid + template phức tạp). Cần typed ControlProps models, WPF dedicated panels, và Blazor renderer thật thay placeholder.

#### WAVE 1 — Nền tảng

- [x] **T1** — Domain: `ComboBoxControlProps.cs` + `LookupBoxControlProps.cs` — typed C# models cho Control_Props_Json _(2026-03-28)_
- [x] **T2** — DB Migration `014_ui_field_lookup_add_cols.sql`: thêm `Reload_Trigger_Field`, `EditBox_Mode`, `Code_Field`, `DropDown_Width`, `DropDown_Height` vào `Ui_Field_Lookup` _(2026-03-28)_
- [x] **T3** — `FieldLookupConfig.cs` domain entity + `FieldLookupConfigRecord.cs` WPF DTO + `FieldDataService.cs` SELECT/UPSERT: thêm 5 fields mới từ T2 _(2026-03-28)_

#### WAVE 2 — WPF Config Studio

- [x] **T4** — WPF NEW: `Views/Panels/ControlProps/ComboBoxPropsPanel.xaml` — 3 sections: Data Source + Search + Display _(2026-03-28)_
- [x] **T5** — WPF NEW: `Views/Panels/ControlProps/LookupBoxPropsPanel.xaml` — FK source + EditBox mode + Popup grid + Diễn giải _(2026-03-28)_
- [x] **T6** — `FieldConfigViewModel.cs`: thêm IsLookupOrComboBoxEditor + IsRadioGroupEditor + props ComboBox (SearchMode, SearchFilterCondition, AllowUserInput, NullTextKey, DropDownWidthMode, ClearButton, GroupFieldName, DisabledFieldName) _(2026-03-28)_
- [x] **T7** — `FieldConfigViewModel.cs`: thêm props LookupBox (EditBoxMode, CodeField, DropDownWidth, DropDownHeight, ReloadTriggerField) + SaveAsync/LoadAsync updated _(2026-03-28)_
- [x] **T8** — `FieldConfigView.xaml`: thêm `panels:` namespace, RadioGroup section, LookupBoxPropsPanel thay FK Lookup inline, ComboBoxPropsPanel _(2026-03-28)_

#### WAVE 3 — Blazor Runtime

- [x] **T9** — Blazor NEW: `Services/ILookupQueryService.cs` + `Services/LookupQueryService.cs` — POST /api/v1/lookups/query-dynamic _(2026-03-28)_
- [x] **T10** — Blazor NEW: `Components/FieldRenderers/ComboBoxRenderer.razor` — HTML select với dynamic data + cascade reload _(2026-03-28)_
- [x] **T11** — Blazor NEW: `Components/FieldRenderers/LookupComboBoxRenderer.razor` — DxComboBox static Sys_Lookup, searchMode/allowUserInput/clearButton, thay HTML select _(2026-03-31)_
- [x] **T12** — Blazor NEW: `Components/FieldRenderers/LookupBoxRenderer.razor` — popup grid + 3 EditBox modes + search _(2026-03-28)_
- [x] **T13** — `FieldRenderer.razor` + `FormRunner.razor`: add combobox/fklookup cases + Context param + NormalizeFieldType _(2026-03-28)_

#### WAVE 4 — Backend API

- [x] **T14** — `POST /api/v1/lookups/query-dynamic` trong `LookupController.cs` + `IDynamicLookupRepository` + `DynamicLookupRepository` Dapper + CQRS handler _(2026-03-28)_
- [x] **DB** — Chạy migration `014_ui_field_lookup_add_cols.sql` trên DB thật _(xác nhận 2026-04-25: schema canonical 000_create_schema.sql)_

#### WAVE 5 — Bug Fix: Migration 014 columns bị bỏ quên (2026-03-30)

> **Root cause:** 3 SQL queries trong backend không SELECT các cột mới từ Migration 014 (`EditBox_Mode`, `Code_Field`, `DropDown_Width`, `DropDown_Height`, `Reload_Trigger_Field`).
> `DynamicLookupRepository.BuildSafeSql` chỉ SELECT 2 cột (ValueColumn + DisplayColumn) — popup grid không thấy cột bổ sung.

- [x] **B1** — `FormRepository.cs` `sqlLookupConfigs`: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B2** — `FieldRepository.cs` batch load SQL: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B3** — `FieldRepository.cs` `LoadLookupConfigAsync` single SQL: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B4** — `DynamicLookupRepository.cs`: thêm `Code_Field` vào config SQL + `LookupCfgRow` _(2026-03-30)_
- [x] **B5** — `DynamicLookupRepository.BuildSafeSql`: mở rộng SELECT gồm cột từ `PopupColumnsJson` + `CodeField` — dùng `BuildSelectColumns()` helper _(2026-03-30)_

#### WAVE 6 — Bug Fix: Runtime 500 errors khi load /form/sys_UI_Design (2026-03-30)

> **Root cause 1:** `EventRepository.cs` dùng `uf.Field_Code` — cột không tồn tại trên `Ui_Field`. FieldCode thực ra là `Sys_Column.Column_Code` (join qua `Ui_Field.Column_Id`).
> **Root cause 2:** `DynamicLookupRepository` throw `InvalidOperationException` khi `Source_Name` NULL/empty trong DB — nên return `[]` gracefully thay vì 500.

- [x] **B6** — `EventRepository.cs`: thêm `LEFT JOIN Sys_Column sc ON sc.Column_Id = uf.Column_Id`, đổi `uf.Field_Code` → `sc.Column_Code` ở cả SELECT + WHERE _(2026-03-30)_
- [x] **B7** — `DynamicLookupRepository.cs`: guard `string.IsNullOrWhiteSpace(cfg.SourceName)` → return `[]` (không throw 500) _(2026-03-30)_

#### WAVE 7 — Documentation: Form Runtime Flow (2026-03-30)

- [x] **D1** — `docs/form-runtime-flow.puml` — PlantUML sequence diagram đầy đủ 8 phase (LOAD → RENDER → INTERACT → SUBMIT) _(2026-03-30)_
- [x] **D2** — `docs/form-runtime-flow.txt` — ASCII text version, readable trong bất kỳ editor nào _(2026-03-30)_

---

## ✅ Done — Field Config Schema Fix (2026-03-26)

> **Bối cảnh:** Phân tích lại schema phát hiện inconsistency: `Is_ReadOnly` là cột trong `Ui_Field` nhưng `Is_Required` lại được lưu như `Val_Rule`. Quyết định ADR-010: cả 3 (`Is_Visible`, `Is_ReadOnly`, `Is_Required`) phải là cột tĩnh trong `Ui_Field`.
> Đồng thời bổ sung `Is_Enabled` (disabled ≠ readonly), 2 rule type mới (`Length`, `Compare`), 3 action type mới (`SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`).

### Wave A — Database Migration ✅ (2026-04-25)

- [x] Tạo `db/migrations/010_field_behavior_columns.sql` _(gộp vào 000_create_schema.sql)_
  - `ALTER TABLE Ui_Field ADD Is_Required bit NOT NULL DEFAULT 0`
  - `ALTER TABLE Ui_Field ADD Is_Enabled  bit NOT NULL DEFAULT 1`
- [x] Tạo `db/migrations/011_add_rule_types.sql` _(gộp vào 001_seed_all.sql)_
  - INSERT `Val_Rule_Type`: `Length` + `Compare`
- [x] Tạo `db/migrations/012_add_action_types.sql` _(gộp vào 001_seed_all.sql)_
  - INSERT `Evt_Action_Type`: `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] **Chạy migrations 010–012 trên DB thật** _(xác nhận Wave C commit 707c882)_

### Wave B — Backend ✅ (2026-03-26)

- [x] `FieldMetadata.cs` — thêm `IsRequired`, `IsEnabled`
- [x] `FieldRepository.cs` — thêm `Is_Required`, `Is_Enabled` vào SELECT
- [x] `ValidationEngine.cs` — Length + Compare rules: supported via AST evaluation (len() đã có trong BuiltinFunctions) + unit tests xác nhận (145/145 pass)
- [x] `EventEngine.cs` — thêm handler cho `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] `UiDelta.cs` — thêm comment 3 action types mới
- [x] Required rule: giữ backward compat (deprecated comment), không xóa (ADR-011)
- [x] Resource System: IResourceRepository, ResourceResolver, MetadataEngine, CacheKeys mới
- [x] RuntimeController: dùng IMetadataEngine thay IFormRepository
- [x] Build verify — 0 errors, 0 warnings, 145/145 tests

### Wave C — ConfigStudio WPF ✅ (commit 707c882, 2026-03-26)

- [x] `db/migrations/010`: chạy migration trước Wave C
- [x] `FieldConfigRecord.cs` (Core/Data) — thêm `IsRequired`, `IsEnabled`
- [x] `IFieldDataService.cs` — không thay đổi interface (IsRequired/IsEnabled đã trong record)
- [x] `FieldDataService.cs` — cập nhật SQL SELECT/UPSERT thêm `Is_Required`, `Is_Enabled`
- [x] `FieldConfigViewModel.cs` — thêm `IsEnabled`; xóa `ToggleRequiredRule`
- [x] `FieldConfigView.xaml` — Behavior card 2×2 Grid: IsVisible + IsReadOnly / IsRequired + IsEnabled
- [x] `ValidationRuleEditorViewModel.cs` — xóa `Required`, thêm `Length` + `Compare` (IsCompareType, EditCompareField, EditCompareOp, CompareOpOptions, preview)
- [x] `ValidationRuleEditorView.xaml` — thêm section Compare + preview Border
- [x] `EventEditorViewModel.cs` — thêm `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] Build verify WPF — 0 errors, 0 warnings

### Wave D — Tài liệu ✅ (2026-03-26)

- [x] Tạo `docs/spec/09_FIELD_CONFIG_GUIDE.md` — hướng dẫn end-user đầy đủ (2026-03-26)
- [x] Cập nhật `docs/spec/02_DATABASE_SCHEMA.md` — thêm cột `Is_Required`, `Is_Enabled` vào bảng `Ui_Field`; cập nhật Val_Rule_Type + Evt_Action_Type
- [x] Cập nhật `docs/spec/04_ENGINE_SPEC.md` — thêm rule type `Length`, `Compare`; action type `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`; fix Val_Rule_Field → Field_Id trực tiếp
- [x] Cập nhật `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` — deprecated `Required`; thêm schema đầy đủ `Length`, `Compare`, `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`

---

## ✅ Done (session 2026-04-17)

### Wave 10 — i18n captionKey + WPF UX fixes (2026-04-17) ✅

> i18n hóa caption cột popup LookupBox, fix 4 bugs WPF ConfigStudio liên quan đến LookupBoxPropsPanel.

- [x] **i18n captionKey** — Đổi `Caption` → `CaptionKey` (i18n resource key) trong `FkColumnConfig`; WPF auto-gen key theo pattern `{table}.col.{snake_case}` khi nhập FieldName via `WireFkColumnHandlers` _(2026-04-17)_
- [x] **RegisterI18nKeysAsync** — Khi lưu field, tự động INSERT key vào `Sys_Resource` với default value = FieldName (user vào I18nManager để đặt bản dịch thật) _(2026-04-17)_
- [x] **MetadataEngine resolve captionKey** — `ResolvePopupColumnCaptionsAsync`: batch load từ `Sys_Resource` qua `GetByKeysAsync`, rewrite JSON với `caption` resolved trước khi cache; Blazor nhận text đã dịch _(2026-04-17)_
- [x] **FieldLookupConfig** — `PopupColumnsJson` đổi `init` → `set` để MetadataEngine mutate sau construction _(2026-04-17)_
- [x] **SpinEdit race condition** — `UpdateSourceTrigger=PropertyChanged` cho Width/DropDownWidth/DropDownHeight → lưu đúng giá trị khi nhấn "Lưu Field" _(2026-04-17)_
- [x] **SysLookupManagerView XamlParseException** — `AutoGenerateColumns="False"` → `"None"` (DX enum) _(2026-04-17)_
- [x] **MainWindow fullscreen che taskbar** — Hook `WM_GETMINMAXINFO` (0x0024) dùng `MonitorFromWindow` + `GetMonitorInfo` giới hạn MaxSize = WorkArea; `DragMove()` chỉ gọi khi `WindowState == Normal` _(2026-04-17)_
- [x] **Popup columns UX** — Thêm nút ▲▼ di chuyển thứ tự cột; nút xóa ✕ đỏ rõ ràng; fix `columns: []` trong JSON preview (gọi `RebuildControlPropsJson()` sau khi load xong từ DB) _(2026-04-17)_

**Quyết định thiết kế:**
- captionKey pattern: `{table_lower}.col.{field_snake_case}` — auto-gen khi nhập FieldName, chỉ overwrite khi còn empty hoặc vẫn theo auto-gen pattern
- `RegisterI18nKeysAsync` dùng `IF NOT EXISTS INSERT` để không overwrite bản dịch user đã nhập
- WM_GETMINMAXINFO hook dùng `MonitorFromWindow(MONITOR_DEFAULTTONEAREST)` để hỗ trợ multi-monitor

---

## ✅ Done (session 2026-04-16)

### Wave 9 — Bug Fix: Blazor Renderer UI bugs (2026-04-16) ✅

> Phân tích cấu hình JSON export, xác định và fix 6 bugs liên quan đến rendering ComboBox/LookupBox/DatePicker.

- [x] **ComboBoxRenderer** — đổi `<select>` HTML → `DxComboBox` với dynamic data (map `DynamicRows` → `List<LookupItem>` typed để DxComboBox bind được) _(2026-04-16)_
- [x] **LookupBoxRenderer** — fix `PopupColDef`: `Column`/`Title` → thêm `[JsonPropertyName("fieldName")]`/`[JsonPropertyName("caption")]` khớp với WPF output _(2026-04-16)_
- [x] **DynamicLookupRepository** — fix `PopupColEntry`: thêm `[JsonPropertyName("fieldName")]` → BUILD SELECT đúng cột popup _(2026-04-16)_
- [x] **LookupBoxRenderer.razor.css** — tạo mới CSS scoped: `position: absolute` cho popup → không còn chiếm layout inline _(2026-04-16)_
- [x] **LookupComboBoxRenderer** — đổi `@bind-Value` từ `ValueField=ItemCode` sang bind `LookupOptionDto?` trực tiếp → fix DX hiển thị item đầu tiên khi null _(2026-04-16)_
- [x] **LookupComboBoxRenderer** — thêm `Task.Delay(50)` trong `HandleLostFocus` → tránh race condition với `ValueChanged` _(2026-04-16)_
- [x] **FormRunner** — race condition fix trong `OnFieldBlur`: snapshot value trước API, bỏ qua kết quả nếu value đã thay đổi _(2026-04-16)_
- [x] **FormRunner** — thêm `ExportConfigJsonAsync()` + button "⬇ Export config JSON" → download `{FormCode}_config.json` _(2026-04-16)_
- [x] **DatePickerRenderer** — thêm `ClearButtonDisplayMode="Auto"` → nút xóa ngày hiển thị đúng _(2026-04-16)_
- [x] **index.html** — thêm `icare.downloadJson()` JS helper cho blob download _(2026-04-16)_

**Quyết định thiết kế:**
- `PopupColumnsJson` key format là `fieldName`/`caption`/`width` (WPF output) — không phải `column`/`title`. Tất cả consumer phải dùng `[JsonPropertyName]`.
- `LookupComboBoxRenderer` bind `LookupOptionDto?` (full object) thay vì `string` (ItemCode via ValueField) — tránh DevExpress null display bug.

---

## ✅ Done (session 2026-04-15)

### Wave 8 — Bug Fix: Repository SQL mismatches + UI bugs (2026-04-15) ✅

> Phân tích 4 bugs giao diện FormRunner từ screenshots, fix toàn bộ SQL column mismatches giữa code vs DB schema.

- [x] Bug fix: `DependencyRepository.cs` dùng `src.Field_Code` / `tgt.Field_Code` không tồn tại → JOIN `Sys_Column sc_src/sc_tgt`, dùng `Column_Code` _(2026-04-15)_
- [x] Bug fix: `FormRepository.sqlFields` thiếu `Is_Required`, `Is_Enabled` → FieldState.IsRequired luôn false → asterisk * không hiện _(2026-04-15)_
- [x] Bug fix: `FormRepository.sqlCloneFields` thiếu `Is_Required`, `Is_Enabled`, `Col_Span`, `Lookup_Source`, `Lookup_Code` → clone form mất config _(2026-04-15)_
- [x] Bug fix: `FieldRepository.GetByFormIdAsync` trả `Label_Key` thô → thêm `langCode` param + JOIN `Sys_Resource` → COALESCE(Resource_Value, Label_Key) _(2026-04-15)_
- [x] Bug fix: `IFieldRepository` + `ValidationEngine` — thêm `langCode` param, truyền đúng vào cả 2 calls _(2026-04-15)_
- [x] Bug fix: `LookupOptionDto.ToString()` override → DxComboBox render đúng label trong dropdown list _(2026-04-15)_
- [x] Bug fix: `StubFieldRepository` trong tests — update signature mới _(2026-04-15)_
- [x] Doc fix: `FieldMetadata.Label` comment sai "đã resolve" → mô tả đúng cả 2 repository _(2026-04-15)_

**Vấn đề còn tồn đọng:**
- `DefaultValueJson` orphan property — `FieldMetadata` có property nhưng DB không có cột. Luôn null. Cần quyết định: thêm migration hoặc xóa.
- `ComboBoxRenderer` dùng native `<select>` — visual inconsistency so với DxComboBox (chưa cần fix ngay).

---

## ✅ Done (session 2026-04-01)

### Wave — FormRunner Renderers (2026-03-29 → 2026-04-01) ✅

> Nâng DX 24.2→25.2.3, xây đầy đủ 6 renderer (TextBox/Memo/CheckBox/NumericBox/DatePicker/LookupComboBox), fix CSS, fix backend SQL bugs.

- [x] Fix DX v25 CSS: theme file `blazing-berry.bs5.min.css` (không phải `blazing-berry.min.css`) _(2026-04-01)_
- [x] NumericBoxRenderer, DatePickerRenderer, LookupComboBoxRenderer _(2026-03-31)_
- [x] TextBoxRenderer, MemoRenderer, CheckBoxRenderer _(2026-03-30)_
- [x] WPF ControlProps schemas sync _(2026-03-30)_
- [x] Bug fix: RuleRepository `Field_Code` → `Sys_Column.Column_Code` _(2026-03-31)_
- [x] Bug fix: DynamicLookupRepository dùng sai DB (Config vs Data) _(2026-03-31)_
- [x] Bug fix: EventRepository `Field_Code` → `Sys_Column.Column_Code` _(2026-03-30)_
- [x] Bug fix: DynamicLookupRepository guard SourceName NULL _(2026-03-30)_

### Wave — ComboBox/LookupBox System (2026-03-28) ✅

> 14 tasks hoàn thành: Domain models, DB migration 014, WPF panels, Blazor renderers, Backend API.

---

## ✅ Done (session 2026-03-25)

### Phase 10 — Schema Extension: Tab + Lookup (2026-03-25)

> Thảo luận thiết kế + viết migration SQL + cập nhật spec

- [x] Thiết kế multi-tab form: Ui_Form → Ui_Tab → Ui_Section → Ui_Field
- [x] `005_add_ui_tab.sql` — bảng Ui_Tab (Tab_Code unique, filtered index Is_Default)
- [x] `006_alter_ui_section_add_tab.sql` — thêm Tab_Id nullable FK vào Ui_Section
- [x] `007_alter_ui_field_add_cols.sql` — thêm Col_Span (tinyint 1-3), Lookup_Source, Lookup_Code + 3 constraints
- [x] `008_add_ui_field_lookup.sql` — bảng Ui_Field_Lookup (1-1 với Ui_Field, Query_Mode/Source/Filter/Popup_Columns_Json)
- [x] `009_fix_sys_lookup_tenant.sql` — fix Sys_Lookup.Tenant_Id: DEFAULT 0 → NULL, rebuild filtered indexes trong transaction
- [x] Cập nhật `docs/spec/02_DATABASE_SCHEMA.md` — toàn bộ schema mới + sơ đồ quan hệ + thống kê 33 bảng

**Việc tiếp theo (máy khác):**
- [x] Chạy migrations 003→009 trên DB thật (theo thứ tự) — done trước session này
- [x] Cập nhật Domain entities: SectionMetadata (+ TabId), FieldMetadata (+ ColSpan, LookupSource, LookupCode), TabMetadata, FieldLookupConfig
- [x] Cập nhật Repositories: FormRepository, FieldRepository (query Ui_Tab, Col_Span, Lookup_Source, Lookup_Code, Ui_Field_Lookup)
- [x] Cập nhật ConfigStudio: IFieldDataService + FieldDataService + FieldConfigViewModel + FieldConfigView (ColSpan radio, FK Lookup config, transaction save)
- [x] Blazor: FieldType `select` (static Sys_Lookup) + `fklookup` placeholder — LookupApiService, FormRunner, FieldRenderer

### Design System — Brand & UI Foundation

- [x] Thảo luận chốt brand direction: "I Care 24/7" — ấm áp, tận tâm, đa ngành
- [x] Chọn color strategy: Multi-color 3 màu (Coral → Violet → Teal), gradient logo
- [x] Chọn typography: Plus Jakarta Sans (heading) + Inter (body)
- [x] Generate `docs/design-system/tokens.css` — toàn bộ CSS custom properties
- [x] Generate `docs/design-system/README.md` — documentation Design System
- [x] Tạo `.claude/agents/design-agent.md` — custom agent cho thiết kế UI
- [x] Tạo `.claude/commands/design.md` — slash command `/design`

---

## 🟡 Cần làm (Todo)

### Phase 1 — Foundation

- [x] Tạo Domain entities (FormMetadata, FieldMetadata, SectionMetadata, RuleMetadata)
- [x] Tạo Domain AST nodes (IExpressionNode, LiteralNode, IdentifierNode, BinaryNode, ...)
- [x] Tạo Domain Engine interfaces (IAstEngine, IValidationEngine, IEventEngine, IMetadataEngine)
- [x] Tạo EvaluationContext value object
- [x] Tạo Application interfaces (IFormRepository, IFieldRepository, IDbConnectionFactory, ICacheService)
- [x] Tạo CacheKeys.cs
- [x] Tạo GetFormByCodeQuery + Handler (skeleton)
- [x] Implement Infrastructure: SqlConnectionFactory
- [x] Implement Infrastructure: FormRepository (Dapper)
- [x] Implement Infrastructure: FieldRepository (Dapper)
- [x] Implement Infrastructure: HybridCacheService (Memory + Redis)
- [x] Tạo FormController (skeleton)
- [x] Tạo ExceptionHandlingMiddleware

### Phase 2 — Grammar V1 / AST Engine

- [x] Implement AstParser (Expression_Json → IExpressionNode)
- [x] Implement AstCompiler (IExpressionNode → Func<context, object?>)
- [x] Implement FunctionRegistry + BuiltinFunctions (len, trim, iif, toDate, today, ...)
- [x] Unit tests cho AstEngine (125 tests)

### Phase 3 — Validation Engine

- [x] Implement ValidationEngine (validate field/form, Required/Custom rules, condition check)
- [x] Implement IRuleRepository + RuleRepository (Dapper, Val_Rule + Val_Rule_Field)
- [x] Implement IDependencyRepository + DependencyRepository (Dapper, Sys_Dependency topological sort)
- [x] Unit tests cho ValidationEngine (16 tests)

### Phase 4 — Event Engine

- [x] Tạo Domain entities (EventDefinition, EventAction) — Entities/Event/
- [x] Tạo IEventRepository interface — Application/Interfaces/
- [x] Implement EventEngine (handle event → evaluate condition → execute actions → UiDelta)
- [x] 6 action handlers: SET_VALUE, SET_VISIBLE, SET_REQUIRED, SET_READONLY, RELOAD_OPTIONS, TRIGGER_VALIDATION
- [x] DI registration (scoped) trong DependencyInjection.cs
- [x] Build verify — 0 errors, 0 warnings

### Phase 5 — API + Infrastructure bổ sung

- [x] TenantMiddleware — extract X-Tenant-Id → ITenantContext (scoped)
- [x] CorrelationMiddleware — extract/generate X-Correlation-Id → Serilog LogContext + response header
- [x] JWT Auth — SymmetricSecurityKey, TokenValidationParameters từ appsettings.json
- [x] OpenTelemetry TracerProvider — ASP.NET Core + HttpClient instrumentation
- [x] Scalar UI (đã có sẵn) + Health Check endpoint /health
- [x] appsettings.json + appsettings.Development.json — JWT, Serilog config
- [x] Database seed script — db/001_seed_lookup_data.sql (triggers, actions, functions)
- [x] ValidationBehavior — MediatR pipeline auto-validate trước handler
- [x] EventRepository — Dapper multi-mapping (Evt_Definition + Evt_Action)
- [x] RuntimeController — POST validate-field, validate, handle-event
- [x] Build verify — 0 errors, 0 warnings

### Phase 6 — Form Management CRUD (Backend API)

> Dựa trên phân tích DB + wireframe Ui_Form (2026-03-18)

**Application Layer — Queries**
- [x] `GetFormsListQuery` + Handler — phân trang, filter theo Platform/Table_Id/Is_Active, search theo Form_Code
- [x] `GetFormByIdQuery` + Handler — lấy form metadata + related counts (dùng chung GetByCodeAsync)
- [x] `GetFormAuditLogQuery` + Handler — lấy từ Sys_Audit_Log WHERE Object_Type='Form'

**Application Layer — Commands**
- [x] `CreateFormCommand` + Handler — validate Form_Code unique, set Version=1, Checksum, insert Sys_Audit_Log
- [x] `UpdateFormCommand` + Handler — Version++, recalc Checksum, invalidate cache + Sys_Audit_Log
- [x] `DeactivateFormCommand` + Handler — set Is_Active=0, invalidate cache, insert Sys_Audit_Log
- [x] `RestoreFormCommand` + Handler — set Is_Active=1, insert Sys_Audit_Log
- [x] `CloneFormCommand` + Handler — sao chép Form + Sections + Fields sang Form_Code mới

**Infrastructure Layer**
- [x] `FormRepository.GetListAsync` — Dapper query với paging, multi-filter, ORDER BY Form_Code
- [x] `FormRepository.GetByIdAsync` — JOIN Sys_Table để lấy Table_Name, Tenant context
- [x] `FormRepository.CreateAsync` — INSERT Ui_Form, trả về Form_Id mới
- [x] `FormRepository.UpdateAsync` — UPDATE Ui_Form + Version++ + Checksum
- [x] `FormRepository.SetActiveAsync` — UPDATE Is_Active (dùng chung cho Deactivate + Restore)
- [x] `FormRepository.ExistsCodeAsync` — CHECK Form_Code unique (dùng trong validation)
- [x] `FormRepository.CloneAsync` — transaction: copy Form → Sections → Fields → Events
- [x] `AuditLogRepository` — INSERT + GetByObject (Dapper, phân trang)

**API Layer**
- [x] `GET  /api/v1/config/forms` — list + filter + paging
- [x] `GET  /api/v1/config/forms/{code}` — detail by Form_Code
- [x] `POST /api/v1/config/forms` — tạo form mới
- [x] `PUT  /api/v1/config/forms/{code}` — cập nhật form
- [x] `POST /api/v1/config/forms/{code}/deactivate` — vô hiệu hóa
- [x] `POST /api/v1/config/forms/{code}/restore` — khôi phục
- [x] `POST /api/v1/config/forms/{code}/clone` — nhân bản
- [x] `GET  /api/v1/config/forms/{code}/audit` — audit log

### Phase 7 — Form Management CRUD (ConfigStudio WPF)

> Màn hình quản lý Ui_Form theo wireframe đã thiết kế (2026-03-18)
> Stack: Prism 9 + MaterialDesign + MVVM — đặt trong module Forms

**Screen 02 nâng cấp — FormManagerView (List + Filter)**
- [x] `FormManagerViewModel` — thêm ObservableCollection, filter properties (Platform, TableId, SearchText), paging support
- [x] Filter toolbar: ComboBox Platform (`Tất cả/web/mobile/wpf`), ComboBox Table (load mock/API), SearchBox debounce
- [x] DataGrid nâng cấp: thêm cột Version, Is_Active (toggle chip), row style mờ khi inactive
- [x] Actions per row: Edit, Clone, Preview, Deactivate/Restore (ẩn/hiện theo Is_Active)
- [x] Summary bar footer: tổng / active / inactive count
- [x] Navigate sang FormDetailView khi click vào Form_Code link

**Screen mới — FormDetailView (readonly)**
- [x] `FormDetailView.xaml` + `FormDetailViewModel` — Prism navigation, nhận param `FormCode`
- [x] Header metadata: Form_Code, Platform, Table, Version, Is_Active chip, Checksum, Updated
- [x] TabControl: Sections (count) | Fields (count) | Events (count) | Rules (count) | Audit Log
- [x] Sub-DataGrid cho từng tab (readonly, không edit inline)
- [x] Tab Audit Log: DataGrid Thời gian / Action / User / Correlation_Id

**Screen 02 nâng cấp — FormEditDialog (Create/Edit)**
- [x] `FormEditDialogView.xaml` + `FormEditDialogViewModel` — IDialogAware (Prism Dialog)
- [x] Tab 1 — Thông tin cơ bản: TextBox Form_Code (regex validate `^[A-Z0-9_]+$`, unique check), ComboBox Platform, ComboBox Table, ComboBox Layout_Engine, TextBox Description, ToggleButton Is_Active, readonly Version/Checksum
- [x] Tab 2 — Sections & Fields: ListBox sections trái, DataGrid fields phải (navigate sang FieldConfig)
- [x] Tab 3 — Events: DataGrid (Trigger, Field_Target, Condition snippet, Actions count), navigate sang EventEditor
- [x] Tab 4 — Permissions: DataGrid roles × CheckBox (Can_Read, Can_Write, Can_Submit)
- [x] ValidationSummary TextBlock hiện lỗi khi submit
- [x] Dirty check: hiện ConfirmDialog khi đóng mà chưa lưu

**Dialogs**
- [x] `DeactivateFormDialog.xaml` + VM — Dialog confirm: tên form, impact (sections/fields/events), nút Xác nhận/Hủy
- [x] `CloneFormDialog.xaml` + VM — TextBox Form_Code mới (regex + unique validate realtime), nút Clone/Hủy

### ConfigStudio.WPF.UI — Skeleton Screens

- [x] Screen 01: Shell + Navigation
- [x] Screen 02: Form Manager
- [x] Screen 03: Form Editor
- [x] Screen 04: Field Config (4 tabs: Basic, Control Props, Validation Rules, Events)
- [x] Screen 05: Validation Rule Editor (skeleton)
- [x] Screen 06: Event Editor (skeleton)
- [x] Screen 07: Expression Builder Dialog (5 models, 4 AST services, IDialogAware)
- [x] Screen 08: Dependency Viewer (Canvas graph, auto-layout, filter, detail panel)
- [x] Screen 09: Grammar Library (skeleton)
- [x] Screen 10: i18n Manager (skeleton)
- [x] Screen 11: Publish Checklist (11 check items, async run, jump-to navigation)

### ConfigStudio.WPF.UI — Triển khai UI thật (thứ tự ưu tiên)

- [x] Screen 03: Form Editor — TreeView sections/fields, toolbar, property panel, navigate FieldConfig
- [x] Screen 02: Form Manager — DataGrid danh sách form, search/filter, Add/Edit/Delete, navigate FormEditor
- [x] Screen 05: Validation Rule Editor — DataGrid rules, add/edit/delete, link Expression Builder
- [x] Screen 06: Event Editor — DataGrid events, trigger config, action config
- [x] Screen 09: Grammar Library — 2 tab Functions/Operators, DataGrid whitelist, add/edit
- [x] Screen 10: i18n Manager — DataGrid key/language matrix, filter, import/export

### ConfigStudio.WPF.UI — Direct DB (Hướng B: Dapper trực tiếp, không qua API)

> WPF admin tool kết nối SQL Server trực tiếp. Backend API giữ nguyên cho Blazor runtime sau.

**Wave 1 — Foundation + Standalone modules**
- [x] Tạo 6 service interfaces (IFormDetailDataService, IFieldDataService, IRuleDataService, IEventDataService, IGrammarDataService, II18nDataService)
- [x] Tạo 15 record DTOs (Core/Data/)
- [x] Tạo 6 service implementations (Infrastructure/) — Dapper
- [x] Register DI trong App.xaml.cs
- [x] Migrate GrammarLibraryViewModel → IGrammarDataService
- [x] Migrate I18nManagerViewModel → II18nDataService
- [x] Build verify Wave 1

**Wave 2 — FormDetail (read-only)**
- [x] Migrate FormDetailViewModel → IFormDetailDataService (header, sections, fields, events, rules, audit)
- [x] Build verify Wave 2

**Wave 3 — FieldConfig**
- [x] Migrate FieldConfigViewModel → IFieldDataService + II18nDataService + IRuleDataService + IEventDataService
- [x] Build verify Wave 3

**Wave 4 — Rule + Event editors**
- [x] Migrate ValidationRuleEditorViewModel → IRuleDataService
- [x] Migrate EventEditorViewModel → IEventDataService
- [x] Build verify Wave 4

### ConfigStudio.WPF.UI — P0 UX Features (sau khi Direct DB hoàn thành)

- [x] Auto-save — AutoSaveService: debounce 3s, status indicator (Idle/Pending/Saving/Saved/Error)
- [x] Undo/Redo — UndoRedoService: stack-based (max 50), JSON snapshot, Ctrl+Z/Y commands
- [x] Live Linting — LintingService: debounce 500ms, 8 lint rules (naming, duplicate, required, structure)
- [x] Impact Preview — ImpactPreviewService: scan rules/events cho field references, expression analysis
- [x] FormEditorViewModel integrated: all 4 services wired in OnNavigatedTo/OnNavigatedFrom
- [x] Build verify — 0 errors, 0 warnings (backend + WPF 7 projects)

### Phase 8 — Auto-generate Fields từ DB Schema (ConfigStudio WPF)

> Phân tích: 2026-03-21 | Mục tiêu: giảm thời gian cấu hình form bằng cách đọc cấu trúc cột DB thực sự và tự động sinh fields

**Bước 1 — Target DB Connection trong SettingsView** ✅ Done (2026-03-21)
- [x] Thêm `TargetServer`, `TargetDatabase`, `TargetUserId`, `TargetPassword`, `TargetTrustServerCertificate` vào `AppConfig` model + persist file
- [x] Thêm card "Target Database" vào `SettingsView.xaml` (giống card Config DB hiện tại)
- [x] Thêm properties tương ứng + `TestTargetConnectionCommand` vào `SettingsViewModel`
- [x] Thêm `TargetConnectionString` property (computed) vào `IAppConfigService`

**Bước 2 — Schema Inspector Service** ✅ Done (2026-03-21)
- [x] Tạo interface `ISchemaInspectorService` — `GetColumnsAsync` + `GetTableNamesAsync`
- [x] Tạo `ColumnSchemaDto` — đầy đủ: ColumnName, DataType, NetType, IsNullable, IsIdentity, IsPrimaryKey, OrdinalPosition, MaxLength, Precision, Scale + computed ShouldSkip, DisplayLabel
- [x] Implement `SchemaInspectorService` — query `INFORMATION_SCHEMA.COLUMNS + KEY_COLUMN_USAGE + COLUMNPROPERTY(IsIdentity)`
- [x] Tạo `DataTypeMapper` (Core/Helpers) — map 25 SQL types → NetType + EditorType
- [x] Register DI trong `App.xaml.cs`

**Bước 3 — Auto-generate Fields trong FormEditorView** ✅ Done (2026-03-21)
- [x] Thêm `AutoGenerateColumnItem` (BindableBase), `SectionOptionItem` (record)
- [x] Tạo `AutoGenerateFieldsDialogViewModel` (IDialogAware) — inject ISchemaInspectorService + IAppConfigService, load columns async, SelectAll/DeselectAll, trả OK + selectedColumns + targetSectionCode
- [x] Tạo `AutoGenerateFieldsDialog.xaml` — header stats, column list CheckBox, EditorType badge, section ComboBox, footer buttons
- [x] `FormEditorViewModel` — inject ISchemaInspectorService + IDialogService, thêm `AutoGenerateFieldsCommand`, `ExecuteAutoGenerateFieldsAsync` (mở dialog → add FormTreeNode → IsDirty)
- [x] `FormEditorView.xaml` — thêm button "✨ Tạo Fields tự động" trong toolbar
- [x] `FormsModule.cs` — register `AutoGenerateFieldsDialog`
- [x] `ViewNames.cs` — thêm constant `AutoGenerateFieldsDialog`
- [x] Build verify — 0 errors, 0 warnings

**Bước 4 — Schema Sync / Fallback khi DB thay đổi** ✅ Done (2026-03-21)
- [x] Tạo `SchemaDiffResult` + `TypeMismatchItem` record + `OrphanedFieldItem` (BindableBase)

**Bước 5 — Section Properties Panel: TitleKey + Sys_Resource inline** ✅ Done (2026-03-21)
- [x] Thêm `TitleKey`, `ResourceVi`, `ResourceEn` vào `FormTreeNode` — lưu i18n data trực tiếp trên node
- [x] Tạo `SectionUpsertRequest` record (Core.Data) — upsert Ui_Section + detect TitleKey rename
- [x] Thêm `UpsertSectionAsync` vào `IFormDetailDataService` + implement trong `FormDetailDataService` — transaction rename Resource_Key + INSERT/UPDATE Ui_Section
- [x] `FormEditorViewModel` — inject `II18nDataService`, thêm `SectionTitleKeyPreview` (computed realtime), `SectionCodeError`, `ValidateSectionCode` ([a-z0-9_]), `LoadSectionResourcesAsync`, `ExecuteSaveSectionAsync`, `SaveSectionCommand`, `CancelSectionCommand`
- [x] `FormEditorView.xaml` — redesign Section Properties: Section Code + validation error, Title Key readonly auto-gen, Thứ tự + Is Active row, TÊN HIỂN THỊ card (ResourceVi + ResourceEn), nút Lưu Section + Hủy
- [x] Build verify — 0 errors, 0 warnings
- [x] Tạo `SyncSchemaDialogViewModel` (IDialogAware) — 3 tab, SelectAll/DeselectAll, ApplyCommand trả columnsToAdd + fieldsToRemove + targetSectionCode
- [x] Tạo `SyncSchemaDialog.xaml` — 3 tab: "Có thể thêm" / "⚠ Cảnh báo" / "Type Mismatch" + section picker + footer
- [x] `FormEditorViewModel` — `CheckSchemaDiffAsync` (auto-run sau LoadFromDatabase, 15s timeout, fail-silent), `SyncSchemaCommand`, `SchemaSyncBadgeCount`, `HasSchemaSyncIssues`
- [x] `FormEditorView.xaml` — nút "🔄 Đồng bộ Schema" + badge cam trong toolbar
- [x] `ViewNames.cs` + `FormsModule.cs` — register `SyncSchemaDialog`
- [x] Build verify — 0 errors, 0 warnings

### Phase 9 — Bug fixes + Refactor Val_Rule (2026-03-22)

**Bugs đã fix:**
- [x] `SectionTitleKeyPreview` dùng `TableCode` thay `FormCode` (lowercase)
- [x] `ExistsFormCodeAsync` báo trùng sai khi edit — thêm `excludeFormId` loại chính nó
- [x] Auto-generate fields: FK violation `ColumnId=0` → thêm `EnsureColumnExistsAsync`
- [x] Auto-generate fields: section chưa persist trước khi insert fields
- [x] Auto-generate fields: `DisplayName` hiển thị tên cột thô → PascalCase split
- [x] Field summary panel cho sửa nhầm → set `IsReadOnly=True` + `Mode=OneWay`
- [x] Field `DisplayName` trong TreeView load từ `Sys_Resource` (ngôn ngữ vi)
- [x] Nút back từ `FieldConfig` restore đúng field đang chọn trên `FormEditor`
- [x] Bug "MaNhanVien → SoLuong": `catch` nuốt lỗi → fallback mock sai — tách catch, show error banner

**i18n Manager nâng cấp:**
- [x] Thêm nút Back (GoBackCommand qua IRegionNavigationJournal)
- [x] Thêm bộ lọc Table/Form động
- [x] Cho sửa inline trên lưới (`AllowEditing=True`, `CellValueChanged`)
- [x] Auto-save khi commit cell qua `SaveCellCommand`

**Validation Rules Editor redesign:**
- [x] Breadcrumb `← Cấu hình Field › Section › FieldCode`
- [x] Grid: badge style RuleType, color Severity, ẩn Expression khi Required
- [x] Auto-generate `ErrorKey` theo pattern `{table}.val.{column}.{ruletype}` (readonly)
- [x] Auto-init `Sys_Resource` vi/en khi save rule (IF NOT EXISTS)
- [x] Xác nhận trước khi xóa rule — default No

**Refactor Val_Rule — bỏ bảng junction Val_Rule_Field:**
- [x] Tạo `docs/migrations/003_remove_val_rule_field.sql` — migration trong transaction
- [x] `Val_Rule` thêm `Field_Id` (FK→Ui_Field), `Severity`, `Order_No` trực tiếp
- [x] Cập nhật `RuleDataService` — query thẳng Val_Rule, không JOIN junction
- [x] Cập nhật `RuleRepository` (backend) — bỏ JOIN Val_Rule_Field
- [x] Fix tất cả 5 chỗ còn reference `Val_Rule_Field` trong SQL: `FormDetailDataService`, `PublishCheckService`
- [x] Thêm `InitResourceIfMissingAsync` vào `II18nDataService` + implement
- [x] Build verify — 0 errors, 0 warnings (backend + frontend)

> ⚠️ **Chưa thực hiện**: Chạy `003_remove_val_rule_field.sql` trên DB thật

---

## ✅ Đã xong (Done)

- [x] Setup git repo
- [x] Tạo AI agent configuration files (CLAUDE.md, AGENTS.md)
- [x] Tạo solution structure (4 projects: Domain, Application, Infrastructure, Api)
- [x] Cài NuGet packages (Dapper, MediatR, FluentValidation, Serilog, Redis, JWT, Scalar)
- [x] Sửa ICare247.Api.csproj — thêm JWT Bearer, Serilog, Scalar
- [x] Viết lại Program.cs — Serilog bootstrap + AddApplication/AddInfrastructure + middleware pipeline
- [x] Tạo Application/DependencyInjection.cs — MediatR + FluentValidation auto-scan
- [x] Tạo Infrastructure/DependencyInjection.cs — skeleton với TODO placeholders
- [x] Tạo docs/spec/ (00 → 08) — 9 spec files đầy đủ
- [x] ConfigStudio Screen 04 — FieldConfig: Models (5 DTO), ViewModel, View (4 tabs), đăng ký navigation
- [x] ConfigStudio Screen 07 — ExpressionBuilderDialog: 5 models, 4 AST services, ViewModel IDialogAware, 3-column XAML
- [x] ConfigStudio Screen 08 — DependencyViewer: 2 models, ViewModel (graph + auto-layout), Canvas-based XAML
- [x] ConfigStudio Screen 11 — PublishChecklist: ChecklistItem model, ViewModel (async checks), XAML checklist
- [x] **Toàn bộ 11 screens ConfigStudio.WPF.UI đã có skeleton** — sẵn sàng P0 UX features
- [x] **Toàn bộ 6 placeholder screens đã có UI thật** — DataGrid, TreeView, mock data, filter, CRUD, navigation

---

## 📝 Decisions Log

| Ngày       | Quyết định                                                                 | Lý do                                                     |
| ---------- | -------------------------------------------------------------------------- | --------------------------------------------------------- |
| 2026-06-06 | Master Data CRUD: tên bảng đọc từ `Ui_Form→Sys_Table` server-side, không nhận từ client. Mọi identifier validate SafeIdentifierRegex, mọi giá trị qua Dapper param | Security: tránh SQL injection qua tên bảng/cột |
| 2026-06-06 | Soft-check FK theo quy ước tên: PK `CongTyID` → cột tham chiếu `CongTyID` hoặc `%_CongTyID`. Quét Sys_Column KHÔNG lọc Is_Active để bắt dữ liệu cũ | DB không có FK vật lý, quy ước tên là duy nhất để detect dependency |
| 2026-06-06 | PK resolve: ưu tiên `Sys_Column.Is_PK=1`, fallback INFORMATION_SCHEMA.TABLE_CONSTRAINTS khi metadata chưa set (DB thật toàn Is_PK=False) | Metadata không đáng tin → phải có fallback vật lý |
| 2026-06-06 | `Display_Mode` là cột mới trên Ui_Form (không repurpose `Layout_Engine`). WPF cấu hình, engine đọc để quyết định Popup vs Tab. `Layout_Engine` giữ nguyên (deprecated nhưng không xóa) | Tách biệt concern, backward compat |
| 2026-06-06 | `Show_In_List` cờ trên Ui_Field; fallback: nếu 0 field bật → hiện tất cả (áp dụng cả FE + BE) | Tránh lưới rỗng khi cấu hình chưa setup |
| 2026-03-03 | Api.csproj giữ reference đến Infrastructure                                | Program.cs cần gọi AddInfrastructure() — chấp nhận exception này cho composition root |
| 2026-03-03 | Dùng Scalar thay vì Swagger UI                                             | Scalar hiện đại hơn, tích hợp tốt với .NET 9 OpenAPI     |
| 2026-03-03 | Docs spec đặt trong docs/spec/ (không phải docs/ trực tiếp)               | docs/ đã có files AI config, tách biệt để rõ ràng hơn    |
| 2026-03-03 | Forms module thêm PackageReference MaterialDesignThemes                    | Module dùng MaterialDesign XAML cần reference trực tiếp để XAML compiler nhận attached properties |
| 2026-03-03 | Prism 9 dùng `Prism.Navigation.Regions` thay `Prism.Regions`              | Breaking change trong Prism 9.x — áp dụng cho tất cả module cần IRegionManager |
| 2026-03-03 | Screen 08 (DependencyViewer) đặt trong Grammar module                     | Gần với AST services, tránh tạo thêm module mới cho 1 screen                   |
| 2026-03-03 | Screen 11 (PublishChecklist) đặt trong Forms module                        | Launch từ FormEditor, là phần cuối của form lifecycle                           |
| 2026-03-04 | Prism 9 dùng `Prism.Dialogs` thay `Prism.Services.Dialogs`                | Breaking change trong Prism 9.x — áp dụng cho Rules, Events modules            |
| 2026-03-04 | Rules, Events, I18n modules thêm PackageReference MaterialDesignThemes     | Giống Forms module — cần reference trực tiếp cho XAML attached properties       |
| 2026-03-04 | InverseBoolToVisConverter đặt trong Forms module (không phải Core)          | Core project dùng net9.0 (không net9.0-windows) — không có WPF types           |
| 2026-03-18 | Form delete = soft delete (Is_Active=0), không xóa vật lý                  | Child records (Section, Field, Event) phải giữ lại cho audit/rollback           |
| 2026-03-18 | Form_Code là natural key trong toàn bộ API — không dùng Form_Id             | API contract dùng Form_Code, cache key cũng dùng Form_Code                     |
| 2026-03-18 | Chưa có cột Status (Draft/Published) trong Ui_Form schema                   | Nếu cần workflow phức tạp hơn, phải thêm cột Status vào schema trước           |
| 2026-03-21 | TitleKey = `{form_code_lower}.section.{section_code_lower}` — ghép auto    | Convention rõ ràng, không hardcode, dễ trace; user chỉ nhập phần section_code  |
| 2026-03-21 | Section Code enforce lowercase [a-z0-9_] tại ViewModel layer                | Tránh TitleKey bị mixed-case; enforce ngay khi gõ không cần converter XAML     |
| 2026-03-21 | UpsertSectionAsync dùng transaction: rename Resource_Key trước, sau đó UPDATE Section | Đảm bảo atomic — nếu rename fail thì Section không update, tránh dangling key  |
| 2026-03-22 | Bỏ bảng junction `Val_Rule_Field` — Field_Id gộp trực tiếp vào `Val_Rule`   | ErrorKey pattern `{table}.val.{column}.{type}` đã unique per field → quan hệ thực tế là 1-N, không cần N-N |
| 2026-03-22 | ErrorKey pattern: `{table}.val.{column}.{ruletype}` — auto-generate, readonly | Enforces naming convention, không để người dùng nhập tùy ý, tránh trùng lặp   |
| 2026-03-22 | `InitResourceIfMissingAsync` — IF NOT EXISTS INSERT cho Sys_Resource          | Khi save rule tự động tạo bản dịch mặc định nhưng không overwrite bản dịch người dùng đã nhập |
| 2026-03-22 | Mọi thao tác xóa dữ liệu DB phải confirm dialog, default = No                | Xóa không thể hoàn tác — user cần cơ hội từ chối nếu nhấn nhầm               |
| 2026-03-22 | `LoadFromDatabaseAsync` tách catch: lỗi bước chính → error banner, KHÔNG fallback mock | Fallback mock với data sai (SoLuong) gây hiểu nhầm nghiêm trọng — lỗi phải hiện rõ |
| 2026-03-25 | Multi-tab form dùng bảng Ui_Tab riêng (Option A), 0-1 tab = render phẳng như cũ | Rõ ràng, backward compat, không breaking change với form cũ |
| 2026-03-25 | Col_Span là column riêng trên Ui_Field (tinyint 1-3), không để trong Control_Props_Json | FormRunner cần đọc trực tiếp để build CSS grid — layout/structure phải là column riêng |
| 2026-03-25 | Ui_Field.Lookup_Source phân biệt 'static' (Sys_Lookup) / 'dynamic' (Ui_Field_Lookup) / NULL | Type-safe, integrity rõ ràng, không phụ thuộc vào JSON string parsing |
| 2026-03-25 | Ui_Field_Lookup bảng riêng 1-1, Popup_Columns_Json dùng JSON array | Load tách biệt chỉ khi cần, popup columns ít thay đổi + không cần query riêng từng cột |
| 2026-03-25 | Sys_Lookup.Tenant_Id đổi DEFAULT 0 → NULL = global | Nhất quán với toàn hệ thống (Sys_Table, Sys_Config, Sys_Role đều dùng NULL = global) |
| 2026-03-25 | FK lookup config deprecated JSON → Ui_Field_Lookup table (Option B) | JSON cũ không có integrity; Ui_Field_Lookup có FK constraint, query trực tiếp, transaction-safe |
| 2026-03-25 | SaveFieldAsync dùng transaction: UPSERT Ui_Field + Ui_Field_Lookup atomically | Nếu save field thành công nhưng lookup config fail → data inconsistent; transaction bắt buộc |
| 2026-03-25 | FieldRenderer `case "select"`: fallback text input khi Options.Count == 0 | Options chưa load hoặc LookupCode không tồn tại — graceful degradation, không crash |
| 2026-03-25 | `fklookup` tách biệt với `select` trong NormalizeFieldType | FK dynamic lookup cần UI khác (popup search), chưa implement; placeholder text input tránh nhầm lẫn với static select |
