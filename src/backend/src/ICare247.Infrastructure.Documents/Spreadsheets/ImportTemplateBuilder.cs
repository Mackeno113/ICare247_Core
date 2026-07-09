// File    : ImportTemplateBuilder.cs
// Module  : Import (Spreadsheet I/O)
// Layer   : Infrastructure (Documents)
// Purpose : Impl IImportTemplateBuilder bằng DevExpress.Spreadsheet — sheet chính (tiêu đề i18n + comment kiểu/
//           bắt buộc) + mỗi cột FK 1 sheet phụ {Mã,Tên} + Data Validation dropdown chọn Mã. Spec 25 §7/§11.
//           DevExpress cô lập trong project này (như in biểu mẫu). Chỉ số hàng/cột 0-based.

using System.Drawing;
using DevExpress.Spreadsheet;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents.Spreadsheets;

/// <summary>
/// Dựng workbook template import: sheet chính (tiêu đề bôi đậm, cột bắt buộc thêm '*', comment gợi ý) và mỗi
/// cột FK một sheet phụ {Mã,Tên} kèm dropdown chọn Mã ngay trong ô. Thuần định dạng — không chạm DB.
/// </summary>
internal sealed class ImportTemplateBuilder : IImportTemplateBuilder
{
    private const int DataValidationRows = 2000;   // số dòng gắn dropdown sẵn cho người nhập
    private const int MaxSheetNameLength = 31;      // giới hạn Excel
    private static readonly Color HeaderFill = Color.FromArgb(0xEE, 0xF2, 0xF7);

    /// <inheritdoc />
    public byte[] Build(ImportTemplateSpec spec)
    {
        using var wb = new Workbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // DevExpress tạo sẵn 1 sheet mặc định → dùng lại làm sheet chính (đổi tên), tránh sinh sheet thừa.
        var main = wb.Worksheets.Any() ? wb.Worksheets.First() : wb.Worksheets.Add();
        main.Name = SafeSheetName(spec.SheetName, "DuLieu", usedNames);
        usedNames.Add(main.Name);

        for (var i = 0; i < spec.Columns.Count; i++)
        {
            var col = spec.Columns[i];   // cột tuyệt đối 0-based
            var colIdx = i;

            // Tiêu đề cột (bôi đậm, nền nhạt, cột bắt buộc thêm dấu *).
            var header = main[0, colIdx];
            header.SetValue(col.Required ? col.Caption + " *" : col.Caption);
            var hf = header.BeginUpdateFormatting();
            hf.Font.Bold = true;
            hf.Fill.PatternType = PatternType.Solid;
            hf.Fill.BackgroundColor = HeaderFill;
            header.EndUpdateFormatting(hf);

            main.Columns[colIdx].WidthInCharacters = 20;

            // Cột FK → sheet phụ {Mã,Tên} + dropdown chọn Mã (tạo TRƯỚC để ghi chú trỏ tên sheet).
            string? auxName = null;
            if (col.Fk is { Items.Count: > 0 } fk)
            {
                auxName = SafeSheetName(col.Caption, "DanhMuc" + (colIdx + 1), usedNames);
                usedNames.Add(auxName);
                var aux = wb.Worksheets.Add(auxName);

                aux[0, 0].SetValue("Mã");
                aux[0, 1].SetValue("Tên");
                var ahf = aux["A1:B1"].BeginUpdateFormatting();
                ahf.Font.Bold = true;
                aux["A1:B1"].EndUpdateFormatting(ahf);
                for (var r = 0; r < fk.Items.Count; r++)
                {
                    aux[r + 1, 0].SetValue(fk.Items[r].Code);
                    aux[r + 1, 1].SetValue(fk.Items[r].Display ?? string.Empty);
                }
                aux.Columns.AutoFit(0, 1);

                var lastRow = fk.Items.Count + 1;   // Excel 1-based (tiêu đề dòng 1 + N dòng)
                var listFormula = $"='{auxName}'!$A$2:$A${lastRow}";
                var colLetter = ColumnLetter(colIdx);
                var dvRange = main[$"{colLetter}2:{colLetter}{DataValidationRows + 1}"];
                var dv = main.DataValidations.Add(dvRange, DataValidationType.List, listFormula);
                dv.ShowDropDown = true;   // hiện dropdown trong ô
            }

            // Ghi chú ô tiêu đề. LƯU Ý: CommentCollection.Add(range, string) → string là AUTHOR, KHÔNG phải
            // nội dung; phải dùng overload Add(range, author, text) mới có chữ (bản ClosedXML trước ghi thẳng).
            var note = BuildNote(col, auxName);
            if (note is not null)
                main.Comments.Add(header, "ICare247", note);
        }

        main.FreezeRows(0);   // ghim dòng tiêu đề (hàng 0-based đầu tiên)

        return wb.SaveDocument(DocumentFormat.Xlsx);
    }

    /// <summary>
    /// Dựng chú thích ô tiêu đề: bắt buộc + kiểu dữ liệu + gợi ý "nhập Mã" cho cột FK (kèm tên sheet phụ
    /// {Mã,Tên} để tra đúng). Null = không có ghi chú.
    /// </summary>
    private static string? BuildNote(ImportTemplateColumn col, string? auxSheetName)
    {
        var parts = new List<string>();
        if (col.Required) parts.Add("• Bắt buộc");
        if (!string.IsNullOrWhiteSpace(col.TypeHint)) parts.Add("• Kiểu: " + col.TypeHint);
        if (col.Fk is not null)
        {
            parts.Add("• Nhập MÃ (chọn từ dropdown)");
            if (!string.IsNullOrWhiteSpace(auxSheetName))
                parts.Add($"• Tra Mã ↔ Tên ở sheet \"{auxSheetName}\"");
        }
        return parts.Count == 0 ? null : string.Join("\n", parts);
    }

    /// <summary>Đổi chỉ số cột 0-based → chữ cái cột Excel (0→A, 26→AA) cho địa chỉ A1 của vùng dropdown.</summary>
    private static string ColumnLetter(int index)
    {
        var s = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26)
            s = (char)('A' + (n - 1) % 26) + s;
        return s;
    }

    /// <summary>
    /// Chuẩn hóa tên sheet: bỏ ký tự cấm <c>[]:*?/\'</c>, cắt ≤31 ký tự, đảm bảo không rỗng + duy nhất.
    /// </summary>
    private static string SafeSheetName(string raw, string fallback, ISet<string> used)
    {
        var cleaned = new string((raw ?? string.Empty)
            .Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\' or '\''))
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
