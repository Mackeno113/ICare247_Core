// File    : RequirePermissionForTargetAttribute.cs
// Module  : Authorization
// Layer   : Api
// Purpose : Enforce quyền cho endpoint engine generic — mã đối tượng (form/view) lấy từ ROUTE.
//           [RequirePermissionForTarget("Form", PermissionOp.Sua, "formCode")].
//           Enforce-if-mapped (xem IPermissionService); SUPERADMIN bỏ qua; thiếu mã route → cho qua.

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ICare247.Api.Authorization;

/// <summary>Yêu cầu quyền <paramref name="op"/> trên đối tượng (form/view) lấy từ route key.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionForTargetAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string SuperAdminRole = "SUPERADMIN";

    private readonly string _targetType;
    private readonly PermissionOp _op;
    private readonly string _routeKey;

    /// <param name="targetType">'Form' hoặc 'View'.</param>
    /// <param name="op">Thao tác cần (Xem/Thêm/Sửa/Xóa/In).</param>
    /// <param name="routeKey">Tên tham số route chứa mã đối tượng (mặc định "formCode").</param>
    public RequirePermissionForTargetAttribute(string targetType, PermissionOp op, string routeKey = "formCode")
    {
        _targetType = targetType;
        _op = op;
        _routeKey = routeKey;
    }

    /// <summary>Chặn nếu thiếu quyền. Sự kiện theo sau: 401 chưa xác thực · 403 thiếu quyền · cho qua nếu chưa map.</summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (!long.TryParse(raw, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.FindAll("role").Any(c => string.Equals(c.Value, SuperAdminRole, StringComparison.OrdinalIgnoreCase)))
            return;

        var code = context.RouteData.Values.TryGetValue(_routeKey, out var v) ? v?.ToString() : null;
        if (string.IsNullOrWhiteSpace(code)) return; // không có mã đối tượng trong route → không enforce

        var svc = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var allowed = await svc.HasPermissionForTargetAsync(userId, _targetType, code, _op, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Không đủ quyền",
                Detail = $"Thiếu quyền '{_op}' trên {_targetType.ToLowerInvariant()} '{code}'."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
