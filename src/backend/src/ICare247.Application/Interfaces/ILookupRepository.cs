// File    : ILookupRepository.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Contract truy vấn Sys_Lookup — danh mục dùng chung (Gender, MaritalStatus,...).

using ICare247.Domain.Entities.Lookup;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Sys_Lookup</c>.
/// Tenant_Id = 0 là global (dùng chung mọi tenant).
/// </summary>
public interface ILookupRepository
{
    /// <summary>
    /// Lấy tất cả items của một lookup code, kèm label đã resolve theo ngôn ngữ.
    /// Ưu tiên tenant riêng trước, fallback về global (Tenant_Id = 0).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetByCodeAsync(
        string lookupCode, int tenantId, string langCode = "vi",
        CancellationToken ct = default);
}
