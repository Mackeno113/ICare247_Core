// File    : IMasterDataRepository.cs
// Module  : MasterData
// Layer   : Application
// Purpose : CRUD generic, metadata-driven cho dữ liệu nghiệp vụ của 1 danh mục.
//           Bảng đích đọc từ Ui_Form → Sys_Table ở server (KHÔNG nhận từ client).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository CRUD dữ liệu danh mục (Master Data) — sinh động từ metadata.
/// </summary>
/// <remarks>
/// Mọi thao tác:
/// <list type="bullet">
///   <item>Đọc bảng đích từ <c>Ui_Form.Table_Id → Sys_Table</c> (Schema_Name + Table_Code).</item>
///   <item>Verify tenant qua Sys_Table.Tenant_Id.</item>
///   <item>Identifier validate regex; giá trị luôn truyền qua Dapper params (chống injection).</item>
/// </list>
/// </remarks>
public interface IMasterDataRepository
{
    /// <summary>
    /// Lấy thông tin bảng đích + danh sách cột (cho list/form) theo Form_Code.
    /// Trả null nếu form không tồn tại trong tenant.
    /// </summary>
    Task<MasterDataFormInfo?> GetFormInfoAsync(
        string formCode, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách bản ghi (chỉ cột Show_In_List + PK), có search + active filter + paging.
    /// </summary>
    Task<MasterDataListResult> GetListAsync(
        string formCode, int tenantId,
        string? search = null, bool? activeOnly = null,
        int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>Lấy đầy đủ 1 bản ghi theo giá trị PK (cho form Sửa).</summary>
    Task<IDictionary<string, object?>?> GetByIdAsync(
        string formCode, int tenantId, object id, CancellationToken ct = default);

    /// <summary>
    /// Insert 1 bản ghi. Trả về giá trị PK mới (OUTPUT INSERTED).
    /// Chỉ nhận cột thuộc Ui_Field của form (lọc cột lạ).
    /// </summary>
    Task<object?> InsertAsync(
        string formCode, int tenantId, Dictionary<string, object?> values,
        CancellationToken ct = default);

    /// <summary>Update 1 bản ghi theo PK. Trả số dòng bị ảnh hưởng.</summary>
    Task<int> UpdateAsync(
        string formCode, int tenantId, object id, Dictionary<string, object?> values,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa cứng 1 bản ghi theo PK (DELETE row).
    /// LƯU Ý: caller phải tự gọi soft-check tham chiếu trước — repo này không tự chặn.
    /// </summary>
    Task<int> DeleteAsync(
        string formCode, int tenantId, object id, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra giá trị đã tồn tại ở cột chưa (chống trùng — field Is_Unique).
    /// excludeId = PK loại trừ khi Update. Trả false nếu value rỗng.
    /// </summary>
    Task<bool> ExistsValueAsync(
        string formCode, int tenantId, string column, object? value, object? excludeId,
        CancellationToken ct = default);
}

/// <summary>Thông tin bảng đích + cột của 1 form danh mục.</summary>
public sealed class MasterDataFormInfo
{
    public int    FormId      { get; init; }
    public string FormCode    { get; init; } = "";
    public int    TableId     { get; init; }
    public string SchemaName  { get; init; } = "dbo";
    /// <summary>Tên bảng vật lý = Sys_Table.Table_Code.</summary>
    public string TableName   { get; init; } = "";
    /// <summary>Cột khóa chính (Sys_Column.Is_PK = 1).</summary>
    public string PkColumn    { get; init; } = "";
    public string DisplayMode { get; init; } = "Popup";
    /// <summary>Toàn bộ cột field của form (theo Ui_Field).</summary>
    public IReadOnlyList<MasterDataColumn> Columns { get; init; } = [];
}

/// <summary>Metadata 1 cột field dùng cho CRUD + render lưới.</summary>
public sealed class MasterDataColumn
{
    public string ColumnCode { get; init; } = "";
    public string NetType    { get; init; } = "string";
    public string EditorType { get; init; } = "TextBox";
    public string Label      { get; init; } = "";
    public bool   ShowInList { get; init; }
    public bool   IsReadOnly { get; init; }
    public bool   IsUnique   { get; init; }
    public int    OrderNo    { get; init; }
}

/// <summary>Kết quả list có phân trang.</summary>
public sealed class MasterDataListResult
{
    public IReadOnlyList<IDictionary<string, object?>> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
