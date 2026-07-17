// File    : ControlPropsJsonService.cs
// Module  : Forms / Services
// Layer   : Presentation (logic thuần — không đụng UI/DB)
// Purpose : REFACTOR-B1 — gom toàn bộ logic Control_Props_Json tách từ FieldConfigViewModel:
//           schema prop theo editor type, parse/restore JSON đã lưu, ép kiểu giá trị,
//           và LẮP JSON từ snapshot cấu hình (BuildJson). VM chỉ còn giữ state + gọi vào đây.
//           Class static thuần → unit-test được không cần dựng VM/DI.

using System.Text.Json;
using ConfigStudio.WPF.UI.Modules.Forms.Models;

namespace ConfigStudio.WPF.UI.Modules.Forms.Services;

/// <summary>
/// Logic thuần cho Control_Props_Json (Ui_Field.Control_Props_Json). Hành vi giữ NGUYÊN
/// từ FieldConfigViewModel trước refactor — thứ tự chèn key JSON không đổi (so khớp characterization).
/// </summary>
public static class ControlPropsJsonService
{
    /// <summary>
    /// Snapshot đầu vào để lắp JSON — VM chụp state hiện tại đưa vào, service không đọc ngược VM.
    /// </summary>
    public sealed class BuildInput
    {
        public required IReadOnlyList<ControlPropValue> ControlProps { get; init; }

        // Sys_Lookup (editor static lookup)
        public bool IsLookupEditor { get; init; }
        public string? LookupCode { get; init; }

        // ComboBox / LookupComboBox
        public bool IsComboLike { get; init; }
        public string CbSearchMode { get; init; } = "";
        public string CbSearchFilterCondition { get; init; } = "";
        public bool CbAllowUserInput { get; init; }
        public string CbDropDownWidthMode { get; init; } = "";
        public string CbClearButton { get; init; } = "";
        public string? CbNullTextKey { get; init; }
        public string? CbGroupFieldName { get; init; }
        public string? CbDisabledFieldName { get; init; }

        // FK Lookup (LookupBox)
        public bool IsFkLookupEditor { get; init; }
        public string QueryMode { get; init; } = "table";
        public string? FkValueField { get; init; }
        public string? FkDisplayField { get; init; }
        public bool FkSearchEnabled { get; init; }
        public string? FkOrderBy { get; init; }
        public string? FkTableName { get; init; }
        public string? FkFilterSql { get; init; }
        public string? FkFunctionName { get; init; }
        public string? FkSelectSql { get; init; }
        public IReadOnlyList<FkFilterParam> FilterParams { get; init; } = [];
        public IReadOnlyList<FunctionParam> FunctionParams { get; init; } = [];
        public IReadOnlyList<FkColumnConfig> PopupColumns { get; init; } = [];
        public IReadOnlyList<DataSourceCondition> DataSourceConditions { get; init; } = [];
    }

    /// <summary>
    /// Lắp Control_Props_Json từ snapshot (thân cũ của RebuildControlPropsJson — giữ nguyên
    /// thứ tự key). Sự kiện theo sau: VM gán kết quả vào ControlPropsJson + IsDirty.
    /// </summary>
    public static string BuildJson(BuildInput input)
    {
        var dict = input.ControlProps.ToDictionary(
            p => p.Definition.PropName,
            p => CoercePropValue(p));

        // Sys_Lookup: đưa lookupCode vào JSON
        if (input.IsLookupEditor && !string.IsNullOrWhiteSpace(input.LookupCode))
            dict["lookupCode"] = input.LookupCode;

        // ComboBox / LookupComboBox: merge search + display props vào JSON
        if (input.IsComboLike)
        {
            dict["searchMode"]            = input.CbSearchMode;
            dict["searchFilterCondition"] = input.CbSearchFilterCondition;
            dict["allowUserInput"]        = (object)input.CbAllowUserInput;
            dict["dropDownWidthMode"]     = input.CbDropDownWidthMode;
            dict["clearButton"]           = input.CbClearButton;
            if (!string.IsNullOrWhiteSpace(input.CbNullTextKey))
                dict["nullTextKey"] = input.CbNullTextKey;
            if (!string.IsNullOrWhiteSpace(input.CbGroupFieldName))
                dict["groupFieldName"] = input.CbGroupFieldName;
            if (!string.IsNullOrWhiteSpace(input.CbDisabledFieldName))
                dict["disabledFieldName"] = input.CbDisabledFieldName;
        }

        // FK Lookup: serialize toàn bộ config LookupBox theo queryMode
        if (input.IsFkLookupEditor)
        {
            dict["queryMode"]     = input.QueryMode;
            dict["valueField"]    = input.FkValueField;
            dict["displayField"]  = input.FkDisplayField;
            dict["searchEnabled"] = input.FkSearchEnabled;
            dict["orderBy"]       = input.FkOrderBy;

            switch (input.QueryMode)
            {
                case "table":
                    dict["tableName"] = (object?)input.FkTableName;
                    dict["filterSql"] = input.FkFilterSql;
                    if (input.FilterParams.Count > 0)
                        dict["filterParams"] = input.FilterParams.Select(p => new
                            { param = p.Param, fieldRef = p.FieldRef, type = p.Type }).ToList();
                    break;

                case "function":
                    dict["functionName"] = (object?)input.FkFunctionName;
                    if (input.FunctionParams.Count > 0)
                        dict["functionParams"] = input.FunctionParams.Select(p => new
                        {
                            name       = p.Name,
                            sourceType = p.SourceType,
                            fieldRef   = p.SourceType == "field"  ? p.FieldRef  : (string?)null,
                            systemKey  = p.SourceType == "system" ? p.SystemKey : (string?)null,
                            type       = p.Type
                        }).ToList();
                    break;

                case "sql":
                    dict["selectSql"] = (object?)input.FkSelectSql;
                    if (input.FilterParams.Count > 0)
                        dict["filterParams"] = input.FilterParams.Select(p => new
                            { param = p.Param, fieldRef = p.FieldRef, type = p.Type }).ToList();
                    break;
            }

            // Cột popup, dataSourceConditions — dùng cho cả 3 mode.
            // Lưu captionKey (i18n key) — backend resolve → text theo langCode khi trả Blazor.
            dict["columns"] = input.PopupColumns.Select(c => new
                { fieldName = c.FieldName, captionKey = c.CaptionKey, width = c.Width }).ToList();

            // Multi-Trigger lưu ở cột Ui_Field_Lookup.Reload_Trigger_Fields (Migration 068),
            // KHÔNG serialize vào Control_Props_Json (runtime đọc từ cột).

            if (input.DataSourceConditions.Count > 0)
                dict["dataSourceConditions"] = input.DataSourceConditions.Select(c => new
                {
                    when = new { field = c.WhenField, op = c.WhenOp, value = c.WhenValue },
                    tableName    = c.TableName,
                    displayField = c.DisplayField,
                    filterSql    = c.FilterSql
                }).ToList();
        }

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Ép giá trị prop về đúng kiểu trước khi serialize JSON.
    /// DevExpress TextEdit trả EditValue dạng string ("2") nên prop kiểu Number
    /// phải parse về số — nếu không Blazor renderer (int/double) sẽ deserialize lỗi.
    /// </summary>
    private static object? CoercePropValue(ControlPropValue p)
    {
        if (p.Definition.PropType != "Number") return p.Value;

        return p.Value switch
        {
            null                                                    => null,
            string s when string.IsNullOrWhiteSpace(s)              => null,
            // Số nguyên thì giữ long, có phần thập phân thì giữ double
            string s when long.TryParse(s, out var l)               => l,
            string s when double.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d)                                          => d,
            string s when double.TryParse(s, out var d)             => d,
            _                                                       => p.Value
        };
    }

    /// <summary>Parse JSON string thành dictionary prop values (dùng khi restore từ DB).</summary>
    public static Dictionary<string, object?> ParseControlPropsJson(string json)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (raw is null) return [];
            return raw.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }
        catch { return []; }
    }

    /// <summary>Đọc string từ prop dict, fallback về <paramref name="def"/> nếu không tìm thấy.</summary>
    public static string GetStr(Dictionary<string, object?> d, string key, string def)
    {
        if (!d.TryGetValue(key, out var v) || v is not JsonElement je) return def;
        return je.ValueKind == JsonValueKind.String ? (je.GetString() ?? def) : def;
    }

    /// <summary>Đọc bool từ prop dict, fallback về <paramref name="def"/> nếu không tìm thấy.</summary>
    public static bool GetBool(Dictionary<string, object?> d, string key, bool def)
    {
        if (!d.TryGetValue(key, out var v) || v is not JsonElement je) return def;
        return je.ValueKind is JsonValueKind.True  ? true
             : je.ValueKind is JsonValueKind.False ? false
             : def;
    }

    /// <summary>Chuyển JsonElement thành đúng kiểu dựa trên PropType của definition.</summary>
    public static object? ConvertJsonPropValue(object? raw, string propType)
    {
        if (raw is not JsonElement je) return raw;
        return propType switch
        {
            "Number"  => je.ValueKind == JsonValueKind.Number  ? je.GetDouble()  : (object?)null,
            "Boolean" => je.ValueKind is JsonValueKind.True
                                      or JsonValueKind.False   ? je.GetBoolean() : (object?)null,
            _         => je.ValueKind == JsonValueKind.String  ? je.GetString()  : je.ToString()
        };
    }

    /// <summary>
    /// Trả về danh sách <see cref="ControlPropDefinition"/> mock theo editor type.
    /// Sau này sẽ load từ <c>Ui_Control_Map.Default_Props_Json</c>.
    /// </summary>
    public static List<ControlPropDefinition> GetPropDefinitions(string editorType) => editorType switch
    {
        "NumericBox" =>
        [
            new() { PropName = "minValue",  PropType = "Number",  DefaultValue = 0,      Label = "Giá trị tối thiểu" },
            new() { PropName = "maxValue",  PropType = "Number",  DefaultValue = 999999, Label = "Giá trị tối đa" },
            new() { PropName = "decimals",  PropType = "Number",  DefaultValue = 0,      Label = "Số chữ số thập phân" },
            new() { PropName = "spinStep",  PropType = "Number",  DefaultValue = 1,      Label = "Bước nhảy" },
            new() { PropName = "allowNull", PropType = "Boolean", DefaultValue = false,   Label = "Cho phép rỗng" },
        ],
        "TextBox" =>
        [
            new() { PropName = "maxLength",       PropType = "Number",  DefaultValue = 255,          Label = "Độ dài tối đa" },
            new() { PropName = "isPassword",      PropType = "Boolean", DefaultValue = false,         Label = "Ẩn ký tự (password)" },
            new() { PropName = "autoComplete",    PropType = "Enum",    DefaultValue = "off",         Label = "AutoComplete",
                    AllowedValues = ["off", "on", "new-password"] },
            new() { PropName = "bindValueMode",   PropType = "Enum",    DefaultValue = "OnLostFocus", Label = "Khi nào cập nhật giá trị",
                    AllowedValues = ["OnLostFocus", "OnInput"] },
            new() { PropName = "inputDelay",      PropType = "Number",  DefaultValue = 300,           Label = "Delay (ms) khi OnInput" },
            new() { PropName = "clearButtonMode", PropType = "Enum",    DefaultValue = "Auto",        Label = "Nút xóa",
                    AllowedValues = ["Auto", "Never"] },
        ],
        // TextArea = DxMemo — control riêng, user tự chọn
        "TextArea" =>
        [
            new() { PropName = "maxLength",     PropType = "Number",  DefaultValue = 4000,          Label = "Độ dài tối đa" },
            new() { PropName = "rows",          PropType = "Number",  DefaultValue = 4,             Label = "Số dòng hiển thị" },
            new() { PropName = "bindValueMode", PropType = "Enum",    DefaultValue = "OnLostFocus", Label = "Khi nào cập nhật giá trị",
                    AllowedValues = ["OnLostFocus", "OnInput"] },
            new() { PropName = "inputDelay",    PropType = "Number",  DefaultValue = 300,           Label = "Delay (ms) khi OnInput" },
        ],
        // ComboBox dùng dedicated ComboBoxPropsPanel — không qua generic ControlProps
        "ComboBox" => [],
        "DatePicker" =>
        [
            new() { PropName = "format",  PropType = "Enum",   DefaultValue = "dd/MM/yyyy", Label = "Định dạng ngày", AllowedValues = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm", "MM/yyyy", "yyyy"] },
            new() { PropName = "minDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối thiểu" },
            new() { PropName = "maxDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối đa" },
        ],
        // CheckBox = DxCheckBox với CheckType.CheckBox
        "CheckBox" =>
        [
            new() { PropName = "allowIndeterminate", PropType = "Boolean", DefaultValue = false,   Label = "3 trạng thái (bool?)" },
            new() { PropName = "labelPosition",      PropType = "Enum",    DefaultValue = "Right",  Label = "Vị trí label",
                    AllowedValues = ["Right", "Left"] },
            new() { PropName = "labelWrapMode",      PropType = "Enum",    DefaultValue = "WordWrap", Label = "Xuống dòng label",
                    AllowedValues = ["WordWrap", "Ellipsis", "NoWrap"] },
        ],
        // ToggleSwitch = DxCheckBox với CheckType.Switch (không hỗ trợ indeterminate)
        "ToggleSwitch" =>
        [
            new() { PropName = "labelPosition", PropType = "Enum", DefaultValue = "Right", Label = "Vị trí label",
                    AllowedValues = ["Right", "Left"] },
        ],
        // LookupBox dùng panel riêng (FkTableName, FkValueField...) — không qua generic ControlProps
        "LookupBox" => [],
        _ => []
    };
}
