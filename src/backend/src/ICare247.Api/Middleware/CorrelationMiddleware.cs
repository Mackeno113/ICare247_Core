// File    : CorrelationMiddleware.cs
// Module  : Observability
// Layer   : Api
// Purpose : Extract hoặc generate X-Correlation-Id → đưa vào Serilog LogContext + response header.

using Serilog.Context;

namespace ICare247.Api.Middleware;

/// <summary>
/// Middleware xử lý Correlation-Id:
/// 1. Nếu client gửi header <c>X-Correlation-Id</c> → dùng giá trị đó
/// 2. Nếu không → tự generate GUID mới
/// 3. Đưa vào Serilog LogContext → tất cả log trong request đều có CorrelationId
/// 4. Trả về header <c>X-Correlation-Id</c> trong response
/// </summary>
public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Tên header chuẩn cho Correlation-Id.</summary>
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Lấy từ request header hoặc generate mới
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.FirstOrDefault())
                ? values.First()!
                : Guid.NewGuid().ToString("N");

        // Đưa vào HttpContext.Items để các layer khác dùng (ExceptionHandlingMiddleware, etc.)
        context.Items["CorrelationId"] = correlationId;

        // Trả header về cho client
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Push vào Serilog LogContext — tất cả log trong scope có CorrelationId
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
