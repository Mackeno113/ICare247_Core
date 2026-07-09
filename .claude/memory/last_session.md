# Last Session Summary

> Cập nhật: 2026-07-09 (session 80 — Doc Template GĐ4 + Import đổi sang DevExpress; ĐÃ commit+push). Lịch sử → [session_history.md](session_history.md).
> Việc đang mở đầy đủ → [../../TASKS.md](../../TASKS.md).

## Session 80 (2026-07-09) — Doc Template GĐ4 (gắn mẫu vào lưới) + Import đổi ClosedXML→DevExpress

**6 commit trên `master`, build backend + web 0 error, ĐÃ push:**

**1) Doc Template GĐ4 — gắn mẫu vào màn lưới (commit `1c2dad2`).** Phát hiện `Ui_View_Action` (Spec 14) đã sẵn cơ chế (`Engine=Server`+`Target`). Grid-first, KHÔNG thêm bảng. Gắn = 1 dòng action `Type=Export|Print`, `Engine=Server`, **`Target=Doc_Template.Ma`**; bấm → dòng chọn làm `keyParams` → `Doc_Template_Param(Nguon='key')` → @proc (chuỗi backend sẵn từ GĐ1).
- BE `GetTemplateIdByCodeAsync`+`RenderByCodeAsync`+`POST by-code/{code}/render`; Web `DocTemplateApiService`+`DataView.TryServerRenderAsync`+`ViewPage.OnServerRenderAsync`; ConfigStudio combo "Bộ mẫu (Xuất tài liệu)" tab Actions; guide `cau-hinh-xuat-tai-lieu.md`+Spec 28 §7.4.

**2) Import: ClosedXML → DevExpress Spreadsheet (commit `c48bbc5`).** User chốt đồng nhất 1 thư viện Office, cô lập như in biểu mẫu; chấp nhận watermark trial+license (đảo điểm 1 ADR-034, có addendum). Seam `ISpreadsheetReader`+`SheetGrid` (Application) → impl DevExpress ở `Infrastructure.Documents`; `ImportEngine` KHÔNG đụng DevExpress. Gỡ hẳn ClosedXML. API verify reflection (0-based). Warning build = license trial DX1000/DX1001.

**3) Fix theo test của user + dọn nợ:**
- Web Doc GĐ4: `293182f` (DataView sao dòng chọn sang Dictionary — ExpandoObject không implement IReadOnlyDictionary).
- Import template: `ac1bd27` (ghi chú trống — DevExpress `Comments.Add(range,string)` là AUTHOR → dùng 3 tham số `Add(range,author,text)`); `4257491` (dropdown FK hiện **"Mã — Tên"**, import cắt lấy Mã qua `ImportConventions.ExtractFkCode` — có nối thì cắt, không thì cả ô là Mã).
- Dọn nợ RCL session 79: `417a8fe` chuyển `LookupAddDialog` (+CSS scoped) từ host vào RCL `DynamicForms` (hết RZ10012, nút "➕ Thêm mới" hết hỏng); host dựng `FieldState` sau `ILookupQueryService.GetAddFormAsync`.

**⚠️ Còn (E2E — chưa chạy):** Doc (db/077 + đăng ký proc/param SQL + soạn mẫu → xuất từ lưới); Import (validate/commit .xlsx thật, kiểm dropdown "Mã — Tên"); LookupBox có `AddFormCode` (xác nhận dialog bung). Dọn header Spec 25 (còn "ClosedXML"). Pha sau Doc: `Ui_Form_Action`, Scope='Row', in hàng loạt. 4 file i18n pre-existing vẫn để nguyên.

## Session 79 (2026-07-09) — Doc Template (xuất Word/PDF theo mẫu) + tách RCL control động

**Bối cảnh:** user hỏi–chốt nhiều vòng về DevExpress Office File API (đã cài 25.2.4 trên máy; license per-seat/royalty-free/trial-watermark; deploy IIS) → dùng cho xuất hợp đồng Word/PDF. Song song tách RCL control động.

**Đã làm (7 commit trên `master`, build 3 solution 0 error, ĐÃ push — xác nhận ở session 80):**
- **Tách RCL** (`fb26e33`): `ICare247.UI.DynamicForms` (FieldRenderer + 11 renderer + FieldState/models); interface hóa lookup/attachment service; **xóa hẳn `ICare247.Blazor.RuntimeCheck`** (mồ côi).
- **Doc Template BE GĐ1** (`0542e5d`,`79d2818`,`4609752`): spec 28 + migration `db/077` (4 bảng, CHƯA chạy); `ICare247.Infrastructure.Documents` (DevExpress DUY NHẤT backend) — engine ghép-fragment master(dọc)+detail(ngang) + PDF, proc-runner whitelist, `IDocTemplateRenderer`, API describe/render. **PoC runtime xuất PDF 2 trang OK**.
- **Doc Template WPF GĐ3** (`4b6d3ea`,`43cc0b4`): module `Modules.DocTemplate` (RichEditControl + panel biến + chèn MERGEFIELD + hướng giấy) + `IDocTemplateDataService` (CRUD bộ mẫu/mảnh + nạp/lưu fragment); menu "Mẫu tài liệu". WPF chỉ verify compile.
- **Dọn solution**: `.slnx` backend/frontend/ConfigStudio bỏ RuntimeCheck + thêm 3 project mới.

**⚠️ Để thấy kết quả:** chạy `db/077` + đăng ký `Doc_Proc_Registry` + soạn stored proc → E2E; mở ConfigStudio để test màn soạn WPF; mua license Universal khi prod (bỏ watermark).

**Việc gợi ý tiếp:** tùy chọn UI param/registry + GĐ2 soạn Web (Blazor DxRichEdit). (Ghi chú cũ "các session 72/76/77/78 CHƯA commit" đã lỗi thời — working tree sạch, chỉ còn 4 file i18n pre-existing.)

## Session 77 (2026-07-07) — Hệ đính kèm / Upload file tổng quát

**Bối cảnh:** user yêu cầu tính năng upload file (tối ưu ảnh + UX file lớn + bảo mật + lưu trữ linh hoạt). Hỏi–chốt nhiều vòng (storage hybrid, di dời gốc, đa-node, thư viện ảnh, dedup, 2 chế độ đính kèm).

**Đã làm (CHƯA commit — branch `master`; build BE + web + WPF module = 0 error):**
- **Backend P1–P5**: `IFileStore` 3 provider (Db/FileSystem/Object) + selector + key-builder tương đối (di dời gốc = đổi BaseRoot) + startup fail-fast; `FileValidator` (allowlist+magic-byte+chặn mã thực thi); streaming spool; `SkiaImageOptimizer` (MIT); dedup MERGE HOLDLOCK + RefCount. Migration `db/070` + file gộp `db/dev/create_tt_attachment_full.sql`. Endpoint `AttachmentsController` (upload/get/thumbnail/info/list/link/delete).
- **Frontend P6**: `AttachmentRenderer` 2 chế độ auto theo `IsVirtual` (ảo=đa-tệp bảng phụ; cột=1-tệp Logo_Id) + JS uploader (XHR progress + nén canvas + Bearer) + `AttachmentApiService`. Đa-tệp-khi-thêm-mới = upload treo → link sau Lưu.
- **Tích hợp**: dispatch `case "attachment"` + `NormalizeFieldType` ở CẢ `FormRunner` VÀ `MasterDataForm` (bug quên MasterDataForm); host bơm `__ownerTable/__ownerId`; WPF thêm EditorType `AttachmentBox` + guide.
- **Docs**: `docs/spec/26_FILE_UPLOAD_SPEC.md` + `docs/huong-dan-wpf/cau-hinh-attachment.md`.
- **Spec-only**: `docs/spec/27_SYSTEM_SETTINGS_SPEC.md` (quản lý thông số hệ thống — schema-driven, hybrid file+DB, Blazor web admin; CHƯA code, còn 5 điểm chốt §11).

**⚠️ Deploy để thấy kết quả:** chạy migration (`db/dev/create_tt_attachment_full.sql`) trên Data DB tenant → rebuild+restart API + rebuild web + hard reload + rebuild WPF. Cấu hình `FileStorage` (mặc định Db chạy ngay). Kiểm thử E2E trình duyệt CHƯA chạy.

**Việc gợi ý tiếp:** commit session 77 (chờ user chốt message). Code P1 tính năng quản lý thông số (spec 27) sau khi chốt §11. Job dọn tệp mồ côi hoãn (spawn task `task_56b62113`). Loạt session 72/76 (FK auto-JOIN, UI/UX) vẫn CHƯA commit — cùng nhánh `master`.

## Session 76 (2026-07-06) — UI/UX loạt màn (WPF ConfigStudio + web Blazor + backend)

**Bối cảnh:** user rà soát trực quan từng màn, yêu cầu vá lần lượt (đối chiếu ảnh chụp). Loạt cải tiến cross-cutting.

**Đã làm (CHƯA commit — branch `master`):**
- **ConfigStudio WPF — Field Navigator (`FieldConfigView`)**: (1) bulk multi-select (checkbox) + context-menu "Chuyển N field sang Section/Tab khác" — COPY pattern từ FormEditor, KHÔNG refactor shared. (2) Hiển thị TÊN section + field (resolve i18n 2-pass qua INotifyPropertyChanged, fallback mã). Files: `Models/FieldNavGroup.cs` (`FieldMoveTargetItem` mới, `IsMultiChecked`, `SectionName/DisplayTitle`, `LabelKey/DisplayName/Title`), `FieldConfigViewModel.cs`, `FieldConfigView.xaml(.cs)`.
- **Web `MasterDataForm`**: chia cụm theo section (`.section-card`, mirror FormRunner) + `.form-body` gap; modal ghim header/footer (`.dm-modal` flex-column, `.dm-body` cuộn, footer sticky), **toast lỗi sticky-top+phải** + **auto-focus field lỗi đầu tiên** (`icare.focusField`). Backend `FormRepository.cs` resolve `SectionName` từ `Ui_Section.Title_Key` (trước hardcode `''`).
- **Web `DataView` TreeList (parity grid)**: cột Sửa/Xóa (luật xóa CHỈ node lá/cha-không-con qua `__parentKey`) + double-click sửa + toolbar dùng chung + CSS header; **#1 cây lồng**: backend `ViewRepository.cs` emit `b.[ParentField] AS [__parentKey]` (id cha thô) cho TreeList → frontend `ParentKeyFieldName="__parentKey"`; **#2 filter** + **#3 STT** + **#4 lưu layout** (`LayoutAuto*`+`TreeListPersistentLayout`).
- **API DevExpress xác minh bằng reflection** `DevExpress.Blazor.v25.2.dll` 25.2.3 (không đoán): RowDoubleClick, VisibleIndex, LayoutAuto*, FilterRowEditorVisible, TreeListColumnFixedPosition.Right.
- **Memory:** `project_current_phase.md` — ghi "Phạm vi tạm thời": BỎ QUA `ICare247.Blazor.RuntimeCheck` cho đến khi user nhắc lại.
- Build 3 solution (backend / ICare247_UI / ConfigStudio WPF): **0 error**.

**⚠️ Deploy để thấy kết quả:** backend đổi (`FormRepository`+`ViewRepository`) → **rebuild + restart API + Xóa cache** (tên section web + cây lồng). Rebuild web + WPF. Kiểm thử trực quan chưa chạy.

**Việc gợi ý tiếp:** commit loạt session 76 (backend+web+WPF+memory) — chờ user chốt message + push. FK auto-JOIN (session 72) vẫn TẠM DỪNG (cùng file `ViewRepository.cs`). 3 file i18n pre-existing chưa commit.

## Session 75 (2026-07-06) — FormEditor: chuyển bulk field sang Section/Tab khác

**Bối cảnh:** user bulk-select nhiều field (checkbox trong cây) nhưng KHÔNG có cách chuyển cả nhóm sang "selection"
(section/tab) khác — thanh ⚡ `ApplyBulk` chỉ đổi thuộc tính. Thao tác chuyển-section duy nhất trước đó là nút ↑↓
nhảy-1-bậc-liền-kề, chỉ áp cho 1 field đang chọn.

**Đã làm:**
- **Context-menu chuột phải trên TreeView cấu trúc** → "Chuyển N field đã chọn sang…" → submenu section đích.
  Form có Tab → header `{Tab} ▸ {Section}`. **Field gắn qua `Ui_Field.Section_Id`; `Tab_Id` nằm trên `Ui_Section`** →
  "chuyển sang Tab" = chọn section thuộc tab đó, KHÔNG đổi schema.
- Tái dùng `MoveFieldToSectionAsync` + `PersistSectionOrderAsync` (như nhánh cross-section của `ExecuteMoveAsync`);
  reindex + lưu Order_No cả section nguồn & đích; field đã ở đích → bỏ qua; không di chuyển thật → không set dirty.
- **Files:** `Models/MoveTargetItem.cs` (mới), `FormEditorViewModel.cs` (`MoveTargets`/`CanMoveBulk`/`BulkMoveHeader`/
  `MoveBulkToSectionCommand`/`RefreshMoveTargets`/`ExecuteMoveBulkToSectionAsync`), `FormEditorView.xaml`
  (`TreeView.ContextMenu`, item mang sẵn `MoveCommand` — tránh RelativeSource xuyên Popup submenu),
  `FormEditorView.xaml.cs` (`OnTreeContextMenuOpening` → refresh danh sách đích). Build ConfigStudio 0/0.

**Việc gợi ý tiếp:** 3 file i18n pre-existing + `run-all.bat` (M) vẫn chưa commit — xử hoặc bỏ. Kiểm thử trực quan
bulk-move trên app WPF (chưa chạy). FK Pha 2/3 (import Mã→Id) còn chặn (session 74).

## Session 74 (2026-07-05) — Error surface toàn cục ConfigStudio + hết nuốt lỗi nạp FK

**Bối cảnh:** field LookupBox (VD `TinhThanhPho_Id`) badge "đã cấu hình" nhưng panel Control Props TRỐNG.
Gốc: ConfigStudio build mới SELECT cột `Tree_Selectable_Level` (db/069) + `Reload_Trigger_Fields` (db/068);
khi migration CHƯA áp → `SqlException: Invalid column name` trong `GetFieldLookupConfigAsync` → bị `catch {}`
nuốt IM LẶNG (không log, không hiện) → panel trống. Data KHÔNG mất khi chỉ mở; NHƯNG nếu bấm Lưu lúc trống →
ghi đè `Ui_Field_Lookup` bằng rỗng → mất thật. User đã chạy db/067/068/069 + db/062 trên Config DB.

**Đã làm (commit `c7527ed`, đã push master):**
- **Error surface toàn cục (chốt scope 1+3):** `IUserNotifier`+`UserNotifier` (singleton, marshal UI thread,
  `NotificationSeverity`) → `ShellViewModel` banner màu-theo-mức-độ + auto-ẩn (lỗi 15s) + nút đóng; `MainWindow.xaml`
  banner trên status bar (message + chi tiết kỹ thuật); `App.xaml.cs` DI + `DispatcherUnhandledException` cũng hiện banner.
- **Màn Field:** 2 catch nạp FK/ComboBox → `HandleFkConfigLoadError` = log file + banner shell + banner đỏ trên màn
  (kèm "Invalid column name" + cách khắc phục). Cờ `_fkConfigLoadFailed` → **KHÓA nút Lưu** khi nạp FK lỗi (chống save-đè rỗng).
- Build ConfigStudio 0/0. `detect-changes`: medium, đúng phạm vi.

**⚠️ Cần xác nhận (user tự làm):** restart ConfigStudio → mở lại `TinhThanhPho_Id`: hiện lại = data còn; vẫn trống +
KHÔNG banner đỏ = đã bị save-đè trước đó → cấu hình lại 1 lần.

**Việc gợi ý tiếp:** 3 file i18n pre-existing + `run-all.bat` (M) vẫn chưa commit — xử hoặc bỏ. FK Pha 2/3 (import Mã→Id)
còn chặn: set `Ui_Field_Lookup.Code_Field='Ma'` (Field 34) + Q3 chọn thư viện Excel. F1 config sync: db/062 đã áp → còn E2E.

## Session 73 (2026-07-05) — Cascade lookup, Multi-Trigger, Cache lookup

**Đã làm (đã commit + push master):**
- **Fix gốc cascade field ảo** — `MasterDataForm` (ICare247_UI) bỏ `.Where(!IsVirtual)` → field ảo cha (Tỉnh/Ngân hàng) render + vào context; payload Lưu loại field ảo. Đây là gốc lỗi 500 `Must declare @TinhThanhPho_Id` (không phải cache/DB — đã điều tra hết chuỗi). Commit `c2ffcff`.
- **Reload cascade đa-@param** — `LookupBoxRenderer` (ICare247_UI) tự dò mọi `@param` trong Filter SQL → reload khi bất kỳ cha nào đổi. Ẩn `reloadOnChange` cũ, ô "Tự reload" đơn → Nâng cao, bỏ cảnh báo P3. Commit `47e3b2d` + `a2d6bdf` (spec 12).
- **Multi-Trigger** (`Reload_Trigger_Fields`, `db/068`) — danh sách field cha khai tay, hợp với @param. Commit `b9e30d5`.
- **TreeLookupBox: Tree_Selectable_Level** (all/leaf/branch, `db/069`) — chặn chọn node sai cấp. Commit `3ffdc03`.
- **Cache dữ liệu lookup** (cache-aside, thay lazy-load) — `DynamicLookupRepository` dùng HybridCacheService; key gắn version theo (tenant, bảng nguồn) + hash @param; `ILookupCacheVersion` bump khi `SaveMasterData` (invalidation B). Tôn trọng `Cache:Enabled`. Commit `da7ff83`.
- **Badge trạng thái field** + cờ `Ui_Field.Is_Configured` (`db/067`) + reset field mới + STT chèn-sau + thu gọn diễn giải (ConfigStudio). Commit `47e3b2d`.
- **Gộp branch** `feat/lookupbox-cascade-field-config` + merge backlog OPT-1..8 (nhánh 2) lên master; xóa 2 nhánh local.

**⚠️ Triển khai/kiểm thử:**
- Chạy `db/067, 068, 069` trên `ICare247_Config`. Rebuild + restart API + ICare247_UI + ConfigStudio.
- App dev chạy `Cache:Enabled=false` → cache TẮT (luôn đọc DB) — muốn thấy cache phải `Cache:Enabled=true`.
- Chỉ nâng ICare247_UI (app đang chạy); RuntimeCheck (harness) chưa đụng.

**Việc gợi ý tiếp:**
1. TreePicker **lazy-load** (Load_Mode + Root_Filter) — hoãn (cache đã thay); chỉ làm nếu cây cực lớn.
2. Xử 3 file i18n pre-existing (chưa commit) + cân nhắc xóa nhánh remote `origin/claude/dynamic-tree-control-bLerc` (đã port chọn lọc).
3. Quay lại FK auto-JOIN (session 72) nếu còn dở.

---

## Trạng thái hiện tại

**Đã xong (đã commit):** MasterData — lỗi "chưa có khóa chính" rõ ràng + i18n (ADR-032) + fix `LockOnEdit`
rớt cho field lookup động (`FormRepository.cs:236`). Commit `053bf92` (session 71).

**Đang dở (CHƯA commit — branch `master`):** FK lookup auto-JOIN hiện TÊN cha cho cột lưới (engine + ConfigStudio).
Engine BE (`ViewRepository.ResolveFkJoinsAsync`) + WPF tab Cột (dropdown "FK lookup (cha)") **đã code + user test chạy đúng**;
`db/064-066` đã áp live tenant 1. Chi tiết + message commit đã soạn → mục "TẠM DỪNG" đầu `TASKS.md`.

**Next step:**
1. Build + restart API để LƯỚI WEB (Blazor) hiện tên (engine mới chưa deploy — đừng để engine cũ chạy với cấu hình mới).
2. Tạo branch (đang ở `master`) → `node .gitnexus/run.cjs detect-changes` → commit FK feature.
3. ⚠️ **KHÔNG commit `.claude/skills/gitnexus/`** (untracked) — license GitNexus (PolyForm Noncommercial) chưa rõ, đang chờ tác giả hồi âm.

**Chờ ngoài:** hồi âm license GitNexus (đã gửi email). Hướng đầu tư đúng chất ICare247 = **phương án B** (impact-engine nội bộ trên `Sys_Dependency`, clean-room) — xem session_history session 72.
