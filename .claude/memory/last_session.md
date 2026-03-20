# Last Session Summary

> Cập nhật: 2026-03-20

## Đã làm (session 20/03 — tiếp)

### Backend .NET 9 — Phase 1 + Phase 6 ✅
- Phase 1 Foundation: Application interfaces, Infrastructure (SqlConnectionFactory, FormRepository, FieldRepository, AuditLogRepository, HybridCacheService), FormController + ExceptionHandlingMiddleware
- Phase 6 Form Management CRUD: 3 Queries + 5 Commands (GetFormsList, GetFormByCode, GetFormAuditLog, CreateForm, UpdateForm, DeactivateForm, RestoreForm, CloneForm)

### ConfigStudio.WPF.UI — Direct DB (Hướng B) ✅ hoàn thành 4 wave
- **Quyết định kiến trúc**: Chuyển từ WPF→API→DB sang WPF→Dapper→DB trực tiếp (admin tool 1-2 user, không cần API intermediary)
- **Option C**: Multiple focused service interfaces (ISP), mỗi module 1 interface riêng

**Wave 1 — Foundation + Standalone modules**
- 6 interfaces: IFormDetailDataService, IFieldDataService, IRuleDataService, IEventDataService, IGrammarDataService, II18nDataService
- 15 record DTOs trong Core/Data/
- 6 Dapper implementations trong Infrastructure/
- DI registration trong App.xaml.cs
- GrammarLibraryViewModel + I18nManagerViewModel migrated (DB hoặc mock fallback)

**Wave 2 — FormDetail (read-only)**
- FormDetailViewModel: load header, sections, fields, events summary, rules summary, audit log từ DB
- Deactivate/Restore gọi DB trực tiếp

**Wave 3 — FieldConfig**
- FieldConfigViewModel: inject 4 services (Field, I18n, Rule, Event), load columns, field detail, linked rules/events
- i18n preview resolve từ DB khi có
- Save field metadata qua IFieldDataService

**Wave 4 — Rule + Event editors**
- ValidationRuleEditorViewModel: load/save rules qua IRuleDataService
- EventEditorViewModel: load events, actions, trigger/action types qua IEventDataService

## Trạng thái
- Build WPF thành công: 0 errors, 0 warnings
- Tất cả 6 ViewModel đã migrated: mock fallback khi DB chưa cấu hình, load DB thật khi đã cấu hình
- TASKS.md đã cập nhật

## Task tiếp theo
- P0 UX Features: Auto-save, Undo/Redo, Live Linting, Impact Preview
- Hoặc Backend Phase 2 (Grammar V1 / AST Engine)
