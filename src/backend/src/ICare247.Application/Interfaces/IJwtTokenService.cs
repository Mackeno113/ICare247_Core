// File    : IJwtTokenService.cs
// Module  : Auth
// Layer   : Application
// Purpose : Hợp đồng cấp access token (JWT) + refresh token (opaque) cho luồng đăng nhập.

namespace ICare247.Application.Interfaces;

/// <summary>Thông tin định danh người dùng để nhúng vào access token.</summary>
/// <param name="UserId">HT_NguoiDung.Id.</param>
/// <param name="Username">Tên đăng nhập.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
/// <param name="IsAdmin">Cờ quản trị toàn quyền.</param>
/// <param name="DefaultCompanyId">Công ty mặc định (phạm vi dữ liệu) — có thể null.</param>
/// <param name="Roles">Danh sách mã vai trò.</param>
public sealed record TokenSubject(
    long UserId,
    string Username,
    int TenantId,
    bool IsAdmin,
    long? DefaultCompanyId,
    IReadOnlyList<string> Roles);

/// <summary>Access token đã ký kèm thời điểm hết hạn.</summary>
/// <param name="Token">Chuỗi JWT.</param>
/// <param name="ExpiresAtUtc">Thời điểm hết hạn (UTC).</param>
/// <param name="ExpiresInSeconds">Số giây còn hiệu lực tính từ lúc cấp.</param>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds);

/// <summary>Refresh token: chuỗi gốc trả cho client + hash để lưu DB + hạn dùng.</summary>
/// <param name="Token">Chuỗi gốc (trả cho client, KHÔNG lưu DB).</param>
/// <param name="TokenHash">Hash SHA-256 để lưu/tra cứu trong DB.</param>
/// <param name="ExpiresAtUtc">Thời điểm hết hạn (UTC).</param>
public sealed record RefreshTokenValue(string Token, string TokenHash, DateTime ExpiresAtUtc);

/// <summary>
/// Dịch vụ cấp token cho xác thực. Access token = JWT ký HMAC-SHA256 từ <c>Jwt:SecretKey</c>;
/// refresh token = chuỗi ngẫu nhiên (opaque), chỉ lưu hash.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Tạo access token (JWT) cho người dùng.</summary>
    AccessToken CreateAccessToken(TokenSubject subject);

    /// <summary>Tạo refresh token ngẫu nhiên mới (kèm hash + hạn dùng).</summary>
    RefreshTokenValue CreateRefreshToken();

    /// <summary>Tính hash SHA-256 của 1 refresh token gốc (để tra cứu khi client gửi lên).</summary>
    string HashRefreshToken(string rawToken);
}
