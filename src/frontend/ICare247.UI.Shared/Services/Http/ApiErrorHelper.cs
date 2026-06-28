// File    : ApiErrorHelper.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Tiện ích chung bóc "Mã lỗi" (correlationId) từ body ProblemDetails của backend rồi
//           gắn vào thông báo lỗi cho người dùng. Mục tiêu: khi khách báo lỗi 500, họ đọc được
//           mã định danh → dev grep thẳng log server (logs/icare247-*.log) ra đúng request.

using System.Text.Json;

namespace ICare247.UI.Shared.Services.Http;

/// <summary>
/// Helper dùng chung cho mọi *ApiService: trích <c>correlationId</c> trong body lỗi
/// (RFC 7807 ProblemDetails — backend đặt ở ROOT do <c>[JsonExtensionData]</c>) và nối "Mã lỗi"
/// vào thông điệp hiển thị. Tách riêng để các service không lặp code parse.
/// </summary>
public static class ApiErrorHelper
{
    /// <summary>Số ký tự đầu của correlationId hiển thị cho người dùng (đủ để grep, gọn để đọc).</summary>
    private const int ShortLength = 8;

    /// <summary>
    /// Bóc <c>correlationId</c> từ body lỗi JSON (ProblemDetails). Trả null nếu body rỗng,
    /// không phải JSON, hoặc không có trường. Sự kiện theo sau: caller nối mã vào thông báo.
    /// </summary>
    /// <param name="body">Nội dung response lỗi (chuỗi thô đã đọc từ HttpResponseMessage).</param>
    public static string? ExtractCorrelationId(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            // ProblemDetails.Extensions dùng [JsonExtensionData] → correlationId nằm ở ROOT.
            if (doc.RootElement.TryGetProperty("correlationId", out var c)
                && c.ValueKind == JsonValueKind.String)
                return c.GetString();
        }
        catch { /* body không phải JSON hợp lệ → coi như không có mã */ }
        return null;
    }

    /// <summary>
    /// Bóc <c>code</c> (mã lỗi ổn định backend đặt qua <c>[JsonExtensionData]</c> → ở ROOT) từ body lỗi.
    /// Trả null nếu không có. Sự kiện theo sau: caller map code → thông báo i18n.
    /// </summary>
    public static string? ExtractCode(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (doc.RootElement.TryGetProperty("code", out var c)
                && c.ValueKind == JsonValueKind.String)
                return c.GetString();
        }
        catch { /* body không phải JSON hợp lệ → coi như không có code */ }
        return null;
    }

    /// <summary>
    /// Nối hậu tố "(Mã lỗi: …)" vào <paramref name="message"/> nếu <paramref name="correlationId"/>
    /// có giá trị; ngược lại trả nguyên message. Mã rút gọn <see cref="ShortLength"/> ký tự đầu —
    /// vẫn grep được vì log server giữ mã đầy đủ.
    /// </summary>
    /// <param name="message">Thông điệp lỗi thân thiện đã dựng (có thể đã i18n).</param>
    /// <param name="correlationId">Mã định danh request lấy từ <see cref="ExtractCorrelationId"/>.</param>
    public static string WithErrorCode(string message, string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return message;
        var code = correlationId.Length > ShortLength ? correlationId[..ShortLength] : correlationId;
        return $"{message} (Mã lỗi: {code})";
    }

    /// <summary>
    /// Tiện ích gộp: bóc mã từ <paramref name="body"/> rồi nối vào <paramref name="message"/> trong
    /// 1 bước (cho call-site đã có sẵn cả message lẫn body thô).
    /// </summary>
    public static string WithErrorCodeFromBody(string message, string? body)
        => WithErrorCode(message, ExtractCorrelationId(body));
}
