// File    : ResourceResolver.cs
// Module  : Resource
// Layer   : Application
// Purpose : Static helper resolve resource key → text với fallback hierarchy.
//           Xem spec: docs/spec/10_RESOURCE_KEY_CONVENTION.md

namespace ICare247.Application;

/// <summary>
/// Helper resolve thông báo validation từ ResourceMap theo fallback hierarchy:
/// <list type="number">
///   <item><c>{formCode}.val.{fieldCode}.{qualifier}</c> — form+field specific (ưu tiên cao nhất)</item>
///   <item><c>sys.val.{qualifier}</c> — global template, format với field label</item>
///   <item>Hardcoded fallback — khi Sys_Resource chưa setup</item>
/// </list>
/// </summary>
public static class ResourceResolver
{
    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolve một resource key cụ thể (VD: <c>Error_Key</c> của <c>Val_Rule</c>).
    /// Nếu không tìm thấy trong map → trả <paramref name="fallback"/>.
    /// </summary>
    public static string Resolve(
        IReadOnlyDictionary<string, string>? map,
        string key,
        string fallback)
    {
        if (map is not null
            && map.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value))
            return value;

        return fallback;
    }

    /// <summary>
    /// Resolve thông báo "bắt buộc nhập" theo fallback hierarchy.
    /// <para>
    /// 1. <c>{formCode}.val.{fieldCode}.Required</c><br/>
    /// 2. <c>sys.val.Required</c> template → format(<paramref name="fieldLabel"/>)<br/>
    /// 3. Hardcoded: "{fieldLabel} không được để trống" / "is required"
    /// </para>
    /// </summary>
    public static string ResolveRequired(
        IReadOnlyDictionary<string, string>? map,
        string formCode,
        string fieldCode,
        string fieldLabel,
        string langCode)
    {
        if (map is not null)
        {
            // 1. Form+field specific
            var specificKey = $"{formCode}.val.{fieldCode}.Required";
            if (map.TryGetValue(specificKey, out var specific)
                && !string.IsNullOrWhiteSpace(specific))
                return specific;

            // 2. Global template
            if (map.TryGetValue("sys.val.Required", out var template)
                && !string.IsNullOrWhiteSpace(template))
                return FormatTemplate(template, fieldLabel);
        }

        // 3. Hardcoded fallback
        var label = string.IsNullOrWhiteSpace(fieldLabel) ? fieldCode : fieldLabel;
        return langCode.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? $"{label} is required"
            : $"{label} không được để trống";
    }

    /// <summary>
    /// Resolve thông báo lỗi rule từ <c>Error_Key</c> (pattern: <c>{table}.val.{column}.{type}</c>).
    /// <para>
    /// 1. Tra trực tiếp <paramref name="errorKey"/> trong map<br/>
    /// 2. Trả <paramref name="errorKey"/> làm fallback (hiển thị key thay vì crash)
    /// </para>
    /// </summary>
    public static string ResolveRuleMessage(
        IReadOnlyDictionary<string, string>? map,
        string errorKey,
        string fieldLabel)
    {
        if (map is not null
            && map.TryGetValue(errorKey, out var value)
            && !string.IsNullOrWhiteSpace(value))
            return FormatTemplate(value, fieldLabel);

        // Fallback: trả errorKey để dev biết key chưa có trong Sys_Resource
        return errorKey;
    }

    // ── Internal helpers ───────────────────────────────────────────────

    /// <summary>
    /// Format template: thay thế {0} → <paramref name="arg0"/>.
    /// Dùng string.Format an toàn — nếu template không có {0} thì trả nguyên template.
    /// </summary>
    private static string FormatTemplate(string template, string arg0)
    {
        try
        {
            return template.Contains("{0}", StringComparison.Ordinal)
                ? string.Format(template, arg0)
                : template;
        }
        catch
        {
            return template;
        }
    }
}
