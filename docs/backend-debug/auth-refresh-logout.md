# Debug: Làm mới token (Refresh) & Đăng xuất (Logout)

> Xem [auth-login.md](auth-login.md) cho bối cảnh pipeline + cách đặt breakpoint chung.

---

## A. Refresh — `POST /api/v1/auth/refresh`

### Tóm tắt
Khi access token (JWT) hết hạn, FE dùng **refresh token** để xin cặp token mới mà không cần
đăng nhập lại. Áp dụng **rotation**: token cũ bị thu hồi, cấp token mới.

### Truyền cái gì
Header `X-Tenant-Id: 1`. Body:
```json
{ "refreshToken": "xK9..." }
```
Response 200: giống `/login` (`accessToken`, `refreshToken` mới, `user`).
Thất bại → **401** *"Phiên không hợp lệ"* (`AuthStatus.InvalidRefreshToken`).

### Code ở lớp nào
| Lớp | File |
|---|---|
| Api | `Controllers/AuthController.cs` → `Refresh` |
| Application | `Features/Auth/Refresh/RefreshTokenCommand.cs` + `RefreshTokenCommandHandler.cs` |
| Infrastructure | `Repositories/RefreshTokenRepository.cs`, `Auth/JwtTokenService.cs`, `Repositories/AuthRepository.cs` |

### Luồng
```
AuthController.Refresh → IMediator.Send(RefreshTokenCommand)
  └─ RefreshTokenCommandHandler.Handle
       ├─ JwtTokenService.HashRefreshToken(raw)  → SHA-256
       ├─ RefreshTokenRepository.GetByHashAsync ──► SELECT HT_RefreshToken
       │     • null / DaThuHoi / HetHan ≤ now → InvalidRefreshToken (401)
       ├─ AuthRepository.GetByIdAsync (user còn HoatDong?)
       │     • không hợp lệ → RevokeAsync(token cũ) → InvalidRefreshToken
       ├─ RefreshTokenRepository.RevokeAsync(token cũ)         ← rotation
       ├─ JwtTokenService.CreateAccessToken + CreateRefreshToken
       └─ RefreshTokenRepository.InsertAsync(token mới)
```

### Breakpoint
1. `RefreshTokenCommandHandler.Handle` — sau `GetByHashAsync`: `record` null? `DaThuHoi`? `HetHanUtc`?
2. `RefreshTokenRepository.RevokeAsync` — xác nhận token cũ bị set `DaThuHoi=1`.

### Lỗi thường gặp
- **401 ngay lần refresh đầu** → token đã bị rotate ở request trước (FE gọi refresh 2 lần song song),
  hoặc client gửi token gốc đã hết hạn. Kiểm `HetHanUtc` (mặc định 7 ngày, 30 ngày nếu RememberMe).
- **Hash không khớp** → token bị cắt/encode sai khi truyền; so `HashRefreshToken` 2 phía.

---

## B. Logout — `POST /api/v1/auth/logout`

### Tóm tắt
Thu hồi refresh token của phiên hiện tại. Access token (JWT) **không** thu hồi được — tự hết hạn
theo thời gian (stateless). FE xóa token cục bộ.

### Truyền cái gì
Header `X-Tenant-Id: 1`. Body: `{ "refreshToken": "xK9..." }`. Response: **204 No Content**
(idempotent — token không tồn tại vẫn trả 204).

### Code ở lớp nào
`AuthController.Logout` → `Features/Auth/Logout/LogoutCommand(+Handler)` → `RefreshTokenRepository`.

### Luồng
```
AuthController.Logout → IMediator.Send(LogoutCommand)
  └─ LogoutCommandHandler.Handle
       ├─ HashRefreshToken → GetByHashAsync
       └─ nếu còn & chưa thu hồi → RevokeAsync   → 204
FE: AuthService.LogoutAsync → TokenStore.ClearAsync + gỡ Bearer + NotifyAuthenticationChanged
    → MainLayout guard → Nav "/login"
```

### Breakpoint
`LogoutCommandHandler.Handle` — `record` tìm thấy không? (best-effort, không lỗi nếu null).

### Lưu ý
- Đăng xuất **không** vô hiệu access token đang còn hạn. Nếu cần "đá" mọi phiên ngay → gọi
  `RefreshTokenRepository.RevokeAllForUserAsync` (đã có sẵn, chưa expose endpoint).
