// File    : BuiltinFunctions.cs
// Module  : Engines
// Layer   : Application
// Purpose : Đăng ký tất cả built-in functions vào FunctionRegistry.

using System.Globalization;

namespace ICare247.Application.Engines;

/// <summary>
/// Đăng ký tất cả built-in functions theo Grammar V1 spec.
/// Null-safe: hầu hết function trả null khi nhận null argument.
/// </summary>
public static class BuiltinFunctions
{
    /// <summary>
    /// Đăng ký tất cả functions vào registry.
    /// </summary>
    public static void RegisterAll(FunctionRegistry registry)
    {
        // ── String functions ─────────────────────────────────
        registry.Register("len", 1, 1, (args, _) =>
        {
            var val = args[0];
            if (val is null) return null;
            return Convert.ToString(val)?.Length ?? 0;
        });

        registry.Register("trim", 1, 1, (args, _) =>
        {
            var val = args[0];
            if (val is null) return null;
            return Convert.ToString(val)?.Trim();
        });

        registry.Register("upper", 1, 1, (args, _) =>
        {
            var val = args[0];
            if (val is null) return null;
            return Convert.ToString(val)?.ToUpperInvariant();
        });

        registry.Register("lower", 1, 1, (args, _) =>
        {
            var val = args[0];
            if (val is null) return null;
            return Convert.ToString(val)?.ToLowerInvariant();
        });

        registry.Register("concat", 1, -1, (args, _) =>
        {
            // concat trả "" nếu tất cả null, không trả null
            return string.Concat(args.Select(a => Convert.ToString(a) ?? ""));
        });

        registry.Register("substring", 2, 3, (args, _) =>
        {
            if (args[0] is null) return null;
            var str = Convert.ToString(args[0]) ?? "";
            var start = ToInt(args[1]) ?? 0;
            if (start >= str.Length) return "";
            if (args.Length > 2)
            {
                var length = ToInt(args[2]) ?? str.Length;
                return str.Substring(start, Math.Min(length, str.Length - start));
            }
            return str[start..];
        });

        registry.Register("contains", 2, 2, (args, _) =>
        {
            if (args[0] is null || args[1] is null) return null;
            var str = Convert.ToString(args[0]) ?? "";
            var search = Convert.ToString(args[1]) ?? "";
            return str.Contains(search, StringComparison.OrdinalIgnoreCase);
        });

        registry.Register("startsWith", 2, 2, (args, _) =>
        {
            if (args[0] is null || args[1] is null) return null;
            var str = Convert.ToString(args[0]) ?? "";
            var prefix = Convert.ToString(args[1]) ?? "";
            return str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        });

        registry.Register("endsWith", 2, 2, (args, _) =>
        {
            if (args[0] is null || args[1] is null) return null;
            var str = Convert.ToString(args[0]) ?? "";
            var suffix = Convert.ToString(args[1]) ?? "";
            return str.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        });

        // ── Math functions ───────────────────────────────────
        registry.Register("round", 1, 2, (args, _) =>
        {
            var val = ToDouble(args[0]);
            if (val is null) return null;
            var decimals = args.Length > 1 ? (ToInt(args[1]) ?? 0) : 0;
            return Math.Round(val.Value, decimals, MidpointRounding.AwayFromZero);
        });

        registry.Register("floor", 1, 1, (args, _) =>
        {
            var val = ToDouble(args[0]);
            return val.HasValue ? Math.Floor(val.Value) : null;
        });

        registry.Register("ceil", 1, 1, (args, _) =>
        {
            var val = ToDouble(args[0]);
            return val.HasValue ? Math.Ceiling(val.Value) : null;
        });

        registry.Register("abs", 1, 1, (args, _) =>
        {
            var val = ToDouble(args[0]);
            return val.HasValue ? Math.Abs(val.Value) : null;
        });

        registry.Register("min", 2, -1, (args, _) =>
        {
            double? result = null;
            foreach (var arg in args)
            {
                var val = ToDouble(arg);
                if (val is null) continue;
                result = result is null ? val.Value : Math.Min(result.Value, val.Value);
            }
            return result;
        });

        registry.Register("max", 2, -1, (args, _) =>
        {
            double? result = null;
            foreach (var arg in args)
            {
                var val = ToDouble(arg);
                if (val is null) continue;
                result = result is null ? val.Value : Math.Max(result.Value, val.Value);
            }
            return result;
        });

        // ── Logic functions ──────────────────────────────────
        registry.Register("iif", 3, 3, (args, _) =>
        {
            var condition = ToBool(args[0]);
            if (condition is null) return null;
            return condition.Value ? args[1] : args[2];
        });

        registry.Register("isNull", 1, 1, (args, _) => args[0] is null);

        registry.Register("coalesce", 1, -1, (args, _) =>
        {
            foreach (var arg in args)
            {
                if (arg is not null) return arg;
            }
            return null;
        });

        // ── Date functions ───────────────────────────────────
        registry.Register("today", 0, 0, (_, _) => DateTime.Today);

        registry.Register("now", 0, 0, (_, _) => DateTime.Now);

        registry.Register("toDate", 1, 2, (args, _) =>
        {
            if (args[0] is null) return null;
            if (args[0] is DateTime dt) return dt;

            var str = Convert.ToString(args[0]);
            if (string.IsNullOrWhiteSpace(str)) return null;

            var format = args.Length > 1 ? Convert.ToString(args[1]) : null;

            if (!string.IsNullOrEmpty(format))
            {
                return DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed) ? parsed : null;
            }

            return DateTime.TryParse(str, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result) ? result : null;
        });

        registry.Register("dateDiff", 3, 3, (args, _) =>
        {
            if (args[1] is null || args[2] is null) return null;

            var unit = Convert.ToString(args[0])?.ToLowerInvariant();
            var date1 = ToDateTime(args[1]);
            var date2 = ToDateTime(args[2]);

            if (date1 is null || date2 is null) return null;

            var diff = date2.Value - date1.Value;
            return unit switch
            {
                "day" or "days" => (int)diff.TotalDays,
                "hour" or "hours" => (int)diff.TotalHours,
                "minute" or "minutes" => (int)diff.TotalMinutes,
                "second" or "seconds" => (int)diff.TotalSeconds,
                _ => (int)diff.TotalDays
            };
        });

        // ── Conversion functions ─────────────────────────────
        registry.Register("toNumber", 1, 1, (args, _) =>
        {
            if (args[0] is null) return null;
            var val = ToDouble(args[0]);
            return val;
        });

        registry.Register("toString", 1, 1, (args, _) =>
        {
            if (args[0] is null) return null;
            return Convert.ToString(args[0]);
        });

        registry.Register("toBool", 1, 1, (args, _) =>
        {
            if (args[0] is null) return null;
            return ToBool(args[0]);
        });
    }

    // ── Type conversion helpers (null-safe) ──────────────────

    internal static double? ToDouble(object? value) => value switch
    {
        null => null,
        double d => d,
        int i => i,
        long l => l,
        decimal m => (double)m,
        float f => f,
        string s => double.TryParse(s, CultureInfo.InvariantCulture, out var r) ? r : null,
        bool b => b ? 1.0 : 0.0,
        _ => null
    };

    internal static int? ToInt(object? value) => value switch
    {
        null => null,
        int i => i,
        long l => (int)l,
        double d => (int)d,
        decimal m => (int)m,
        string s => int.TryParse(s, CultureInfo.InvariantCulture, out var r) ? r : null,
        _ => null
    };

    internal static bool? ToBool(object? value) => value switch
    {
        null => null,
        bool b => b,
        int i => i != 0,
        long l => l != 0,
        double d => d != 0.0,
        string s => s.Length > 0 && !s.Equals("false", StringComparison.OrdinalIgnoreCase)
                     && !s.Equals("0", StringComparison.Ordinal),
        _ => true
    };

    internal static DateTime? ToDateTime(object? value) => value switch
    {
        null => null,
        DateTime dt => dt,
        string s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r) ? r : null,
        _ => null
    };
}
