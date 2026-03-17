# Architecture Rules — ICare247

## Layer Dependency (Clean Architecture)

```
Domain          ← KHÔNG import gì (pure C#, no ORM)
Application     ← chỉ import Domain
Infrastructure  ← import Application (để implement interfaces)
Api             ← chỉ import Application (KHÔNG import Infrastructure trực tiếp)
```

## Exception: Composition Root

- `Api.csproj` reference Infrastructure CHỈ để `Program.cs` gọi `AddInfrastructure()`
- Controllers KHÔNG được `new` bất kỳ class Infrastructure nào

## DI Registration

- Mỗi layer có `DependencyInjection.cs` riêng
- Program.cs chỉ gọi:
  ```csharp
  builder.Services.AddApplication();
  builder.Services.AddInfrastructure();
  ```

## CQRS Pattern (MediatR)

- **Query**: `IRequest<TResponse>` — đọc dữ liệu
- **Command**: `IRequest<TResponse>` — ghi/thực thi
- **Handler**: `IRequestHandler<TRequest, TResponse>`
- **Validator**: `AbstractValidator<TRequest>` (FluentValidation)
- **Flow**: Request → IMediator.Send() → Handler → Repository → DB/Cache

## File Structure Per Feature

```
Application/Features/{Module}/Queries/{QueryName}/
├── {QueryName}Query.cs
├── {QueryName}QueryHandler.cs
└── {QueryName}QueryValidator.cs
```

## Coding Checklist (Architecture)

```
✅ Namespace phải match folder path (ICare247.Domain.Entities.Form)
✅ Mỗi file = đúng 1 class / interface / record (không gộp)
✅ Query = IRequest<TResponse>, Command = IRequest<TResponse>
✅ Handler = IRequestHandler<TRequest, TResponse>
✅ KHÔNG new Infrastructure class trong Api layer
✅ Exception bubble lên — không swallow trong engine
```
