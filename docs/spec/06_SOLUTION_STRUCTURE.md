# 06 — Solution Structure

## Thư mục gốc

```
d:/ICare247_Core/
├── src/
│   ├── backend/
│   │   ├── ICare247.slnx
│   │   └── src/
│   │       ├── ICare247.Domain/
│   │       ├── ICare247.Application/
│   │       ├── ICare247.Infrastructure/
│   │       └── ICare247.Api/
│   └── frontend/
│       └── (Blazor WASM — Phase 5)
├── db/
│   ├── ICare247_Config.sql
│   └── ICare247_SeedData.sql
├── docs/
│   └── spec/           ← File specs này
└── CLAUDE.md, TASKS.md
```

## Backend — Chi tiết từng layer

### ICare247.Domain
```
ICare247.Domain/
├── Entities/
│   ├── Form/
│   │   ├── FormMetadata.cs
│   │   ├── FieldMetadata.cs
│   │   └── SectionMetadata.cs
│   └── Rule/
│       └── RuleMetadata.cs
├── Ast/
│   ├── IExpressionNode.cs
│   ├── LiteralNode.cs
│   ├── IdentifierNode.cs
│   ├── BinaryNode.cs
│   ├── UnaryNode.cs
│   ├── FunctionCallNode.cs
│   └── MemberAccessNode.cs
├── Engine/
│   ├── IAstEngine.cs
│   ├── IValidationEngine.cs
│   ├── IEventEngine.cs
│   └── IMetadataEngine.cs
└── ValueObjects/
    └── EvaluationContext.cs
```

### ICare247.Application
```
ICare247.Application/
├── DependencyInjection.cs
├── Common/
│   ├── CacheKeys.cs
│   └── Interfaces/
│       ├── IDbConnectionFactory.cs
│       ├── IFormRepository.cs
│       ├── IFieldRepository.cs
│       ├── ISectionRepository.cs
│       └── ICacheService.cs
└── Features/
    └── Forms/
        └── Queries/
            ├── GetFormByCode/
            │   ├── GetFormByCodeQuery.cs
            │   ├── GetFormByCodeQueryHandler.cs
            │   └── GetFormByCodeQueryValidator.cs
            └── GetFormMetadata/
                ├── GetFormMetadataQuery.cs
                └── GetFormMetadataQueryHandler.cs
```

### ICare247.Infrastructure
```
ICare247.Infrastructure/
├── DependencyInjection.cs
├── Data/
│   └── SqlConnectionFactory.cs
├── Repositories/
│   ├── FormRepository.cs
│   └── FieldRepository.cs
├── Cache/
│   └── HybridCacheService.cs
└── Engine/
    ├── AstEngine/
    │   ├── AstParser.cs
    │   ├── AstCompiler.cs
    │   └── Functions/
    │       ├── FunctionRegistry.cs
    │       └── BuiltinFunctions.cs
    └── ValidationEngine.cs
```

### ICare247.Api
```
ICare247.Api/
├── Program.cs
├── Controllers/
│   └── Forms/
│       └── FormController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── TenantMiddleware.cs
│   └── CorrelationMiddleware.cs
└── Extensions/
    └── WebApplicationExtensions.cs
```

## Naming Quick Reference

| Loại                | Pattern                  | Ví dụ                        |
| ------------------- | ------------------------ | ---------------------------- |
| Query               | `Get{Entity}By{Key}Query`| `GetFormByCodeQuery`         |
| Query Handler       | `...QueryHandler`        | `GetFormByCodeQueryHandler`  |
| Command             | `{Verb}{Entity}Command`  | `ValidateFieldCommand`       |
| Repository Interface| `I{Entity}Repository`    | `IFormRepository`            |
| Repository Impl     | `{Entity}Repository`     | `FormRepository`             |
| Cache Key Method    | `CacheKeys.{Entity}(...)`| `CacheKeys.Form(...)`        |
