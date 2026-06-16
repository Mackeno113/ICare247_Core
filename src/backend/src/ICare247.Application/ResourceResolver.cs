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
        var label = string.IsNullOrWhiteSpace(fieldLabel) ? fieldCode : fieldLabel;
        if (map is not null)
        {
            // 1. Form+field specific — thay token ({0}=giá trị (rỗng với required) · {1}=nhãn).
            var specificKey = $"{formCode}.val.{fieldCode}.Required";
            if (map.TryGetValue(specificKey, out var specific)
                && !string.IsNullOrWhiteSpace(specific))
                return ApplyTokens(specific, "", label);

            // 2. Global template
            if (map.TryGetValue("sys.val.Required", out var template)
                && !string.IsNullOrWhiteSpace(template))
                return ApplyTokens(template, "", label);
        }

        // 3. Hardcoded fallback
        return langCode.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? $"{label} is required"
            : $"{label} không được để trống";
    }

    /// <summary>
    /// Resolve thông báo "đã tồn tại / trùng" (field Is_Unique) theo fallback hierarchy:
    /// <para>
    /// 1. <c>{formCode}.val.{fieldCode}.Unique</c><br/>
    /// 2. <c>sys.val.Unique</c> template → format(<paramref name="fieldLabel"/>)<br/>
    /// 3. Hardcoded: "{fieldLabel} đã tồn tại" / "already exists"
    /// </para>
    /// </summary>
    public static string ResolveUnique(
        IReadOnlyDictionary<string, string>? map,
        string formCode,
        string fieldCode,
        string fieldLabel,
        string langCode)
    {
        var label = string.IsNullOrWhiteSpace(fieldLabel) ? fieldCode : fieldLabel;
        if (map is not null)
        {
            // Token ({0}=giá trị · {1}=nhãn). Giá trị không có ở đường này → rỗng.
            var specificKey = $"{formCode}.val.{fieldCode}.Unique";
            if (map.TryGetValue(specificKey, out var specific)
                && !string.IsNullOrWhiteSpace(specific))
                return ApplyTokens(specific, "", label);

            if (map.TryGetValue("sys.val.Unique", out var template)
                && !string.IsNullOrWhiteSpace(template))
                return ApplyTokens(template, "", label);
        }

        return langCode.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? $"{label} already exists"
            : $"{label} đã tồn tại";
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
    /// Thay token thông báo validation: <c>{0}</c> = giá trị người dùng nhập ·
    /// <c>{1}</c> = nhãn field. Dùng Replace (an toàn với dấu ngoặc lẻ / token thừa).
    /// Áp cho CẢ message per-field lẫn template (Required/Unique).
    /// </summary>
    private static string ApplyTokens(string text, string value, string label)
        => text.Replace("{0}", value).Replace("{1}", label);

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
