// File    : Models.cs
// Module  : I18nScanner
// Layer   : Tool
// Purpose : Các kiểu dữ liệu mô tả kết quả quét i18n (key, vị trí, chuỗi hardcode).

namespace ICare247.Tools.I18nScanner;

/// <summary>1 vị trí xuất hiện của key/chuỗi trong source (đường dẫn tương đối + dòng).</summary>
/// <param name="File">Đường dẫn file tương đối gốc repo.</param>
/// <param name="Line">Số dòng (1-based).</param>
public sealed record Occurrence(string File, int Line);

/// <summary>
/// 1 mục catalog: gom mọi lần gọi <c>L("key","fallback")</c> trùng key.
/// </summary>
public sealed class CatalogEntry
{
    /// <summary>Resource key (tham số 1 của L). Rỗng nếu key dựng động.</summary>
    public string Key { get; set; } = "";

    /// <summary>Văn bản gốc tiếng Việt (tham số 2 của L) — bản dịch base.</summary>
    public string Vi { get; set; } = "";

    /// <summary>Project i18n-root chứa key (vd "ICare247_UI", "ICare247.UI.Shared").</summary>
    public string Source { get; set; } = "";

    /// <summary>Mọi vị trí gọi key này.</summary>
    public List<Occurrence> Occurrences { get; } = [];

    /// <summary>True nếu phát hiện L() có key KHÔNG phải literal (chỉ runtime mới biết).</summary>
    public bool Dynamic { get; set; }
}

/// <summary>1 chuỗi tiếng Việt hardcode (không đi qua L) — ứng viên cần bọc i18n.</summary>
/// <param name="Text">Đoạn chữ phát hiện.</param>
/// <param name="File">File tương đối.</param>
/// <param name="Line">Dòng.</param>
public sealed record HardcodedString(string Text, string File, int Line);

/// <summary>Một i18n-root: project Blazor có thư mục wwwroot/i18n để chứa {lang}.json.</summary>
/// <param name="Name">Tên project (tên thư mục).</param>
/// <param name="ProjectDir">Đường dẫn tuyệt đối thư mục project.</param>
/// <param name="I18nDir">Đường dẫn tuyệt đối thư mục wwwroot/i18n.</param>
public sealed record I18nRoot(string Name, string ProjectDir, string I18nDir);
