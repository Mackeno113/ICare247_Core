// File    : IFormDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn Ui_Form và Ui_Section, Ui_Field qua Dapper.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Truy vấn metadata form từ SQL Server. Implementation dùng Dapper.
/// Mọi method phải truyền tenantId để đảm bảo multi-tenant isolation.
/// </summary>
public interface IFormDataService
{
    /// <summary>
    /// Lấy danh sách tất cả form của tenant (bao gồm inactive nếu <paramref name="includeInactive"/>=true).
    /// </summary>
    Task<IReadOnlyList<FormRecord>> GetAllFormsAsync(
        int tenantId,
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>
    /// Tạo form mới. Trả về <c>Form_Id</c> vừa được insert.
    /// </summary>
    Task<int> CreateFormAsync(
        string formCode,
        string formName,
        string platform,
        int tenantId,
        CancellationToken ct = default);
}
