// File    : ISysLookupDataService.cs
// Module  : Core
// Layer   : Interfaces
// Purpose : Contract truy vấn Sys_Lookup từ WPF ConfigStudio.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Truy vấn danh mục dùng chung từ <c>Sys_Lookup</c>.
/// Label resolve theo ngôn ngữ từ <c>Sys_Resource</c>.
/// </summary>
public interface ISysLookupDataService
{
    /// <summary>
    /// Lấy danh sách items của một lookup code.
    /// Trả về rỗng nếu DB chưa cấu hình hoặc code không tồn tại.
    /// </summary>
    Task<IReadOnlyList<LookupItemRecord>> GetByCodeAsync(
        string lookupCode, string langCode = "vi",
        CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả lookup codes hiện có (dùng cho dropdown chọn lookupCode trong ConfigStudio).
    /// </summary>
    Task<IReadOnlyList<string>> GetAllCodesAsync(CancellationToken ct = default);
}
