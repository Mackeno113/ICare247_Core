// File    : JwtParser.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Giải mã payload JWT (phần giữa) thành danh sách Claim — không verify chữ ký
//           (chữ ký do backend kiểm; client chỉ đọc claim để hiển thị/điều hướng).

using System.Security.Claims;
using System.Text.Json;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Đọc claim từ access token (JWT) phía client. CHỈ để hiển thị tên / vai trò / hạn dùng —
/// mọi quyết định bảo mật thật vẫn do backend verify chữ ký.
/// </summary>
public static class JwtParser
{
    /// <summary>Tách danh sách Claim từ payload JWT. Trả rỗng nếu token không hợp lệ.</summary>
    public static IReadOnlyList<Claim> ParseClaims(string? jwt)
    {
        var claims = new List<Claim>();
        if (string.IsNullOrWhiteSpace(jwt)) return claims;

        var parts = jwt.Split('.');
        if (parts.Length < 2) return claims;

        try
        {
            var json = DecodeBase64Url(parts[1]);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                        claims.Add(new Claim(prop.Name, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(prop.Name, prop.Value.ToString()));
                }
            }
        }
        catch
        {
            // Token hỏng → coi như không có claim (chưa đăng nhập).
            return [];
        }

        return claims;
    }

    /// <summary>True nếu claim "exp" (Unix seconds) đã qua thời điểm hiện tại.</summary>
    public static bool IsExpired(IReadOnlyList<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (long.TryParse(exp, out var seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
        return false;
    }

    /// <summary>Giải base64url (bù padding) → chuỗi JSON UTF-8.</summary>
    private static string DecodeBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        var bytes = Convert.FromBase64String(s);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
