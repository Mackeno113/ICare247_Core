# Last Session Summary

> Cập nhật: 2026-07-18 (session 89 — sửa bug ④ mismatch query-mode "function"/"sql" vs canonical
> "tvf"/"custom_sql" trong ConfigStudio; session 88 xong REFACTOR B4.2+B5 + bộ 3 control TreeList/
> Lookup Feature A/B/C). Lịch sử → [session_history.md](session_history.md).
> **Trạng thái việc treo session 88:** ① migration `db/085→088` — **XONG** (user chạy) · ② smoke 3
> Feature — **XONG** · ③ smoke REFACTOR FieldConfigViewModel — **XONG** · ④ bug query-mode
> (`task_2b59b40a`) — **XONG** (session 89).
> **Task tiếp theo gợi ý:** màn Phòng ban (no-code qua ConfigStudio, dùng cả 3 control vừa xây,
> KHÔNG viết SQL tay).

## Session 89 (2026-07-18) — Fix bug ④ query-mode literal lệch canonical (ConfigStudio)

**Root cause:** ConfigStudio WPF so/gửi literal query-mode CŨ (`"function"`/`"sql"`) trong khi nguồn
sự thật duy nhất — DB CHECK constraint `Ui_Field_Lookup.Query_Mode` (db/008+088: `table|tvf|custom_sql|
self_parent`) + backend `DynamicLookupRepository.BuildSafeSql` + runtime DTO `FieldLookupConfigDto` —
đều dùng `tvf`/`custom_sql`. Load/Save trong VM truyền THẲNG giá trị DB (không map) ⇒ ConfigStudio là
nơi DUY NHẤT lệch. Verify bằng grep + đọc DB CHECK/backend, không đoán.

**Sửa 6 chỗ / 4 file:** `FkLookupConfigVm` (`IsFunctionMode`→`"tvf"`, `IsSqlMode`→`"custom_sql"`) ·
`LookupBoxPropsPanel.xaml` + `ComboBoxPropsPanel.xaml` (radio TVF CommandParameter `"function"`→`"tvf"`;
radio SQL đã đúng `custom_sql` từ trước) · `ControlPropsJsonService` + `FieldConfigExplainService`
(case `"function"`/`"sql"` → `"tvf"`/`"custom_sql"`, thêm nhánh `self_parent` diễn giải).

**Không cần dọn dữ liệu:** CHECK constraint đã chặn `"function"` từ đầu (Lưu TVF cũ luôn lỗi → 0 hàng
rác); SQL luôn lưu `custom_sql` (chỉ hỏng hiển thị khi mở lại). **User đã smoke — OK.** Chưa commit.

## Session 88 (2026-07-18) — Xong REFACTOR B4.2+B5 + đổi tên fnt_/fns_ + Bộ 3 control TreeList/Lookup

**Đầu phiên — fix migration đổi tên (commit `63e874e`):** user báo đã chạy db/082+083+084 bằng SSMS
với quy tắc mới: hàm bảng (TVF) tiền tố `fnt_`, hàm vô hướng tiền tố `fns_`. Đổi `fn_CongTyTheoQuyen`
→ `fnt_CongTyTheoQuyen` (db/084 thêm bước DROP tên cũ, db/083 tự vá dòng seed TPL_CONG_TY — cả 2
idempotent, an toàn chạy lại). Ghi quy tắc tiền tố vào `.claude-rules/database-design.md` §7b.
User re-run 083+084 xong trong phiên.

**REFACTOR FieldConfigViewModel — hoàn tất B4.2 (4 nhóm) + B5** (commit `ff69dec`/`7fbe336`/
`6397389`/`aeb98e3`): dời hết state/logic FK Lookup từ root sang `FkLookupConfigVm` theo công thức
đã chốt từ nhóm 1 (side-effect gọi ngược root, restore/reset qua method internal). Nhóm 2 kèm fix
bug nhóm 1 để lại (RadioGroup Sys_Lookup binding gãy). Nhóm 3 (khối lớn nhất — nguồn FK + 13 command,
diệt lambda `+=` → handler đặt tên) kèm xóa ~380 dòng XAML legacy Collapsed. Nhóm 4 (template P4 +
cascade + diễn giải) có coupling khác — `RecomputeCascadeWarnings` cần Navigator+FieldId nên
`GetSiblingFieldCodes` giữ ở root. B5 gộp 3 lệnh reset rời rạc thành 1 method `ResetForNewField()`.
**Kết quả: VM root 4.030 → 2.137 dòng (−47%)**, FkLookupConfigVm 1.172 dòng. Build 0W/0E mỗi bước.
**CHƯA smoke test** (đè lên cả 5 bước, cần restart ConfigStudio).

**Bộ 3 control TreeList/Lookup dùng chung** (Plan mode duyệt, xem
`C:\Users\Mackeno_01\.claude\plans\inherited-floating-shannon.md`) — ban đầu định làm thẳng màn
Phòng ban, user chốt xây control tái dùng TRƯỚC (không SQL tay):
- **Feature C** (`4c656a7`) — kéo-thả sắp xếp TreeList (ADR-027, nay đã code — xem TASKS.md bảng ADR):
  proc generic `sp_RecomputeTreeOrder` (db/085) + `Ui_View.Allow_Reorder` (db/086) + API reorder
  (chặn cycle qua CTE hậu duệ) + wiring `DxTreeList.AllowDragRows`/`ItemsDropped` (API xác nhận có
  thật qua reflection DLL, không đoán) + checkbox WPF.
- **Feature A** (`6d467fe`) — lọc TreeList/Grid theo công ty declarative: `Ui_View.Scope_By_Company`
  (db/087) + mở rộng `GetDataAsync` tự JOIN `fnt_CongTyTheoQuyen` + `@CongTyID_Active`; kèm fix bug
  `Validate_Sql` CongTyID_Active thiếu nhánh vai trò (dùng thẳng hàm đã có).
- **Feature B** (`44ed677`) — self-ref parent picker: `Query_Mode='self_parent'` (db/088), CTE loại
  chính nó + hậu duệ, `@__SelfId` tự bind qua cơ chế contextValues generic sẵn có (không cần code
  riêng); thêm ở CẢ `MasterDataForm` VÀ `FormRunner` (tránh lặp bug "quên 1 renderer" session 77).

**⚠️ Cả 3 Feature đụng `ViewRepository.GetByCodeAsync` (hub mọi màn View)** → GitNexus báo risk
CRITICAL cho Feature C/A — không phải bug, chỉ vì thêm cột SELECT vào hub dùng chung. **Bắt buộc
chạy `db/085`→`086`→`087`→`088` ĐÚNG THỨ TỰ trước khi restart API**, nếu không mọi màn `/view/...`
lỗi "Invalid column name".

**Phát hiện tình cờ, đã flag riêng KHÔNG sửa** (`task_2b59b40a`): `FkLookupConfigVm.IsFunctionMode`/
`IsSqlMode` so sánh chuỗi "function"/"sql" nhưng CommandParameter + DB CHECK constraint dùng
"tvf"/"custom_sql" — bug tiền tồn tại (không phải do session này tạo ra), field cấu hình TVF/SQL
không hiện đúng radio khi mở lại + Lưu có thể vi phạm CHECK constraint.

**Ghi nhận:** Codex (AI khác, quy trình `AI_HANDOFF.md`) làm song song `WEB-UX-04` (chuẩn hóa design
system Web) trong cùng working tree — không đụng vào, đã cẩn thận `git add -p` tách hunk khi TASKS.md
bị 2 bên cùng sửa.

**Build verify `/finish-task`:** backend `dotnet build ICare247.slnx` 0 `error CS` (fail còn lại
thuần file-lock do API đang chạy — coi như OK theo quy tắc dự án) · Web + ConfigStudio WPF 0W/0E ·
`dotnet test ICare247.Application.Tests` **145/145 pass**.

## Session 87 (tiếp) — REFACTOR FieldConfigViewModel (WPF 4.030 dòng) — ĐANG DỞ B4.2

**Kế hoạch 5 bước đã duyệt** (phân tích 8 nhóm trách nhiệm + code smell nằm trong chat; tóm tắt + trạng thái từng bước ở TASKS.md khối "REFACTOR FieldConfigViewModel"). Nguyên tắc: không đổi hành vi/XAML surface, mỗi bước 1 commit build 0W/0E.

**ĐÃ XONG (commit theo thứ tự):** B1 3 service thuần (`a3d63fa`/`3313e69`/`904b3f8` — ControlPropsJsonService/FieldI18nKeyService/FieldConfigExplainService) · B2 FieldNavigatorVm (`59e372f`/`3c47dee` — XAML → Navigator.*) · B3 FieldRulesEventsVm (`d329b09` — fix async void DeleteEvent, XAML → RulesEvents.*) · B4.1 FkLookupConfigVm FACADE strangler (`534f467` — 2 panel DataContext=FkLookup, ~65 member ủy quyền + bridge notify re-raise) · B4.2 nhóm 1 (`84a4a97` — state Cb* + Sys_Lookup tĩnh SỞ HỮU trong con, hook internal NotifyLookupPropChanged giữ guard rebuild). Root VM 4.030 → **2.597 dòng**.

**DỞ DANG — làm tiếp theo đúng công thức nhóm 1:** B4.2 nhóm 2 (EditBox/Tree/AddNew: EditBoxMode/CodeField/ImportGlobalCode/DropDownW-H/ReloadTriggerField/ParentColumn/TreeSelectableLevel/AllowAddNew/AddFormCode/AvailableFormCodes + LoadFormCodesAsync) → nhóm 3 (QueryMode + Fk* + 5 collection + 13 command + WireFkColumnHandlers → handler đặt tên diệt lambda +=; kèm xóa ~500 dòng XAML legacy Collapsed "FK Lookup Config" trong FieldConfigView) → nhóm 4 (template P4 + cascade + diễn giải) → B5 phân rã ResetFieldStateForNew theo VM con.
**Công thức nhóm:** dời backing field + prop vào FkLookupConfigVm (side-effect qua hook root), root truy cập FkLookup.X, restore/reset gói thành method internal của con, XAML KHÔNG đụng (đã DataContext=FkLookup từ B4.1). Cờ `_isRebuildingProps` vẫn ở root.

**Smoke chưa chạy** (user cần restart ConfigStudio bản mới): navigator (chuyển field/↑↓/bulk move), 2 tab Rules-Events (xóa có confirm), panel LookupBox đủ mục (3 radio mode, lưới popup, mẫu lookup P4, Diễn giải), panel ComboBox (search props + preview Sys_Lookup), sửa → Lưu → mở lại đối chiếu.

## Session 87 (tiếp) — Spec 31 Shared Pickers + PICKER-P2

**Spec 31** (`docs/spec/31_SHARED_PICKER_CONTROLS_SPEC.md`, commit `d3795b3`): control dữ liệu dùng chung
2 tầng — hợp đồng dữ liệu + tham số canonical (ThoiDiem/CongTyId/ParentId; màn map tên field riêng);
RCL IcControls (bespoke) + Ui_Lookup_Template/Param_Map (engine, P4). User chốt: song song 2 tầng,
spec trước, IcCompanyPicker 1 control 2 chế độ.

**PICKER-P2 đã code:** `Shared/Components/Pickers/` — IcPickerModels (IcPickerItem, IcPickerMode,
ICompanyPickerSource) + IcCompanyPicker.razor (Single dropdown cây + AllowAll; MultiCheck WYSIWYG
controlled-component, slot NodeExtra). MeCompanyApiService cài ICompanyPickerSource (DI forward).
Refactor 3 chỗ về 1: CompanySwitcher (chỉ còn nghiệp vụ localStorage/default/reload),
UserManagementPage tab Công ty (NodeExtra = radio Mặc định + badge Theo vaiTrò, page giữ
_companyById tra cờ), PermissionMatrixPage view Phạm vi công ty. Build UI 0W/0E.

**PICKER-P4 đã code (commit cùng ngày):** db/083 (Ui_Lookup_Template + 2 cột Ui_Field_Lookup + seed
3 mẫu) + db/084 (fn_CongTyTheoQuyen — vì DangerousKeywords chặn chuỗi con "IsDeleted" trong
custom_sql nên nguồn phải là view/TVF) · DynamicLookupRepository: LoadCfgAsync resolve template
(CASE template thắng, fallback legacy) + BuildParamsAsync (Param_Map: field/@token/hằng số; token
tự resolve @param thiếu qua IContextParamResolver; **cache key hash sau khi bind đủ** — chống share
cache sai user) · FormRepository merge Param_Map→Reload_Trigger_Fields · ConfigSync descriptor ·
WPF: LookupTemplateRecord + 3 method FieldDataService phòng thủ + FieldConfigViewModel
(SelectedLookupTemplate/ParamRows, hook load ~d1910/save ~d3500) + LookupBoxPropsPanel section mới.
Build BE + WPF 0W/0E. CHƯA chạy migration 083/084, chưa smoke. Hover-help topic cho section mới: nợ.

**PICKER-P3 đã code (commit cùng ngày):** BE PickersController `/api/v1/pickers/dia-ban`
(tỉnh/xã/resolve-id, IPickerRepository trên DM_TinhThanhPho+DM_PhuongXa schema db/037) ·
Shared IDiaBanPickerSource + IcAddressBlock (DiaChi+PhuongXaId là 2 giá trị lưu, tỉnh = bộ lọc
suy ra; xã search server-side debounce 300ms; đổi tỉnh xóa xã) · host PickerApiService cache L0 tỉnh.
Build BE+FE 0W/0E. **Runtime P2+P3 chưa smoke (server tắt)** — khi chạy lại: bấm switcher,
2 cây gán quyền, và màn Phòng ban sắp tới dùng IcCompanyPicker + IcAddressBlock đầu tiên.
> Việc đang mở đầy đủ → [../../TASKS.md](../../TASKS.md).
> **Task tiếp theo gợi ý:** FDOC-1 (migration Ui_Form_Detail + Formula_Json + ConfigStudio tab Lưới chi tiết — chờ user ra lệnh code) · TM-001 chốt 5 câu hỏi spec 29 §9 · nghiệm thu lưới WPF session 84.
>
> **FDOC-001 ĐÃ CHỐT (user, 2026-07-15):** EditMode = cả 3 chế độ per lưới (`Ui_Form_Detail.Edit_Mode`),
> mặc định **EntryPanel** — khu nhập trên + lưới dưới kiểu legacy (Lưu dòng → đẩy vào lưới; click dòng
> → nạp lên sửa; badge "Đang sửa dòng #n" + nút Thêm mới để thoát) · aggregate lên master ngay FDOC-3 ·
> vệ tinh 1-1 = section field, payload `satellites` riêng · có ca 100+ dòng → virtual scroll từ FDOC-2.

## Session 87 (2026-07-16) — Switcher công ty dạng cây + màn Người dùng + phân quyền cây công ty

**User đặt bài:** màn Phòng ban cần lọc công ty theo quyền → chốt qua hỏi-đáp nhiều vòng:
switcher tree (chọn node = scope đúng node, KHÔNG gộp nhánh, giữ @CongTyID_Active đơn) · màn Người
dùng bespoke · cây checkbox WYSIWYG (tick cha auto-tick nhánh trên UI, bỏ tick tự do, lưu đúng tập
tick, bỏ tick con KHÔNG rớt cha) · **"nhóm quyền" = HT_VaiTro mở rộng**, kế thừa ĐỘNG cả 2 trục
(user chốt Động sau khi thấy PermissionService join động — bỏ phương án copy).

**Đã làm (commit `e6bc0b8`):** db/082 HT_VaiTro_CongTy · BE MeCompanyRepository union + ParentId/CanAccess
· AdminUserController CRUD/roles/companies (PBKDF2, chặn tự xóa) · roles/{id}/companies · FE
CompanySwitcher cây + UserManagementPage 3 tab + PermissionMatrixPage view "Phạm vi công ty".
**Verify trên app thật** (user restart server): login admin → màn Người dùng OK, lưu gán công ty 204;
bắt + fix bug Dictionary key null khi dựng cây switcher. **CÒN LẠI:** user chạy db/082 bằng SSMS
(chưa chạy → Lưu ở "Phạm vi công ty" lỗi, đọc OK nhờ guard) · xem mắt switcher cây sau fix ·
smoke test tạo user thường + login kiểm quyền. Lưu ý: verify đã gán admin 2 công ty + mặc định
"Cầu Nối" vào HT_NguoiDung_CongTy thật — muốn bỏ thì "Bỏ hết gán riêng" → Lưu.

## Session 86 (2026-07-15) — Hướng dẫn sử dụng khi trỏ chuột — màn Cấu hình Field (WPF)

**User đặt bài:** nâng cấp tính năng hướng dẫn sử dụng — trỏ chuột vào là có hướng dẫn chi tiết
phải làm gì để cấu hình đúng control. Chốt qua hỏi-đáp: làm CẢ HAI (tooltip từng ô nhập + popup
quy trình từng bước trên banner tên control), phạm vi TẤT CẢ editor type.

**Đã làm (commit `13e999f`):** `FieldHelpTopic` + `FieldHelpCatalog` (~35 topic tiếng Việt:
mục đích/cách làm/ví dụ/lỗi thường gặp) + attached property `Behaviors/HelpAssist` (Topic key tĩnh,
Prop cho dynamic props có fallback) + `ControlTypeGuide.Steps` (12 editor type) hiện tooltip trên
2 banner (tab Cơ bản + banner ghim Control Props). Gắn đủ LookupBoxPropsPanel, ComboBoxPropsPanel,
RadioGroup Sys_Lookup, 4 template dynamic props. Style theo DesignTokens (ADR-031). Build 0W/0E.

**Dọn nợ:** commit riêng `94b0693` — thay đổi dở từ session trước (badge Field Navigator theo
`Ui_Field.Is_Configured` db/067 + mẫu thông báo Required/Unique mặc định) đã build OK, tách khỏi
commit tính năng theo yêu cầu user. Chi tiết → TASKS_WPF.md mục Done 2026-07-15.

## Session 85 (tiếp) — Đánh giá 3 kiểu màn + Spec 30 Form chứng từ master-detail

**User đặt bài:** phần mua bán có 3 kiểu màn — ① danh mục (đáp ứng rồi), ② master list (nút CRUD/in,
lọc ngày, Xem dữ liệu, 1–2 lưới), ③ màn thêm mới đơn hàng (1 đơn/1 khách/n dòng hàng/nhiều giá,
event dày — WPF Config chưa cấu hình được). Yêu cầu: đánh giá với những gì đang có, chốt tài liệu.

**Kết quả verify trên code:** ② đạt ~80% (Ui_View_Action + panel lọc Ui_View_Filter + in DocTemplate
ĐÃ chạy; THIẾU 2 lưới — `Detail_View_Id` chỉ đặt chỗ schema, dữ liệu NULL, 0 dòng runtime Blazor).
③ gap lớn nhất: renderer chỉ có 10 control field đơn, không có lưới chi tiết editable; FIELD_CHANGED→
UiDelta có nhưng chỉ form 1 bản ghi; API save theo 1 bảng; ConfigStudio chưa có màn cấu hình.

**Phát hiện kiến trúc đắt giá:** `AstParser`/`AstCompiler` ở `ICare247.Domain` THUẦN C# (csproj không
package hạ tầng) → RCL DynamicForms tham chiếu trực tiếp được ⇒ **công thức dòng chạy client-side
trong WASM bằng chính AST Grammar V1** — không JS, không round-trip; server recalc lại khi save.

**Tài liệu đã chốt:** spec 29 thêm §10 (bảng đánh giá 3 kiểu màn); spec 14 thêm §11 (hiện trạng +
hành vi dự kiến `Detail_View_Id`, task VIEWMD-001); **spec 30_FORM_CHUNG_TU_SPEC.md mới** —
`Ui_Form_Detail` (cột lưới = Ui_Field của form CON, tái dùng control map/validation/i18n),
`Ui_Field.Formula_Json`, DetailGridRenderer (cell-inline, row state client), event server mức dòng
(RowContext + UiDelta target dòng — ca chuẩn: chọn hàng → tra chính sách giá → set DonGia),
`POST /forms/{code}/save-document` 1 transaction (điểm cắm posting engine spec 29). TASKS.md thêm
FDOC-000→FDOC-1→6 + VIEWMD-001. Còn mở: 4 câu hỏi spec 30 §8.

## Session 85 (2026-07-15) — Phân tích legacy Ngọc Chương → Spec 29 Thu mua nông sản ký gửi

**Đầu vào:** user thêm `src/frontend/source_can_update/` — app WPF legacy .NET 4.8 (4 project, ~488 file C#),
là cơ sở phần kinh doanh. Yêu cầu: tập trung Ngọc Chương, đề xuất nâng cấp, chuyển bảng tiếng Việt,
đồng nhất khái niệm/trường dữ liệu giữa các màn, phân tích chi tiết lõi nghiệp vụ.

**Kết quả:** viết **`docs/spec/29_THU_MUA_NONG_SAN_SPEC.md`** + block TM-000→TM-004 trong TASKS.md.

**Lõi nghiệp vụ trích được (6 cụm):** ① ký gửi–chốt giá (cân hàng: bì 200gr/bao, độ cà/tạp/thủy phần
→ quy chuẩn → quy nhân; TK ngoài bảng 002; chốt giá → rút tiền/trả hàng); ② mua 4 biến thể
(tươi/khô/nhân/non — non có thế chấp, TK 151); ③ sơ chế mẻ (phơi/sấy/xay); ④ tín dụng nông hộ
(cho vay 1283 / nhận vay 341, lãi tháng); ⑤ bán (HĐ kinh tế/giao hàng — hàng bán vs hàng gửi 157,
cân 2 lần); ⑥ báo cáo proc-only từ bút toán.

**Bệnh legacy (lý do không port code):** nhồi nghĩa cột (`SalePrice` = số bao HOẶC %thủy phần HOẶC tiền vay
tùy màn — bảng dẫn chứng ở spec §2); định khoản hardcode trong ViewModel (`"1561"/"331"/"002"`, có cả `"X"`
placeholder); lưu không transaction (đếm `StepSave`); SQL string-format; view/proc đặt tên theo ngày.

**4 quyết định user chốt (2026-07-15):** tiền tố `KD_`+`KT_` · viết trọn spec trước khi code ·
thiết kế generic ngành thu mua nông sản (cà phê = tenant đầu) · posting engine **C#** cùng transaction
(khớp ADR-029). Kiến trúc: `KD_LoaiGiaoDich` (cây ADR-027) + `KD_LoaiGiaoDich_DinhKhoan` (Nợ/Có + biểu thức
AST nguồn số liệu) → engine sinh `KT_ButToan`; bảng vệ tinh theo cờ (`KD_CanHang/KyGui/ChotGia/KhoanVay/SoChe/HopDong`);
`KD_KyGui` là ledger SUM chứ không lưu số dư đè. Còn mở: 5 câu hỏi spec §9 (TM-001).

**Phụ:** `.claude/launch.json` thêm `"autoPort": false` cho `icare247-ui` (app cần đúng cổng 5173 vì CORS;
preview mở thẳng instance đang chạy, không khởi động bản trùng).

## Session 84 (2026-07-14) — Chuẩn hóa lưới WPF (grid design rules)

**Commit `fb6f634` trên `master`** (3 file, +137/−5). Build ConfigStudio **0 warning / 0 error**.
Chi tiết → [TASKS_WPF.md](../../docs/ICare247%20Config%20Studio/TASKS_WPF.md) mục Done 2026-07-14.

**User yêu cầu:** mọi lưới WPF phải bật lọc kiểu "chứa", lưu format lưới, cho đổi vị trí + độ rộng cột.

**Hiện trạng trước đó:** `GridLayoutBehavior` (lưu/phục hồi layout ra XML local) + `AllowResizing` ĐÃ có sẵn
trong implicit style `dxg:TableView`. Thiếu: hàng auto-filter, điều kiện Contains, `AllowColumnMoving`.

**Đã làm:** bổ sung setter vào implicit style (`Controls.xaml`) + mở rộng `GridLayoutBehavior.ApplyFilterPolicy`
(nghe `ItemsSourceChanged` + `AutoGeneratedColumns`). Rule mới **§12** trong `.claude-rules/wpf-configstudio.md`.

**Quyết định user chốt:** (a) hàng lọc dưới header (không phải search panel chung); (b) **KHÔNG nhớ bộ lọc**
giữa các lần mở màn — chỉ nhớ format cột; (c) nút "khôi phục layout mặc định" → để sau.

**Gotcha đắt giá (ghi rule):** `RestoreLayoutFromXml` khôi phục cả tuỳ chọn view → file layout cũ mang
`ShowAutoFilterRow=False` **đè** setter mới ⇒ phải ép lại chính sách lọc SAU restore. `AutoFilterCondition`
gán theo `FieldType` trong code (text→Contains, số/ngày→Default), KHÔNG đặt ở style.

## Session 83 (2026-07-14) — Dọn nợ ADR-035 (R-1 + R-2)

**Commit `e92f5d0` trên `master`** (CHƯA push; 13 file, +101/−127). Thuần docs + SQL seed chưa chạy lại,
KHÔNG có code C# → không build/E2E. Đầu session đã **push 6 commit tồn** (`cd2bd1f..ee8f2a9`).

**R-1 — 4 migration seed** (`db/032,047,065,066`): gỡ hết tham chiếu `Tenant_Id` → chạy lại **sau** `db/078`
không còn `Invalid column name`. 032 bỏ cột+`t.Tenant_Id` khỏi INSERT/SELECT + NOT EXISTS chỉ theo `View_Code`;
047 bỏ `AND Tenant_Id IS NULL` (4 chỗ); 065/066 bỏ `ORDER BY CASE WHEN Tenant_Id IS NULL`.

**R-2 — 6 spec + 2 snapshot migration:**
- Spec 01/08/09/12/14/28: nguyên tắc mới = **cô lập ở tầng connection** (resolver chọn DB), **không** lọc SQL
  theo `Tenant_Id`; **cache key VẪN giữ `TenantId`** (Redis L2 dùng chung). Gỡ `@TenantId` khỏi bảng tham số
  Filter SQL + thêm cảnh báo "gõ vào ConfigStudio sẽ lỗi runtime". Spec 14 DDL `Ui_View` bỏ cột+FK→Sys_Tenant,
  gộp 2 filtered index → `UQ_Ui_View_Code`. Spec 28 bỏ `Tenant_Id` khỏi `Doc_Template`/`Doc_Proc_Registry`+§13-A.
- `docs/migrations/000` **viết lại đầy đủ** (user chốt): DROP `Sys_Tenant` + gỡ `Tenant_Id` **và `Is_Tenant`**
  (b413ad7) khỏi `Sys_Table`/`Sys_Lookup`/`Sys_Role`/`Sys_Config`, gộp filtered index → unique thường. Header
  ghi rõ **vẫn là snapshot 000–016** (chưa gồm migration 017+) — nguồn chuẩn provisioning là chuỗi `db/000→078`.
- `docs/migrations/001` gỡ block seed `Sys_Tenant` + cột `Tenant_Id` khỏi MERGE `Sys_Lookup` GENDER.

**Gỡ hết `Is_Tenant`** (user chốt, commit sau): gỡ khai báo cột `Is_Tenant BIT` khỏi `db/000_create_schema.sql`
(migration đầu chuỗi) → thay bằng comment. `db/081_drop_sys_table_is_tenant.sql` GIỮ (idempotent, drop cho DB đã
provision trước 081). Grep toàn repo: không còn khai báo cột `Is_Tenant` nào, chỉ còn comment + file 081 + tracking.
Lưu ý: `Tenant_Id` ở `db/000` (dòng kế) GIỮ nguyên — đó là scope R-1 hoãn riêng, `db/078` lo.

**Còn lại sau ADR-035:** R-3 (`Sys_Menu`/`Sys_MenuCatalog`) — HOÃN, chờ pha nâng cấp menu (chốt 2026-07-10).
6 file `db/` (`002/009/031/043/044/077`) CREATE cột `Tenant_Id` chưa dọn — không gấp (chạy trước 078, không vỡ).
**Quy tắc mới:** 3 file i18n LUÔN bỏ qua (không commit) — user chốt session này.

## Session 82 (2026-07-11) — Sinh nhanh Form/Lưới từ Sys_Table + xóa bulk field + loại cột audit

**Commit trên `master`** (CHƯA push): `16f7645` (9 files, +663/−24). Build ConfigStudio compile OK
(chỉ lock DLL do app đang chạy — coi như đạt). Chi tiết → [TASKS_WPF.md](../../docs/ICare247%20Config%20Studio/TASKS_WPF.md).

**Bối cảnh:** user muốn "khai báo Sys_Table xong thì tạo nhanh form/view". Auto-generate Form vốn ĐÃ có
nhưng đi 2 bước (mở FormEditor → dialog "Tạo Fields tự động"). User chốt: **2 nút tách biệt, độc lập**
(không phải lúc nào tạo Form cũng kèm View), mỗi nút **1-chạm headless**; hiện chỉ 1 màn/1 lưới đơn,
master-detail để nâng cấp sau.

**Đã làm:**
1. **Service mới `IScreenScaffolder`/`ScreenScaffolder`** — sinh Form (Ui_Form+section+field) và Lưới
   (Ui_View+cột) headless, ghi thẳng Config DB; tái dùng data-service sẵn có (không Dapper trực tiếp).
2. **2 nút trên Sys_Table** — 📝 Sinh Form / 📊 Sinh Lưới; nút Form cũ (điều hướng) đổi thành 1-chạm.
3. **Loại khối cột audit** (CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver) qua `AuditColumnTemplate`
   — áp cả nút mới lẫn dialog "Tạo Fields tự động" cũ (user phát hiện audit lọt vào form sinh ra).
4. **Xóa bulk field trong FormEditor** — nút 🗑 Xóa (N) trong thanh Bulk + context-menu; trước chỉ có
   đổi thuộc tính, không có xóa nhóm.

**Lưu ý bàn giao:** sửa forward-only — form/view đã lỡ sinh kèm audit (VD `TC_PhongBan`) phải dọn tay
(xóa bulk hoặc xóa form rồi sinh lại). Config sinh nằm ở Config DB master → cần ConfigSync sang tenant.
3 file i18n (`catalog.json`×2, `i18n-report.md`) sửa từ trước, KHÔNG gộp vào commit này.

## Session 81 (2026-07-10) — ADR-035: bỏ HẲN cột `Tenant_Id` + dọn ADR mục + bug runtime

**Commit trên `master`** (đã push): `904fbb3` backend · `83717b2` WPF+migration+spec · `a302c37`
PublishCheckService · `ff7653b` Sys_Lookup Manager · `7643bb5`+`7dbaa8f` TASKS · `943c4c3` **dọn ADR mục** ·
`24acc2f` i18n · `b53329c` **2 bug E2E MasterData**. Build backend+WPF 0 error, 145/145 test. `db/078` đã chạy.

**Dọn ADR mục (`943c4c3`):** gỡ toàn bộ 18 dòng `Status:` khỏi `architecture_decisions.md` — ADR = quyết định
bất biến, trạng thái chuyển sang bảng `TASKS.md § Trạng thái triển khai ADR`. Rà thấy **8/18 dòng Status SAI**
(ghi "chưa code" trong khi code đã chạy). `/finish-task` thêm bước soát ADR. Xem [[feedback-adr-no-status]].

**3 bug runtime lộ khi E2E màn danh mục (`b53329c` + `a302c37`), đều có sẵn:**
1. `InsertAsync` CreatedBy NULL (LookupBox thêm mới) — không bơm cột audit. Sửa: dò `INFORMATION_SCHEMA` +
   bơm CreatedBy/CreatedAt, chặn client giả mạo, nối userId từ claim.
2. `ReferenceCheckService` chặn nhầm mọi xóa — fallback đoán-theo-tên khớp `Id`/`*_Id` toàn Data DB sau khi
   ADR-019 đổi PK thành `Id`. **GỠ HẲN** fallback; chỉ `Sys_Relation` + FK vật lý. Xem [[feedback-no-fk-inference]].
3. `PublishCheckService` 3 query cột không tồn tại; #2 trong `catch` trần → check vòng lặp chưa từng chạy.

**Quyết định (ADR-035):** cô lập tenant ở **tầng connection**, không ở tầng cột. ADR-018 cho mỗi tenant 1 Config
DB riêng → cột định danh tenant *bên trong* DB đã-thuộc-1-tenant không phân biệt được gì. Vai trò "master vs
tenant tùy biến" đã do ConfigSync đảm nhiệm qua `Is_System`/`Is_Customized`/`Source_Ver` (db/050).
**Giữ `TenantId` runtime** (resolver chọn connection + `CacheKeys` — Redis L2 dùng chung).
Quy tắc mới: `.claude-rules/database-design.md`.

**Khảo sát DB live lật ngược giả định:** không phải 4 bảng như spec 02 ghi mà **9 bảng** có cột — `db/*.sql`
và spec đều KHÔNG phản ánh đủ. `Sys_Table` 11/11 tenant-specific (0 global) ⇒ mệnh đề `OR Tenant_Id IS NULL`
rải khắp ~20 chỗ SQL là **nhánh chết**.

**5 bug lộ ra, đều là hệ quả của `Tenant_Id` tràn lan:**
1. `LookupRepository` lọc `OR Tenant_Id = 0` trong khi db/009 đã đổi global sang `NULL` → **13 lookup global
   im lặng biến mất** (`HINHTHUC_2FA`, `LOAI_TAIKHOAN`, `TRANGTHAI_DONVI`, `TRANGTHAI_NGUOIDUNG`).
2. `SysLookupDataService` (WPF) mang **bản sao y hệt** bug đó — cùng sai lầm, 2 nơi.
3–5. `PublishCheckService` có **3 query tham chiếu cột KHÔNG tồn tại**: `Ui_Field.ColumnCode`,
   `Sys_Dependency.Source_Field_Code`, `Sys_Language.Is_Active`. Query thứ 2 nằm trong **`catch` trần**
   → im lặng báo "Sys_Dependency chưa được build" ⇒ **check vòng lặp phụ thuộc CHƯA BAO GIỜ chạy**.

**Bài học:** `catch` trần biến lỗi schema thành thông điệp sai lệch, sống lâu không ai biết.
Hai quy ước cho cùng một khái niệm (`0` vs `NULL` cho "global") thì lỗi sinh sôi ở mọi nơi chạm vào cột.

**Đính chính memory:** ADR-018 từng ghi `Status: 🔴 chưa code (infra resolver)` — **SAI**,
`TenantConnectionResolver` đã code và đang chạy. Giả định sai đó suýt dẫn tới kết luận nhầm.

**Cũng làm:** `Sys_Lookup Manager` (**đã có sẵn**, không phải dựng mới) — xóa `AddLookupCodeAsync` chết
(`return exists == 0 || true;`), làm thật `DeleteCodeCommand` (nút 🗑 + confirm + đếm item **từ DB**,
không từ `Items.Count` vì `LoadItemsAsync` chạy fire-and-forget).

**⚠️ Còn lại → xem mục "🔜 CÒN LẠI sau ADR-035" trong TASKS.md:** 4 migration seed sẽ vỡ nếu chạy lại
(`032`/`047`/`065`/`066`) · 6 spec vẫn dạy mô hình cũ (`01_ARCHITECTURE.md:44` ghi quy tắc cứng nay SAI) ·
**chờ user chốt** `Sys_Menu`/`Sys_MenuCatalog` nằm ở Config DB hay Catalog DB (spec 15 vs 16 mâu thuẫn).
4 file i18n pre-existing vẫn để nguyên.

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
