// File    : SpreadsheetReader.cs
// Module  : Import (Spreadsheet I/O)
// Layer   : Infrastructure (Documents)
// Purpose : Impl ISpreadsheetReader bằng DevExpress.Spreadsheet — đọc .xlsx → lưới ô thuần (DisplayText đã trim).
//           DevExpress cô lập trong project này (nguyên tắc như in biểu mẫu). Spec 25 §11 / Spec 28 §2.3.

using DevExpress.Spreadsheet;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents.Spreadsheets;

/// <summary>
/// Đọc workbook Excel bằng DevExpress Office File API → <see cref="SheetGrid"/>. Chỉ đọc, không sửa/ghi.
/// Chỉ số hàng/cột DevExpress là 0-based; grid trả về giữ chỉ số cột tuyệt đối 0-based (cột A = 0).
/// </summary>
internal sealed class SpreadsheetReader : ISpreadsheetReader
{
    /// <inheritdoc />
    public SheetGrid Read(Stream workbook, string? preferredSheetName)
    {
        using var wb = new Workbook();
        try
        {
            wb.LoadDocument(workbook);   // tự nhận định dạng theo nội dung
        }
        catch (Exception ex)
        {
            throw new SpreadsheetReadException("Không đọc được workbook (.xlsx) — file hỏng hoặc sai định dạng.", ex);
        }

        var ws = !string.IsNullOrWhiteSpace(preferredSheetName) && wb.Worksheets.Contains(preferredSheetName)
            ? wb.Worksheets[preferredSheetName!]
            : wb.Worksheets.First();

        var used = ws.GetUsedRange();
        var top = used.TopRowIndex;                                   // 0-based hàng đầu có dữ liệu
        var bottom = used.BottomRowIndex;                             // 0-based hàng cuối
        var right = used.LeftColumnIndex + used.ColumnCount - 1;      // 0-based cột cuối (tuyệt đối)
        var colCount = right + 1;

        var rows = new List<IReadOnlyList<string?>>(Math.Max(0, bottom - top + 1));
        for (var r = top; r <= bottom; r++)
        {
            var row = new string?[colCount];
            for (var c = 0; c <= right; c++)
            {
                var text = ws[r, c].DisplayText?.Trim();              // như ClosedXML .GetString() (đã trim)
                row[c] = string.IsNullOrEmpty(text) ? null : text;
            }
            rows.Add(row);
        }

        return new SheetGrid
        {
            SheetName = ws.Name,
            FirstRowNumber = top + 1,   // Excel 1-based cho hàng tiêu đề
            Rows = rows,
            ColumnCount = colCount
        };
    }
}
