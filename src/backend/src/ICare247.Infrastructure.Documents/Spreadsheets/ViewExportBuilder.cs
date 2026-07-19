// File    : ViewExportBuilder.cs
// Module  : View (Spreadsheet I/O)
// Layer   : Infrastructure (Documents)
// Purpose : Impl IViewExportBuilder bằng DevExpress.Spreadsheet — sheet 1 header (Caption cột, bôi
//           đậm + ghim dòng) + toàn bộ dòng dữ liệu. DevExpress cô lập trong project này (như template
//           import). WEB-UX-01 export: người dùng lọc ra bao nhiêu dòng thì xuất đúng bấy nhiêu.

using System.Drawing;
using DevExpress.Spreadsheet;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents.Spreadsheets;

/// <summary>
/// Dựng workbook xuất dữ liệu lưới: 1 sheet, dòng đầu = Caption cột (bôi đậm, nền nhạt, ghim), các dòng
/// sau = dữ liệu theo đúng thứ tự cột. Thuần định dạng — không chạm DB (nhận sẵn <see cref="ViewExportSpec"/>).
/// </summary>
internal sealed class ViewExportBuilder : IViewExportBuilder
{
    private const int MaxSheetNameLength = 31;
    private static readonly Color HeaderFill = Color.FromArgb(0xEE, 0xF2, 0xF7);

    /// <inheritdoc />
    public byte[] Build(ViewExportSpec spec)
    {
        using var wb = new Workbook();
        var ws = wb.Worksheets.Any() ? wb.Worksheets.First() : wb.Worksheets.Add();
        ws.Name = SafeSheetName(spec.SheetName);

        for (var c = 0; c < spec.Columns.Count; c++)
        {
            var header = ws[0, c];
            header.SetValue(spec.Columns[c].Caption);
            ws.Columns[c].WidthInCharacters = 20;
        }
        var headerRange = ws[$"A1:{ColumnLetter(Math.Max(0, spec.Columns.Count - 1))}1"];
        var hf = headerRange.BeginUpdateFormatting();
        hf.Font.Bold = true;
        hf.Fill.PatternType = PatternType.Solid;
        hf.Fill.BackgroundColor = HeaderFill;
        headerRange.EndUpdateFormatting(hf);

        for (var r = 0; r < spec.Rows.Count; r++)
        {
            var row = spec.Rows[r];
            for (var c = 0; c < spec.Columns.Count; c++)
            {
                if (!row.TryGetValue(spec.Columns[c].FieldName, out var value) || value is null)
                    continue;
                // SetValue(object) — DevExpress tự nhận kiểu (số/ngày/bool/text) theo runtime type CLR.
                ws[r + 1, c].SetValue(value);
            }
        }

        ws.FreezeRows(0);
        return wb.SaveDocument(DocumentFormat.Xlsx);
    }

    /// <summary>Đổi chỉ số cột 0-based → chữ cái cột Excel (0→A, 26→AA) cho địa chỉ vùng header.</summary>
    private static string ColumnLetter(int index)
    {
        var s = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26)
            s = (char)('A' + (n - 1) % 26) + s;
        return s;
    }

    /// <summary>Chuẩn hóa tên sheet: bỏ ký tự cấm <c>[]:*?/\'</c>, cắt ≤31 ký tự, đảm bảo không rỗng.</summary>
    private static string SafeSheetName(string raw)
    {
        var cleaned = new string((raw ?? string.Empty)
            .Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\' or '\''))
            .ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "DuLieu";
        return cleaned.Length > MaxSheetNameLength ? cleaned[..MaxSheetNameLength] : cleaned;
    }
}
