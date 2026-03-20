// File    : ITenantContext.cs
// Module  : Multi-Tenant
// Layer   : Application
// Purpose : Interface cung cấp Tenant_Id cho toàn bộ request scope — inject vào handlers/repositories.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Context chứa Tenant_Id cho request hiện tại.
/// Được set bởi TenantMiddleware, inject vào handlers/repositories qua DI (scoped).
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Tenant_Id hiện tại. Luôn > 0 sau khi qua TenantMiddleware.
    /// </summary>
    int TenantId { get; }
}

/// <summary>
/// Mutable implementation — TenantMiddleware set giá trị, các layer sau chỉ đọc qua <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public int TenantId { get; set; }
}
