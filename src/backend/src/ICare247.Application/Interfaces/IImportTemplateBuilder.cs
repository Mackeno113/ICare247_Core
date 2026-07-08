// File    : IImportTemplateBuilder.cs
// Module  : Import
// Layer   : Application
// Purpose : Sinh workbook template import (.xlsx): sheet chính (cột cần nhập) + mỗi FK 1 sheet phụ
//           {Mã,Tên} + Data Validation dropdown chọn Mã. Spec 25 §7/§11, ADR-034.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Dựng file template import Excel từ mô tả cột (<see cref="ImportTemplateSpec"/>).
/// Cột FK kèm nguồn {Mã,Tên} đã lọc quyền → sinh sheet phụ + dropdown chọn Mã trong ô.
/// </summary>
public interface IImportTemplateBuilder
{
    /// <summary>
    /// Dựng workbook và trả về bytes <c>.xlsx</c>. Không chạm DB (nhận sẵn dữ liệu FK đã resolve).
    /// Không phát sự kiện — thuần định dạng.
    /// </summary>
    byte[] Build(ImportTemplateSpec spec);
}

/// <summary>Mô tả 1 template import: tên sheet chính + danh sách cột cần nhập (đúng thứ tự).</summary>
public sealed record ImportTemplateSpec(
    string SheetName,
    IReadOnlyList<ImportTemplateColumn> Columns);

/// <summary>
/// Một cột trên sheet chính. <paramref name="Fk"/> non-null ⇒ cột khóa ngoại: ô nhập <b>Mã</b> +
/// dropdown Data Validation trỏ tới sheet phụ.
/// </summary>
public sealed record ImportTemplateColumn(
    string FieldName,
    string Caption,
    bool Required,
    string? TypeHint,
    FkTemplateSource? Fk);

/// <summary>Nguồn {Mã,Tên} cho cột FK (đã lọc phân quyền) → dựng sheet phụ + dropdown.</summary>
public sealed record FkTemplateSource(
    IReadOnlyList<FkTemplateItem> Items);

/// <summary>Một dòng lookup {Mã, Tên} hiển thị trên sheet phụ template.</summary>
public sealed record FkTemplateItem(string Code, string? Display);
