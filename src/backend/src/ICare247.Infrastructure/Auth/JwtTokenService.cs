// File    : JwtTokenService.cs
// Module  : Auth
// Layer   : Infrastructure
// Purpose : Cấp access token (JWT HMAC-SHA256) + refresh token (opaque ngẫu nhiên) từ cấu hình Jwt:*.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ICare247.Infrastructure.Auth;

/// <summary>
/// Sinh JWT cho access token (ký HMAC-SHA256 bằng <c>Jwt:SecretKey</c>) và refresh token dạng
/// chuỗi ngẫu nhiên 256-bit. Issuer/Audience/hạn dùng đọc từ section <c>Jwt</c>.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _secretBytes;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        _issuer = jwt["Issuer"] ?? "icare247";
        _audience = jwt["Audience"] ?? "icare247-api";
        var secret = jwt["SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SecretKey chưa cấu hình an toàn (rỗng hoặc < 32 ký tự). " +
                "Đặt key thật qua appsettings.local.json.");
        _secretBytes = Encoding.UTF8.GetBytes(secret);
        _expirationMinutes = int.TryParse(jwt["ExpirationMinutes"], out var m) && m > 0 ? m : 480;
    }

    /// <inheritdoc />
    public AccessToken CreateAccessToken(TokenSubject subject)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_expirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, subject.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("tenant", subject.TenantId.ToString()),
            new("admin", subject.IsAdmin ? "true" : "false")
        };

        if (subject.DefaultCompanyId is { } companyId)
            claims.Add(new Claim("company", companyId.ToString()));

        // Role claim dùng tên ngắn "role" (gọn payload). Backend chưa dùng [Authorize(Roles)];
        // khi cần, đặt TokenValidationParameters.RoleClaimType = "role".
        foreach (var role in subject.Roles)
            claims.Add(new Claim("role", role));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(_secretBytes), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresIn = (int)(expires - now).TotalSeconds;
        return new AccessToken(jwt, expires, expiresIn);
    }

    /// <inheritdoc />
    public RefreshTokenValue CreateRefreshToken()
    {
        var raw = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncode(raw);
        var hash = HashRefreshToken(token);
        // Hạn lưu trong DB do handler quyết định; ở đây trả thời điểm mặc định để record đầy đủ.
        return new RefreshTokenValue(token, hash, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Mã hóa base64url (an toàn cho URL/JSON, bỏ padding).</summary>
    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
