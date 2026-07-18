// File    : CompositeFieldHelper.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : Tiện ích cho control composite trong form động (1 control render THAY nhiều field).
//           Hiện có: đánh dấu field companion của AddressBox (field "địa chỉ chi tiết" text) là
//           IsHiddenByComposite để host KHÔNG render riêng nhưng VẪN gửi trong payload Lưu.
//           Dùng chung cho mọi host (MasterDataForm + FormRunner) — tránh lặp logic 2 nơi.

using System.Text.Json;
using ICare247.UI.DynamicForms.Models;

namespace ICare247.UI.DynamicForms.Components;

/// <summary>Tiện ích liên kết các field thuộc cùng một control composite.</summary>
public static class CompositeFieldHelper
{
    /// <summary>
    /// Quét mọi field neo AddressBox (FieldType="address"), đọc ControlProps.addressTextField,
    /// đánh dấu field companion tương ứng là <see cref="FieldState.IsHiddenByComposite"/>.
    /// Gọi SAU khi dựng xong danh sách field states, TRƯỚC khi render.
    /// Sự kiện theo sau: FieldRenderer bỏ render companion; payload Lưu vẫn giữ (IsVisible &amp;&amp; !IsVirtual).
    /// </summary>
    public static void MarkAddressCompanions(IEnumerable<FieldState> fields)
    {
        var list = fields as IList<FieldState> ?? fields.ToList();
        foreach (var anchor in list.Where(f => f.FieldType == "address"))
        {
            var code = ReadAddressTextField(anchor.ControlPropsJson);
            if (string.IsNullOrEmpty(code)) continue;
            var companion = list.FirstOrDefault(
                f => string.Equals(f.FieldCode, code, StringComparison.OrdinalIgnoreCase));
            if (companion is not null) companion.IsHiddenByComposite = true;
        }
    }

    /// <summary>Đọc FieldCode field địa chỉ text từ Control_Props_Json (null nếu thiếu/hỏng JSON).</summary>
    private static string? ReadAddressTextField(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("addressTextField", out var el)
                   ? el.GetString()
                   : null;
        }
        catch { return null; }
    }
}
