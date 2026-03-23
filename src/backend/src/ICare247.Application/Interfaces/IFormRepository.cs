// File    : IFormRepository.cs
// Module  : Form
// Layer   : Application
// Purpose : Repository interface cho bảng Ui_Form — CRUD + clone + soft delete.

using ICare247.Domain.Entities.Form;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho bảng <c>Ui_Form</c> và các bảng liên quan (Section, Field, Event).
/// Tất cả query phải có <c>tenantId</c> (resolve qua Sys_Table.Tenant_Id).
/// </summary>
public interface IFormRepository
{
    /// <summary>
    /// Lấy danh sách form có phân trang và filter.
    /// </summary>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="platform">Filter theo platform (null = tất cả).</param>
    /// <param name="tableId">Filter theo Table_Id (null = tất cả).</param>
    /// <param name="isActive">Filter theo Is_Active (null = tất cả).</param>
    /// <param name="searchText">Tìm theo Form_Code (LIKE).</param>
    /// <param name="page">Trang hiện tại (1-based).</param>
    /// <param name="pageSize">Số record mỗi trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple: danh sách form + tổng số record.</returns>
    Task<(IReadOnlyList<FormListItem> Items, int TotalCount)> GetListAsync(
        int tenantId,
        string? platform = null,
        int? tableId = null,
        bool? isActive = null,
        string? searchText = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy form metadata đầy đủ (aggregate root) theo Form_Code.
    /// Bao gồm Sections + Fields đã sắp xếp theo Order_No.
    /// </summary>
    /// <param name="langCode">Mã ngôn ngữ để resolve Label từ Sys_Resource (mặc định "vi").</param>
    Task<FormMetadata?> GetByCodeAsync(
        string formCode, int tenantId, string langCode = "vi", CancellationToken ct = default);

    /// <summary>
    /// Lấy form metadata đầy đủ theo Form_Id.
    /// </summary>
    Task<FormMetadata?> GetByIdAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra Form_Code đã tồn tại trong tenant chưa.
    /// </summary>
    Task<bool> ExistsCodeAsync(string formCode, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Tạo form mới. Trả về Form_Id vừa tạo.
    /// </summary>
    Task<int> CreateAsync(FormCreateParams form, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật form. Tự động Version++ và recalc Checksum.
    /// </summary>
    Task UpdateAsync(FormUpdateParams form, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Set Is_Active theo Form_Id (dùng chung cho Deactivate + Restore).
    /// </summary>
    Task SetActiveAsync(int formId, bool isActive, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Set Is_Active theo Form_Code (dùng cho Restore khi form đã inactive).
    /// </summary>
    Task SetActiveByCodeAsync(string formCode, bool isActive, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Nhân bản form sang Form_Code mới (transaction: copy Form → Sections → Fields → Events).
    /// Trả về Form_Id mới.
    /// </summary>
    Task<int> CloneAsync(int sourceFormId, string newFormCode, int tenantId, CancellationToken ct = default);
}

/// <summary>DTO cho danh sách form (không load đầy đủ aggregate).</summary>
public sealed class FormListItem
{
    public int FormId { get; init; }
    public string FormCode { get; init; } = string.Empty;
    public string FormName { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public int TableId { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public int SectionCount { get; init; }
    public int FieldCount { get; init; }
    public string? Checksum { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Params tạo form mới.</summary>
public sealed class FormCreateParams
{
    public string FormCode { get; init; } = string.Empty;
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string LayoutEngine { get; init; } = "Grid";
    public string? Description { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

/// <summary>Params cập nhật form.</summary>
public sealed class FormUpdateParams
{
    public int FormId { get; init; }
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string LayoutEngine { get; init; } = "Grid";
    public string? Description { get; init; }
    public string UpdatedBy { get; init; } = string.Empty;
}
