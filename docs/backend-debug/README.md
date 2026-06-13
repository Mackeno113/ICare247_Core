# Backend Debug Guide — ICare247

> Mục đích: giúp dev **truy vết & debug từng tính năng** của backend. Mỗi trang trả lời 4 câu:
> **(1)** màn hình gọi **API nào**, **(2)** truyền **cái gì**, **(3)** code viết ở **lớp nào**,
> **(4)** **luồng** đi ra sao + đặt **breakpoint** ở đâu.

Đây là tài liệu tra cứu khi sửa lỗi / phát triển. Không phải spec (spec ở `docs/spec/`).

---

## 0. Bản đồ kiến trúc (nhắc lại nhanh)

Clean Architecture 4 lớp — request đi **từ ngoài vào**:

```
[Blazor WASM]  ──HTTP──>  Api ──> Application (CQRS/MediatR) ──> Infrastructure (Dapper) ──> SQL Server
 ICare247_UI              Controller   Command/Query + Handler     Repository                Config/Live DB
```

| Lớp | Project | Vai trò | Ví dụ |
|---|---|---|---|
| **Api** | `src/backend/src/ICare247.Api` | Controller, middleware, DI, JWT | `Controllers/AuthController.cs` |
| **Application** | `src/backend/src/ICare247.Application` | CQRS (MediatR), interface, validate | `Features/Auth/...`, `Interfaces/...` |
| **Infrastructure** | `src/backend/src/ICare247.Infrastructure` | Dapper repo, JWT, hasher, cache | `Repositories/...`, `Auth/...` |
| **Domain** | `src/backend/src/ICare247.Domain` | Entity thuần, không phụ thuộc | `Entities/Auth/NguoiDung.cs` |

**Quy tắc vàng khi đọc luồng:** Controller chỉ điều phối → `IMediator.Send(command)` → MediatR tìm `Handler` →
Handler gọi `IRepository` (interface ở Application) → impl Dapper ở Infrastructure → DB.

---

## 1. Pipeline middleware (mọi request đều đi qua)

Thứ tự trong [`Program.cs`](../../src/backend/src/ICare247.Api/Program.cs) (mục "Middleware pipeline"):

```
1. ExceptionHandlingMiddleware   — bắt mọi lỗi → ProblemDetails 500
2. CorrelationMiddleware         — gắn X-Correlation-Id
3. SerilogRequestLogging         — log request
4. (Dev) OpenAPI + Scalar UI
5. /health
6. UseHttpsRedirection + UseCors
7. TenantMiddleware              — ★ phân giải tenant → set connection string
8. UseAuthentication/Authorization
9. MapControllers                — vào Controller
```

> ★ **TenantMiddleware** là mấu chốt multi-tenant. Nó đọc subdomain (khi có Catalog) hoặc header
> **`X-Tenant-Id`** (fallback hiện tại = `1`), rồi set `TenantContext.ConfigConnectionString` /
> `DataConnectionString`. Repository mở connection từ context này.
> Hiện tại Data DB = connstring **`LiveData`** (`ICare247_Solution`) — xem
> [`TenantConnectionResolver.cs`](../../src/backend/src/ICare247.Infrastructure/MultiTenancy/TenantConnectionResolver.cs).

---

## 2. Chuẩn bị môi trường debug

1. **Cấu hình thật** nằm ngoài repo: `%APPDATA%\ICare247\Api\appsettings.local.json`
   (connection string `Config` / `LiveData` / `Demo`, `Jwt:SecretKey`).
2. Chạy API ở chế độ Debug để gắn breakpoint:
   ```powershell
   dotnet run --project src/backend/src/ICare247.Api/ICare247.Api.csproj
   ```
   Hoặc nhấn **F5** trong Visual Studio (project khởi động = `ICare247.Api`).
3. Khi khởi động, đọc log **`ConnectionChecker`** trên console — xác nhận `Live DB` kết nối OK
   trước khi nghi ngờ code.
4. Gọi thử API không cần UI: mở **Scalar UI** `https://localhost:7130/scalar` (chỉ Development),
   hoặc dùng file `.http` (xem từng trang). **Nhớ luôn gửi header `X-Tenant-Id: 1`.**

---

## 3. Cách đặt breakpoint hiệu quả (theo lớp)

Đi theo đúng thứ tự để khoanh vùng lỗi nhanh:

| Bước | Đặt breakpoint tại | Kiểm tra điều gì |
|---|---|---|
| ① Vào API | `AuthController.<Action>` | Body nhận đúng? `GetTenantId()` ra `1`? |
| ② Tenant | `TenantMiddleware.InvokeAsync` | `TenantContext.DataConnectionString` = ICare247_Solution? |
| ③ Validate | `*CommandValidator` | Input có rớt FluentValidation không? |
| ④ Nghiệp vụ | `*CommandHandler.Handle` | Rẽ nhánh nào (sai mk / khóa / disabled…)? |
| ⑤ DB | `*Repository.<Method>` | SQL + tham số đúng? Kết nối đúng DB? |

> Lỗi không rõ → bật log SQL: `appsettings.local.json` → `Serilog:MinimumLevel:Override:Dapper = Debug`.

---

## 4. Lỗi thường gặp (bảng tra nhanh)

| Triệu chứng | Nguyên nhân hay gặp | Sửa ở đâu |
|---|---|---|
| HTTP 400 *"Thiếu thông tin tenant"* | Request thiếu header `X-Tenant-Id` | FE gửi header (Program.cs HttpClient) / thêm header khi test |
| Lỗi connection / timeout lúc gọi | `LiveData` không tới được hoặc sai DB | `appsettings.local.json` → `ConnectionStrings:LiveData` |
| 401 *"Tên đăng nhập/mật khẩu không đúng"* | Sai mk, hash lệch, tài khoản ≠ Local | DB `HT_NguoiDung`, `IdentityPasswordHasher.Verify` |
| 423 *"Tài khoản tạm khóa"* | Sai mk ≥ 5 lần → `KhoaDenKhi` | Đợi 15' hoặc reset `SoLanDangNhapSai`/`KhoaDenKhi` trong DB |
| API không khởi động được (prod) | `Jwt:SecretKey` rỗng/yếu/placeholder | Đặt key ≥ 32 ký tự ở local config |
| 401 ở API cần đăng nhập | Thiếu/`hết hạn` Bearer token | FE: `AuthService` gắn `Authorization`; thử `/refresh` |

---

## 5. Danh mục trang theo tính năng

| Nhóm | Tính năng | Trang | DB chính |
|---|---|---|---|
| Auth | Đăng nhập (mẫu chi tiết nhất) | [auth-login.md](auth-login.md) | Live |
| Auth | Làm mới token / Đăng xuất | [auth-refresh-logout.md](auth-refresh-logout.md) | Live |
| Auth | Quên / Đặt lại mật khẩu (stub) | [auth-forgot-reset.md](auth-forgot-reset.md) | — |
| Form | Cấu hình form (metadata) | [forms-config.md](forms-config.md) | Config |
| Form | Runtime: validate + event (engine) | [runtime-form.md](runtime-form.md) | Config (+Live) |
| Data | Master Data (CRUD danh mục) | [master-data.md](master-data.md) | Config + Live |
| Data | Views (lưới Grid/TreeList) | [views.md](views.md) | Config + Live |

> **Thêm tính năng mới?** Copy bố cục của `auth-login.md` (8 mục: Tóm tắt → API → Payload →
> Lớp code → Luồng → Breakpoint → Lỗi thường gặp → Test) cho nhất quán.
