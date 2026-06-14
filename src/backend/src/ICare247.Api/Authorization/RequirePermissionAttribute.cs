// File    : RequirePermissionAttribute.cs
// Module  : Authorization
// Layer   : Api
// Purpose : Filter enforce quyền theo chức năng (HT_ChucNang.Ma) + thao tác, deny-by-default.
//           Gắn lên action/controller: [RequirePermission("administration.permissions", PermissionOp.Sua)].
//           userId suy từ claim sub; vai trò SUPERADMIN được bỏ qua kiểm (an toàn không khóa quản trị).

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ICare247.Api.Authorization;

/// <summary>Yêu cầu user có <paramref name="op"/> trên chức năng <c>funcCode</c> mới cho qua.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string SuperAdminRole = "SUPERADMIN";

    private readonly string _funcCode;
    private readonly PermissionOp _op;

    public RequirePermissionAttribute(string funcCode, PermissionOp op)
    {
        _funcCode = funcCode;
        _op = op;
    }

    /// <summary>Chặn request nếu thiếu quyền. Sự kiện theo sau: 401 nếu chưa xác thực, 403 nếu thiếu quyền.</summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (!long.TryParse(raw, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // SUPERADMIN: bỏ qua kiểm (claim role tên "role" — JWT phát theo mã vai trò).
        if (user.FindAll("role").Any(c => string.Equals(c.Value, SuperAdminRole, StringComparison.OrdinalIgnoreCase)))
            return;

        var svc = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var allowed = await svc.HasPermissionAsync(userId, _funcCode, _op, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Không đủ quyền",
                Detail = $"Thiếu quyền '{_op}' trên chức năng '{_funcCode}'."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
