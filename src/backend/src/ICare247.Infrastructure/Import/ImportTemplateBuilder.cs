// File    : ImportTemplateBuilder.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : ClosedXML implementation của IImportTemplateBuilder — sheet chính + sheet phụ FK + dropdown.
//           Spec 25 §7/§11, ADR-034. Thư viện ClosedXML (MIT).

using ClosedXML.Excel;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Import;

/// <summary>
/// Dựng workbook template import: sheet chính (tiêu đề i18n + ghi chú kiểu/bắt buộc) và mỗi cột FK
/// một sheet phụ liệt kê {Mã,Tên} kèm Data Validation dropdown chọn Mã ngay trong ô. Thuần định dạng.
/// </summary>
public sealed class ImportTemplateBuilder : IImportTemplateBuilder
{
    private const int DataValidationRows = 2000;   // số dòng gắn dropdown sẵn cho người nhập
    private const int MaxSheetNameLength = 31;      // giới hạn Excel

    /// <inheritdoc />
    public byte[] Build(ImportTemplateSpec spec)
    {
        using var wb = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var main = wb.Worksheets.Add(SafeSheetName(spec.SheetName, "DuLieu", usedNames));
        usedNames.Add(main.Name);

        for (var i = 0; i < spec.Columns.Count; i++)
        {
            var col = spec.Columns[i];
            var colIdx = i + 1;

            // Tiêu đề cột (bôi đậm, cột bắt buộc thêm dấu *).
            var header = main.Cell(1, colIdx);
            header.Value = col.Required ? col.Caption + " *" : col.Caption;
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromArgb(0xEE, 0xF2, 0xF7);

            // Ghi chú: kiểu dữ liệu + FK nhập Mã (comment ô, không chiếm dòng → import parse header ở dòng 1).
            var note = BuildNote(col);
            if (note is not null)
                header.CreateComment().AddText(note);

            main.Column(colIdx).Width = 20;

            // Cột FK → sheet phụ {Mã,Tên} + dropdown chọn Mã.
            if (col.Fk is { Items.Count: > 0 } fk)
            {
                var auxName = SafeSheetName("FK_" + col.FieldName, "FK_" + colIdx, usedNames);
                usedNames.Add(auxName);
                var aux = wb.Worksheets.Add(auxName);

                aux.Cell(1, 1).Value = "Ma";
                aux.Cell(1, 2).Value = "Ten";
                aux.Row(1).Style.Font.Bold = true;
                for (var r = 0; r < fk.Items.Count; r++)
                {
                    aux.Cell(r + 2, 1).Value = fk.Items[r].Code;
                    aux.Cell(r + 2, 2).Value = fk.Items[r].Display ?? string.Empty;
                }
                aux.Columns().AdjustToContents();

                var lastRow = fk.Items.Count + 1;
                var listFormula = $"='{auxName}'!$A$2:$A${lastRow}";
                var dv = main.Range(2, colIdx, DataValidationRows + 1, colIdx).CreateDataValidation();
                dv.List(listFormula, true);   // true = hiện dropdown trong ô
            }
        }

        main.SheetView.FreezeRows(1);   // ghim dòng tiêu đề

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>Dựng chú thích ô tiêu đề: kiểu dữ liệu + gợi ý "nhập Mã" cho cột FK. Null = không có.</summary>
    private static string? BuildNote(ImportTemplateColumn col)
    {
        var parts = new List<string>();
        if (col.Required) parts.Add("Bắt buộc");
        if (!string.IsNullOrWhiteSpace(col.TypeHint)) parts.Add("Kiểu: " + col.TypeHint);
        if (col.Fk is not null) parts.Add("Nhập MÃ (chọn từ danh sách)");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>
    /// Chuẩn hóa tên sheet: bỏ ký tự cấm <c>[]:*?/\</c>, cắt ≤31 ký tự, đảm bảo không rỗng + duy nhất.
    /// </summary>
    private static string SafeSheetName(string raw, string fallback, ISet<string> used)
    {
        var cleaned = new string((raw ?? string.Empty)
            .Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\'))
            .ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = fallback;
        if (cleaned.Length > MaxSheetNameLength) cleaned = cleaned[..MaxSheetNameLength];

        var name = cleaned;
        var n = 1;
        while (used.Contains(name))
        {
            var suffix = "_" + n++;
            var keep = Math.Min(cleaned.Length, MaxSheetNameLength - suffix.Length);
            name = cleaned[..keep] + suffix;
        }
        return name;
    }
}
