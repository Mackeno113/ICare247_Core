// File    : IdentityPasswordHasher.cs
// Module  : Auth
// Layer   : Infrastructure
// Purpose : Cài đặt IPasswordHasher bằng PasswordHasher<T> của ASP.NET Core Identity (PBKDF2 v3).

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ICare247.Infrastructure.Auth;

/// <summary>
/// Bọc <see cref="PasswordHasher{TUser}"/> (PBKDF2 HMAC-SHA256). Verify đọc tham số nhúng
/// trong blob hash nên khớp hash đã seed bất kể cấu hình mặc định của runtime.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // PasswordHasher cần kiểu TUser nhưng không dùng tới instance — dùng object rỗng.
    private static readonly object Dummy = new();
    private readonly PasswordHasher<object> _inner = new();

    /// <inheritdoc />
    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    /// <inheritdoc />
    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(Dummy, hashedPassword, providedPassword);
        // Success hoặc SuccessRehashNeeded đều coi là đúng mật khẩu.
        return result != PasswordVerificationResult.Failed;
    }
}
