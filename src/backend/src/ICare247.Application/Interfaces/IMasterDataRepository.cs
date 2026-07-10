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
///   <item>Cô lập tenant ở tầng connection (ADR-035) — KHÔNG lọc theo cột.</item>
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
        long? userId = null, CancellationToken ct = default);

    /// <summary>Update 1 bản ghi theo PK. Trả số dòng bị ảnh hưởng.</summary>
    Task<int> UpdateAsync(
        string formCode, int tenantId, object id, Dictionary<string, object?> values,
        long? userId = null, CancellationToken ct = default);

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
    /// <param name="info">Metadata bảng đích (caller đã load — tránh GetFormInfoAsync mỗi field unique).</param>
    Task<bool> ExistsValueAsync(
        MasterDataFormInfo info, string column, object? value, object? excludeId,
        CancellationToken ct = default);

    /// <summary>
    /// Lưu 1 bản ghi qua HOOK STORE trong **1 transaction** Data DB (ADR-029):
    /// <c>spc_Grid_&lt;Table&gt;</c> (validate) → INSERT/UPDATE → <c>sp_AfterSave_Grid_&lt;Table&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><b>Opt-in từng màn:</b> store nào <c>OBJECT_ID</c> null → bỏ qua bước đó (màn chưa bật chạy như cũ).</item>
    ///   <item><c>spc_</c> trả ≥1 lỗi → rollback, KHÔNG ghi, trả <c>Errors</c> (<c>Success=false</c>).</item>
    ///   <item><c>sp_AfterSave_</c> trả lỗi / RAISERROR → rollback cả bản ghi vừa ghi.</item>
    /// </list>
    /// Store trả KEY (chưa resolve text) — handler resolve i18n server-side.
    /// </remarks>
    /// <param name="info">Metadata bảng đích (caller đã load — tránh GetFormInfoAsync lần 2 trong save path).</param>
    /// <param name="id">PK: null = Insert, có giá trị = Update. Truyền vào store <c>@Id</c> (null → 0).</param>
    /// <param name="langCode">Ngôn ngữ truyền cho store (<c>@LangCode</c>).</param>
    /// <param name="hasValidateProc">Có <c>spc_Grid_&lt;Table&gt;</c> không (tra qua <see cref="IHookStoreCatalog"/> — KHÔNG query lúc lưu).</param>
    /// <param name="hasAfterSaveProc">Có <c>sp_AfterSave_Grid_&lt;Table&gt;</c> không.</param>
    /// <param name="source">Ngữ cảnh ghi cho hook after-save: "MANUAL" | "IMPORT". Truyền <c>@Source</c> chỉ khi import.</param>
    /// <param name="importSessionId">Phiên import → hook <c>@ImportSessionId</c>; null = nhập tay (EXEC giữ contract cũ).</param>
    Task<MasterDataHookSaveResult> SaveWithHooksAsync(
        MasterDataFormInfo info, int tenantId, object? id,
        Dictionary<string, object?> values, long? userId, string langCode,
        bool hasValidateProc, bool hasAfterSaveProc,
        CancellationToken ct = default,
        string source = "MANUAL", Guid? importSessionId = null);
}

/// <summary>Kết quả lưu qua hook store: thành công kèm Id, hoặc fail kèm lỗi store (CHƯA ghi/đã rollback).</summary>
public sealed class MasterDataHookSaveResult
{
    /// <summary>true = đã commit (ghi thành công).</summary>
    public bool Success { get; init; }
    /// <summary>PK: Insert trả Id mới; Update trả lại Id cũ. null khi fail.</summary>
    public object? Id { get; init; }
    /// <summary>Lỗi do store trả (rỗng khi Success). Key + tham số — chưa resolve text.</summary>
    public IReadOnlyList<ProcError> Errors { get; init; } = [];
}

/// <summary>1 lỗi do hook store trả về (key i18n + tham số, CHƯA resolve text).</summary>
/// <param name="ErrorKey">Key i18n (sys.val.*, {table}.val.{field}.{rule}…).</param>
/// <param name="ArgsJson">Mảng tham số JSON theo vị trí token (<c>[value, label, ...]</c>); null nếu không có.</param>
/// <param name="FieldName">Field để UI tô đỏ; null = thông báo cấp form (banner).</param>
/// <param name="Severity">error / warning.</param>
public sealed record ProcError(string ErrorKey, string? ArgsJson, string? FieldName, string Severity);

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
