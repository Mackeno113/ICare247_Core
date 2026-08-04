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

## Exception Policy trong Engine (làm rõ 2026-08-03)

> Audit backend (`docs/reviews/2026-08-03-backend-code-audit.md` #4) phát hiện engine nuốt exception
> im lặng ở nhiều chỗ (EventEngine/ValidationEngine/MetadataEngine), mâu thuẫn quy tắc "không swallow".
> Quy tắc dưới đây là chuẩn hiện hành:

- **Lỗi cấu hình** (AST/JSON hỏng, expression sai, param không parse được) → **KHÔNG nuốt im lặng**:
  tối thiểu `LogWarning`/`LogError` có `Id` cấu hình + context; ưu tiên để nổi lên nếu là lỗi lập trình.
- **Giá trị an toàn có chủ đích** (VD condition eval fail → skip rule an toàn hơn throw) → được phép trả
  mặc định, NHƯNG **phải log** và ghi rõ lý do trong comment. `catch { }` rỗng là CẤM.
- Không dùng `catch (Exception)` để che bug logic — chỉ bắt đúng loại lường trước được.

## Coding Checklist (Architecture)

```
✅ Namespace phải match folder path (ICare247.Domain.Entities.Form)
✅ Mỗi file = đúng 1 class / interface / record (không gộp)
✅ Query = IRequest<TResponse>, Command = IRequest<TResponse>
✅ Handler = IRequestHandler<TRequest, TResponse>
✅ KHÔNG new Infrastructure class trong Api layer
✅ Engine: lỗi config phải log/nổi lên; chỉ trả mặc định khi an toàn CÓ CHỦ ĐÍCH + có log (KHÔNG catch rỗng)
✅ SQL động: theo .claude-rules/sql-safety.md (guard chung, whitelist, có test injection)
```
