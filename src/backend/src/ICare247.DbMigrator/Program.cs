// =============================================================================
// File    : Program.cs
// Project : ICare247.DbMigrator
// Purpose : Chạy DB migrations cho ICare247_Config database dùng DbUp.
//           Tất cả .sql files trong thư mục db/ được chạy theo thứ tự tên file.
//
// Usage:
//   dotnet run -- "<connectionString>"
//   dotnet run -- "<connectionString>" "<path-to-db-folder>"
//
// Environment variables (fallback nếu không có args):
//   ICARE247_CONFIG_CONNECTION : connection string
//   ICARE247_DB_SCRIPTS_PATH   : đường dẫn tới thư mục chứa .sql files
// =============================================================================

using DbUp;
using DbUp.Engine;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Resolve connection string ─────────────────────────────────────────────────
var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ICARE247_CONFIG_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("LỖI: Cần cung cấp connection string.");
    Console.Error.WriteLine("     Cách 1: dotnet run -- \"<connectionString>\"");
    Console.Error.WriteLine("     Cách 2: set ICARE247_CONFIG_CONNECTION=<connectionString>");
    Console.ResetColor();
    return 1;
}

// ── Resolve scripts folder ────────────────────────────────────────────────────
string scriptsPath;
if (args.Length > 1)
{
    scriptsPath = args[1];
}
else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ICARE247_DB_SCRIPTS_PATH")))
{
    scriptsPath = Environment.GetEnvironmentVariable("ICARE247_DB_SCRIPTS_PATH")!;
}
else
{
    // Mặc định: tìm thư mục db/ tương đối so với vị trí project
    // Khi chạy từ repo root: db/
    // Khi chạy từ project folder: ../../../../db/  (src/backend/src/ICare247.DbMigrator → db/)
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "db"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "db"),
        Path.Combine(AppContext.BaseDirectory, "db"),
    };

    scriptsPath = candidates.FirstOrDefault(Directory.Exists)
        ?? Path.Combine(Directory.GetCurrentDirectory(), "db");
}

scriptsPath = Path.GetFullPath(scriptsPath);

if (!Directory.Exists(scriptsPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"LỖI: Thư mục scripts không tồn tại: {scriptsPath}");
    Console.Error.WriteLine("     Dùng arg thứ 2 hoặc env ICARE247_DB_SCRIPTS_PATH để chỉ định.");
    Console.ResetColor();
    return 1;
}

// ── Print header ──────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  ICare247 DB Migrator  (DbUp + SQL Server)");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.ResetColor();
Console.WriteLine($"  Scripts : {scriptsPath}");
Console.WriteLine($"  DB      : {MaskConnectionString(connectionString)}");
Console.WriteLine();

// ── Ensure database exists ────────────────────────────────────────────────────
EnsureDatabase.For.SqlDatabase(connectionString);

// ── Configure DbUp ────────────────────────────────────────────────────────────
var upgradeEngine = DeployChanges.To
    .SqlDatabase(connectionString)
    // Đọc tất cả .sql files từ thư mục db/ theo thứ tự tên file (000, 001, 002,...)
    .WithScriptsFromFileSystem(
        scriptsPath,
        f => Path.GetExtension(f).Equals(".sql", StringComparison.OrdinalIgnoreCase))
    // Bảng tracking: ICare247_Migrations (thay vì SchemaVersions mặc định)
    .JournalToSqlTable("dbo", "ICare247_Migrations")
    // Mỗi script chạy trong transaction riêng — nếu fail thì rollback script đó
    .WithTransactionPerScript()
    // Log ra console với màu sắc
    .LogToConsole()
    .Build();

// ── Kiểm tra scripts cần chạy ─────────────────────────────────────────────────
var scriptsToExecute = upgradeEngine.GetScriptsToExecute();
if (!scriptsToExecute.Any())
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Database đã up-to-date. Không có migration mới.");
    Console.ResetColor();
    return 0;
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"Cần chạy {scriptsToExecute.Count} migration(s):");
foreach (var script in scriptsToExecute)
    Console.WriteLine($"  → {script.Name}");
Console.ResetColor();
Console.WriteLine();

// ── Chạy migrations ───────────────────────────────────────────────────────────
var result = upgradeEngine.PerformUpgrade();

Console.WriteLine();
if (result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Tất cả migrations đã chạy thành công!");
    Console.ResetColor();
    return 0;
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("✗ Migration thất bại!");
    Console.WriteLine($"  Script : {result.ErrorScript?.Name ?? "unknown"}");
    Console.WriteLine($"  Lỗi   : {result.Error?.Message ?? "unknown error"}");
    Console.ResetColor();
    return 1;
}

// ── Helper: ẩn password trong connection string khi in ra màn hình ───────────
static string MaskConnectionString(string cs)
{
    // Che password để không lộ trong log
    return System.Text.RegularExpressions.Regex.Replace(
        cs,
        @"(Password|pwd)=([^;]+)",
        "$1=***",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
