# C# Naming Conventions — ICare247

## General

| Loại           | Convention       | Ví dụ                           |
| -------------- | ---------------- | ------------------------------- |
| Class          | PascalCase       | `FormMetadata`, `AstParser`     |
| Interface      | `I` + PascalCase | `IFormRepository`, `IAstEngine` |
| Method         | PascalCase       | `GetByCodeAsync`, `Evaluate`    |
| Property       | PascalCase       | `FormCode`, `IsActive`          |
| Private field  | `_` + camelCase  | `_repository`, `_cache`         |
| Local variable | camelCase        | `formMetadata`, `ruleList`      |
| Constant       | PascalCase       | `MaxAstDepth`, `DefaultTimeout` |
| Async method   | suffix `Async`   | `GetFormByCodeAsync`            |

## CQRS Naming

```csharp
// Query (đọc dữ liệu)
public record GetFormByCodeQuery(string FormCode, string LangCode) : IRequest<FormDto>;

// Query Handler
public class GetFormByCodeQueryHandler : IRequestHandler<GetFormByCodeQuery, FormDto>
{
    public async Task<FormDto> Handle(GetFormByCodeQuery request, CancellationToken ct) { }
}

// Command (ghi/thực thi)
public record ValidateFieldCommand(int FormId, string FieldCode, object Value)
    : IRequest<ValidateFieldResponse>;
```

## Repository Naming

- Interface: `I{Entity}Repository` (VD: `IFormRepository`)
- Implementation: `{Entity}Repository` (VD: `FormRepository`)
- Method: `GetByCodeAsync`, `GetByIdAsync`, `GetByFormIdAsync`

## Naming Quick Reference

| Loại                | Pattern                   | Ví dụ                        |
| ------------------- | ------------------------- | ---------------------------- |
| Query               | `Get{Entity}By{Key}Query` | `GetFormByCodeQuery`         |
| Query Handler       | `...QueryHandler`         | `GetFormByCodeQueryHandler`  |
| Command             | `{Verb}{Entity}Command`   | `ValidateFieldCommand`       |
| Repository Interface| `I{Entity}Repository`     | `IFormRepository`            |
| Repository Impl     | `{Entity}Repository`      | `FormRepository`             |
| Cache Key Method    | `CacheKeys.{Entity}(...)` | `CacheKeys.Form(...)`        |
