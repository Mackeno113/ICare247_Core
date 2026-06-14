// File    : Scanner.cs
// Module  : I18nScanner
// Layer   : Tool
// Purpose : Lõi quét — trích lời gọi L("key","fallback") (.cs qua Roslyn, .razor qua regex)
//           và dò chuỗi tiếng Việt hardcode chưa bọc i18n.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICare247.Tools.I18nScanner;

/// <summary>Kết quả quét 1 file.</summary>
/// <param name="Calls">Các lời gọi L() có key + fallback literal.</param>
/// <param name="Dynamic">Các lời gọi L() có key dựng động (chỉ runtime biết).</param>
/// <param name="Hardcoded">Chuỗi tiếng Việt nghi hardcode (chưa qua L).</param>
public sealed record FileScan(
    List<RawCall> Calls,
    List<Occurrence> Dynamic,
    List<HardcodedString> Hardcoded);

/// <summary>1 lời gọi L() thô trích được (chưa gộp theo key).</summary>
/// <param name="Key">Resource key.</param>
/// <param name="Vi">Văn bản gốc tiếng Việt.</param>
/// <param name="Line">Dòng gọi.</param>
public sealed record RawCall(string Key, string Vi, int Line);

/// <summary>
/// Bộ quét i18n. Mỗi file → <see cref="FileScan"/>. Caller tự gộp + map vào i18n-root.
/// </summary>
public static class Scanner
{
    // ── Regex dùng cho .razor ────────────────────────────────────────────

    // L("key", "fallback"  — cho phép escaped quote, fallback verbatim (@") tuỳ chọn.
    // (?<![\w]) chặn khớp khi L là phần đuôi của định danh khác (vd "HtmlL(").
    private static readonly Regex CallRx = new(
        "(?<![\\w])L\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s*,\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"",
        RegexOptions.Compiled);

    // L( theo sau bởi thứ KHÔNG phải dấu " ⇒ key dựng động (biến/nội suy).
    private static readonly Regex DynamicCallRx = new(
        """(?<![\w])L\(\s*(?!")[^)\s,][^,)]*""",
        RegexOptions.Compiled);

    // Có ít nhất 1 ký tự Latin có dấu (đặc trưng tiếng Việt).
    private static readonly Regex VietnameseRx = new(
        "[À-ɏḀ-ỿ]",
        RegexOptions.Compiled);

    // Chuỗi literal "..." (cho dò hardcode trên 1 dòng).
    private static readonly Regex StringLiteralRx = new(
        "@?\"((?:[^\"\\\\]|\\\\.)*)\"",
        RegexOptions.Compiled);

    /// <summary>
    /// Quét 1 file theo phần mở rộng. Sự kiện theo sau: trả về toàn bộ call/dynamic/hardcoded.
    /// </summary>
    public static FileScan Scan(string absPath, string relPath, string ext)
    {
        var text = File.ReadAllText(absPath);
        return ext == ".cs"
            ? ScanCs(text, relPath)
            : ScanRazor(text, relPath);
    }

    // ── .cs qua Roslyn ───────────────────────────────────────────────────

    /// <summary>
    /// Quét file C# bằng Roslyn: tìm mọi invocation tên "L", phân biệt key literal vs động.
    /// Hardcode VN trong .cs cũng được dò theo dòng (bỏ comment).
    /// </summary>
    private static FileScan ScanCs(string text, string relPath)
    {
        var calls = new List<RawCall>();
        var dynamic = new List<Occurrence>();
        // Dòng chứa literal là tham số của log/throw → KHÔNG phải UI, loại khỏi dò hardcode.
        var excluded = new HashSet<int>();

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();

        foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = inv.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                IdentifierNameSyntax id        => id.Identifier.ValueText,
                _ => null
            };

            // Lời gọi logger (Log*, _logger.*) → loại mọi string literal đối số.
            if (name is not null && (name.StartsWith("Log") || inv.Expression.ToString().Contains("ogger")))
            {
                ExcludeLiteralLines(inv, excluded);
                continue;
            }
            if (name != "L") continue;

            var args = inv.ArgumentList.Arguments;
            if (args.Count < 1) continue;

            int line = inv.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var keyLit = AsStringLiteral(args[0].Expression);
            if (keyLit is null) { dynamic.Add(new Occurrence(relPath, line)); continue; }

            var viLit = args.Count >= 2 ? AsStringLiteral(args[1].Expression) ?? "" : "";
            calls.Add(new RawCall(keyLit, viLit, line));
        }

        // throw new XxxException("..."): message kỹ thuật, không i18n.
        foreach (var node in root.DescendantNodes())
            if (node is ThrowStatementSyntax or ThrowExpressionSyntax
                || (node is ObjectCreationExpressionSyntax oc && oc.Type.ToString().EndsWith("Exception")))
                ExcludeLiteralLines(node, excluded);

        var hardcoded = ScanHardcoded(text, relPath, isRazor: false, excluded);
        return new FileScan(calls, dynamic, hardcoded);
    }

    /// <summary>Đánh dấu dòng của mọi string literal con trong 1 node để loại khỏi dò hardcode.</summary>
    private static void ExcludeLiteralLines(SyntaxNode node, HashSet<int> excluded)
    {
        foreach (var lit in node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>())
            if (lit.IsKind(SyntaxKind.StringLiteralExpression))
                excluded.Add(lit.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
    }

    /// <summary>Lấy giá trị string literal (kể cả verbatim/raw); null nếu không phải literal thuần.</summary>
    private static string? AsStringLiteral(ExpressionSyntax expr)
        => expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression)
            ? lit.Token.ValueText
            : null;

    // ── .razor qua regex ─────────────────────────────────────────────────

    /// <summary>Quét file Razor bằng regex (markup + @code dùng chung cú pháp L()).</summary>
    private static FileScan ScanRazor(string text, string relPath)
    {
        var calls = new List<RawCall>();
        var dynamic = new List<Occurrence>();

        foreach (Match m in CallRx.Matches(text))
        {
            int line = LineOf(text, m.Index);
            calls.Add(new RawCall(Unescape(m.Groups[1].Value), Unescape(m.Groups[2].Value), line));
        }

        // Key động: L( không theo sau bởi literal. Trừ các vị trí đã khớp CallRx.
        var literalStarts = new HashSet<int>(CallRx.Matches(text).Select(m => m.Index));
        foreach (Match m in DynamicCallRx.Matches(text))
            if (!literalStarts.Contains(m.Index))
                dynamic.Add(new Occurrence(relPath, LineOf(text, m.Index)));

        var hardcoded = ScanHardcoded(text, relPath, isRazor: true, excludedLines: null);
        return new FileScan(calls, dynamic, hardcoded);
    }

    // ── Dò hardcode tiếng Việt ───────────────────────────────────────────

    /// <summary>
    /// Dò chuỗi tiếng Việt chưa bọc L(): heuristic theo dòng, bỏ comment.
    /// (1) literal "..." chứa dấu VN nhưng KHÔNG phải tham số của L trên dòng đó.
    /// (2) [razor] text giữa thẻ chứa dấu VN, không bắt đầu bằng @.
    /// Kết quả là ỨNG VIÊN cần review (có thể lọt vài false-positive).
    /// </summary>
    private static List<HardcodedString> ScanHardcoded(string text, string relPath, bool isRazor, ISet<int>? excludedLines)
    {
        var result = new List<HardcodedString>();

        // Opt-out cấp file: file định nghĩa dữ liệu-fallback (vd AppNav.cs) đặt marker để bỏ dò hardcode.
        if (text.Contains("i18n:skip-hardcode", StringComparison.Ordinal)) return result;

        var lines = text.Replace("\r\n", "\n").Split('\n');

        bool inBlockC = false;   // /* ... */
        bool inBlockR = false;   // @* ... *@

        for (int i = 0; i < lines.Length; i++)
        {
            if (excludedLines is not null && excludedLines.Contains(i + 1)) continue;

            // Nhận diện khối comment trên dòng GỐC (tránh strip làm hỏng dòng mở @* ).
            var raw = lines[i];
            var rtrim = raw.TrimStart();
            if (inBlockR) { if (raw.Contains("*@")) inBlockR = false; continue; }
            if (inBlockC) { if (raw.Contains("*/")) inBlockC = false; continue; }
            if (rtrim.StartsWith("@*")) { if (!raw.Contains("*@")) inBlockR = true; continue; }
            if (rtrim.StartsWith("/*")) { if (!raw.Contains("*/")) inBlockC = true; continue; }
            if (rtrim.StartsWith("//") || rtrim.StartsWith("*")) continue;

            // Cắt comment // đuôi dòng (ngoài chuỗi) trước khi dò literal.
            var line = StripTrailingComment(raw);

            // Các literal là tham số của L() trên dòng này → bỏ qua (đã i18n).
            var lCallLiterals = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match c in CallRx.Matches(line))
            {
                lCallLiterals.Add(c.Groups[1].Value);
                lCallLiterals.Add(c.Groups[2].Value);
            }

            // (1) literal VN ngoài L()
            foreach (Match s in StringLiteralRx.Matches(line))
            {
                var inner = s.Groups[1].Value;
                if (!VietnameseRx.IsMatch(inner)) continue;
                if (lCallLiterals.Contains(inner)) continue;
                result.Add(new HardcodedString(Trunc(inner), relPath, i + 1));
            }

            // (2) razor text node VN ngoài "..." và không bắt đầu bằng @
            if (isRazor)
                foreach (Match t in Regex.Matches(line, ">([^<>\"]*?)<"))
                {
                    var inner = t.Groups[1].Value.Trim();
                    if (inner.Length == 0 || inner.StartsWith('@')) continue;
                    if (!VietnameseRx.IsMatch(inner)) continue;
                    result.Add(new HardcodedString(Trunc(inner), relPath, i + 1));
                }
        }
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Cắt bỏ comment đuôi dòng (// hoặc @*) nằm NGOÀI chuỗi literal.</summary>
    private static string StripTrailingComment(string line)
    {
        bool inStr = false;
        for (int i = 0; i < line.Length - 1; i++)
        {
            char c = line[i];
            if (c == '\\') { i++; continue; }            // bỏ ký tự escape kế tiếp
            if (c == '"') { inStr = !inStr; continue; }
            if (inStr) continue;
            if (c == '/' && line[i + 1] == '/') return line[..i];
        }
        return line;
    }

    /// <summary>Số dòng (1-based) tại vị trí ký tự index.</summary>
    private static int LineOf(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
            if (text[i] == '\n') line++;
        return line;
    }

    /// <summary>Giải escape cơ bản cho chuỗi lấy từ regex razor (\" \\ \n \t).</summary>
    private static string Unescape(string s) => s
        .Replace("\\\"", "\"").Replace("\\\\", "\\")
        .Replace("\\n", "\n").Replace("\\t", "\t");

    /// <summary>Cắt ngắn chuỗi dài cho báo cáo gọn.</summary>
    private static string Trunc(string s) => s.Length <= 80 ? s : s[..77] + "...";
}
