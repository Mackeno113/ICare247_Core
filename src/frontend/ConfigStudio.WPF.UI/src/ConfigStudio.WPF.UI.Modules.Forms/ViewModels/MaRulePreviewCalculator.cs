// File    : MaRulePreviewCalculator.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Ghép mã mẫu (preview) từ danh sách MaRuleSegmentRecord — thuật toán thuần, dùng chung
//           cho panel Preview trong popup Sửa (qua MaRuleSegmentRowVm) và cột "Mã dự kiến" ở lưới
//           danh sách (MaRuleManagerViewModel). Client-side only: đoạn FIELD/LOOKUP không có giá
//           trị thật (ConfigStudio không đọc Data DB) nên hiện placeholder.

using System.Text;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Thuật toán ghép mã mẫu từ các đoạn — tách khỏi <see cref="MaRuleSegmentRowVm"/> để dùng lại được ở lưới danh sách.</summary>
public static class MaRulePreviewCalculator
{
    /// <summary>Ghép mã mẫu đầy đủ từ các đoạn theo đúng thứ tự <c>OrderNo</c>.</summary>
    public static string BuildPreview(IEnumerable<MaRuleSegmentRecord> segments)
    {
        var full = new StringBuilder();
        foreach (var s in segments.OrderBy(x => x.OrderNo))
            full.Append(BuildSegmentSample(s));
        return full.ToString();
    }

    /// <summary>Chuỗi mẫu của riêng 1 đoạn — dùng giá trị thật cho LITERAL/DATE/SEQ; placeholder cho FIELD/LOOKUP.</summary>
    public static string BuildSegmentSample(MaRuleSegmentRecord s)
    {
        var type = (s.SegmentType ?? "").Trim().ToUpperInvariant();
        var raw = type switch
        {
            "LITERAL" => s.TextValue ?? "",
            "DATE" => SampleDate(s.TextValue),
            "SEQ" => "1",
            "FIELD" => SampleFixed(s.FieldCode, s.Length),
            "LOOKUP" => SampleFixed(s.LookupValCol, s.Length),
            _ => "",
        };
        return ApplyFormat(raw, s);
    }

    /// <summary>DATE mẫu theo hôm nay (đúng ý các token yyyy/yy/MM/dd và ghép).</summary>
    private static string SampleDate(string? fmt)
    {
        if (string.IsNullOrWhiteSpace(fmt)) return "";
        var now = DateTime.Now;
        return fmt
            .Replace("yyyy", now.ToString("yyyy"))
            .Replace("yy", now.ToString("yy"))
            .Replace("MM", now.ToString("MM"))
            .Replace("dd", now.ToString("dd"));
    }

    /// <summary>
    /// FIELD/LOOKUP: không có giá trị thật (Config DB) → nếu đặt Độ rộng thì hiện chuỗi 'XXXX' đúng
    /// độ rộng (để thấy hình dạng/tiền tố cố định); ngược lại hiện ‹tên› gợi ý.
    /// </summary>
    private static string SampleFixed(string? name, int? length)
    {
        if (length is > 0)
            return new string(MaRuleUiText.SamplePlaceholderChar[0], length.Value);
        return string.IsNullOrWhiteSpace(name) ? "‹?›" : $"‹{name}›";
    }

    /// <summary>Áp cắt / độ rộng / đệm / biến đổi chữ cho phần thô (SEQ chỉ dùng độ rộng + đệm).</summary>
    private static string ApplyFormat(string value, MaRuleSegmentRecord s)
    {
        var v = value ?? "";
        var type = (s.SegmentType ?? "").Trim().ToUpperInvariant();

        if (s.SubstringStart is > 1 && v.Length >= s.SubstringStart.Value)
            v = v[(s.SubstringStart.Value - 1)..];

        if (s.TextTransform == "UPPER") v = v.ToUpperInvariant();
        else if (s.TextTransform == "LOWER") v = v.ToLowerInvariant();

        if (s.Length is > 0)
        {
            var pad = string.IsNullOrEmpty(s.PadChar) ? ' ' : s.PadChar[0];
            if (v.Length < s.Length.Value)
                v = s.PadSide == "R" ? v.PadRight(s.Length.Value, pad) : v.PadLeft(s.Length.Value, pad);
            else if (type is "FIELD" or "LOOKUP" or "LITERAL" or "DATE" && v.Length > s.Length.Value)
                v = v[..s.Length.Value];   // đoạn phi-SEQ có độ rộng cố định thì cắt; SEQ KHÔNG cắt (tràn)
        }
        return v;
    }
}
