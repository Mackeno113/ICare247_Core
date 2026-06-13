// File    : IPasswordHasher.cs
// Module  : Auth
// Layer   : Application
// Purpose : Hợp đồng băm/verify mật khẩu — bọc PasswordHasher của ASP.NET Core Identity (PBKDF2 v3).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Băm và xác minh mật khẩu theo định dạng PBKDF2 (ASP.NET Core Identity v3).
/// Tham số (PRF, iteration, salt) nhúng trong blob hash nên verify đúng bất kể cấu hình runtime.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Băm 1 mật khẩu thô thành blob lưu DB.</summary>
    string Hash(string password);

    /// <summary>
    /// Xác minh mật khẩu thô với blob hash đã lưu. True nếu khớp.
    /// </summary>
    bool Verify(string hashedPassword, string providedPassword);
}
