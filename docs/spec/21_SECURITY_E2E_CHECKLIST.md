# Security E2E Checklist — Kiểm thử thực tế Tầng 1→5

**Doc:** 21_SECURITY_E2E_CHECKLIST
**Phiên bản:** 1.0
**Ngày:** 2026-06-25
**Spec gốc:** `20_SECURITY_HARDENING_SPEC.md` · commit `5a4e226`
**Mục đích:** Xác nhận các thay đổi bảo mật **chạy đúng trên app thật**, không chỉ build pass.
Đặc biệt bắt sớm rủi ro: khóa nhầm endpoint làm vỡ login, hoặc luồng cookie/refresh sai.

---

## 0. Chuẩn bị (BẮT BUỘC trước khi test)

1. **Dừng server + app WASM đang chạy** (có thay đổi dependency + frontend).
2. **Rebuild + chạy lại**:
   - Backend API: `dotnet run --project src/backend/src/ICare247.Api` (hoặc file .bat).
   - Frontend WASM: build + serve lại (đừng build khi server đang serve → lỗi SRI).
3. Base URL API (dev): **`https://localhost:7130`** · tenant dev: **`X-Tenant-Id: 1`**.
4. Công cụ: `curl` (dùng `-k` vì cert dev tự ký) hoặc Postman (Import → Raw text); trình duyệt + DevTools (F12).
5. Cần 1 tài khoản **SUPERADMIN** (vd `admin`) và lý tưởng thêm 1 tài khoản **thường** (để test gate quyền).

### Quy ước đọc
- Mỗi mục: **Lệnh** → **Kỳ vọng** → ô **Kết quả** (PASS/FAIL).
- Mục nào lệch kỳ vọng → ghi lại status + body, dừng và báo để chẩn.

### Lấy access token (dùng cho các test cần đăng nhập)
```bash
# Lưu cookie vào cookies.txt + xem body. Thay MAT_KHAU.
curl -k -c cookies.txt -i -X POST https://localhost:7130/api/v1/auth/login \
  -H "Content-Type: application/json" -H "X-Tenant-Id: 1" \
  -d '{"username":"admin","password":"MAT_KHAU","rememberMe":false}'
```
Copy chuỗi `accessToken` (eyJ...) trong body → dùng cho header `Authorization: Bearer <token>`.
Git Bash tự bắt: `TOKEN=$(curl -sk -c cookies.txt -X POST ... | jq -r .accessToken)`

---

## TẦNG 1 — Access Control

### 1.1 Deny-by-default: vô danh gọi endpoint cần auth → **401**
```bash
curl -k -i https://localhost:7130/api/v1/me -H "X-Tenant-Id: 1"
```
**Kỳ vọng:** `401 Unauthorized`. → ☐ PASS ☐ FAIL

### 1.2 i18n trước login vẫn mở → **200** (RỦI RO CAO NHẤT — nếu 401 là login vỡ)
```bash
curl -k -i "https://localhost:7130/api/v1/languages" -H "X-Tenant-Id: 1"
curl -k -i "https://localhost:7130/api/v1/resources/overlay?lang=vi" -H "X-Tenant-Id: 1"
```
**Kỳ vọng:** cả hai `200`. → ☐ PASS ☐ FAIL

### 1.3 Có token + tenant đúng → **200**
```bash
curl -k -i https://localhost:7130/api/v1/me -H "X-Tenant-Id: 1" -H "Authorization: Bearer <TOKEN>"
```
**Kỳ vọng:** `200` + thông tin user. → ☐ PASS ☐ FAIL

### 1.4 Chống nhảy tenant: token tenant 1 + `X-Tenant-Id: 2` → **403**
```bash
curl -k -i https://localhost:7130/api/v1/me -H "X-Tenant-Id: 2" -H "Authorization: Bearer <TOKEN>"
```
**Kỳ vọng:** `403` (tenant-mismatch). *Nếu dev 1-tenant chưa cấu hình Catalog → có thể `400` "thiếu tenant" — vẫn = bị chặn.* → ☐ PASS ☐ FAIL

### 1.5 Gate SUPERADMIN cho Form-config
```bash
curl -k -i "https://localhost:7130/api/v1/config/forms" -H "X-Tenant-Id: 1" -H "Authorization: Bearer <TOKEN>"
```
**Kỳ vọng:** token **SUPERADMIN** → `200`; token **thường** → `403`. → ☐ PASS ☐ FAIL

### 1.6 Runtime KHÔNG bị khóa nhầm: user có quyền mở form/data → **200**
```bash
curl -k -i "https://localhost:7130/api/v1/master-data/<MA_FORM>?page=1&pageSize=20" \
  -H "X-Tenant-Id: 1" -H "Authorization: Bearer <TOKEN>"
```
**Kỳ vọng:** `200` (thay `<MA_FORM>` = formCode thật user được cấp quyền). → ☐ PASS ☐ FAIL

---

## TẦNG 2 — Token & Session (cookie HttpOnly + RAM)

### 2.1 Login: cookie HttpOnly được set, body KHÔNG có refreshToken
Dùng lệnh login ở mục 0 (có `-c cookies.txt -i`).
**Kỳ vọng:**
- Header response có: `Set-Cookie: ic247.rt=...; path=/api/v1/auth; secure; httponly; samesite=lax`.
- Body JSON có `accessToken`, **KHÔNG có** `refreshToken`.
→ ☐ PASS ☐ FAIL

### 2.2 Refresh đọc từ cookie (không body) → **200** + token mới
```bash
curl -k -b cookies.txt -c cookies.txt -i -X POST https://localhost:7130/api/v1/auth/refresh -H "X-Tenant-Id: 1"
```
**Kỳ vọng:** `200` + `accessToken` mới + `Set-Cookie` mới (rotation). → ☐ PASS ☐ FAIL

### 2.3 Refresh KHÔNG cookie → **401**
```bash
curl -k -i -X POST https://localhost:7130/api/v1/auth/refresh -H "X-Tenant-Id: 1"
```
**Kỳ vọng:** `401`. → ☐ PASS ☐ FAIL

### 2.4 logout-all yêu cầu đăng nhập
```bash
curl -k -i -X POST https://localhost:7130/api/v1/auth/logout-all -H "X-Tenant-Id: 1"                       # vô danh
curl -k -i -X POST https://localhost:7130/api/v1/auth/logout-all -H "X-Tenant-Id: 1" -H "Authorization: Bearer <TOKEN>"  # có token
```
**Kỳ vọng:** vô danh → `401`; có token → `204`. → ☐ PASS ☐ FAIL

### 2.5 Sau logout-all, refresh bằng cookie cũ → **401** (mọi phiên đã thu hồi)
```bash
curl -k -b cookies.txt -i -X POST https://localhost:7130/api/v1/auth/refresh -H "X-Tenant-Id: 1"
```
**Kỳ vọng:** `401`. → ☐ PASS ☐ FAIL

### 2.6 (Trình duyệt) Access token RAM-only — KHÔNG ở localStorage
Đăng nhập trong app → DevTools (F12) → **Application**:
- **Local Storage**: KHÔNG có `ic247.accessToken` và `ic247.refreshToken`.
- **Cookies**: có `ic247.rt`, cột **HttpOnly = ✓**.
→ ☐ PASS ☐ FAIL

### 2.7 (Trình duyệt) Silent refresh giữ phiên qua F5
Đang đăng nhập, ở 1 trang nội bộ → **F5**.
**Kỳ vọng:** vẫn đăng nhập (không bị đẩy về login). **Network** tab thấy `POST /api/v1/auth/refresh → 200` lúc tải.
→ ☐ PASS ☐ FAIL

### 2.8 (Trình duyệt) Xóa cookie → F5 → về login
DevTools → Application → Cookies → xóa `ic247.rt` → **F5**.
**Kỳ vọng:** bị đẩy về `/login` (silent refresh `401`). → ☐ PASS ☐ FAIL

### 2.9 (Trình duyệt) Đăng xuất
Bấm Đăng xuất → về login; **F5** vẫn ở login; Cookies không còn `ic247.rt`.
→ ☐ PASS ☐ FAIL

---

## TẦNG 3 — HTTP Hardening

### 3.1 Security headers có mặt mọi response
```bash
curl -k -i "https://localhost:7130/api/v1/languages" -H "X-Tenant-Id: 1" | grep -iE \
  "x-content-type-options|x-frame-options|referrer-policy|content-security-policy|permissions-policy"
```
**Kỳ vọng:** thấy đủ:
`X-Content-Type-Options: nosniff` · `X-Frame-Options: DENY` · `Referrer-Policy: no-referrer` ·
`Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` · `Permissions-Policy: ...`
→ ☐ PASS ☐ FAIL

### 3.2 Rate limit /auth → **429**
```bash
for i in $(seq 1 12); do \
  curl -ks -o /dev/null -w "%{http_code}\n" -X POST https://localhost:7130/api/v1/auth/login \
  -H "Content-Type: application/json" -H "X-Tenant-Id: 1" \
  -d '{"username":"x","password":"y","rememberMe":false}'; done
```
**Kỳ vọng:** vài dòng đầu `401`, sau đó `429` (chạm 10/phút theo IP). → ☐ PASS ☐ FAIL

### 3.3 App KHÔNG bị rate-limit nhầm
Dùng app bình thường (đăng nhập, mở vài màn). **Kỳ vọng:** không gặp `429` (global 200/10s đủ rộng).
→ ☐ PASS ☐ FAIL

---

## TẦNG 4 — Input / Data (XSS)

> Cần có (hoặc tạo) 1 View có cột cấu hình **renderMode = link** và/hoặc **html**, với 1 dòng dữ liệu chứa payload.

### 4.1 Cột "link" chặn scheme `javascript:`
Dữ liệu cell = `javascript:alert(1)`.
**Kỳ vọng:** hiển thị dạng **text thường**, KHÔNG phải thẻ `<a>` click chạy script.
→ ☐ PASS ☐ FAIL

### 4.2 Cột "html" được sanitize
Dữ liệu cell = `<img src=x onerror=alert(1)>` hoặc `<script>alert(1)</script>`.
**Kỳ vọng:** payload bị **lọc sạch** (không có alert; `onerror`/`<script>` bị loại). Nội dung HTML an toàn (vd `<b>`) vẫn render.
→ ☐ PASS ☐ FAIL

---

## TẦNG 5 — Ops / Dependency

### 5.1 Không còn dependency có lỗ hổng
```bash
dotnet list src/backend/src/ICare247.Api/ICare247.Api.csproj package --vulnerable --include-transitive
dotnet list src/frontend/ICare247_UI/ICare247_UI.csproj package --vulnerable --include-transitive
```
**Kỳ vọng:** "has no vulnerable packages" cho cả hai. → ☐ PASS ☐ FAIL

### 5.2 Prod KHÔNG lộ stack trace
Gây 1 lỗi 500 bất kỳ (vd request làm proc lỗi). Xem body response.
**Kỳ vọng:** chỉ `{ title: "Lỗi hệ thống", detail: "Đã xảy ra lỗi không mong đợi...", correlationId }` —
KHÔNG có stack trace / exception message. (Chi tiết chỉ ở log server theo correlationId.)
→ ☐ PASS ☐ FAIL

### 5.3 Không có secret thật trong git
```bash
git ls-files | grep -i "local.json"        # rỗng = OK
grep -i "SecretKey" src/backend/src/ICare247.Api/appsettings.json   # = PLACEHOLDER...
```
**Kỳ vọng:** không file `.local.json` bị track; `SecretKey` committed = placeholder. → ☐ PASS ☐ FAIL

---

## Bảng tổng kết

| Test | Kỳ vọng | Kết quả |
|---|---|---|
| 1.1 /me vô danh | 401 | |
| 1.2 i18n trước login | 200 | |
| 1.3 /me token đúng | 200 | |
| 1.4 đổi X-Tenant-Id | 403 (hoặc 400) | |
| 1.5 /config/forms | SUPERADMIN 200 · thường 403 | |
| 1.6 master-data có quyền | 200 | |
| 2.1 login Set-Cookie | HttpOnly + no refreshToken in body | |
| 2.2 refresh cookie | 200 + rotation | |
| 2.3 refresh no cookie | 401 | |
| 2.4 logout-all | vô danh 401 · token 204 | |
| 2.5 refresh sau logout-all | 401 | |
| 2.6 localStorage | không có token | |
| 2.7 F5 silent refresh | giữ phiên + /refresh 200 | |
| 2.8 xóa cookie + F5 | về login | |
| 2.9 logout | về login, cookie mất | |
| 3.1 security headers | đủ 5 header | |
| 3.2 rate limit /auth | 429 sau ~10 lần | |
| 3.3 app không 429 nhầm | bình thường | |
| 4.1 link javascript: | render text | |
| 4.2 html sanitize | payload bị lọc | |
| 5.1 vulnerable scan | 0 vulnerable | |
| 5.2 prod 500 | không lộ stack trace | |
| 5.3 secret git | sạch | |

---

## Ghi chú
- **Chưa kiểm được trong dev 1-tenant:** test 1.4 đúng `403` cần Catalog đa tenant; dev fallback có thể ra `400` (vẫn = chặn).
- **Tầng 4** chỉ kiểm được nếu có View cấu hình cột link/html — nếu chưa có, tạo tạm 1 cột để thử rồi gỡ.
- **SEC3-4 (CSP cho app WASM)** KHÔNG nằm trong checklist này — thuộc host phục vụ frontend, làm khi dựng hosting prod.
