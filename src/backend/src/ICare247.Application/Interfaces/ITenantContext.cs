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
/// Implementation cho request scope. Chỉ populate qua <see cref="Assign"/> (setter private) —
/// TenantMiddleware gọi, các layer sau chỉ đọc. Các factory kết nối đọc connection string ở đây.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public int TenantId { get; private set; }

    /// <summary>
    /// Connection string Config DB của tenant — phân giải qua <see cref="ITenantConnectionResolver"/>.
    /// Factory đọc giá trị này để mở kết nối.
    /// </summary>
    public string ConfigConnectionString { get; private set; } = "";

    /// <summary>Connection string Data DB của tenant — tương tự ConfigConnectionString.</summary>
    public string DataConnectionString { get; private set; } = "";

    /// <summary>Connection string Audit DB của tenant (NK_*) — rỗng thì fallback Data DB.</summary>
    public string AuditConnectionString { get; private set; } = "";

    /// <summary>
    /// Gán context cho request TỪ MỘT NGUỒN DUY NHẤT (defense-in-depth cô lập tenant, AUDIT-3):
    /// <c>Tenant_Id</c> + cả 3 connection string luôn đến từ cùng một <see cref="TenantConnections"/>
    /// đã phân giải — không thể lệch (id tenant A + connection tenant B = rò rỉ chéo im lặng). Vì setter
    /// private, đây là đường DUY NHẤT populate; gán lại sẽ thay TOÀN BỘ field (không còn state cũ lẫn vào).
    /// Fallback Audit→Data gom về một chỗ (trước đây lặp ở middleware).
    /// </summary>
    /// <param name="connections">Cặp connection đã phân giải cho tenant của request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connections"/> null.</exception>
    /// <exception cref="ArgumentException"><c>TenantId</c> ≤ 0 (dữ liệu phân giải bất thường).</exception>
    public void Assign(TenantConnections connections)
    {
        ArgumentNullException.ThrowIfNull(connections);
        if (connections.TenantId <= 0)
            throw new ArgumentException(
                $"TenantConnections.TenantId không hợp lệ ({connections.TenantId}) — không gán context.",
                nameof(connections));

        TenantId               = connections.TenantId;
        ConfigConnectionString = connections.ConfigConnectionString;
        DataConnectionString   = connections.DataConnectionString;
        AuditConnectionString  = string.IsNullOrWhiteSpace(connections.AuditConnectionString)
            ? connections.DataConnectionString
            : connections.AuditConnectionString;
    }
}
