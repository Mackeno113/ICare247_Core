// File    : SecurityHeadersMiddleware.cs
// Module  : Security
// Layer   : Api
// Purpose : SEC3-1 (spec 20) — gắn security header cho mọi response của API: chống MIME sniffing,
//           chống đóng khung (clickjacking), giới hạn referrer/permissions, CSP chặt cho JSON.
//           LƯU Ý: CSP chống XSS cho APP nằm ở host phục vụ WASM (web.config/nginx), KHÔNG ở API này.

namespace ICare247.Api.Middleware;

/// <summary>
/// Đặt các header bảo mật chuẩn lên response. API chỉ trả JSON nên CSP để <c>default-src 'none'</c>;
/// bỏ qua CSP cho trang tài liệu dev (Scalar/OpenAPI) vì chúng cần tải script/style.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Path tài liệu dev không áp CSP chặt (Scalar UI + OpenAPI cần script/style/inline).</summary>
    private static readonly string[] DocPaths = ["/scalar", "/openapi"];

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Gắn header trước khi response bắt đầu. Sự kiện theo sau: client nhận response đã có header bảo mật.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Chống trình duyệt đoán kiểu nội dung khác Content-Type (giảm XSS qua MIME confusion).
        headers["X-Content-Type-Options"] = "nosniff";
        // Chống nhúng response vào iframe (clickjacking).
        headers["X-Frame-Options"] = "DENY";
        // Không gửi URL (có thể chứa thông tin) sang origin khác.
        headers["Referrer-Policy"] = "no-referrer";
        // Tắt các API trình duyệt nhạy cảm — API không cần.
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=()";

        // CSP chặt cho response JSON; bỏ qua cho trang tài liệu dev.
        var path = context.Request.Path.Value ?? string.Empty;
        var isDocPage = DocPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (!isDocPage)
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        return _next(context);
    }
}
