# Last Session Summary

> Cập nhật: 2026-03-21

## Đã làm (session 21/03 — ConfigStudio WPF: Real DB + User Manual)

### DependencyViewer — Real Dapper Data ✅
- Thay `LoadMockGraph()` bằng `LoadRealGraphAsync()` dùng `IFormDetailDataService` + `IRuleDataService` + `IEventDataService`
- Build node graph từ Fields/Rules/Events thực từ DB
- Edge Field→Rule: `GetRulesByFieldAsync(fieldId)` per field
- Edge Field→Event: `GetEventsByFieldAsync(fieldId)` per field
- Edge Event→Field: `EventSummaryRecord.FieldTarget` → lookup node dict
- `LoadNodeImpactAsync()`: gọi `IImpactPreviewService.AnalyzeFieldImpactAsync()` + fallback từ graph edges
- Real DFS circular dependency detection

### DependencyViewerView.xaml ✅
- Thêm `UserControl.Resources`: `BoolToVis`, `InvBoolToVis` (fix lỗi StaticResource not found)
- Loading overlay: `ProgressBar IsIndeterminate` thay `dx:LoadingIndicator`
- Impact panel: WrapPanel chips màu theo type (field=Indigo, rule=Teal, event=Amber)
- Fix tất cả `{StaticResource BooleanToVisibilityConverter}` → `{StaticResource BoolToVis}`

### InverseBoolToVisConverter (Grammar module) ✅ (ADR-005)
- Tạo `Converters/InverseBoolToVisConverter.cs` trong Grammar module
- Core là `net9.0` không có WPF types → converter phải ở module WPF

### ExpressionBuilderDialogViewModel ✅
- Inject `IGrammarDataService`
- `LoadPaletteFromDbAsync()`: gọi `GetOperatorsAsync()` + `GetFunctionsAsync()` từ DB
- Fallback hardcode nếu DB empty
- `_availableOperatorSymbols` populate từ DB
- `IsLoadingPalette` property

### PublishChecklistViewModel ✅
- Inject `IPublishCheckService`
- 11 real checks qua Dapper (thay `Task.Delay` mock)
- `ApplyResult()`: map `CheckResult` → `ChecklistItem.Status` (Passed/Warning/Failed)
- Summary count bao gồm warning riêng biệt
- `tenantId` từ navigation parameters (default = 1)

### IPublishCheckService + PublishCheckService ✅
- 11 checks: Label_Key, JSON parse, function/operator whitelist, return type, circular dep (DFS), AST depth, i18n, CallAPI URL, Sys_Dependency
- `PublishCheckService` injects `IAppConfigService`, dùng `SqlConnection` Dapper trực tiếp

### FormEditorViewModel — Bỏ MockData → Real DB ✅
- Bỏ `LoadMockData()` hoàn toàn
- Edit mode: `_ = LoadFromDatabaseAsync()` dùng `FormId` (không dùng FormCode)
- `LoadFromDatabaseAsync()`: load header → table lookup → sections → fields → events → permissions
- Inject `IFormDetailDataService` thêm vào constructor
- `_loadCts` CancellationTokenSource để cancel khi navigate away
- `ErrorMessage` + `HasError` property hiển thị lỗi DB
- **Dual Key principle**: `FormId` cho DB query/navigate nội bộ; `FormCode` chỉ cho display/log/cache

### App.xaml.cs ✅
- Register `IPublishCheckService`, `IImpactPreviewService`

### User Manual ✅
- `docs/generate_manual.py`: Python script tạo Word document
- `docs/ICare247_ConfigStudio_UserManual.docx`: 14 chương, A4, Calibri

## Trạng thái
- Build backend: 0 errors, 0 warnings
- Phase 1-5 ✅ | P0 UX ✅ | DB Schema ✅
- ConfigStudio Direct DB ✅: DependencyViewer, ExpressionBuilder, PublishChecklist, FormEditor — tất cả dùng real Dapper

## Task tiếp theo
- `ExecuteSave()` trong FormEditorViewModel: gọi thực Dapper save sections + fields + events
- `LoadDefaultPermissions()` → load từ `Sys_Role` thực
- FormDetailView: đọc real data từ DB (hiện vẫn có mock ở một số chỗ)
- MetadataEngine implementation (IMetadataEngine)
- Integration tests
- Blazor runtime frontend
