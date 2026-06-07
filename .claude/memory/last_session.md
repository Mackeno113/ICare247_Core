# Last Session Summary

> Cập nhật: 2026-06-07 (session 39 — Tab tier + i18n popup + Layout config + Unique check + ConfigCache design)

## Trạng thái cuối session
- **Branch:** `master`
- **Build:** ⚠️ CHƯA verify (user tự build). Nhiều thay đổi WPF + backend + Blazor.
- **Migrations CHƯA chạy:** `db/025` → `db/030` (xem dưới) — phải chạy trên DB thật.

## Đã làm (session 39)

### 1. Spec resource key (docs/spec/10)
- Convention i18n cho **Form/Tab/Section title**: `{table_code}.form|tab|section.{code}.title` + `sys.val.unique`.

### 2. Tầng Tab cho FormEditor (full-stack WPF)
- Quyết định: KHÔNG dựng tree 3 tầng (đụng ~50 chỗ `Sections.SelectMany`). Thay bằng **TabItem "📑 Tabs" riêng** (master-detail) + dropdown "Thuộc Tab" trong panel Section.
- DTO `TabDetailRecord`/`TabUpsertRequest`; `IFormDetailDataService` Get/Upsert/DeleteTab; `FormTabItem`; FormEditorViewModel + View. Clone form copy Ui_Tab (CloneTabsAsync + remap Tab_Id).

### 3. I18nEditorDialog (popup i18n dùng chung) — Modules.I18n
- `I18nValueRow` + `I18nEditorDialogViewModel` + `I18nEditorDialog.xaml`; `ViewNames.I18nEditorDialog`; RegisterDialog.
- Nút 🌐 Dịch tích hợp: Section, Tab, Field (Label/Placeholder/Tooltip/Required), Event SHOW_MESSAGE (structured editor `ActionItemDto` messageKey/severity).
- Form title i18n (`Ui_Form.Title_Key`) — backend resolve → FormMetadata.FormName → dialog "Thêm mới: {tên}".

### 4. Layout form per-form (`db/027`)
- `Ui_Form.Max_Width` + `Form_Columns`; backend FormMetadata + FormRunner áp `max-width` + `--form-cols` (responsive giữ qua `min()`). WPF card "BỐ CỤC HIỂN THỊ".

### 5. Blazor FormRunner/MasterData
- 1 section → render phẳng; ≥2 → card group. Default ColSpan = Half.
- **Fix bug:** LookupAddDialog render label 2 lần → bỏ label thủ công.

### 6. Chống trùng mã (Is_Unique) — full-stack (`db/029`)
- Cờ `Ui_Field.Is_Unique` + toggle "🔑 Duy nhất" + section "Thông báo khi trùng (i18n)" trong FieldConfig.
- Backend check 2 đường: MasterData (`ExistsValueAsync`) + Lookup add-new (`DuplicateValueException`).
- Message i18n: handler **resolve key→text server-side** qua `IResourceRepository` (key `{table}.val.{column}.unique`, fallback `sys.val.unique`), default `vi`. Auto-tạo key khi lưu field (RegisterI18nKeysAsync vi+en).
- Chuẩn hóa UI: 5 section i18n key cùng layout (input → nút dưới → preview dưới).

### 7. Thiết kế ConfigCache (ADR-014) + roadmap — CHỈ TÀI LIỆU
- `IConfigCache` facade đọc config qua cache (L1/L2); web/handler cấm chọc repo config trực tiếp. Invalidation: Version-stamp + Event + TTL → ADR-014.
- **Làm rõ kiến trúc:** RuntimeCheck chỉ là **Blazor WASM test client** gọi API; IConfigCache nằm tầng **Application (backend)**, viết 1 lần dùng chung cho mọi web app qua API. Web app thật = thêm 1 client.
- Roadmap trong TASKS.md: ConfigCache (CC-0a→CC-4) + tách `ICare247.ApiClient` SDK (SDK-1→4).

## Session 40 (2026-06-07) — đã làm
- ✅ Chạy migrations 025→030 trên DB thật (029 đã ổn — `ALTER ... ADD Is_Unique` trong batch riêng có `GO`, idempotent `IF NOT EXISTS`).
- ✅ Build verify backend + WPF: **0/0**. Sửa lỗi build commit 49738e7 — `InsertLookupCommandHandler.cs` CS0136 (biến `v` trùng scope catch vs method) → đổi tên `newValue`.
- ✅ Re-save field Is_Unique seed key i18n.
- ✅ **CC-0a**: tạo `IConfigCache` (Application/Interfaces) + entity `FormPermission` (Domain/Entities/Permission, deny-by-default).
- ✅ **CC-0b**: `ConfigCache` (Application/Engines) — form metadata ủy quyền `MetadataEngine`; resource map + lookup cache-aside; `ResolveKeyAsync` derive scope từ prefix key; key `ConfigResourceMap/ConfigLookup/ConfigPermission` gắn slot `:v{version}` (const 0). Permission tạm null (CC-3).
- ✅ **CC-0d (DI)**: đăng ký `IConfigCache→ConfigCache` scoped. Build backend 0/0.

- ✅ **CC-1a**: `InsertLookupCommandHandler` + `SaveMasterDataCommandHandler` bỏ inject `IResourceRepository`, resolve message trùng qua `IConfigCache.ResolveKeyAsync`. Build 0/0.
- ✅ **CC-0c**: helper `GetOrLoadAsync<T>` trong `ConfigCache` — stampede lock `SemaphoreSlim` per-key + negative cache `NegTtl=30s` cho kết quả rỗng. Áp cho resource map + lookup. Application compile 0/0.

- ✅ Full backend build `ICare247.slnx` verify **0/0** (đã stop API rồi build lại).

- ✅ Commit `98e699a` cụm CC-0/CC-1.
- ✅ **CC-2**: `GetLookupByCodeQueryHandler` delegate `IConfigCache.GetLookupOptionsAsync` (xóa dead `CacheKeys.Lookup`). Thêm `InvalidateLookupAsync` + endpoint `POST /api/v1/lookups/{code}/invalidate-cache`. Build 0/0.
- ✅ **CC-1b**: rà runtime i18n. Sửa 2 bug thật — `SaveMasterDataCommandHandler` (resourceMap null → lấy qua facade) + `EventEngine` TRIGGER_VALIDATION (thêm `FormCode`/`LangCode` vào `FormEvent`, inject `IConfigCache`). RuntimeController vốn đã OK. Build 0/0.

## Commits session 40
- `98e699a` — CC-0a/0b/0c/0d(DI) + CC-1a (facade nền tảng + dọn anti-pattern i18n).
- `47f1e8d` — CC-2 (lookup options qua facade) + CC-1b (sửa 2 bug i18n runtime).

> ⚠️ API đã bị **stop** để build verify — khởi động lại khi cần chạy app.

## ⏳ Việc cần làm ngay (đầu session sau)
1. **CC-3 (permission) — HOÃN**: chờ chốt schema bảng `Sys_Permission` (role/user × form × CRUD, tenant scope). Sub-task CC-3a→3d đã ghi trong TASKS.md. `GetFormPermissionsAsync` hiện trả null (deny-by-default), entity `FormPermission` là contract sẵn.
2. **CC-4** — version-stamp scale-out + WPF wiring invalidate (chỉ cần khi ≥2 instance).
3. Các việc tồn khác: BE-002 integration tests, BE-004 Design System tokens, E2E test Master Data với DB thật.

## Điểm vào việc tiếp theo
- **CC-0a** (nếu code ConfigCache): tạo `ICare247.Application/Interfaces/IConfigCache.cs` + record `FormPermission` — chỉ interface, build vẫn xanh. Xem TASKS.md roadmap ConfigCache + ADR-014.
- **SDK-1** (nếu dựng web app mới): tạo `ICare247.ApiClient` class lib, gom client + DTO; refactor RuntimeCheck dùng SDK.

## Migrations tích lũy cần có trên DB (017→030)
017 lock_on_edit · 018 is_virtual · 019 column_id_nullable · 020 field_code · 021 lookup_parent · 022 lookup_addnew · 023 display_mode · 024 show_in_list · **025 section .title** · **026 fix sys_language** · **027 form layout** · **028 form title_key** · **029 field is_unique** · **030 sys.val.unique**
