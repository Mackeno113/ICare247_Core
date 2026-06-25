# Security Hardening Spec — Nâng cấp bảo vệ tối đa (Tầng 1 → 5)

**Spec:** 20_SECURITY_HARDENING_SPEC
**Phiên bản:** 1.0 (kế hoạch — CHƯA code)
**Ngày:** 2026-06-24
**ADR liên quan:** ADR-023 (phân quyền/navigation) · ADR-018 (DB-per-tenant) · ADR-021 (DataProtection keyring) · ADR-014 (resource facade)
**Spec liên quan:** `15_AUTHZ_NAVIGATION_SPEC.md` · `18_SAVE_VALIDATION_HOOK_SPEC.md` · `07_API_CONTRACT.md`

---

## 0. Mục tiêu & phạm vi

Nâng cấp bảo mật toàn nền tảng ICare247 lên mức "bảo vệ tối đa" trước các kỹ thuật tấn công
hiện hành (OWASP Top 10 2021, các đòn phổ biến với SPA + REST API + multi-tenant). Tài liệu này
là **bản đồ thi công**: nêu hiện trạng (kèm bằng chứng `file:line`), mục tiêu, và công việc cụ thể
theo 5 tầng, kèm thứ tự ưu tiên và cách kiểm thử.

> **Bối cảnh kiến trúc:** Blazor WASM (chạy trong trình duyệt) + ASP.NET Core API + SQL Server
> (DB-per-tenant). Frontend và API **khác origin** (BaseUrl trỏ tới backend). Tenant phân giải qua
> subdomain HOẶC header `X-Tenant-Id`. Production dự kiến: **cùng domain, khác subdomain**
> (vd `app.icare247.vn` + `api.icare247.vn`). Hiện chỉ chạy localhost.

> **Nguyên tắc xuyên suốt — Deny by default + Defense in depth.** Mặc định từ chối; mở quyền
> tường minh. Mỗi lớp bảo vệ độc lập: lọc client là UX, **server enforce mới là bảo mật**. Code
> WASM tải nguyên về trình duyệt nên **mọi endpoint là công khai về mặt thông tin** — bảo mật
> KHÔNG được dựa vào việc giấu đường dẫn/cấu trúc request.

---

## 1. Hiện trạng — Những phần ĐÃ chắc (giữ nguyên, không sửa)

Ghi nhận để tránh "sửa lại đồ đang tốt" và để biết nền tảng đã có gì:

| Hạng mục | Tình trạng | Bằng chứng |
|---|---|---|
| Hash mật khẩu | ✅ PBKDF2 HMAC-SHA256 (Identity v3) | `IdentityPasswordHasher.cs` |
| JWT validation | ✅ verify Issuer/Audience/Lifetime/SigningKey; `MapInboundClaims=false`; `ClockSkew=2'` | `Program.cs:119` |
| JWT secret | ✅ bắt buộc ≥32 ký tự ở prod, lấy từ env/appsettings.local | `Program.cs:100` |
| Refresh token | ✅ chỉ lưu hash SHA-256 (không lưu raw), có rotate | `JwtTokenService.cs:93`, `RefreshTokenCommandHandler.cs:74` |
| Brute-force login | ✅ lockout 5 lần sai / 15 phút | `LoginCommandHandler.cs:20` |
| CORS | ✅ whitelist origin + `AllowCredentials`, KHÔNG `AllowAnyOrigin`; prod rỗng → fail-fast | `Program.cs:173` |
| SQL injection (query động) | ✅ tên bảng/cột qua regex allowlist `^[a-zA-Z_][a-zA-Z0-9_]*$` + `[bracket]` + giá trị parameterized | `MasterDataRepository.cs:28,452`, `ViewRepository.cs:32` |
| Over-posting / mass-assignment | ✅ allowlist cột theo cấu hình form, bỏ readonly + PK; audit cột bơm server-side | `MasterDataRepository.cs:452` |
| Validation server-side | ✅ required + Val_Rules (AST) + unique + hook store, trước khi ghi | `SaveMasterDataCommandHandler.cs:56`, `ValidationEngine.cs:61` |
| HTTPS redirect | ✅ `UseHttpsRedirection` | `Program.cs:233` |
| DataProtection keyring | ✅ chia sẻ cho scale-out (ADR-021) | `Program.cs:144` |
| Observability | ✅ CorrelationId mỗi dòng log; audit writer | `CorrelationMiddleware`, `AuditNkWriter.cs` |

---

## 2. Mô hình mối đe dọa (threat model)

| # | Kỹ thuật tấn công | Liên quan tầng | Hiện trạng |
|---|---|---|---|
| T1 | Broken Access Control — gọi thẳng API không đăng nhập | Tầng 1 | 🔴 HỞ |
| T2 | Cross-tenant / IDOR — đổi `X-Tenant-Id` đọc dữ liệu tenant khác | Tầng 1 | 🔴 HỞ |
| T3 | Thiếu phân quyền theo chức năng (đăng nhập = làm mọi thứ) | Tầng 1 | 🔴 HỞ |
| T4 | Token theft qua XSS (token ở localStorage) | Tầng 2/3 | 🟠 RỦI RO |
| T5 | Session không thu hồi được (đổi mật khẩu/khóa vẫn dùng token cũ) | Tầng 2 | 🟠 CẦN KIỂM |
| T6 | Clickjacking / MIME sniffing / thiếu CSP | Tầng 3 | 🔴 HỞ |
| T7 | DoS / scraping / credential stuffing (không rate-limit toàn cục) | Tầng 3 | 🟠 RỦI RO |
| T8 | SQL injection ở nguồn động `Sp`/`Sql` chưa rà hết | Tầng 4 | 🟡 CẦN KIỂM |
| T9 | Lộ secret (connection string / JWT key trong git) | Tầng 5 | 🟡 CẦN KIỂM |
| T10 | Supply chain — phụ thuộc NuGet có lỗ hổng | Tầng 5 | 🟡 CẦN KIỂM |
| T11 | DB account quyền quá rộng (db_owner) | Tầng 5 | 🟡 CẦN KIỂM |

---

## 3. TẦNG 1 — Access Control (🔴 ƯU TIÊN CAO NHẤT)

> Đây là OWASP #1 và là lỗ hổng thật sự nghiêm trọng nhất. Phải làm trước; các tầng sau (đặc
> biệt Tầng 2 cookie/token) chỉ có giá trị sau khi tầng này xong.

### 3.1 Hiện trạng (bằng chứng)
- `AddAuthorization()` **không có FallbackPolicy** → mặc định cho qua tất cả. `Program.cs:142`
- Controller THIẾU `[Authorize]` (gọi được vô danh):
  - `MasterDataController` — **CRUD đầy đủ** GET/POST/PUT/DELETE. `MasterDataController.cs:27,87,104,120`
  - `ViewController` — đọc dữ liệu lưới công khai (chỉ `my-layout` có `[Authorize]`). `ViewController.cs:24,212`
  - `RuntimeController` — validate/handle-event công khai. `RuntimeController.cs:20`
  - `FormController`, `LookupController` — công khai.
  - `ResourceController`, `LanguageController` — công khai (CẦN xác nhận có cố ý không — i18n/ngôn ngữ có thể cho phép anonymous).
- Tenant lấy từ header `X-Tenant-Id` (client tự gửi) — `TenantMiddleware`, client gửi tại `ICare247_UI/Program.cs:38`. Vì không có auth → đổi header là **nhảy tenant**.
- Phân quyền hiện tốt nhất chỉ là "đã đăng nhập?", chưa kiểm 5 cờ quyền (Xem/Thêm/Sửa/Xóa/Duyệt) theo ADR-023.

### 3.2 Mục tiêu
1. **Deny-by-default toàn API.** Mọi endpoint yêu cầu đăng nhập, trừ allowlist tường minh.
2. **Tenant ràng buộc vào token.** Tenant hiệu lực = claim trong JWT (server ký), KHÔNG tin header. Lệch giữa tenant-token và tenant-subdomain → 403.
3. **Phân quyền theo chức năng (ADR-023).** Map quyền Xem/Thêm/Sửa/Xóa/Duyệt vào endpoint.
4. **Chống IDOR.** Mọi truy vấn theo `{id}` kèm điều kiện tenant + quyền sở hữu ở tầng WHERE.

### 3.3 Công việc
| # | Việc | File ảnh hưởng |
|---|---|---|
| 1.1 | Thêm `FallbackPolicy = RequireAuthenticatedUser()` | `Program.cs:142` |
| 1.2 | Gắn `[AllowAnonymous]` tường minh cho endpoint công khai hợp lệ (auth, health, có thể language/i18n) — rà từng controller | các `*Controller.cs` |
| 1.3 | Phát hành tenant_id trong JWT (claim), backend lấy tenant từ token sau khi đã đăng nhập | `JwtTokenService.cs`, `LoginCommandHandler.cs` |
| 1.4 | `TenantMiddleware`: với request đã xác thực, so khớp tenant-token vs tenant-subdomain/header → lệch = 403 | `TenantMiddleware.cs` |
| 1.5 | Authorization handler/policy theo `HT_ChucNang` + 5 cờ quyền; áp `[Authorize(Policy=...)]` per endpoint | mới + các controller |
| 1.6 | Rà mọi truy vấn `{id}`: thêm điều kiện tenant trong WHERE (không chỉ lọc tầng app) | các repository |

### 3.4 Kiểm thử
- Gọi mọi endpoint **không token** → kỳ vọng `401` (trừ allowlist).
- Đăng nhập tenant A, đổi `X-Tenant-Id` sang B → kỳ vọng `403` (không đọc được dữ liệu B).
- Tài khoản chỉ có quyền "Xem" gọi POST/PUT/DELETE → kỳ vọng `403`.
- Gõ thẳng URL endpoint admin bằng tài khoản thường → `403`.

---

## 4. TẦNG 2 — Token & Session

### 4.1 Hiện trạng
- Access + refresh token lưu `localStorage` → JS đọc được, dễ bị XSS lấy. `TokenStore.cs:16,46`
- Access token sống dài: 480' (Prod) / 1440' (Dev). `appsettings.json:24`, `appsettings.Development.json:10`
- Refresh token: 7 ngày (thường) / 30 ngày (RememberMe). `LoginCommandHandler.cs:26,29`
- **Chưa có auto-refresh / xử lý 401** ở client (không có DelegatingHandler). Token gắn cứng qua `DefaultRequestHeaders`. `AuthService.cs:108`
- MFA/2FA mới là stub (`TwoFactorRequired`). `AuthController.cs:113`
- Thu hồi token khi đổi mật khẩu/khóa tài khoản — CẦN xác minh.

### 4.2 Mục tiêu (= "Hướng 3" đã phân tích — kịch bản cùng domain khác subdomain)
1. **Refresh token → cookie `HttpOnly` + `Secure` + `SameSite=Lax`** (JS không đọc được). Vì app/API same-site (cùng `icare247.vn`, kể cả `localhost` khác port), `SameSite=Lax` đủ — KHÔNG cần `SameSite=None`.
2. **Access token chỉ ở RAM** (biến C#, không persist) + **silent refresh lúc khởi động** (gọi `/auth/refresh`, cookie tự đính kèm).
3. **DelegatingHandler bắt 401 → refresh → retry**, có single-flight (nhiều request 401 cùng lúc chỉ refresh 1 lần).
4. **Thu hồi tức thì:** đổi mật khẩu / khóa tài khoản / logout → vô hiệu mọi refresh token đang sống ("revoke all sessions").
5. (Tùy chọn) **MFA/2FA** cho tài khoản admin.

### 4.3 Công việc
| # | Việc | File ảnh hưởng |
|---|---|---|
| 2.1 | Login/Refresh: set refresh token qua `Set-Cookie` (HttpOnly/Secure/SameSite=Lax/Path=`/api/v1/auth`); bỏ refreshToken khỏi JSON body | `AuthController.cs:130` |
| 2.2 | Refresh/Logout endpoint: đọc refresh token từ **Cookie** | `AuthController.cs:53,63` |
| 2.3 | Config cookie theo môi trường (Domain: rỗng ở dev / quyết định scope multi-tenant ở prod) | `appsettings*.json` |
| 2.4 | Frontend: bật `SetBrowserRequestCredentials(Include)` cho request auth | `ICare247_UI/Program.cs:30`, `AuthService.cs` |
| 2.5 | `TokenStore`: bỏ localStorage, access token chỉ RAM | `TokenStore.cs` |
| 2.6 | Silent refresh lúc boot + cổng loading | `AuthService.InitializeAsync` |
| 2.7 | DelegatingHandler 401→refresh→retry (single-flight) | mới |
| 2.8 | Revoke-all-sessions khi đổi mật khẩu/khóa | `RefreshTokenRepository.cs`, các handler liên quan |
| 2.9 | CSRF: dựa `SameSite=Lax` + header tùy biến `X-Tenant-Id` (đã có) + cân nhắc anti-forgery token tường minh | `AuthController.cs` |

> **Quyết định cần chốt:** API prod là **1 host chung** (`api.icare247.vn`) hay **per-tenant**? →
> 1 host chung: nên cookie **host-only** trên host API (KHÔNG `Domain=.icare247.vn`) để tránh rải
> cookie sang mọi subdomain tenant. Per-tenant: cookie host-only tự tách bạch.

### 4.4 Kiểm thử
- Đăng nhập → kiểm `Set-Cookie` có `HttpOnly; Secure; SameSite=Lax`; JSON body KHÔNG còn refreshToken.
- `document.cookie` trong console → KHÔNG thấy refresh token.
- F5 reload → silent refresh thành công, vẫn đăng nhập; refresh fail → về login.
- Đổi mật khẩu → token/refresh cũ bị từ chối ngay.

---

## 5. TẦNG 3 — Hardening HTTP / Transport

### 5.1 Hiện trạng
- **Không có security headers** (CSP, X-Content-Type-Options, frame-ancestors, Referrer-Policy, Permissions-Policy).
- **Không có HSTS** (chỉ `UseHttpsRedirection`). `Program.cs:233`
- **Không có rate limiting toàn cục** (chỉ lockout login).
- **Chưa giới hạn kích thước request body** tường minh.

### 5.2 Mục tiêu & công việc
| # | Việc | Ghi chú |
|---|---|---|
| 3.1 | Middleware security headers: `Content-Security-Policy` (giảm XSS — phòng thủ chính cho token), `X-Content-Type-Options: nosniff`, `X-Frame-Options`/`frame-ancestors` (clickjacking), `Referrer-Policy`, `Permissions-Policy` | thêm middleware, đặt sớm trong pipeline |
| 3.2 | `app.UseHsts()` ở prod | `Program.cs` |
| 3.3 | `AddRateLimiter` (.NET built-in): giới hạn theo IP/đường dẫn — chống DoS/scraping/credential-stuffing | toàn API + riêng `/auth/*` chặt hơn |
| 3.4 | Giới hạn body size + request timeout | Kestrel limits |

### 5.3 Kiểm thử
- Response header có đủ CSP/nosniff/frame-ancestors/Referrer-Policy.
- Nhúng app trong `<iframe>` lạ → bị chặn.
- Bắn N request/giây vượt ngưỡng → `429 Too Many Requests`.

---

## 6. TẦNG 4 — Đầu vào & Dữ liệu

### 6.1 Hiện trạng
- `ValidationBehavior` (MediatR) có tồn tại — **độ phủ command chưa rõ**. `ValidationBehavior.cs`
- Validation save MasterData: ✅ đã server-side (xem mục 1).
- Dynamic SQL nguồn `Table`/`View`: ✅ đã rà (allowlist + parameterized). Nguồn `Sp`/`Sql`: 🟡 **CHƯA rà hết** (`LookupRepository`, `DynamicLookupRepository`, `ViewRepository` panel lọc).
- Output encoding: Blazor tự encode — cần rà chỗ dùng `MarkupString` / JS interop `innerHTML`.

### 6.2 Công việc
| # | Việc |
|---|---|
| 4.1 | Quét toàn bộ command/query: đảm bảo có validator (FluentValidation) phủ input bắt buộc/định dạng |
| 4.2 | Rà dynamic SQL nguồn `Sp`/`Sql`: chỉ chạy proc/định nghĩa từ config tin cậy, tham số luôn parameterized, không nối chuỗi từ input người dùng |
| 4.3 | Rà mọi `MarkupString` / `IJSRuntime.Invoke...innerHTML` trên frontend — bảo đảm dữ liệu đã encode/sanitize |
| 4.4 | Chuẩn hóa upload (nếu có): whitelist phần mở rộng/MIME, giới hạn dung lượng, lưu ngoài webroot |

### 6.3 Kiểm thử
- Gửi payload có ký tự injection vào field text/lookup → không thực thi, bị từ chối/escape.
- Lưu chuỗi `<script>` → hiển thị lại dạng text, không thực thi.

---

## 7. TẦNG 5 — Vận hành & Giám sát

### 7.1 Công việc
| # | Việc | Ghi chú |
|---|---|---|
| 5.1 | Audit log: đảm bảo log mọi hành động nhạy cảm + truy cập admin; **KHÔNG** log mật khẩu/token/secret | `AuditNkWriter.cs`; rà các chỗ `LogInformation` không in token |
| 5.2 | Quét phụ thuộc: `dotnet list package --vulnerable` định kỳ; pin version; cập nhật bản vá | CI/script |
| 5.3 | Quản lý secret: connection string + JWT key KHÔNG nằm trong file commit git (dùng env / user-secrets / vault); kiểm lịch sử git | `appsettings*.json`, `.gitignore` |
| 5.4 | Phân quyền DB: account app quyền tối thiểu (KHÔNG `db_owner`); tách account cho migration/deploy hook | cấu hình SQL Server |
| 5.5 | Cấu hình lỗi: prod KHÔNG lộ stack trace; ProblemDetails chỉ trả "Mã lỗi" + correlationId | `ExceptionHandlingMiddleware` |

### 7.2 Kiểm thử
- `dotnet list package --vulnerable` → không còn lỗ hổng High/Critical chưa vá.
- `git log -p` / quét repo → không có secret thật.
- Gây lỗi 500 ở prod → response không lộ stack trace, chỉ có mã lỗi + correlationId.

---

## 8. Thứ tự thi công đề xuất

```
Tầng 1 (Access Control)  ──►  CẤP BÁCH, làm trước, phạm vi gọn, chặn phần lớn rủi ro
   │
   ├─ Tầng 3 (security headers + HSTS + rate limit)  ──►  quick wins, độc lập, làm song song được
   │
   ▼
Tầng 2 (cookie/RAM token + revoke + MFA)  ──►  chỉ có giá trị sau khi Tầng 1 xong; cần chốt topology API
   │
   ▼
Tầng 4 (rà validation/SQL động/MarkupString)  ──►  rà soát + vá điểm
   │
   ▼
Tầng 5 (audit/secret/dependency/DB least-privilege)  ──►  vận hành, làm dần
```

**Quyết định cần chốt trước khi vào Tầng 2:** API prod 1 host chung hay per-tenant (ảnh hưởng scope cookie).

---

## 9. Bảng theo dõi tiến độ

| Tầng | Hạng mục | Trạng thái |
|---|---|---|
| 1 | FallbackPolicy + AllowAnonymous allowlist | ☐ Chưa |
| 1 | Tenant ràng buộc vào JWT + middleware so khớp | ☐ Chưa |
| 1 | Phân quyền theo chức năng (ADR-023) | ☐ Chưa |
| 1 | Chống IDOR (WHERE tenant) | ☐ Chưa |
| 2 | Refresh token → cookie HttpOnly | ☐ Chưa |
| 2 | Access token RAM + silent refresh + 401 handler | ☐ Chưa |
| 2 | Revoke-all-sessions | ☐ Chưa |
| 2 | MFA/2FA (tùy chọn) | ☐ Chưa |
| 3 | Security headers + CSP | ☐ Chưa |
| 3 | HSTS | ☐ Chưa |
| 3 | Rate limiting toàn cục | ☐ Chưa |
| 4 | Phủ validator command/query | ☐ Chưa |
| 4 | Rà SQL động Sp/Sql + MarkupString | ☐ Chưa |
| 5 | Audit/secret/dependency/DB least-privilege | ☐ Chưa |

> Cập nhật cột trạng thái khi xử lý từng hạng mục (☐ Chưa → 🔴 Đang làm → ✅ Xong).
