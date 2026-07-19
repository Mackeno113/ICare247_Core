// File    : IViewExportBuilder.cs
// Module  : View
// Layer   : Application
// Purpose : Sinh workbook Excel (.xlsx) xuất toàn bộ dữ liệu lưới (WEB-UX-01 export) từ dữ liệu đã
//           truy vấn sẵn — thuần định dạng, không chạm DB.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Dựng file .xlsx xuất dữ liệu lưới từ <see cref="ViewExportSpec"/> (đã có sẵn cột + dòng, không
/// truy vấn thêm). Cô lập DevExpress.Spreadsheet ở project Infrastructure.Documents (như template import).
/// </summary>
public interface IViewExportBuilder
{
    /// <summary>Dựng workbook 1 sheet: header (Caption cột) + toàn bộ dòng dữ liệu. Trả bytes .xlsx.</summary>
    byte[] Build(ViewExportSpec spec);
}

/// <summary>Mô tả 1 lượt export: tên sheet + cột (đúng thứ tự hiển thị) + dòng dữ liệu.</summary>
public sealed record ViewExportSpec(
    string SheetName,
    IReadOnlyList<ViewExportColumn> Columns,
    IReadOnlyList<IDictionary<string, object?>> Rows);

/// <summary>Một cột xuất — Caption đã resolve i18n, FieldName để đọc giá trị từ mỗi dòng.</summary>
public sealed record ViewExportColumn(string FieldName, string Caption);
