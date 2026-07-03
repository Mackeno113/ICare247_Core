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

> Cập nhật 2026-06-25 (E2E Tầng 1 xác minh). Chi tiết thi công từng hạng mục ở **§9.1** (chuyển
> từ TASKS.md session 72 để gom về nguồn canonical). TASKS.md chỉ giữ các mục **còn mở**.

| Tầng | Hạng mục | Trạng thái |
|---|---|---|
| 1 | FallbackPolicy + AllowAnonymous allowlist (SEC1-1/1-2) | ✅ Xong |
| 1 | Tenant ràng buộc vào JWT + middleware so khớp (SEC1-3) | ✅ Xong |
| 1 | Phân quyền theo chức năng (ADR-023) (SEC1-4) | ✅ Xong (còn TODO tinh chỉnh Lookup-insert) |
| 1 | Chống IDOR (WHERE tenant) (SEC1-5) | 🟡 Một phần (DB-per-tenant + SEC1-3; row-ownership cần rà) |
| 2 | Refresh token → cookie HttpOnly (SEC2-1) | ✅ Xong (E2E chờ) |
| 2 | Access token RAM + silent refresh + 401 handler (SEC2-2) | ✅ Xong (E2E chờ) |
| 2 | Revoke-all-sessions (SEC2-3) | ✅ Xong |
| 2 | MFA/2FA (tùy chọn) (SEC2-4) | ☐ Chưa (tùy chọn) |
| 3 | Security headers + CSP (SEC3-1) | ✅ Xong (E2E chờ) |
| 3 | HSTS (SEC3-2) | ✅ Xong |
| 3 | Rate limiting toàn cục (SEC3-3) | ✅ Xong (E2E chờ); ⏳ prod: UseForwardedHeaders |
| 3 | CSP chống XSS ở host WASM (SEC3-4) | ☐ Chưa (chờ hosting prod) |
| 4 | Phủ validator command/query (SEC4-1) | 🟡 Một phần (6/39; đường ghi trọng yếu đã an toàn) |
| 4 | Rà SQL động Sp/Sql + MarkupString + sanitize HTML (SEC4-2/4-3) | ✅ Xong (đã rà + vá) |
| 5 | Log không lộ secret (SEC5-1) | ✅ Xong |
| 5 | Dependency vá + secret/git + prod stacktrace (SEC5-2) | ✅ Xong |
| 5 | DB least-privilege (SEC5-2) | ☐ Chưa (deployment, không code-fixable) |

> Trạng thái: ☐ Chưa · 🔴 Đang làm · 🟡 Một phần · ✅ Xong.

### 9.1 Chi tiết thi công (chuyển từ TASKS.md — session 2026-06-24/25)

**TẦNG 1 — Access Control (E2E xác minh 2026-06-25):**
- **SEC1-1** — `FallbackPolicy = RequireAuthenticatedUser()` (deny-by-default) — `Program.cs` (AddAuthorization). Build BE 0/0.
- **SEC1-2** — `[AllowAnonymous]` cho endpoint công khai hợp lệ: `LanguageController` (class), `ResourceController.Get`+`GetOverlay`, `/health`, OpenAPI/Scalar (dev). Auth đã có sẵn `[AllowAnonymous]`.
- **SEC1-3** — `TenantClaimGuardMiddleware` (mới) so khớp claim `tenant` (JWT, đã có sẵn ở `JwtTokenService:52`) vs `TenantContext` → lệch = 403. Đặt sau `UseAuthentication`.
- **SEC1-4** — Phân quyền theo chức năng. Quyết định: `Ma` (`HT_ChucNang`) do code TỰ SINH (`form.X`/`view.X`/`group.X`); quyền theo đối tượng dùng cột `LoaiDoiTuong`+`DoiTuong` (KHÔNG dùng `Ma`) → không seed funcCode tay.
  - Màn dữ liệu (MasterData/View-data/Runtime/Form-GetByCode): GIỮ `[RequirePermissionForTarget]` (enforce-if-mapped — chỉ chặn khi admin đã map menu cho form/view đó).
  - Form-config (`FormController` GetList/audit/create/update/deactivate/restore/clone/invalidate-cache) + `ViewController` GetList/invalidate-cache → gắn `[Authorize(Roles="SUPERADMIN")]` (việc builder). Build BE 0/0.
  - Lookup đọc (GET/query/invalidate) + insert → mức "chỉ cần đăng nhập" (qua fallback).
  - ⏳ **TODO tinh chỉnh Lookup-insert** (rủi ro thấp — xem comment `TODO(SEC1-4)` tại `LookupController.Insert`): resolve `Source_Name`→form→`HasPermissionForTargetAsync("Form",formCode,Them)`. Chốt: form nào khi bảng có 0/nhiều form; ngữ nghĩa quyền.
- **Kiểm thử Tầng 1 (E2E)** — verify qua curl/Postman 2026-06-25: vô danh `/me`→401, i18n trước login→200, token đúng→200, gate SUPERADMIN/tenant OK. User xác nhận "mọi thứ ổn".

**TẦNG 2 — Token & Session (build BE+FE 0/0):**
- **SEC2-1 (Step 1)** — Refresh token → cookie `HttpOnly/Secure/SameSite=Lax/Path=/api/v1/auth` (Domain config-driven, mặc định host-only). BE: `AuthController` set/đọc/xóa cookie; login bỏ `refreshToken` khỏi JSON; refresh/logout đọc TỪ COOKIE; `AuthResult.RefreshExpiresAtUtc` (hạn cookie). FE: `AuthService` gửi `credentials:Include`; `TokenStore` chỉ giữ access + dọn key `ic247.refreshToken` cũ. Class `[AllowAnonymous]` → chuyển xuống từng method.
- **SEC2-3** — `logout-all` endpoint (yêu cầu auth) + `LogoutAllCommand/Handler` dùng `RevokeAllForUserAsync`. KHÔNG hook auto-lockout (tránh DoS force-logout). Wire vào reset-password để TODO khi reset hết stub.
- **SEC2-2 (Step 2)** — Access token RAM-only (`TokenStore` bỏ localStorage + dọn key cũ). `TokenRefresher` (client bare riêng, single-flight + dedup) gọi /refresh bằng cookie. `RefreshTokenHandler` (DelegatingHandler) tự đính Bearer từ TokenStore + 401 (trừ /auth/*) → refresh → retry 1 lần (clone request). `AuthService.InitializeAsync` → silent refresh; bỏ `ApplyAuthHeader`. Host `Program.cs` wiring 2 client. Gate boot tận dụng `MainLayout._authChecked`.

**TẦNG 3 — Hardening HTTP/Transport:**
- **SEC3-1** — `SecurityHeadersMiddleware` (mới): `X-Content-Type-Options:nosniff`, `X-Frame-Options:DENY`, `Referrer-Policy:no-referrer`, `Permissions-Policy`, CSP `default-src 'none'; frame-ancestors 'none'` (bỏ qua CSP cho /scalar,/openapi dev). Đặt sau ExceptionHandling.
- **SEC3-2** — `UseHsts()` khi `!IsDevelopment`.
- **SEC3-3** — `AddRateLimiter`: global per-IP 200/10s (bỏ qua /health) + policy `auth` 10/phút cho `/auth/*` (`[EnableRateLimiting("auth")]` trên AuthController); 429 ProblemDetails + Retry-After. ⏳ TODO(prod): `UseForwardedHeaders` để partition theo IP thật sau reverse proxy.

**TẦNG 4 — Đầu vào & Dữ liệu (đã rà + vá):**
- **SEC4-2 (SQL động)** — Rà xong: table mode (identifier allowlist + param), custom_sql/FilterSql/OrderBy (config builder-authored, trusted + blocklist DDL/DML + value param), Sp/Sql (`ValidateSpName` + param chỉ từ whitelist `view.Filters` + context param bind server-side). **AN TOÀN — không cần vá.**
- **SEC4-2 (XSS/MarkupString)** — Rà mọi `MarkupString`: `HighlightLabel` đã HtmlEncode; i18n template (controlled). **VÁ: `DataView` cột link/image** — thêm `IsSafeUrl` chặn `javascript:`/`data:`/`vbscript:`.
- **SEC4-3** — `DataView` renderMode `"html"`: thêm **HtmlSanitizer (Ganss.Xss) 8.2.871** (9.x chỉ target net8, WASM từ chối) → sanitize trước khi render `(MarkupString)`. (Minor backlog: `MenuBuilderPage` MarkupString tên node admin — admin-only.)

**CPM — Central Package Management (2026-06-25):**
- `Directory.Packages.props` (root): 48 `PackageVersion`; 18 project bỏ `Version=` khỏi `PackageReference`. `CentralPackageFloatingVersionsEnabled` giữ floating. Hợp nhất xung đột: `Microsoft.Data.SqlClient`→6.1.4, `Serilog.Sinks.File`→7.0.0. Restore+build 18/18 sạch.
- **Lỗ hổng phát lộ qua CPM**: HtmlSanitizer float 9.0.873 (GHSA-j92c-7v7g-gj3f) → pin **9.0.892** (bản vá + còn netstandard2.0 cho WASM).

**TẦNG 5 — Vận hành & Giám sát (đã rà + vá):**
- **SEC5-1** — Log KHÔNG lộ secret: rà toàn bộ `Log*` — không chèn password/token/secret value (chỉ userId/username/path/correlationId). ✓
- **SEC5-2 (dependency)** — `dotnet list package --vulnerable`: vá HtmlSanitizer (9.0.892), **OpenTelemetry.Api→1.15.3** (GHSA-g94r-2vxg-569j), **System.Security.Cryptography.Xml→8.0.3** (High GHSA-37gx-xxp4-5rgx) qua transitive pinning. Re-scan: **0 vulnerable**.
- **SEC5-2 (secret/git)** — `.gitignore` loại `*.local.json`/`secrets.json`/`appsettings.Production.json`; appsettings.json committed = placeholder. ✓
- **SEC5-2 (prod stack trace)** — `ExceptionHandlingMiddleware`: 500 trả message generic + correlationId; stack trace chỉ vào log server. ✓

### 9.2 Còn mở (theo dõi ở TASKS.md)
- **SEC1-5** — Row-ownership trong cùng tenant (rà thêm).
- **SEC2-4** — MFA/2FA cho admin (tùy chọn).
- **SEC2/3 E2E** — Kiểm thử Tầng 2 Step 1 + Tầng 3 (cookie HttpOnly, 429 rate limit).
- **SEC3-4** — CSP host WASM (chờ hosting prod).
- **SEC4-1** — Bổ sung validator cho các command còn lại (robustness).
- **SEC5-2 (DB least-privilege)** — deployment: account app KHÔNG `db_owner`/`sysadmin`; tách account migration khỏi runtime.
