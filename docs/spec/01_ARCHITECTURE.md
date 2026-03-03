# 01 — Architecture

## Clean Architecture Layers

```
Domain          ← Pure C#, không import gì (entities, AST nodes, interfaces engine)
Application     ← Import Domain (CQRS queries/commands/handlers, repo interfaces)
Infrastructure  ← Import Application (Dapper repos, cache, engine implementations)
Api             ← Import Application + Infrastructure (chỉ composition root)
```

### Quy tắc cứng
- `Api` controllers KHÔNG được `new` bất kỳ class Infrastructure nào
- DI registration chỉ trong `DependencyInjection.cs` của từng layer
- `Program.cs` chỉ gọi `AddApplication()` + `AddInfrastructure()`

## CQRS Pattern (MediatR)

```
Request → IMediator.Send() → Handler → Repository → DB / Cache
```

- **Query**: đọc dữ liệu, implement `IRequest<TResponse>`
- **Command**: ghi/thực thi, implement `IRequest<TResponse>`
- **Handler**: `IRequestHandler<TRequest, TResponse>`
- **Validator**: FluentValidation `AbstractValidator<TRequest>`

## Caching Strategy

```
L1: MemoryCache (trong process, tốc độ cao)
    ↓ miss
L2: Redis (distributed, cross-instance)
    ↓ miss
L3: SQL Server qua Dapper
```

Tất cả cache key dùng `CacheKeys.cs` — không hardcode string.

## Multi-tenant

- Mọi HTTP request phải có header `X-Tenant-Id`
- Middleware extract → đưa vào `ITenantContext`
- Mọi query SQL phải có `AND Tenant_Id = @TenantId`
- Mọi cache key phải có Tenant_Id

## Security

- **Auth**: JWT Bearer token
- **Authorization**: Policy-based (`[Authorize(Policy = "...")]`)
- **SQL injection**: Dapper parameterized — tuyệt đối không string interpolation vào SQL
- **Secrets**: lưu trong `appsettings.{env}.json` hoặc User Secrets (dev) / Key Vault (prod)
