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
    /// Lấy danh sách bảng trong <c>Sys_Table</c> theo tenant để chọn FK <c>Table_Id</c>.
    /// </summary>
    Task<IReadOnlyList<TableLookupRecord>> GetTablesByTenantAsync(
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách cấu hình bảng Sys_Table theo tenant để hiển thị màn quản trị.
    /// </summary>
    Task<IReadOnlyList<SysTableRecord>> GetSysTablesAsync(
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
        int? tableId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra mã form đã tồn tại trong tenant hay chưa (chỉ tính record active).
    /// Truyền <paramref name="excludeFormId"/> &gt; 0 khi edit để loại trừ chính form đang sửa.
    /// </summary>
    Task<bool> ExistsFormCodeAsync(
        string formCode,
        int tenantId,
        int excludeFormId = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Tạo mới cấu hình bảng trong Sys_Table. Trả về Table_Id vừa tạo.
    /// </summary>
    Task<int> CreateSysTableAsync(
        string tableCode,
        string tableName,
        string schemaName,
        bool isTenant,
        int tenantId,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật cấu hình bảng Sys_Table theo Table_Id.
    /// </summary>
    Task UpdateSysTableAsync(
        int tableId,
        string tableCode,
        string tableName,
        string schemaName,
        bool isTenant,
        bool isActive,
        int tenantId,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách roles từ <c>Sys_Role</c> — bao gồm global (Tenant_Id IS NULL)
    /// và roles riêng của tenant. Dùng để build danh sách quyền trong FormEditor.
    /// </summary>
    Task<IReadOnlyList<RoleLookupRecord>> GetRolesAsync(
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Deep clone form: sao chép Ui_Form + Ui_Section + Ui_Field sang Form_Code mới.
    /// Chạy trong transaction. Trả về Form_Id mới.
    /// </summary>
    Task<int> CloneFormAsync(
        int sourceFormId,
        string newFormCode,
        string newFormName,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật metadata của form (Tab Thông tin trong FormEditor).
    /// Dùng optimistic concurrency qua <paramref name="currentVersion"/>.
    /// Trả về <c>true</c> nếu UPDATE thành công.
    /// Trả về <c>false</c> nếu version conflict (form đã được sửa từ nơi khác).
    /// </summary>
    Task<bool> UpdateFormMetadataAsync(
        int formId,
        string formCode,
        string formName,
        string platform,
        string layoutEngine,
        string? description,
        bool isActive,
        int? tableId,
        int currentVersion,
        CancellationToken ct = default);
}
