// File    : GetMasterDataRecordQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — lấy bản ghi qua IMasterDataRepository, sau đó suy giá trị field ảo
//           cascade-cha (Is_Virtual, VD Tỉnh) từ field con đã lưu (VD PhuongXa_Id) — field ảo
//           không map cột DB nên không có sẵn trong bản ghi gốc.

using System.Text.Json;
using System.Text.RegularExpressions;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Form;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataRecord;

public sealed class GetMasterDataRecordQueryHandler
    : IRequestHandler<GetMasterDataRecordQuery, IDictionary<string, object?>?>
{
    private readonly IMasterDataRepository _repo;
    private readonly IConfigCache _configCache;

    public GetMasterDataRecordQueryHandler(IMasterDataRepository repo, IConfigCache configCache)
    {
        _repo = repo;
        _configCache = configCache;
    }

    /// <summary>Lấy 1 bản ghi. Sự kiện theo sau: form Sửa load sẵn dữ liệu.</summary>
    public async Task<IDictionary<string, object?>?> Handle(GetMasterDataRecordQuery r, CancellationToken ct)
    {
        var record = await _repo.GetByIdAsync(r.FormCode, r.TenantId, r.Id, ct);
        if (record is null) return null;

        // Metadata form (cache L1/L2 — ADR-014): chỉ cần FieldCode + LookupConfig, không cần label
        // theo ngôn ngữ cho việc suy field ảo cascade nên "vi"/"web" cố định không ảnh hưởng kết quả.
        var form = await _configCache.GetFormMetadataAsync(r.FormCode, "vi", "web", r.TenantId, ct);
        if (form is not null)
            await ResolveVirtualCascadeFieldsAsync(form, record, ct);

        return record;
    }

    /// <summary>
    /// Field ảo cascade-cha (VD Tỉnh) không lưu cột riêng — giá trị suy từ field con đã chọn
    /// (VD PhuongXa_Id) bằng chính config lookup đã khai cho field con: Filter_Sql dạng
    /// "{Cột}=@{param}" trỏ tới cột chứa giá trị cha trong nguồn dữ liệu của field con.
    /// Tên @-token trong Filter_Sql xác định theo 2 cách field con có thể được khai (thử lần lượt,
    /// dùng cách nào ra kết quả trước — token không có trong Filter_Sql thì regex đơn giản không khớp,
    /// không cần biết trước field con dùng cách nào):
    ///   1) Param_Map (mẫu lookup, Migration 083) — canonical key có value = FieldCode field ảo.
    ///   2) Trực tiếp (Reload_Trigger_Field cũ, hoặc admin tự gõ Filter_Sql không qua mẫu) —
    ///      FieldCode field ảo dùng thẳng làm tên @param.
    /// Không cần khai thêm metadata — dùng lại Ui_Field_Lookup hiện có. Áp dụng cho mọi form
    /// dùng MasterDataForm (không riêng Công ty), mọi field ảo cascade theo 1 trong 2 cách trên.
    /// </summary>
    private async Task ResolveVirtualCascadeFieldsAsync(
        FormMetadata form, IDictionary<string, object?> record, CancellationToken ct)
    {
        foreach (var virtualField in form.Fields.Where(f => f.IsVirtual))
        {
            foreach (var child in form.Fields)
            {
                var cfg = child.LookupConfig;
                if (cfg is null || string.IsNullOrWhiteSpace(cfg.FilterSql))
                    continue;

                // Key-match case-insensitive: cột SELECT * trả về theo casing thật của DB, có thể
                // lệch casing với Field_Code cấu hình (cùng quy ước MasterDataForm.razor dùng khi prefill).
                var childHit = record.FirstOrDefault(
                    kv => kv.Key.Equals(child.FieldCode, StringComparison.OrdinalIgnoreCase));
                if (childHit.Equals(default(KeyValuePair<string, object?>)) || childHit.Value is null)
                    continue;
                var childValue = childHit.Value;

                var paramKey = FindCanonicalParamKey(cfg.ParamMapRaw, virtualField.FieldCode) ?? virtualField.FieldCode;
                var column = ExtractFilterColumn(cfg.FilterSql, paramKey);
                if (column is null) continue;

                var derived = await _repo.ResolveDerivedValueAsync(
                    cfg.SourceName, column, cfg.ValueColumn, childValue, ct);
                if (derived is not null)
                    record[virtualField.FieldCode] = derived;
            }
        }
    }

    /// <summary>Tìm key canonical trong Param_Map (JSON) có value = FieldCode của field ảo cha.</summary>
    private static string? FindCanonicalParamKey(string? paramMapRaw, string fieldCode)
    {
        if (string.IsNullOrWhiteSpace(paramMapRaw)) return null;
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(paramMapRaw);
            if (map is null) return null;
            foreach (var (key, value) in map)
                if (string.Equals(value, fieldCode, StringComparison.OrdinalIgnoreCase))
                    return key;
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Trích tên cột trong Filter_Sql theo mẫu "{Cột} = @{param}" (hai chiều).</summary>
    private static string? ExtractFilterColumn(string filterSql, string paramKey)
    {
        var escaped = Regex.Escape(paramKey);

        var m = Regex.Match(filterSql, $@"([A-Za-z_][A-Za-z0-9_]*)\s*=\s*@{escaped}\b", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;

        m = Regex.Match(filterSql, $@"@{escaped}\s*=\s*([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }
}
