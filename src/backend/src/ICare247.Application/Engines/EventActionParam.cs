// File    : EventActionParam.cs
// Module  : Engines
// Layer   : Application
// Purpose : Helper THUẦN đọc Action_Param_Json cho Event action (AUDIT-5) — tách khỏi EventEngine để
//           test được không cần engine + dọn god-class. Chỉ parse/đọc JSON + resolve placeholder;
//           không đụng AST/repo/logger.

using System.Text.Json;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// Đọc tham số action (Action_Param_Json) — thuần, không I/O. Trả null/rỗng an toàn khi thiếu/lỗi
/// (caller quyết định bỏ qua action). Xem <see cref="EventEngine"/>.
/// </summary>
public static class EventActionParam
{
    /// <summary>
    /// Parse chuỗi JSON thành <see cref="JsonElement"/> (clone để dùng sau khi dispose doc).
    /// Trả <c>null</c> nếu rỗng/whitespace HOẶC JSON không hợp lệ (caller có thể log).
    /// </summary>
    public static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Lấy string property; null nếu element/property không tồn tại.</summary>
    public static string? GetString(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        return element.Value.TryGetProperty(propertyName, out var prop)
            ? prop.GetString()
            : null;
    }

    /// <summary>Lấy nested JsonElement property; null nếu không tồn tại.</summary>
    public static JsonElement? GetElement(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        return element.Value.TryGetProperty(propertyName, out var prop)
            ? prop
            : null;
    }

    /// <summary>Lấy string array; null nếu không tồn tại hoặc không phải array.</summary>
    public static IReadOnlyList<string>? GetStringArray(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        if (!element.Value.TryGetProperty(propertyName, out var prop)) return null;
        if (prop.ValueKind != JsonValueKind.Array) return null;

        return prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();
    }

    /// <summary>
    /// Resolve placeholder <c>{FieldCode}</c> trong template bằng giá trị từ context.
    /// VD "/api/options?province={Province}" → "/api/options?province=HN".
    /// </summary>
    public static string ResolvePlaceholders(string template, EvaluationContext context)
    {
        var result = template;
        var startIdx = 0;

        while (startIdx < result.Length)
        {
            var openBrace = result.IndexOf('{', startIdx);
            if (openBrace < 0) break;

            var closeBrace = result.IndexOf('}', openBrace + 1);
            if (closeBrace < 0) break;

            var fieldCode = result[(openBrace + 1)..closeBrace];
            var value = context.GetValue(fieldCode);
            var valueStr = value?.ToString() ?? string.Empty;

            result = string.Concat(result.AsSpan(0, openBrace), valueStr, result.AsSpan(closeBrace + 1));
            startIdx = openBrace + valueStr.Length;
        }

        return result;
    }
}
