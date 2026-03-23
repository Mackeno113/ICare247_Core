// File    : ExceptionHandlingMiddleware.cs
// Module  : Api
// Layer   : Api
// Purpose : Global exception handler — catch unhandled exceptions → RFC 7807 ProblemDetails.

using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Middleware;

/// <summary>
/// Middleware bắt tất cả unhandled exception → trả ProblemDetails (RFC 7807).
/// Không swallow exception — log Error rồi trả response phù hợp.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>JSON options cho ProblemDetails response.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            // FluentValidation errors → 400 Bad Request
            _logger.LogWarning(ex, "Validation lỗi — Path={Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Type = "https://icare247.vn/errors/validation-failed",
                Title = "Dữ liệu không hợp lệ",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                Extensions = { ["correlationId"] = GetCorrelationId(context) }
            });
        }
        catch (KeyNotFoundException ex)
        {
            // Resource not found → 404
            _logger.LogWarning(ex, "Resource không tìm thấy — Path={Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Type = "https://icare247.vn/errors/not-found",
                Title = "Không tìm thấy tài nguyên",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = GetCorrelationId(context) }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Auth error → 403
            _logger.LogWarning(ex, "Truy cập bị từ chối — Path={Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Type = "https://icare247.vn/errors/forbidden",
                Title = "Không có quyền truy cập",
                Status = StatusCodes.Status403Forbidden,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = GetCorrelationId(context) }
            });
        }
        catch (Exception ex)
        {
            // Mọi exception khác → 500 Internal Server Error
            // ToDetail(): tóm tắt ngắn gọn — loại lỗi + message + dòng code, không có stack trace dài
            _logger.LogError("Lỗi không xử lý được — Path={Path}\n{Detail}",
                context.Request.Path, ex.ToDetail());
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Type = "https://icare247.vn/errors/internal",
                Title = "Lỗi hệ thống",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Đã xảy ra lỗi không mong đợi. Vui lòng thử lại sau.",
                Extensions = { ["correlationId"] = GetCorrelationId(context) }
            });
        }
    }

    /// <summary>Ghi ProblemDetails JSON vào response.</summary>
    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOpts));
    }

    /// <summary>
    /// Lấy Correlation-Id — ưu tiên từ HttpContext.Items (set bởi CorrelationMiddleware),
    /// fallback từ header nếu CorrelationMiddleware chưa chạy.
    /// </summary>
    private static string? GetCorrelationId(HttpContext context)
    {
        // Ưu tiên lấy từ Items (đã normalize bởi CorrelationMiddleware)
        if (context.Items.TryGetValue("CorrelationId", out var itemValue)
            && itemValue is string correlationId)
        {
            return correlationId;
        }

        // Fallback từ header
        return context.Request.Headers.TryGetValue("X-Correlation-Id", out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
