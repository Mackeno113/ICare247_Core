# Debug: Đăng nhập (Login)

> Trang **mẫu** — chi tiết nhất, các tính năng khác copy bố cục này.

## 1. Tóm tắt

Người dùng nhập **tên đăng nhập + mật khẩu** ở màn Login → backend verify mật khẩu (PBKDF2) với
`HT_NguoiDung` trong **Live DB**, kiểm trạng thái/khóa/hết hạn → cấp **access token (JWT)** +
**refresh token** → FE lưu token, gắn header `Bearer`, vào Dashboard.

---

## 2. Gọi API nào

| | |
|---|---|
| **Method + URL** | `POST /api/v1/auth/login` |
| **Auth** | `[AllowAnonymous]` (chưa cần token) |
| **Header bắt buộc** | `X-Tenant-Id: 1` (TenantMiddleware cần để chọn DB) |
| **Controller** | [`AuthController.Login`](../../src/backend/src/ICare247.Api/Controllers/AuthController.cs) |

---

## 3. Truyền cái gì

**Request body** (JSON, camelCase):
```json
{ "username": "admin", "password": "Admin@12345", "rememberMe": true }
```
Ràng buộc ([`LoginCommandValidator`](../../src/backend/src/ICare247.Application/Features/Auth/Login/LoginCommandValidator.cs)):
`username` ≤ 100, `password` ≤ 256, `tenantId` > 0 (lấy từ header, không nằm trong body).

**Response 200** (thành công):
```json
{
  "accessToken": "eyJ...",       // JWT, mặc định sống 480 phút
  "refreshToken": "xK9...",      // opaque, dùng để /refresh
  "tokenType": "Bearer",
  "expiresIn": 28800,            // giây
  "user": { "id": 1, "username": "admin", "isAdmin": true,
            "defaultCompanyId": null, "roles": ["SUPERADMIN"], "mustChangePassword": false }
}
```

**Response lỗi** (ProblemDetails) — map ở `AuthController.MapFailure`:

| Tình huống | HTTP | Title |
|---|---|---|
| Sai tài khoản/mật khẩu | 401 | Đăng nhập thất bại |
| Khóa tạm (sai ≥ 5 lần) | 423 | Tài khoản tạm khóa |
| `TrangThai ≠ HoatDong` | 403 | Tài khoản ngừng hoạt động |
| Hết hạn tài khoản | 403 | Tài khoản hết hạn |
| Có 2FA (chưa hỗ trợ) | 401 | Cần xác thực 2 bước |

---

## 4. Code viết ở lớp nào

| Lớp | File | Vai trò |
|---|---|---|
| **Frontend** | `src/frontend/ICare247_UI/Pages/Auth/Login.razor` | Form + gọi `IAuthService.LoginAsync` |
| Frontend | `ICare247.UI.Shared/Services/Auth/AuthService.cs` | POST API, lưu token, gắn Bearer |
| **Api** | `Controllers/AuthController.cs` → `Login` | Nhận body → tạo `LoginCommand` → `IMediator.Send` |
| **Application** | `Features/Auth/Login/LoginCommand.cs` | Command (input) |
| Application | `Features/Auth/Login/LoginCommandHandler.cs` | ★ Toàn bộ logic đăng nhập |
| Application | `Features/Auth/AuthResult.cs` | Kết quả + enum `AuthStatus` |
| Application | `Interfaces/IAuthRepository.cs`, `IJwtTokenService.cs`, `IPasswordHasher.cs`, `IRefreshTokenRepository.cs` | Hợp đồng |
| **Infrastructure** | `Repositories/AuthRepository.cs` | Đọc/ghi `HT_NguoiDung` (Dapper, Live DB) |
| Infrastructure | `Auth/IdentityPasswordHasher.cs` | Verify PBKDF2 (Identity v3) |
| Infrastructure | `Auth/JwtTokenService.cs` | Ký JWT + sinh refresh token |
| Infrastructure | `Repositories/RefreshTokenRepository.cs` | Lưu `HT_RefreshToken` |
| **Domain** | `Entities/Auth/NguoiDung.cs` | Entity người dùng |

---

## 5. Luồng đi ra sao

```
Login.razor (HandleLogin)
  └─ AuthService.LoginAsync(username, password, rememberMe)
       └─ POST api/v1/auth/login  [header X-Tenant-Id:1]   ───────────────►
                                                                            │
  ── PIPELINE ──────────────────────────────────────────────────────────  │
  ExceptionHandling → Correlation → SerilogLogging → CORS                   │
  → TenantMiddleware: X-Tenant-Id=1 → resolver → TenantContext.DataConn =   │
    LiveData (ICare247_Solution)                                            │
  → Auth (AllowAnonymous, bỏ qua)                                           ▼
  AuthController.Login
     ├─ GetTenantId() = 1, GetClientIp(), GetUserAgent()
     ├─ new LoginCommand(username, password, 1, rememberMe, ip, ua)
     └─ IMediator.Send(cmd)
          ├─ ValidationBehavior → LoginCommandValidator   (rớt input → 400)
          └─ LoginCommandHandler.Handle
               ├─ AuthRepository.GetByUsernameAsync ──► SELECT HT_NguoiDung (Live DB)
               │     • null / không phải Local / chưa có hash → InvalidCredentials
               ├─ KhoaDenKhi > now?           → Locked
               ├─ TrangThai ≠ HoatDong?       → Disabled
               ├─ HetHanTaiKhoan < now?       → Expired
               ├─ IdentityPasswordHasher.Verify(hash, password)
               │     • sai → RecordLoginFailureAsync (đếm, ≥5 → KhoaDenKhi) → Invalid/Locked
               ├─ HinhThuc2FA ≠ None?         → TwoFactorRequired
               └─ THÀNH CÔNG:
                    ├─ RecordLoginSuccessAsync (reset đếm, set LanDangNhapCuoi)
                    ├─ GetRoleCodesAsync (HT_NguoiDung_VaiTro ⋈ HT_VaiTro)
                    ├─ JwtTokenService.CreateAccessToken  (claims: sub, unique_name, tenant, admin, role…)
                    ├─ JwtTokenService.CreateRefreshToken (opaque + hash SHA-256)
                    └─ RefreshTokenRepository.InsertAsync ──► INSERT HT_RefreshToken
     └─ Success → 200 { accessToken, refreshToken, user… }   ◄──────────────
  AuthService:
     ├─ TokenStore.SetAsync(access, refresh)  → localStorage
     ├─ gắn Http.DefaultRequestHeaders.Authorization = Bearer access
     └─ JwtAuthenticationStateProvider.NotifyAuthenticationChanged()
  MainLayout (guard) thấy đã đăng nhập → render shell; Login.razor → Nav "/"
```

---

## 6. Đặt breakpoint ở đâu

1. `AuthController.Login` — dòng tạo `LoginCommand`: xem `body.Username`, `GetTenantId()`.
2. `TenantMiddleware.InvokeAsync` — sau `ResolveTenantAsync`: kiểm `tenant.DataConnectionString`
   trỏ `Database=ICare247_Solution`.
3. `LoginCommandHandler.Handle` — đặt ở **đầu** rồi F10 từng nhánh để biết rớt ở đâu.
4. `AuthRepository.GetByUsernameAsync` — soi `sql` + `user` trả về (null? hash?).
5. `IdentityPasswordHasher.Verify` — `result` = `Success`/`Failed`.
6. `JwtTokenService.CreateAccessToken` — xem `claims` trước khi ký.

---

## 7. Lỗi thường gặp (riêng login)

- **Verify luôn Failed dù mật khẩu đúng** → đang đọc **sai DB** (Demo thay vì LiveData), hoặc cột
  `MatKhauHash` rỗng/định dạng không phải Identity v3. Kiểm breakpoint #2 + #4.
- **400 thiếu tenant** → quên header `X-Tenant-Id` khi test bằng Scalar/curl.
- **Khóa hoài** → `SoLanDangNhapSai`/`KhoaDenKhi` còn trong DB: `UPDATE dbo.HT_NguoiDung SET
  SoLanDangNhapSai=0, KhoaDenKhi=NULL WHERE TenDangNhap='admin'`.
- **Token nhận được nhưng request sau vẫn 401** → FE chưa gắn `Authorization` (xem `AuthService.ApplyAuthHeader`).

---

## 8. Test nhanh (REST Client)

Tạo file `*.http` (VS / VS Code REST Client):
```http
@host = https://localhost:7130

### Đăng nhập
POST {{host}}/api/v1/auth/login
Content-Type: application/json
X-Tenant-Id: 1

{ "username": "admin", "password": "Admin@12345", "rememberMe": true }
```
Hoặc Scalar UI: `https://localhost:7130/scalar` → `auth/login` → thêm header `X-Tenant-Id: 1`.
