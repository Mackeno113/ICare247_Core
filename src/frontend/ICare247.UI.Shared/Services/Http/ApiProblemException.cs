// File    : ApiProblemException.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Exception mang metadata RFC 7807 đã bóc tách (status, code ổn định, detail, correlationId).
//           Dùng thay HttpRequestException ở *ApiService để call-site có thể map `code` → thông báo i18n
//           (xem ApiErrorLocalizer) thay vì chỉ hiện chuỗi thô của backend.

namespace ICare247.UI.Shared.Services.Http;

/// <summary>
/// Lỗi HTTP từ API kèm thông tin ProblemDetails đã parse. Là con của <see cref="Exception"/> nên vẫn
/// rơi vào các <c>catch (Exception)</c> hiện có (tương thích ngược); call-site biết <see cref="Code"/>
/// thì localize, không thì dùng <see cref="Exception.Message"/> như cũ.
/// </summary>
public sealed class ApiProblemException : Exception
{
    /// <summary>Mã trạng thái HTTP (vd 500, 404).</summary>
    public int StatusCode { get; }

    /// <summary>Mã lỗi ổn định từ backend (ProblemDetails.code) — null nếu không có.</summary>
    public string? Code { get; }

    /// <summary>CorrelationId để người dùng báo lỗi / dev grep log.</summary>
    public string? CorrelationId { get; }

    /// <summary>Thông điệp chi tiết (ProblemDetails.detail) — base để localize hoặc hiển thị trực tiếp.</summary>
    public string Detail { get; }

    public ApiProblemException(int statusCode, string detail, string? code, string? correlationId)
        : base($"[{statusCode}] {detail}")
    {
        StatusCode    = statusCode;
        Detail        = detail;
        Code          = code;
        CorrelationId = correlationId;
    }
}
