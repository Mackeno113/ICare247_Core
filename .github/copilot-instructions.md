# ICare247 Core Platform — GitHub Copilot Instructions
# FILE: .github/copilot-instructions.md
# MỤC ĐÍCH: Hướng dẫn GitHub Copilot về project context và coding conventions.
# GitHub Copilot tự động đọc file này khi làm việc trong repo.

## Project Context

ICare247 Core Platform — Metadata-driven low-code form engine.
- **Language:** C# (.NET 9)
- **Database access:** Dapper ONLY (EF Core is FORBIDDEN)
- **Architecture:** Clean Architecture + CQRS (MediatR)
- **Comments:** Vietnamese (Tiếng Việt)

## Mandatory Patterns

### Always use Dapper (never EF Core)
```csharp
// ✅ Correct
using var conn = _connectionFactory.CreateConnection();
return await conn.QueryFirstOrDefaultAsync<T>(
    new CommandDefinition(sql, parameters, cancellationToken: ct));

// ❌ Wrong
_dbContext.Forms.FirstOrDefaultAsync(f => f.FormCode == code);
```

### Always parameterize SQL
```csharp
// ✅ Correct
WHERE Form_Code = @FormCode AND Is_Active = 1

// ❌ Wrong  
WHERE Form_Code = '{formCode}'
```

### Always pass CancellationToken
```csharp
// ✅ Correct
public async Task<T?> GetByCodeAsync(string code, CancellationToken ct = default)

// ❌ Wrong
public async Task<T?> GetByCodeAsync(string code)  // missing ct
```

### Always use CacheKeys.cs
```csharp
// ✅ Correct
var key = CacheKeys.Form(formCode, version, langCode, platform);

// ❌ Wrong
var key = $"form_{formCode}_{langCode}";  // hardcoded
```

### Always include file header
```csharp
// File    : FormRepository.cs
// Module  : Metadata
// Layer   : Infrastructure
// Purpose : Repository truy vấn metadata form từ bảng Ui_Form qua Dapper
```

### CQRS with MediatR
```csharp
public record GetFormByCodeQuery(string FormCode, string LangCode) : IRequest<FormDto>;

public class GetFormByCodeQueryHandler : IRequestHandler<GetFormByCodeQuery, FormDto>
{
    public async Task<FormDto> Handle(GetFormByCodeQuery request, CancellationToken ct) { }
}
```

## Key Naming Conventions

| Pattern | Format | Example |
|---------|--------|---------|
| Query | `Get{Object}By{Key}Query` | `GetFormByCodeQuery` |
| Command | `{Verb}{Object}Command` | `ValidateFieldCommand` |
| Handler | `{...}QueryHandler` / `{...}CommandHandler` | `GetFormByCodeQueryHandler` |
| Repository interface | `I{Entity}Repository` | `IFormRepository` |
| Repository impl | `{Entity}Repository` | `FormRepository` |
| Function handler | `{FunctionCode}FunctionHandler` | `LenFunctionHandler` |

## Layer Rules

- Domain: No imports from other projects
- Application: Import Domain only
- Infrastructure: Import Application only  
- Api: Import Application only (NEVER import Infrastructure directly)
