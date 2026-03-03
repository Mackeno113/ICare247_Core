# ICare247 Core — Task Tracking

## 🔴 Đang làm (In Progress)

<!-- Claude cập nhật section này khi bắt đầu task -->

---

## 🟡 Cần làm (Todo)

### Phase 1 — Foundation

- [x] Tạo Domain entities (FormMetadata, FieldMetadata, SectionMetadata, RuleMetadata)
- [x] Tạo Domain AST nodes (IExpressionNode, LiteralNode, IdentifierNode, BinaryNode, ...)
- [x] Tạo Domain Engine interfaces (IAstEngine, IValidationEngine, IEventEngine, IMetadataEngine)
- [x] Tạo EvaluationContext value object
- [ ] Tạo Application interfaces (IFormRepository, IFieldRepository, IDbConnectionFactory, ICacheService)
- [ ] Tạo CacheKeys.cs
- [ ] Tạo GetFormByCodeQuery + Handler (skeleton)
- [ ] Implement Infrastructure: SqlConnectionFactory
- [ ] Implement Infrastructure: FormRepository (Dapper)
- [ ] Implement Infrastructure: FieldRepository (Dapper)
- [ ] Implement Infrastructure: HybridCacheService (Memory + Redis)
- [ ] Tạo FormController (skeleton)
- [ ] Tạo ExceptionHandlingMiddleware

### Phase 2 — Grammar V1 / AST Engine

- [ ] Implement AstParser (Expression_Json → IExpressionNode)
- [ ] Implement AstCompiler (IExpressionNode → Func<context, object?>)
- [ ] Implement FunctionRegistry + BuiltinFunctions (len, trim, iif, toDate, today, ...)
- [ ] Unit tests cho AstEngine

### Phase 3 — Validation Engine

- [ ] Implement ValidationEngine
- [ ] Implement RuleEvaluator
- [ ] Implement DependencyResolver (qua Sys_Dependency)

### Phase 4 — Event Engine

- [ ] Implement EventEngine
- [ ] Implement ActionExecutor
- [ ] Implement UiDeltaBuilder

### Phase 5 — API + Infrastructure bổ sung

- [ ] Setup TenantMiddleware (extract X-Tenant-Id)
- [ ] Setup CorrelationMiddleware
- [ ] Setup JWT Auth (appsettings.json config)
- [ ] Setup OpenTelemetry TracerProvider
- [ ] Setup Swagger/Scalar UI
- [ ] Setup appsettings.json + appsettings.Development.json
- [ ] Database seed script (db/ folder)

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

---

## 🐛 Bugs / Issues

<!-- Ghi lại bug phát sinh trong quá trình code -->

---

## 📝 Decisions Log

| Ngày       | Quyết định                                                                 | Lý do                                                     |
| ---------- | -------------------------------------------------------------------------- | --------------------------------------------------------- |
| 2026-03-03 | Api.csproj giữ reference đến Infrastructure                                | Program.cs cần gọi AddInfrastructure() — chấp nhận exception này cho composition root |
| 2026-03-03 | Dùng Scalar thay vì Swagger UI                                             | Scalar hiện đại hơn, tích hợp tốt với .NET 9 OpenAPI     |
| 2026-03-03 | Docs spec đặt trong docs/spec/ (không phải docs/ trực tiếp)               | docs/ đã có files AI config, tách biệt để rõ ràng hơn    |
