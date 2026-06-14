// DxReflect — trích [Parameter] của các component DevExpress.Blazor qua MetadataLoadContext
// (đọc metadata, KHÔNG nạp/chạy code). Dùng sinh file note docs/reference/DEVEXPRESS_*.md.
//
// Cách dùng: dotnet run -- <filter> [dxDllPath]
//   filter   : chuỗi lọc tên type (vd "TreeList", "Grid", "ComboBox").
//   dxDllPath: đường dẫn DevExpress.Blazor.v25.2.dll (mặc định dò trong NuGet cache).

using System.Reflection;

string filter = args.Length > 0 ? args[0] : "TreeList";

string dxDll = args.Length > 1
    ? args[1]
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".nuget", "packages", "devexpress.blazor", "25.2.3", "lib", "net8.0", "DevExpress.Blazor.v25.2.dll");

if (!File.Exists(dxDll)) { Console.Error.WriteLine($"Không thấy DLL: {dxDll}"); return 1; }

// Gom assembly runtime (.NET + ASP.NET Core) + thư mục DevExpress làm nguồn resolve.
var paths = new List<string>();
foreach (var fw in new[] { "Microsoft.NETCore.App", "Microsoft.AspNetCore.App" })
{
    var baseDir = Path.Combine(@"C:\Program Files\dotnet\shared", fw);
    if (Directory.Exists(baseDir))
    {
        var latest = Directory.GetDirectories(baseDir).OrderBy(x => x).Last();
        paths.AddRange(Directory.GetFiles(latest, "*.dll"));
    }
}
paths.AddRange(Directory.GetFiles(Path.GetDirectoryName(dxDll)!, "*.dll"));

// Phụ thuộc DevExpress nằm rải ở nhiều gói (devexpress.data, devexpress.drawing…).
// Quét toàn bộ DLL DevExpress trong NuGet cache (ưu tiên net8.0 → netstandard2.0).
var nuget = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
foreach (var pkg in Directory.GetDirectories(nuget, "devexpress.*"))
    foreach (var dll in Directory.GetFiles(pkg, "*.dll", SearchOption.AllDirectories)
                 .Where(f => f.Contains(@"\net8.0\") || f.Contains(@"\netstandard2.0\")))
        paths.Add(dll);

// Khử trùng theo TÊN file (PathAssemblyResolver không cho 2 path cùng simple-name).
var byName = paths.GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
    .Select(g => g.First());
var resolver = new PathAssemblyResolver(byName);
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(dxDll);
var ver = asm.GetName().Version;

bool IsParameter(PropertyInfo p) => p.GetCustomAttributesData()
    .Any(a => a.AttributeType.FullName == "Microsoft.AspNetCore.Components.ParameterAttribute");

var types = asm.GetExportedTypes()
    .Where(t => t.IsClass && t.Name.StartsWith("Dx", StringComparison.Ordinal)
                && t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
    .OrderBy(t => t.Name);

Console.WriteLine($"# DevExpress Blazor — {filter}: Toàn bộ thuộc tính");
Console.WriteLine();
Console.WriteLine($"> Trích xuất tự động từ `DevExpress.Blazor.v25.2` **v{ver}** (đúng version project đang dùng).");
Console.WriteLine($"> Công cụ: `tools/DxReflect` (MetadataLoadContext). Cột **P** = `[Parameter]` (dùng trực tiếp trong `.razor`).");

foreach (var t in types)
{
    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(IsParameter)
        .GroupBy(p => p.Name).Select(g => g.First())
        .OrderBy(p => p.Name)
        .ToList();
    if (props.Count == 0) continue;

    Console.WriteLine();
    Console.WriteLine($"## {t.Name}");
    Console.WriteLine();
    Console.WriteLine($"Tổng: **{props.Count}** thuộc tính (đều là `[Parameter]`).");
    Console.WriteLine();
    Console.WriteLine("| P | Thuộc tính | Kiểu |");
    Console.WriteLine("|---|---|---|");
    foreach (var p in props)
        Console.WriteLine($"| ✅ | `{p.Name}` | `{FriendlyType(p.PropertyType)}` |");
}

return 0;

// Tên kiểu gọn (bỏ namespace, rút gọn generic) cho dễ đọc.
static string FriendlyType(Type t)
{
    if (t.IsGenericType)
    {
        var name = t.Name[..t.Name.IndexOf('`')];
        var args = string.Join(", ", t.GetGenericArguments().Select(FriendlyType));
        if (name == "Nullable") return FriendlyType(t.GetGenericArguments()[0]) + "?";
        return $"{name}<{args}>";
    }
    return t.Name;
}
