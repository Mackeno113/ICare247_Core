// File    : ILookupRepository.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Contract truy vấn Sys_Lookup — danh mục dùng chung (Gender, MaritalStatus,...).

using ICare247.Domain.Entities.Lookup;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Sys_Lookup</c>.
/// Cô lập tenant ở tầng connection (1 Config DB = 1 tenant, ADR-035).
/// </summary>
public interface ILookupRepository
{
    /// <summary>
    /// Lấy tất cả items của một lookup code, kèm label đã resolve theo ngôn ngữ.
    /// <paramref name="tenantId"/> KHÔNG dùng để lọc SQL — chỉ để dựng cache key (Redis L2 dùng chung).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetByCodeAsync(
        string lookupCode, int tenantId, string langCode = "vi",
        CancellationToken ct = default);
}
