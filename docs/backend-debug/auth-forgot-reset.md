# Debug: Quên mật khẩu (Forgot) & Đặt lại mật khẩu (Reset)

> ⚠️ **Hai endpoint này hiện là STUB** — chưa tích hợp SMTP/kho token. Tài liệu mô tả hành vi
> hiện tại + chỗ TODO để nối thật sau (pha-email).

---

## A. Forgot — `POST /api/v1/auth/forgot-password`

### Tóm tắt
Người dùng nhập tên đăng nhập/email. Backend **luôn trả 200** (chống dò tài khoản). Hiện chỉ **log**,
chưa gửi mail.

### Truyền cái gì
Header `X-Tenant-Id: 1`. Body: `{ "usernameOrEmail": "admin" }`. Response 200:
```json
{ "message": "Nếu tài khoản tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi." }
```

### Code ở lớp nào
| Lớp | File |
|---|---|
| Api | `Controllers/AuthController.cs` → `ForgotPassword` |
| Application | `Features/Auth/ForgotPassword/ForgotPasswordCommand.cs` + Handler (**STUB**) |
| Frontend | `Pages/Auth/ForgotPassword.razor` → `IAuthService.RequestPasswordResetAsync` |

### Luồng (hiện tại)
```
AuthController.ForgotPassword → IMediator.Send(ForgotPasswordCommand)
  └─ ForgotPasswordCommandHandler.Handle → _logger.LogInformation(...)  → trả Unit
     (TODO: tra HT_NguoiDung theo email → sinh token → lưu DB → gửi email)
→ 200 message generic
```

### Breakpoint
`ForgotPasswordCommandHandler.Handle` — xác nhận có vào (log dòng "STUB, chưa gửi mail").

---

## B. Reset — `POST /api/v1/auth/reset-password`

### Tóm tắt
Đặt mật khẩu mới bằng token (từ email). Hiện **chưa có kho token** → handler trả `false` →
controller trả **501 Not Implemented**. FE hiển thị "đang hoàn thiện".

### Truyền cái gì
Header `X-Tenant-Id: 1`. Body: `{ "token": "...", "newPassword": "..." }`.
- Thành công (tương lai) → 200 `{ "message": "Đặt lại mật khẩu thành công." }`.
- Hiện tại → **501** ProblemDetails *"Chưa hỗ trợ"*.

> FE [`ResetPassword.razor`](../../src/frontend/ICare247_UI/Pages/Auth/ResetPassword.razor) đọc
> `token` từ query string `?token=...`, kiểm **checklist 4 điều kiện** mật khẩu phía client trước khi gửi.

### Code ở lớp nào
| Lớp | File |
|---|---|
| Api | `Controllers/AuthController.cs` → `ResetPassword` |
| Application | `Features/Auth/ResetPassword/ResetPasswordCommand.cs` + Handler (**STUB**, trả `false`) |
| Frontend | `Pages/Auth/ResetPassword.razor` (gọi thẳng HttpClient) |

### Luồng (hiện tại)
```
AuthController.ResetPassword → IMediator.Send(ResetPasswordCommand) → Handler trả false
  → StatusCode 501 ProblemDetails "Chưa hỗ trợ"
```

### TODO khi nối thật (pha-email)
1. Thêm cột email (hoặc lấy qua `NS_NhanVien`) + bảng/cột lưu **token reset** (hash + hết hạn).
2. `ForgotPassword`: sinh token → lưu → gửi email (service SMTP, cấu hình ngoài repo).
3. `ResetPassword`: verify token → `IPasswordHasher.Hash(newPassword)` → `UPDATE HT_NguoiDung.MatKhauHash`
   → `RefreshTokenRepository.RevokeAllForUserAsync` (đăng xuất mọi thiết bị).
4. Đổi `ResetPasswordCommandHandler` trả `true` + bỏ nhánh 501 ở controller.
