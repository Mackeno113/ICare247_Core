// File    : I18nCatalog.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : DTO đọc catalog.json (do tool I18nScanner sinh) cho màn tra cứu key i18n in-app.

namespace ICare247.UI.Shared.Services.I18n;

/// <summary>1 vị trí xuất hiện của key trong source.</summary>
/// <param name="File">Đường dẫn file tương đối repo.</param>
/// <param name="Line">Số dòng.</param>
public sealed record I18nOccurrence(string File, int Line);

/// <summary>1 mục catalog: key i18n + văn bản gốc + nơi dùng.</summary>
public sealed class I18nCatalogEntry
{
    /// <summary>Resource key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Văn bản gốc tiếng Việt (base).</summary>
    public string Vi { get; set; } = "";

    /// <summary>Project i18n-root chứa key.</summary>
    public string Source { get; set; } = "";

    /// <summary>Mọi vị trí gọi.</summary>
    public List<I18nOccurrence> Occurrences { get; set; } = [];

    /// <summary>Key dựng động (chỉ runtime biết)?</summary>
    public bool Dynamic { get; set; }
}

/// <summary>Bao ngoài của catalog.json (1 file / i18n-root).</summary>
public sealed class I18nCatalogDoc
{
    public string GeneratedAt { get; set; } = "";
    public string Source { get; set; } = "";
    public int Count { get; set; }
    public List<I18nCatalogEntry> Entries { get; set; } = [];
}
