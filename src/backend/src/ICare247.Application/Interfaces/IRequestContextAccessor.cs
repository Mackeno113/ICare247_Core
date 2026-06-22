// File    : IRequestContextAccessor.cs
// Module  : Context
// Layer   : Application
// Purpose : Cấp giá trị THÔ của request hiện tại (claim JWT + HTTP header) cho ContextParamResolver,
//           không để Application/Infrastructure phụ thuộc trực tiếp HttpContext. Api implement (scoped).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Truy cập giá trị ngữ cảnh request: claim JWT (bất biến) + HTTP header. Dùng để resolve token
/// <c>Sys_Context_Param</c> (spec 19). Api cài qua <c>IHttpContextAccessor</c>.
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>NguoiDung_Id từ claim sub/NameIdentifier (0 nếu chưa đăng nhập).</summary>
    long UserId { get; }

    /// <summary>Giá trị claim theo tên (vd <c>sub</c>, <c>tenant</c>); null nếu không có.</summary>
    string? GetClaim(string name);

    /// <summary>Giá trị HTTP header theo tên (vd <c>X-Active-CongTy</c>, <c>X-Lang</c>); null nếu không có.</summary>
    string? GetHeader(string name);
}
