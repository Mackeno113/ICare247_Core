// File    : IDependencyRepository.cs
// Module  : Validation
// Layer   : Application
// Purpose : Repository interface cho Sys_Dependency — đọc dependency graph để topological sort.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Sys_Dependency</c>.
/// Dùng để xác định thứ tự evaluate rules khi field tham chiếu field khác.
/// </summary>
public interface IDependencyRepository
{
    /// <summary>
    /// Lấy danh sách dependencies (active) của một form.
    /// Trả về list (SourceFieldCode, TargetFieldCode) — field nào phụ thuộc field nào.
    /// </summary>
    Task<IReadOnlyList<FieldDependency>> GetByFormAsync(
        int formId, int tenantId,
        CancellationToken ct = default);
}

/// <summary>
/// Quan hệ phụ thuộc giữa 2 fields: TargetFieldCode phụ thuộc SourceFieldCode.
/// Nghĩa là SourceFieldCode cần validate trước TargetFieldCode.
/// </summary>
/// <param name="SourceFieldCode">Field nguồn (được tham chiếu).</param>
/// <param name="TargetFieldCode">Field đích (phụ thuộc vào source).</param>
public sealed record FieldDependency(
    string SourceFieldCode,
    string TargetFieldCode);
