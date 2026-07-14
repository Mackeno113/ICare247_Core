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

## Multi-tenant (ADR-035 — cô lập ở tầng connection, KHÔNG ở tầng cột)

- Mọi HTTP request phải có header `X-Tenant-Id`
- Middleware extract → đưa vào `ITenantContext`
- `TenantConnectionResolver` dùng `TenantId` để **chọn Config DB / Data DB của tenant**
  (mỗi tenant 1 DB riêng — ADR-018). Vì kết nối đã trỏ đúng DB nên **query SQL KHÔNG lọc
  `AND Tenant_Id = @TenantId`** — cột `Tenant_Id` đã bị bỏ hẳn khỏi mọi bảng (`db/078`).
- **Cache key vẫn phải có `TenantId`** — Redis L2 dùng chung giữa các instance/tenant, key gắn
  `TenantId` để không lẫn dữ liệu (xem `CacheKeys.cs`).

## Security

- **Auth**: JWT Bearer token
- **Authorization**: Policy-based (`[Authorize(Policy = "...")]`)
- **SQL injection**: Dapper parameterized — tuyệt đối không string interpolation vào SQL
- **Secrets**: lưu trong `appsettings.{env}.json` hoặc User Secrets (dev) / Key Vault (prod)
