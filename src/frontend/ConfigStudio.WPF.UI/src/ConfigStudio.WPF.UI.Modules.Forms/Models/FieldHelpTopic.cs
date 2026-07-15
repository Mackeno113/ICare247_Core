// File    : FieldHelpTopic.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Nội dung hướng dẫn chi tiết cho 1 ô cấu hình — hiển thị dạng ToolTip khi trỏ chuột.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Nội dung hướng dẫn chi tiết cho một ô cấu hình trên màn Field Config.
/// Được <c>HelpAssist</c> render thành ToolTip khi user trỏ chuột vào ô nhập.
/// Sự kiện theo sau: user đọc hướng dẫn → nhập giá trị đúng → RebuildControlPropsJson.
/// </summary>
public sealed record FieldHelpTopic(
    string Title,
    string Purpose,
    IReadOnlyList<string> HowTo,
    string? Example = null,
    IReadOnlyList<string>? Pitfalls = null
)
{
    /// <summary>True khi có ví dụ cụ thể để hiển thị dòng "Ví dụ:".</summary>
    public bool HasExample => !string.IsNullOrWhiteSpace(Example);

    /// <summary>True khi có danh sách lỗi thường gặp để hiển thị mục "⚠ Lưu ý".</summary>
    public bool HasPitfalls => Pitfalls is { Count: > 0 };
}
