# Codex Current Phase

> Cập nhật lần cuối: 2026-04-25

## Tổng quan (xem đầy đủ trong .claude/memory/project_current_phase.md)

- **Backend .NET 9:** Phase 1-6 hoàn thành ✅
- **ConfigStudio WPF:** 11 screens done, còn 3 tasks nhỏ
- **Blazor RuntimeCheck:** Đang hoàn thiện renderers

## Priorities cho Codex

### WPF (high priority)
1. Pass `tableCode` khi navigate FieldConfig → I18nManager
2. Test LookupBox end-to-end (GioiTinh + PhongBanID)
3. WPF-10: ValidationRuleEditor — Compare rule field list dropdown

### Tests (medium priority)
- Unit tests cho AST Engine
- Integration tests cho Validation Engine
- Integration tests cho Event Engine

## DB Schema

- Canonical schema: `docs/migrations/000_create_schema.sql` (30 tables)
- Seed data: `docs/migrations/001_seed_all.sql`
- DB: ICare247_Config trên MS SQL Server
