// File    : I18nDefaults.cs
// Module  : I18n
// Layer   : Core (Helpers)
// Purpose : Nhận diện giá trị i18n "chưa dịch" — rỗng, hoặc còn giữ mặc định sinh từ mã cột
//           (thô "NganHang_Id" hoặc dạng tách hoa "Ngan Hang_Id"). Dùng chung cho popup bảng dịch
//           (I18nEditorDialog) và lúc Lưu field (FieldConfig) để lan nhãn sang placeholder/tooltip.

using System.Text;

namespace ConfigStudio.WPF.UI.Core.Helpers;

/// <summary>
/// Quy ước "giá trị mặc định chưa ai dịch" của một resource key hiển thị. Key nào còn mang giá trị này
/// thì được phép ghi đè theo nhãn; key đã có bản dịch riêng thì giữ nguyên.
/// </summary>
public static class I18nDefaults
{
    /// <summary>
    /// Dựng tập giá trị coi như "chưa dịch" của một cột: mã cột thô và dạng tách hoa
    /// (<c>NganHang_Id</c> → <c>Ngan Hang_Id</c>) — hai dạng mặc định đang tồn tại trong Sys_Resource.
    /// Rỗng khi không có mã cột. Không phát sự kiện.
    /// </summary>
    public static IReadOnlyList<string> BuildColumnMarkers(string? columnCode)
    {
        if (string.IsNullOrWhiteSpace(columnCode)) return [];

        var raw    = columnCode.Trim();
        var spaced = SplitCamelCase(raw);
        return string.Equals(raw, spaced, StringComparison.Ordinal) ? [raw] : [raw, spaced];
    }

    /// <summary>
    /// True khi giá trị rỗng hoặc trùng một trong các mặc định sinh từ mã cột (so sánh trim + không phân
    /// biệt hoa thường) ⇒ được phép ghi đè. Không phát sự kiện.
    /// </summary>
    public static bool IsUntranslated(string? value, IReadOnlyCollection<string> markers)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;

        var trimmed = value.Trim();
        foreach (var marker in markers)
            if (string.Equals(marker, trimmed, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>Chèn khoảng trắng trước chữ hoa đứng sau chữ thường/chữ số: <c>DiaChi</c> → <c>Dia Chi</c>.</summary>
    private static string SplitCamelCase(string source)
    {
        var sb = new StringBuilder(source.Length + 4);
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(source[i - 1]) || char.IsDigit(source[i - 1])))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
