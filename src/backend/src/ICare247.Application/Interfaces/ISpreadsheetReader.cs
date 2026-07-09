// File    : ISpreadsheetReader.cs
// Module  : Import
// Layer   : Application
// Purpose : Seam đọc file bảng tính (.xlsx) → lưới ô THUẦN (không phụ thuộc thư viện Office cụ thể).
//           Cho phép ImportEngine ở tầng Infrastructure (không DevExpress) đọc file, còn impl DevExpress
//           nằm cô lập trong ICare247.Infrastructure.Documents — nguyên tắc tham chiếu như in biểu mẫu (Spec 28 §2.3).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Đọc 1 workbook Excel thành <see cref="SheetGrid"/> (lưới chuỗi đã trim). Ẩn hoàn toàn thư viện Office
/// khỏi Application/Infrastructure — impl DevExpress ở project cô lập Documents.
/// </summary>
public interface ISpreadsheetReader
{
    /// <summary>
    /// Đọc sheet chính của workbook. Sự kiện theo sau: ném <see cref="SpreadsheetReadException"/> nếu file
    /// hỏng/không phải .xlsx → caller quy về lỗi cấp file.
    /// </summary>
    /// <param name="workbook">Stream file .xlsx.</param>
    /// <param name="preferredSheetName">Tên sheet ưu tiên; không thấy → dùng sheet đầu tiên.</param>
    SheetGrid Read(Stream workbook, string? preferredSheetName);
}

/// <summary>
/// Lưới ô của 1 sheet: <see cref="Rows"/> theo hàng (row 0 = tiêu đề), mỗi hàng là mảng cột theo
/// <b>chỉ số cột tuyệt đối 0-based</b> (cột A = 0). Ô trống = null; ô có giá trị = text hiển thị đã trim.
/// </summary>
public sealed class SheetGrid
{
    /// <summary>Tên sheet đã đọc.</summary>
    public string SheetName { get; init; } = "";

    /// <summary>Số hàng Excel (1-based) của <c>Rows[0]</c> (dòng tiêu đề) — để báo lỗi đúng số dòng.</summary>
    public int FirstRowNumber { get; init; } = 1;

    /// <summary>Các hàng (row 0 = tiêu đề). Mỗi hàng đủ độ dài <see cref="ColumnCount"/>; ô trống = null.</summary>
    public IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; } = [];

    /// <summary>Số cột (bề rộng vùng dữ liệu) — chỉ số cột tuyệt đối 0-based chạy [0..ColumnCount).</summary>
    public int ColumnCount { get; init; }
}

/// <summary>Lỗi đọc file bảng tính (hỏng/sai định dạng). Caller quy về lỗi cấp file "import.file.invalid".</summary>
public sealed class SpreadsheetReadException(string message, Exception? inner = null)
    : Exception(message, inner);
