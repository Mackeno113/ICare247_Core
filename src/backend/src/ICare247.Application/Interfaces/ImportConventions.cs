// File    : ImportConventions.cs
// Module  : Import
// Layer   : Application
// Purpose : Quy ước dùng chung cho import — chuỗi nối "Mã — Tên" hiển thị trong dropdown FK của template.
//           Template builder (Documents) nối bằng quy ước này; ImportEngine (Infrastructure) cắt lấy Mã.
//           Spec 25 §11.

namespace ICare247.Application.Interfaces;

/// <summary>Hằng số/hàm quy ước import — tách để builder (sinh template) và engine (đọc) khớp nhau.</summary>
public static class ImportConventions
{
    /// <summary>
    /// Ngăn cách giữa Mã và Tên khi hiển thị trong dropdown FK (VD <c>00004 — Phường X</c>).
    /// Dùng em-dash có khoảng trắng 2 bên — hiếm xuất hiện trong Mã nên cắt an toàn.
    /// </summary>
    public const string FkCodeNameSeparator = " — ";

    /// <summary>
    /// Trích Mã từ giá trị ô FK: nếu có nối theo <see cref="FkCodeNameSeparator"/> (chọn từ dropdown) →
    /// cắt lấy phần TRƯỚC ngăn cách; nếu không (người dùng gõ thẳng Mã) → trả nguyên giá trị đã trim.
    /// </summary>
    /// <param name="cellValue">Giá trị ô (đã trim, khác rỗng).</param>
    public static string ExtractFkCode(string cellValue)
    {
        var idx = cellValue.IndexOf(FkCodeNameSeparator, StringComparison.Ordinal);
        return idx >= 0 ? cellValue[..idx].Trim() : cellValue.Trim();
    }
}
