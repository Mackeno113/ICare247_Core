// File    : IRefreshTokenRepository.cs
// Module  : Auth
// Layer   : Application
// Purpose : Hợp đồng lưu trữ refresh token (HT_RefreshToken — Data DB) cho luồng làm mới JWT.

namespace ICare247.Application.Interfaces;

/// <summary>Bản ghi refresh token đang còn hiệu lực (chỉ các cột cần cho luồng refresh).</summary>
/// <param name="Id">Khóa chính HT_RefreshToken.</param>
/// <param name="NguoiDungId">Người dùng sở hữu token.</param>
/// <param name="HetHanUtc">Hạn dùng (UTC).</param>
/// <param name="DaThuHoi">Đã thu hồi hay chưa.</param>
public sealed record RefreshTokenRecord(long Id, long NguoiDungId, DateTime HetHanUtc, bool DaThuHoi);

/// <summary>
/// Repository quản lý vòng đời refresh token trong Data DB của tenant.
/// Token gốc KHÔNG lưu — chỉ lưu hash (SHA-256) để tra cứu.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Lưu 1 refresh token mới (đã hash). CreatedBy = chính người dùng.</summary>
    Task InsertAsync(long userId, string tokenHash, DateTime expiresAtUtc,
        string? ipAddress, string? device, CancellationToken ct = default);

    /// <summary>Tra refresh token theo hash. Trả null nếu không tồn tại.</summary>
    Task<RefreshTokenRecord?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Thu hồi 1 token theo Id. Sự kiện theo sau: token không dùng để refresh được nữa.</summary>
    Task RevokeAsync(long tokenId, CancellationToken ct = default);

    /// <summary>Thu hồi toàn bộ token còn hiệu lực của người dùng (đăng xuất mọi thiết bị).</summary>
    Task RevokeAllForUserAsync(long userId, CancellationToken ct = default);
}
