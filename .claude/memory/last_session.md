# Last Session Summary

> Cập nhật: 2026-03-20

## Đã làm (session 20/03 — backend engines)

### Phase 2 — Grammar V1 / AST Engine ✅ (commit `070bbfe`)
- AstParser: JSON → IExpressionNode (6 node types, max depth=20, max size=64KB)
- AstCompiler: IExpressionNode → Func<EvaluationContext, object?> (null-safe, div/0→null)
- FunctionRegistry + BuiltinFunctions: 25 built-in functions (string, math, logic, date, conversion)
- AstEngine: orchestration + ConcurrentDictionary compiled cache (SHA256 key)
- DI: FunctionRegistry, AstParser, AstCompiler, AstEngine — all singleton
- Test project: ICare247.Application.Tests (xUnit) — 125 tests passing

### Phase 3 — Validation Engine ✅ (commit `5ac85a5`)
- ValidationEngine: validate field/form theo Val_Rule list
  - Required built-in check (null/empty/whitespace)
  - Custom/Regex/Range via AST evaluation
  - Condition_Expr support (skip rule khi condition = false)
  - Topological sort via Sys_Dependency (Kahn's algorithm)
  - Warning severity không ảnh hưởng IsValid
- IRuleRepository + RuleRepository (Dapper): load rules by field hoặc by form (1 query)
- IDependencyRepository + DependencyRepository (Dapper): load field-to-field dependencies
- RuleMetadata: thêm ConditionExpr property
- DI: IValidationEngine (scoped), IRuleRepository, IDependencyRepository (scoped)
- 16 unit tests — tổng 141 tests passing

## Trạng thái
- Build backend: 0 errors, 0 warnings, 141 tests passing
- Phase 1 ✅ | Phase 2 ✅ | Phase 3 ✅ | Phase 4-5 pending

## Task tiếp theo
- Phase 4 — Event Engine (EventEngine, ActionExecutor, UiDeltaBuilder)
- Phase 5 — API + Infrastructure (TenantMiddleware, JWT Auth, OpenTelemetry, Swagger)
- P0 UX Features (Auto-save, Undo/Redo, Live Linting, Impact Preview)
