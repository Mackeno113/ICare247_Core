// File    : Program.cs
// Module  : I18nScanner
// Layer   : Tool
// Purpose : Điều phối quét i18n toàn frontend Blazor → catalog.json + merge {lang}.json + báo cáo hardcode.
// Cách chạy:
//   dotnet run --project tools/I18nScanner -- [--root <frontendDir>] [--lang en] [--write]
//   --write : thực sự ghi catalog.json + merge {lang}.json (mặc định dry-run, chỉ in tổng kết).

using System.Text.Json;
using System.Text.Json.Serialization;
using ICare247.Tools.I18nScanner;

// ── Tham số dòng lệnh ────────────────────────────────────────────────────
string? rootArg = GetOpt("--root");
string lang     = GetOpt("--lang") ?? "en";
bool write      = args.Contains("--write");

// Mặc định: <repo>/src/frontend (suy từ vị trí tool: <repo>/tools/I18nScanner).
string frontendDir = rootArg ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "frontend"));
if (!Directory.Exists(frontendDir))
{
    Console.Error.WriteLine($"❌ Không thấy thư mục frontend: {frontendDir}");
    return 1;
}
string repoRoot = Path.GetFullPath(Path.Combine(frontendDir, "..", ".."));

Console.WriteLine($"📂 Frontend : {frontendDir}");
Console.WriteLine($"🌐 Ngôn ngữ : {lang}   |  Chế độ: {(write ? "GHI" : "dry-run")}\n");

// ── 1. Tìm i18n-root (project có wwwroot/i18n) ───────────────────────────
var roots = DiscoverRoots(frontendDir);
if (roots.Count == 0)
{
    Console.Error.WriteLine("❌ Không tìm thấy project nào có wwwroot/i18n.");
    return 1;
}
foreach (var r in roots) Console.WriteLine($"  • i18n-root: {r.Name}");
Console.WriteLine();

// ── 2. Quét toàn bộ .razor + .cs ─────────────────────────────────────────
var entries  = new Dictionary<string, CatalogEntry>(StringComparer.OrdinalIgnoreCase);
var dynamics = new List<Occurrence>();
var hardcoded = new List<HardcodedString>();

foreach (var file in EnumerateSource(frontendDir))
{
    // Chỉ quét file thuộc 1 i18n-root (bỏ project harness/legacy có hệ i18n riêng).
    var owner = RootOf(roots, file);
    if (owner is null) continue;

    var ext = Path.GetExtension(file).ToLowerInvariant();
    var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
    FileScan scan;
    try { scan = Scanner.Scan(file, rel, ext); }
    catch (Exception ex) { Console.Error.WriteLine($"⚠ Lỗi quét {rel}: {ex.Message}"); continue; }

    var source = owner.Name;

    foreach (var call in scan.Calls)
    {
        if (!entries.TryGetValue(call.Key, out var e))
            entries[call.Key] = e = new CatalogEntry { Key = call.Key, Vi = call.Vi, Source = source };
        if (string.IsNullOrEmpty(e.Vi) && !string.IsNullOrEmpty(call.Vi)) e.Vi = call.Vi;
        e.Occurrences.Add(new Occurrence(rel, call.Line));
    }
    foreach (var d in scan.Dynamic)  dynamics.Add(d);
    foreach (var h in scan.Hardcoded) hardcoded.Add(h);
}

var catalog = entries.Values.OrderBy(e => e.Key, StringComparer.Ordinal).ToList();

// ── 3. Tổng kết ──────────────────────────────────────────────────────────
Console.WriteLine($"🔑 Key i18n (literal)     : {catalog.Count}");
Console.WriteLine($"🌀 Lời gọi L() key động   : {dynamics.Count}   (cần phương án A runtime để lấy)");
Console.WriteLine($"📌 Chuỗi VN nghi hardcode  : {hardcoded.Count}   (ứng viên cần bọc L())\n");

// ── 4. Ghi output (chỉ khi --write) ──────────────────────────────────────
if (!write)
{
    Console.WriteLine("ℹ Dry-run — thêm --write để ghi catalog.json + merge {lang}.json + báo cáo.");
    PrintTop(hardcoded);
    return 0;
}

WriteCatalog(roots, catalog, dynamics, hardcoded, lang, repoRoot);
MergeLangFiles(roots, catalog, lang);
WriteHardcodedReport(roots, hardcoded, dynamics);
Console.WriteLine("\n✅ Hoàn tất.");
return 0;

// ════════════════════════════════════════════════════════════════════════
// Hàm cục bộ
// ════════════════════════════════════════════════════════════════════════

/// <summary>Lấy giá trị 1 option dạng "--key value"; null nếu không có.</summary>
string? GetOpt(string key)
{
    int i = Array.IndexOf(args, key);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

/// <summary>Tìm mọi project có thư mục wwwroot/i18n → đó là i18n-root.</summary>
static List<I18nRoot> DiscoverRoots(string frontendDir)
{
    var list = new List<I18nRoot>();
    foreach (var i18nDir in Directory.EnumerateDirectories(frontendDir, "i18n", SearchOption.AllDirectories))
    {
        // chỉ nhận .../wwwroot/i18n
        var parent = Directory.GetParent(i18nDir);
        if (parent is null || !parent.Name.Equals("wwwroot", StringComparison.OrdinalIgnoreCase)) continue;
        var projectDir = parent.Parent!.FullName;
        list.Add(new I18nRoot(new DirectoryInfo(projectDir).Name, projectDir, i18nDir));
    }
    return list.OrderByDescending(r => r.ProjectDir.Length).ToList(); // sâu nhất trước (map ưu tiên)
}

/// <summary>i18n-root chứa file (ancestor sâu nhất khớp đường dẫn).</summary>
static I18nRoot? RootOf(List<I18nRoot> roots, string file)
    => roots.FirstOrDefault(r => file.StartsWith(r.ProjectDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

/// <summary>Liệt kê mọi .razor/.cs frontend (bỏ obj/bin/.tool).</summary>
static IEnumerable<string> EnumerateSource(string frontendDir)
{
    foreach (var file in Directory.EnumerateFiles(frontendDir, "*.*", SearchOption.AllDirectories))
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        if (ext != ".razor" && ext != ".cs") continue;
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
        if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) continue;
        yield return file;
    }
}

/// <summary>Ghi catalog.json vào wwwroot/i18n của MỌI i18n-root (màn in-app fetch được).</summary>
static void WriteCatalog(List<I18nRoot> roots, List<CatalogEntry> catalog,
    List<Occurrence> dynamics, List<HardcodedString> hardcoded, string lang, string repoRoot)
{
    var opts = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    foreach (var root in roots)
    {
        // Mỗi root chỉ ghi catalog các key thuộc nó (map theo Source).
        var own = catalog.Where(e => e.Source == root.Name).ToList();
        var doc = new
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            source = root.Name,
            count = own.Count,
            entries = own
        };
        var path = Path.Combine(root.I18nDir, "catalog.json");
        File.WriteAllText(path, JsonSerializer.Serialize(doc, opts));
        Console.WriteLine($"📝 catalog.json ({own.Count} key) → {Path.GetRelativePath(repoRoot, path).Replace('\\', '/')}");
    }
}

/// <summary>
/// Merge khung {lang}.json cho từng root: thêm key thiếu (value rỗng = chưa dịch),
/// GIỮ NGUYÊN bản dịch đã có, không xoá key lạ (chỉ cảnh báo).
/// </summary>
static void MergeLangFiles(List<I18nRoot> roots, List<CatalogEntry> catalog, string lang)
{
    var opts = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    foreach (var root in roots)
    {
        var path = Path.Combine(root.I18nDir, $"{lang}.json");
        var existing = LoadJsonMap(path);
        var keys = catalog.Where(e => e.Source == root.Name).Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        int added = 0;
        foreach (var k in keys)
            if (!existing.ContainsKey(k)) { existing[k] = ""; added++; }

        var stale = existing.Keys.Where(k => !keys.Contains(k)).ToList();
        var ordered = existing.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                              .ToDictionary(kv => kv.Key, kv => kv.Value);
        File.WriteAllText(path, JsonSerializer.Serialize(ordered, opts));
        Console.WriteLine($"🌐 {lang}.json [{root.Name}] : +{added} key mới, {stale.Count} key lạ"
            + (stale.Count > 0 ? $" (giữ lại): {string.Join(", ", stale.Take(5))}{(stale.Count > 5 ? "…" : "")}" : ""));
    }
}

/// <summary>Ghi báo cáo hardcode + key động ra i18n-report.md tại root đầu tiên.</summary>
static void WriteHardcodedReport(List<I18nRoot> roots, List<HardcodedString> hardcoded, List<Occurrence> dynamics)
{
    var root = roots[0];
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# Báo cáo i18n — chuỗi cần xử lý").AppendLine();
    sb.AppendLine($"_Sinh tự động: {DateTime.Now:yyyy-MM-dd HH:mm}_").AppendLine();

    sb.AppendLine($"## 1. Chuỗi tiếng Việt hardcode ({hardcoded.Count}) — cần bọc L()").AppendLine();
    foreach (var h in hardcoded.OrderBy(h => h.File).ThenBy(h => h.Line))
        sb.AppendLine($"- `{h.File}:{h.Line}` — \"{h.Text}\"");
    sb.AppendLine();

    sb.AppendLine($"## 2. L() key dựng động ({dynamics.Count}) — chỉ runtime (phương án A) lấy được").AppendLine();
    foreach (var d in dynamics.OrderBy(d => d.File).ThenBy(d => d.Line))
        sb.AppendLine($"- `{d.File}:{d.Line}`");

    var path = Path.Combine(root.ProjectDir, "wwwroot", "i18n", "i18n-report.md");
    File.WriteAllText(path, sb.ToString());
    Console.WriteLine($"📋 i18n-report.md → {path}");
}

/// <summary>Đọc file json {key:value}; rỗng nếu không tồn tại/parse lỗi.</summary>
static Dictionary<string, string> LoadJsonMap(string path)
{
    if (!File.Exists(path)) return new(StringComparer.Ordinal);
    try
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        return map is null ? new(StringComparer.Ordinal) : new(map, StringComparer.Ordinal);
    }
    catch { return new(StringComparer.Ordinal); }
}

/// <summary>In nhanh vài chuỗi hardcode đầu tiên khi dry-run.</summary>
static void PrintTop(List<HardcodedString> hardcoded)
{
    if (hardcoded.Count == 0) return;
    Console.WriteLine("\nVài chuỗi hardcode đầu tiên:");
    foreach (var h in hardcoded.Take(15)) Console.WriteLine($"  {h.File}:{h.Line}  \"{h.Text}\"");
}
