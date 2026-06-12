// File    : TenantConnectionProtector.cs
// Module  : MultiTenancy
// Layer   : Infrastructure
// Purpose : Mã hóa / giải mã connection string lưu trong cột catalog (AES-GCM).
//           Key 256-bit (base64) nằm ở appsettings.local.json — KHÔNG commit. Xem ADR-018.

using System.Security.Cryptography;
using System.Text;

namespace ICare247.Infrastructure.MultiTenancy;

/// <summary>
/// Bảo vệ connection string trong catalog bằng AES-GCM. Định dạng lưu:
/// base64(nonce[12] ‖ tag[16] ‖ ciphertext). Không cấu hình key → coi như plaintext
/// (chế độ dev) để giai đoạn đầu không bắt buộc mã hóa.
/// </summary>
public sealed class TenantConnectionProtector
{
    private const int NonceSize = 12;   // 96-bit nonce — chuẩn cho AES-GCM
    private const int TagSize   = 16;   // 128-bit tag

    private readonly byte[]? _key;

    /// <summary>Khởi tạo với key base64 (32 byte sau decode). Null/rỗng → chế độ plaintext.</summary>
    /// <param name="base64Key">Key AES-256 mã hóa base64, hoặc null để tắt mã hóa.</param>
    public TenantConnectionProtector(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            _key = null;
            return;
        }

        var key = Convert.FromBase64String(base64Key);
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException(
                "Catalog:EncryptionKey phải là AES key 128/192/256-bit (16/24/32 byte sau base64).",
                nameof(base64Key));
        _key = key;
    }

    /// <summary>True nếu đang bật mã hóa (có key).</summary>
    public bool IsEnabled => _key is not null;

    /// <summary>
    /// Giải mã chuỗi từ catalog. Không có key → trả nguyên (plaintext dev).
    /// </summary>
    /// <param name="stored">Giá trị lưu trong cột (base64 hoặc plaintext).</param>
    /// <returns>Connection string gốc.</returns>
    public string Decrypt(string stored)
    {
        if (_key is null) return stored;

        var blob = Convert.FromBase64String(stored);
        var nonce  = blob.AsSpan(0, NonceSize);
        var tag    = blob.AsSpan(NonceSize, TagSize);
        var cipher = blob.AsSpan(NonceSize + TagSize);

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// Mã hóa connection string để ghi vào catalog (dùng cho công cụ provisioning tenant).
    /// Không có key → trả nguyên (plaintext dev).
    /// </summary>
    /// <param name="plainText">Connection string gốc.</param>
    /// <returns>Chuỗi base64 nonce‖tag‖cipher, hoặc plaintext nếu chưa cấu hình key.</returns>
    public string Encrypt(string plainText)
    {
        if (_key is null) return plainText;

        var plain  = Encoding.UTF8.GetBytes(plainText);
        var nonce  = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag    = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var blob = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, NonceSize);
        cipher.CopyTo(blob, NonceSize + TagSize);
        return Convert.ToBase64String(blob);
    }
}
