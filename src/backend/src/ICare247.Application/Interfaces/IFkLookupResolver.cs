// File    : IFkLookupResolver.cs
// Module  : Import
// Layer   : Application
// Purpose : Cầu nối khóa ngoại DÙNG CHUNG cho import + xuất template (spec 25 §6/§7, ADR-034).
//           Một định nghĩa FK duy nhất = Ui_Field_Lookup → resolve Mã↔Id + danh sách {Mã,Tên} đã lọc quyền.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Dựng bảng tra <c>Mã→Id</c> đã lọc phân quyền cho một định nghĩa khóa ngoại (từ <c>Ui_Field_Lookup</c>).
/// Dùng chung cho <b>import</b> (resolve Mã→Id) và <b>xuất template</b> (sheet phụ {Mã,Tên}). Spec 25 §11–§14.
/// Việc dò cột FK của màn nằm ở <see cref="IImportMetadataProvider"/> (đọc từ field Edit_Form).
/// </summary>
public interface IFkLookupResolver
{
    /// <summary>
    /// Dựng bảng tra <c>Mã→Id</c> + danh sách <c>{Mã,Tên}</c> cho một định nghĩa FK: chạy
    /// <c>SELECT Code, Value, Display FROM Source WHERE Filter_Sql</c> trên Data DB, bind token ngữ cảnh
    /// (lọc đúng phạm vi quyền như form/lưới). Sau lời gọi caller có thể resolve Mã sang Id hoặc sinh sheet phụ.
    /// </summary>
    Task<FkCodeMap> BuildCodeMapAsync(FkLookupDefinition definition, CancellationToken ct = default);
}

/// <summary>
/// Định nghĩa khóa ngoại đã resolve cho MỘT cột nhập (từ <c>Ui_Field_Lookup</c> của field Edit_Form).
/// <paramref name="CodeField"/> = cầu Mã↔Id (null nếu chưa cấu hình → không import/template được cột này).
/// </summary>
public sealed record FkLookupDefinition(
    string FieldName,
    int FieldId,
    string SourceName,
    string ValueColumn,
    string DisplayColumn,
    string? CodeField,
    string? FilterSql,
    string? OrderBy);

/// <summary>Một dòng lookup {Mã, Id, Tên} đã lọc quyền.</summary>
public sealed record FkLookupItem(string Code, object? Id, string? Display);

/// <summary>
/// Kết quả tra khóa ngoại: danh sách {Mã,Tên} (cho template) + tra <c>Mã→Id</c> (cho import).
/// Mã chuẩn hóa <b>trim + culture-invariant upper</b> khi so khớp (khớp yêu cầu cắt khoảng trắng).
/// </summary>
public sealed class FkCodeMap
{
    private readonly Dictionary<string, object?> _byCode;

    /// <summary>Khởi tạo từ danh sách item; dựng sẵn index Mã→Id đã chuẩn hóa. Không phát sự kiện.</summary>
    public FkCodeMap(IReadOnlyList<FkLookupItem> items, bool hasCodeField)
    {
        Items = items;
        HasCodeField = hasCodeField;
        _byCode = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.Code)) continue;
            _byCode[Normalize(it.Code)] = it.Id;   // trùng Mã → bản sau thắng (nguồn nên unique Mã)
        }
    }

    /// <summary>Danh sách {Mã, Id, Tên} đã lọc quyền (theo Order_By). Dùng dựng sheet phụ template.</summary>
    public IReadOnlyList<FkLookupItem> Items { get; }

    /// <summary>Nguồn FK có khai <c>Code_Field</c> hay không — false ⇒ không thể resolve Mã↔Id.</summary>
    public bool HasCodeField { get; }

    /// <summary>
    /// Tra Id từ Mã (đã chuẩn hóa trim + upper-invariant). Trả false nếu Mã rỗng hoặc ngoài tập đã lọc quyền
    /// (import ⇒ lỗi <c>import.fk.code_not_found</c>). Không phát sự kiện.
    /// </summary>
    public bool TryResolve(string? code, out object? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(code)) return false;
        return _byCode.TryGetValue(Normalize(code), out id);
    }

    /// <summary>Chuẩn hóa Mã để so khớp: cắt khoảng trắng + upper theo culture-invariant.</summary>
    private static string Normalize(string s) => s.Trim().ToUpperInvariant();
}
