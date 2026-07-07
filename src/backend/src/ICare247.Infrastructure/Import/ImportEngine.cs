// File    : ImportEngine.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : ClosedXML implementation của IImportEngine — đọc workbook, trim, validate (format/required/FK/
//           trùng khoá), dựng kế hoạch upsert khoá ghép, làm mờ Row_Json log. Spec 25 §11–§13, ADR-034.

using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Import;

/// <summary>
/// Phân tích file import theo cấu hình View: map cột theo tiêu đề, trim mọi ô, validate kiểu/bắt buộc/FK,
/// resolve Mã→Id (lọc quyền qua <see cref="IFkLookupResolver"/>), dựng khoá ghép và phân loại NEW/UPDATE/ERROR.
/// An toàn injection: identifier whitelist + Dapper params. KHÔNG ghi DB.
/// </summary>
public sealed partial class ImportEngine : IImportEngine
{
    private readonly IFkLookupResolver _fk;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ImportEngine> _logger;

    private const char KeySeparator = '';   // ngăn cách phần khoá ghép (như ConfigSync)

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public ImportEngine(
        IFkLookupResolver fk, IDataDbConnectionFactory dataDb, ILogger<ImportEngine> logger)
    {
        _fk = fk;
        _dataDb = dataDb;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportPlan> BuildPlanAsync(
        ImportPlanRequest req, Stream workbook, CancellationToken ct = default)
    {
        var fileErrors = new List<ImportCellError>();

        // ── Mở workbook + chọn sheet chính ──────────────────────────────────
        IXLWorksheet ws;
        try
        {
            var wb = new XLWorkbook(workbook);
            ws = wb.Worksheets.FirstOrDefault(w =>
                     string.Equals(w.Name, req.SheetName, StringComparison.OrdinalIgnoreCase))
                 ?? wb.Worksheets.First();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Import: không đọc được workbook cho View {View}", req.View.ViewCode);
            fileErrors.Add(new ImportCellError(null, "import.file.invalid", []));
            return Empty(fileErrors);
        }

        // ── Map tiêu đề (dòng 1) → cột theo Caption/FieldName ────────────────
        var headerMap = MapHeaders(ws, req.Fields);
        foreach (var f in req.Fields)
        {
            if (f.Required && !headerMap.ContainsKey(f.FieldName))
                fileErrors.Add(new ImportCellError(f.FieldName, "import.column.missing", [f.Caption]));
        }
        if (fileErrors.Count > 0)
            return Empty(fileErrors);

        // ── FK: nạp bảng tra Mã→Id (lọc quyền) cho các cột FK có trong file ──
        //    Định nghĩa FK lấy từ field Edit_Form (req.FkColumns) — đúng cho mọi màn, kể cả view JOIN tay.
        var fkMaps = new Dictionary<string, FkCodeMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in req.FkColumns)
        {
            if (!headerMap.ContainsKey(def.FieldName)) continue;
            var map = await _fk.BuildCodeMapAsync(def, ct);
            if (!map.HasCodeField)
            {
                fileErrors.Add(new ImportCellError(def.FieldName, "import.fk.no_code_field", [def.FieldName]));
                continue;
            }
            if (map.HasAmbiguousCode)
            {
                // Resolve toàn cục (bỏ lọc cha) nhưng Mã con trùng ⇒ không thể chọn Id đúng → từ chối cả file.
                fileErrors.Add(new ImportCellError(def.FieldName, "import.fk.ambiguous_code", [def.FieldName]));
                continue;
            }
            fkMaps[def.FieldName] = map;
        }
        if (fileErrors.Count > 0)
            return Empty(fileErrors);

        var fieldByName = req.Fields.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);
        var keyFields = req.KeyFields
            .Where(k => fieldByName.ContainsKey(k) || fkMaps.ContainsKey(k))
            .ToList();

        // ── Duyệt từng dòng dữ liệu (từ dòng 2) ─────────────────────────────
        var parsed = new List<ParsedRow>();
        var skipped = 0;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            ct.ThrowIfCancellationRequested();

            var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var anyValue = false;
            foreach (var (field, colNum) in headerMap)
            {
                var text = ws.Cell(r, colNum).GetString().Trim();   // trim đầu/cuối (§11.2 b1)
                raw[field] = text.Length == 0 ? null : text;
                if (text.Length != 0) anyValue = true;
            }
            if (!anyValue) { skipped++; continue; }   // dòng rỗng hoàn toàn → bỏ

            var (values, errors) = ValidateRow(req.Fields, raw, fkMaps);
            var compositeKey = keyFields.Count > 0 ? BuildCompositeKey(keyFields, values) : null;
            parsed.Add(new ParsedRow(r, raw, values, errors, compositeKey));
        }

        // ── Nạp 1 lần tập khoá hiện có (upsert) — sau khi có giá trị resolve ──
        var existing = keyFields.Count > 0
            ? await LoadExistingKeysAsync(req, keyFields, ct)
            : new Dictionary<string, long>(StringComparer.Ordinal);

        // ── Phân loại NEW/UPDATE/ERROR + phát hiện trùng khoá trong file ────
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<ImportRow>(parsed.Count);
        int news = 0, updates = 0, errs = 0;
        foreach (var p in parsed)
        {
            var errorsList = new List<ImportCellError>(p.Errors);
            ImportRowOperation op;
            long? matchedId = null;

            if (errorsList.Count > 0)
                op = ImportRowOperation.Error;
            else if (keyFields.Count == 0 || p.CompositeKey is null)
                op = ImportRowOperation.New;
            else if (!seen.Add(p.CompositeKey))
            {
                errorsList.Add(new ImportCellError(keyFields[0], "import.duplicate.key", [keyFields[0]]));
                op = ImportRowOperation.Error;
            }
            else if (existing.TryGetValue(p.CompositeKey, out var id))
            {
                op = ImportRowOperation.Update;
                matchedId = id;
            }
            else
                op = ImportRowOperation.New;

            switch (op)
            {
                case ImportRowOperation.New: news++; break;
                case ImportRowOperation.Update: updates++; break;
                case ImportRowOperation.Error: errs++; break;
            }

            // Row_Json (đã làm mờ) chỉ cần cho dòng lỗi (log detail chỉ ghi dòng lỗi — §13.2).
            var maskedJson = op == ImportRowOperation.Error
                ? BuildMaskedRowJson(req.Fields, p.Raw)
                : null;

            rows.Add(new ImportRow(p.RowNumber, op, matchedId, p.Values, errorsList, maskedJson));
        }

        var summary = new ImportSummary(rows.Count, news, updates, errs, skipped);
        return new ImportPlan(rows, [], summary);
    }

    /// <summary>Kế hoạch rỗng (chỉ lỗi cấp file) — không phân tích dòng nào.</summary>
    private static ImportPlan Empty(IReadOnlyList<ImportCellError> fileErrors) =>
        new([], fileErrors, new ImportSummary(0, 0, 0, 0, 0));

    /// <summary>Map tiêu đề dòng 1 → số cột theo Caption (bỏ hậu tố '*') rồi FieldName. Không phát sự kiện.</summary>
    private static Dictionary<string, int> MapHeaders(IXLWorksheet ws, IReadOnlyList<ImportFieldSpec> fields)
    {
        // Tiêu đề chuẩn hóa (Caption bỏ '*'/trim, hoặc FieldName) → số cột.
        var headerToCol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = ws.FirstRowUsed();
        if (headerRow is not null)
        {
            foreach (var cell in headerRow.CellsUsed())
            {
                var text = cell.GetString().TrimEnd('*', ' ').Trim();
                if (text.Length > 0)
                    headerToCol.TryAdd(text, cell.Address.ColumnNumber);
            }
        }

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in fields)
        {
            if (headerToCol.TryGetValue(f.Caption.TrimEnd('*', ' ').Trim(), out var c1))
                map[f.FieldName] = c1;
            else if (headerToCol.TryGetValue(f.FieldName, out var c2))
                map[f.FieldName] = c2;
        }
        return map;
    }

    /// <summary>Validate 1 dòng: required + kiểu + FK resolve Mã→Id. Trả (giá trị đã ép kiểu, danh sách lỗi).</summary>
    private static (Dictionary<string, object?> Values, List<ImportCellError> Errors) ValidateRow(
        IReadOnlyList<ImportFieldSpec> fields,
        IReadOnlyDictionary<string, string?> raw,
        IReadOnlyDictionary<string, FkCodeMap> fkMaps)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ImportCellError>();

        foreach (var f in fields)
        {
            raw.TryGetValue(f.FieldName, out var text);   // đã trim; null nếu rỗng/không có cột

            // Cột FK → resolve Mã→Id.
            if (fkMaps.TryGetValue(f.FieldName, out var fkMap))
            {
                if (string.IsNullOrEmpty(text))
                {
                    if (f.Required) errors.Add(new ImportCellError(f.FieldName, "import.required.missing", [f.Caption]));
                    values[f.FieldName] = null;
                }
                else if (fkMap.TryResolve(text, out var id))
                    values[f.FieldName] = id;
                else
                    errors.Add(new ImportCellError(f.FieldName, "import.fk.code_not_found",
                        [f.Caption, MaskForField(f, text)!]));
                continue;
            }

            // Cột thường.
            if (string.IsNullOrEmpty(text))
            {
                if (f.Required) errors.Add(new ImportCellError(f.FieldName, "import.required.missing", [f.Caption]));
                values[f.FieldName] = null;
                continue;
            }
            if (TryConvert(text, f.NetType, out var val))
                values[f.FieldName] = val;
            else
                errors.Add(new ImportCellError(f.FieldName, "import.format.invalid",
                    [f.Caption, MaskForField(f, text)!]));
        }
        return (values, errors);
    }

    /// <summary>Ép chuỗi (đã trim) sang kiểu <paramref name="netType"/>; false nếu sai định dạng.</summary>
    private static bool TryConvert(string raw, string netType, out object? value)
    {
        value = null;
        switch (netType.ToLowerInvariant())
        {
            case "int" or "int32" or "short" or "int16" or "byte":
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { value = i; return true; }
                return false;
            case "long" or "int64":
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) { value = l; return true; }
                return false;
            case "decimal" or "double" or "float" or "single" or "money":
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) { value = d; return true; }
                return false;
            case "bool" or "boolean" or "bit":
                if (raw is "1" or "true" or "True" or "TRUE") { value = true; return true; }
                if (raw is "0" or "false" or "False" or "FALSE") { value = false; return true; }
                return false;
            case "datetime" or "date":
                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    || DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                { value = dt; return true; }
                return false;
            case "guid" or "uniqueidentifier":
                if (Guid.TryParse(raw, out var g)) { value = g; return true; }
                return false;
            default:
                value = raw;   // string
                return true;
        }
    }

    /// <summary>Dựng khoá ghép chuẩn hóa từ các field khoá; null nếu thiếu bất kỳ phần khoá.</summary>
    private static string? BuildCompositeKey(
        IReadOnlyList<string> keyFields, IReadOnlyDictionary<string, object?> values)
    {
        var parts = new string[keyFields.Count];
        for (var i = 0; i < keyFields.Count; i++)
        {
            if (!values.TryGetValue(keyFields[i], out var v) || v is null)
                return null;   // thiếu phần khoá → không match được (coi như New)
            parts[i] = NormalizeKeyPart(v);
        }
        return string.Join(KeySeparator, parts);
    }

    /// <summary>Chuẩn hóa 1 phần khoá: chuỗi = trim+upper-invariant; số/id = chuỗi invariant.</summary>
    private static string NormalizeKeyPart(object? v) => v switch
    {
        null => "",
        string s => s.Trim().ToUpperInvariant(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? ""
    };

    /// <summary>Nạp 1 lần tập khoá hiện có (bảng đích) → map khoá ghép → Id (upsert). Không phát sự kiện.</summary>
    private async Task<Dictionary<string, long>> LoadExistingKeysAsync(
        ImportPlanRequest req, IReadOnlyList<string> keyFields, CancellationToken ct)
    {
        var result = new Dictionary<string, long>(StringComparer.Ordinal);

        if (!SafeIdentifierRegex().IsMatch(req.TargetTable) || !SafeIdentifierRegex().IsMatch(req.PkColumn)
            || !SafeIdentifierRegex().IsMatch(req.Schema)
            || keyFields.Any(k => !SafeIdentifierRegex().IsMatch(k)))
            return result;

        var cols = string.Join(", ", keyFields.Select(Bracket));
        var sql = $"SELECT {Bracket(req.PkColumn)} AS __id, {cols} " +
                  $"FROM {Bracket(req.Schema)}.{Bracket(req.TargetTable)}";

        using var conn = _dataDb.CreateConnection();
        var dbRows = await conn.QueryAsync(new CommandDefinition(sql, cancellationToken: ct));
        foreach (var row in dbRows)
        {
            var d = (IDictionary<string, object?>)row;
            if (!d.TryGetValue("__id", out var idObj) || idObj is null) continue;

            var parts = new string[keyFields.Count];
            var ok = true;
            for (var i = 0; i < keyFields.Count; i++)
            {
                if (!d.TryGetValue(keyFields[i], out var v) || v is null) { ok = false; break; }
                parts[i] = NormalizeKeyPart(v);
            }
            if (!ok) continue;

            var key = string.Join(KeySeparator, parts);
            result[key] = Convert.ToInt64(idObj, CultureInfo.InvariantCulture);   // trùng khoá DB → bản sau thắng
        }
        return result;
    }

    /// <summary>Serialize Row_Json (mọi cột) đã LÀM MỜ cột nhạy cảm — dùng ghi log dòng lỗi (§13.3).</summary>
    private static string BuildMaskedRowJson(
        IReadOnlyList<ImportFieldSpec> fields, IReadOnlyDictionary<string, string?> raw)
    {
        var obj = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var f in fields)
        {
            raw.TryGetValue(f.FieldName, out var text);
            obj[f.FieldName] = MaskForField(f, text);
        }
        return JsonSerializer.Serialize(obj);
    }

    /// <summary>Làm mờ giá trị theo cấu hình cột (Full/Partial/Hash); trả nguyên nếu cột không bật mờ.</summary>
    private static string? MaskForField(ImportFieldSpec f, string? raw)
    {
        if (!f.IsMasked || string.IsNullOrEmpty(raw))
            return raw;

        return (f.MaskMode ?? "Full").ToLowerInvariant() switch
        {
            "partial" => raw.Length > 4 ? new string('*', raw.Length - 4) + raw[^4..] : "***",
            "hash" => "sha256:" + Convert.ToHexString(
                          SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16].ToLowerInvariant(),
            _ => "***"   // Full
        };
    }

    /// <summary>Bọc identifier trong <c>[]</c>, escape <c>]</c>.</summary>
    private static string Bracket(string ident) => "[" + ident.Replace("]", "]]") + "]";

    /// <summary>Dòng đã phân tích (trước phân loại NEW/UPDATE/ERROR).</summary>
    private sealed record ParsedRow(
        int RowNumber,
        IReadOnlyDictionary<string, string?> Raw,
        Dictionary<string, object?> Values,
        List<ImportCellError> Errors,
        string? CompositeKey);
}
