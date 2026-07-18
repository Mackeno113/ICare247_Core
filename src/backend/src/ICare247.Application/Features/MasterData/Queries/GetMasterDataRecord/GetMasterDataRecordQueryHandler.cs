// File    : GetMasterDataRecordQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler lấy 1 bản ghi danh mục cho form Sửa — GetByIdAsync + suy field ẢO cascade-cha.
//           Bảng gốc chỉ lưu field con (VD PhuongXa_Id); field cha (VD Tỉnh) là VIRTUAL không map cột
//           nên record thô không có → form Sửa để trống. Suy giá trị field ảo theo 2 tầng:
//           (1) CHÍNH — đọc từ VIEW danh sách denormalized của form (view đã JOIN sẵn mọi cấp cha,
//               lấy 1 dòng theo khóa chính là đủ; giải bài đa cấp không cần thứ tự).
//           (2) FALLBACK — form không có view: suy 1 cấp từ Ui_Field_Lookup của field con.

using System.Text.Json;
using System.Text.RegularExpressions;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Entities.Form;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataRecord;

public sealed partial class GetMasterDataRecordQueryHandler
    : IRequestHandler<GetMasterDataRecordQuery, IDictionary<string, object?>?>
{
    private readonly IMasterDataRepository _repo;
    private readonly IMetadataEngine       _metadata;

    public GetMasterDataRecordQueryHandler(IMasterDataRepository repo, IMetadataEngine metadata)
    {
        _repo     = repo;
        _metadata = metadata;
    }

    /// <summary>Whitelist identifier (cột trái của "{Cột} = @Param" trong Filter_Sql — fallback).</summary>
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    /// <summary>Tách tên tham số @name tham chiếu trong Filter_Sql (fallback).</summary>
    [GeneratedRegex(@"@([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex ParamRefRegex();

    /// <summary>
    /// Lấy 1 bản ghi rồi suy các field ảo cascade-cha. Sự kiện theo sau: form Sửa prefill đủ
    /// (Tỉnh hiện tên, Phường/Xã cascade đúng theo Tỉnh vừa suy).
    /// </summary>
    public async Task<IDictionary<string, object?>?> Handle(GetMasterDataRecordQuery r, CancellationToken ct)
    {
        var record = await _repo.GetByIdAsync(r.FormCode, r.TenantId, r.Id, ct);
        if (record is null) return null;

        await ResolveVirtualCascadeFieldsAsync(r.FormCode, r.TenantId, r.Id, record, ct);
        return record;
    }

    /// <summary>
    /// Suy giá trị field ẢO (Is_Virtual) CHƯA có trong record theo 2 tầng: đọc từ view denormalized
    /// (chính) → field nào view không cấp thì fallback suy 1 cấp từ field con.
    /// </summary>
    private async Task ResolveVirtualCascadeFieldsAsync(
        string formCode, int tenantId, object id, IDictionary<string, object?> record, CancellationToken ct)
    {
        // langCode/platform không ảnh hưởng cấu hình lookup (chỉ đổi label) — dùng mặc định để tái dùng cache.
        var meta = await _metadata.GetFormMetadataAsync(formCode, "vi", "web", tenantId, ct);
        if (meta is null) return;

        var virtualFields = meta.Fields.Where(f => f.IsVirtual).ToList();
        if (virtualFields.Count == 0) return;

        // Field ảo CHƯA có giá trị thật (cột DB thật hoặc đã suy) trong record.
        var unresolved = virtualFields
            .Where(vf => !(TryGetRecordValue(record, vf.FieldCode, out var v) && v is not null and not DBNull))
            .Select(vf => vf.FieldCode)
            .ToList();
        if (unresolved.Count == 0) return;

        // ── (1) CHÍNH: đọc từ VIEW danh sách denormalized (view JOIN sẵn mọi cấp cha) ──
        var fromView = await _repo.ReadVirtualFieldsFromViewAsync(formCode, tenantId, id, unresolved, ct);
        foreach (var (code, val) in fromView)
            SetRecordValue(record, code, val);

        // ── (2) FALLBACK: field ảo view không cấp → suy 1 cấp từ Ui_Field_Lookup của field con ──
        var stillUnresolved = unresolved
            .Where(code => !fromView.ContainsKey(code))
            .ToList();
        if (stillUnresolved.Count > 0)
            await ReverseDeriveAsync(meta, record, stillUnresolved, ct);
    }

    /// <summary>
    /// Fallback 1 cấp: với mỗi field ảo còn trống, suy từ field con (non-virtual) có Filter_Sql
    /// tham chiếu tới nó. Field con đã lưu Id (VD PhuongXa_Id=1853); Filter_Sql dạng
    /// "TinhThanhPho_Id = @TinhThanhPho_Id" cho biết cột cha trên chính bảng nguồn field con.
    /// Suy: SELECT {cột cha} FROM {nguồn con} WHERE {Value_Column con} = {Id con}.
    /// </summary>
    private async Task ReverseDeriveAsync(
        FormMetadata meta, IDictionary<string, object?> record, List<string> targetFieldCodes,
        CancellationToken ct)
    {
        var childFields = meta.Fields
            .Where(f => !f.IsVirtual && f.LookupConfig is { } lc
                        && !string.IsNullOrWhiteSpace(lc.SourceName)
                        && !string.IsNullOrWhiteSpace(lc.ValueColumn)
                        && !string.IsNullOrWhiteSpace(lc.FilterSql))
            .ToList();
        if (childFields.Count == 0) return;

        foreach (var targetCode in targetFieldCodes)
        {
            foreach (var cf in childFields)
            {
                var lc = cf.LookupConfig!;

                if (!TryGetRecordValue(record, cf.FieldCode, out var childVal)
                    || childVal is null or DBNull)
                    continue;

                var paramName = FindParamForField(lc, targetCode);
                if (paramName is null) continue;

                var parentColumn = ExtractFilterColumn(lc.FilterSql!, paramName);
                if (parentColumn is null) continue;

                var derived = await _repo.ResolveDerivedValueAsync(
                    lc.SourceName, parentColumn, lc.ValueColumn, childVal, ct);
                if (derived is not null and not DBNull)
                {
                    SetRecordValue(record, targetCode, derived);
                    break;   // suy được → sang field ảo kế tiếp
                }
            }
        }
    }

    /// <summary>
    /// Tìm tên @param trong Filter_Sql field con ứng với <paramref name="fieldCode"/> field ảo.
    /// Ưu tiên Param_Map (mẫu lookup: {"@param canonical": "FieldCode"}); không có → so trực tiếp
    /// tên @param == FieldCode. Trả null nếu không param nào ánh xạ tới field ảo này.
    /// </summary>
    private static string? FindParamForField(FieldLookupConfig lc, string fieldCode)
    {
        var paramMap = ParseParamMap(lc.ParamMapRaw);
        foreach (Match m in ParamRefRegex().Matches(lc.FilterSql!))
        {
            var param = m.Groups[1].Value;
            var mappedField = paramMap.TryGetValue(param, out var fc) ? fc : param;
            if (mappedField.Equals(fieldCode, StringComparison.OrdinalIgnoreCase))
                return param;
        }
        return null;
    }

    /// <summary>Trích tên cột trái của mệnh đề "{Cột} = @param" (whitelist identifier). Null nếu không khớp.</summary>
    private static string? ExtractFilterColumn(string filterSql, string paramName)
    {
        var m = Regex.Match(
            filterSql,
            $@"([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*@{Regex.Escape(paramName)}\b",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var col = m.Groups[1].Value;
        return SafeIdentifierRegex().IsMatch(col) ? col : null;
    }

    /// <summary>Parse Param_Map JSON ({"param":"FieldCode"}); rỗng/lỗi → map rỗng.</summary>
    private static Dictionary<string, string> ParseParamMap(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            return map is null
                ? new(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
        }
        catch { return new(StringComparer.OrdinalIgnoreCase); }
    }

    /// <summary>Đọc giá trị record theo FieldCode (case-insensitive — key record = tên cột DB).</summary>
    private static bool TryGetRecordValue(IDictionary<string, object?> record, string fieldCode, out object? value)
    {
        foreach (var kv in record)
            if (kv.Key.Equals(fieldCode, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        value = null;
        return false;
    }

    /// <summary>Ghi giá trị field vào record: ghi đè key trùng (case-insensitive) hoặc thêm mới.</summary>
    private static void SetRecordValue(IDictionary<string, object?> record, string fieldCode, object? value)
    {
        foreach (var key in record.Keys.ToList())
            if (key.Equals(fieldCode, StringComparison.OrdinalIgnoreCase))
            {
                record[key] = value;
                return;
            }
        record[fieldCode] = value;
    }
}
