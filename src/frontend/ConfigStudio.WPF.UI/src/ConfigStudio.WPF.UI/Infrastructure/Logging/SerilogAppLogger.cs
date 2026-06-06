// File    : SerilogAppLogger.cs
// Module  : Infrastructure.Logging
// Layer   : Presentation
// Purpose : Implementation IAppLogger bằng Serilog. Ghi 2 file tách biệt:
//             - sql-errors-yyyymmdd.log : chỉ SqlException (kèm Number/Procedure/Line)
//             - app-yyyymmdd.log        : lỗi C#/.NET + thông tin chẩn đoán
//           Phân loại dựa trên DbErrorClassifier, đánh dấu qua property "ErrorKind".

using System.IO;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace ConfigStudio.WPF.UI.Infrastructure.Logging;

/// <summary>
/// Logger Serilog cho ConfigStudio. Tách lỗi SQL và lỗi C# ra 2 file riêng
/// bằng filter trên property <c>ErrorKind</c> ("Sql" / "Dotnet").
/// </summary>
public sealed class SerilogAppLogger : IAppLogger, IDisposable
{
    private const string SqlKind    = "Sql";
    private const string DotnetKind = "Dotnet";

    private readonly Logger _log;

    public SerilogAppLogger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ICare247", "ConfigStudio", "logs");
        Directory.CreateDirectory(logDir);

        const string template =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        _log = new LoggerConfiguration()
            .MinimumLevel.Information()
            // ── File SQL: chỉ bản ghi có ErrorKind == "Sql" ──────────────
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.WithProperty<string>("ErrorKind", k => k == SqlKind))
                .WriteTo.File(
                    Path.Combine(logDir, "sql-errors-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: template))
            // ── File App: lỗi C#/.NET + Info (mọi thứ KHÔNG phải SQL) ─────
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.WithProperty<string>("ErrorKind", k => k == SqlKind))
                .WriteTo.File(
                    Path.Combine(logDir, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: template))
            .CreateLogger();
    }

    /// <inheritdoc />
    public void Capture(Exception ex, string? context = null)
    {
        var ctx = string.IsNullOrWhiteSpace(context) ? "-" : context;

        if (DbErrorClassifier.TryGetSqlException(ex, out var sql) && sql is not null)
        {
            _log.ForContext("ErrorKind", SqlKind)
                .Error(ex,
                    "SQL error [{Number}] severity={Class} state={State} proc={Procedure} line={LineNumber} server={Server} | {Context} | {Friendly}",
                    sql.Number, sql.Class, sql.State,
                    string.IsNullOrEmpty(sql.Procedure) ? "(ad-hoc)" : sql.Procedure,
                    sql.LineNumber, sql.Server, ctx,
                    DbErrorClassifier.ToFriendlyMessage(sql) ?? "-");
        }
        else
        {
            _log.ForContext("ErrorKind", DotnetKind)
                .Error(ex, "App error: {ExceptionType} | {Context}", ex.GetType().Name, ctx);
        }
    }

    /// <inheritdoc />
    public void Info(string message, string? context = null)
    {
        _log.ForContext("ErrorKind", DotnetKind)
            .Information("{Message} | {Context}", message,
                string.IsNullOrWhiteSpace(context) ? "-" : context);
    }

    /// <inheritdoc />
    public void Flush() => _log.Dispose();

    public void Dispose() => _log.Dispose();
}
