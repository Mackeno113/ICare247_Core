// File    : ComboBoxControlProps.cs
// Module  : Form
// Layer   : Domain
// Purpose : Typed model cho Control_Props_Json của Editor_Type = ComboBox / LookupComboBox / RadioGroup.
//           Parse từ Ui_Field.Control_Props_Json khi runtime cần render DxComboBox.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Typed model cho <c>Control_Props_Json</c> của các editor type dùng <c>DxComboBox</c>:
/// <list type="bullet">
///   <item><c>ComboBox</c> — dynamic data (bảng/TVF/SQL)</item>
///   <item><c>LookupComboBox</c> — static Sys_Lookup</item>
///   <item><c>RadioGroup</c> — static Sys_Lookup, render dạng radio buttons</item>
/// </list>
/// Serialize/deserialize từ JSON stored trong cột <c>Ui_Field.Control_Props_Json</c>.
/// </summary>
public sealed class ComboBoxControlProps
{
    // ── Search & Filter ────────────────────────────────────────────────

    /// <summary>
    /// Chế độ tìm kiếm trong dropdown.
    /// <c>None</c> = tắt; <c>AutoSearch</c> = tìm kiếm; <c>AutoFilter</c> = lọc tự động.
    /// Mặc định: <c>AutoFilter</c>.
    /// </summary>
    [JsonPropertyName("searchMode")]
    public string SearchMode { get; set; } = "AutoFilter";

    /// <summary>
    /// Điều kiện so khớp khi search.
    /// <c>Contains</c> | <c>StartsWith</c> | <c>Equals</c>.
    /// Mặc định: <c>Contains</c>.
    /// </summary>
    [JsonPropertyName("searchFilterCondition")]
    public string SearchFilterCondition { get; set; } = "Contains";

    /// <summary>
    /// Cho phép người dùng nhập text tự do (không ràng buộc vào list).
    /// Mặc định: <c>false</c>.
    /// </summary>
    [JsonPropertyName("allowUserInput")]
    public bool AllowUserInput { get; set; } = false;

    // ── Display ────────────────────────────────────────────────────────

    /// <summary>
    /// I18n key cho placeholder khi chưa chọn giá trị.
    /// Null = dùng fallback mặc định "-- Chọn --".
    /// </summary>
    [JsonPropertyName("nullTextKey")]
    public string? NullTextKey { get; set; }

    /// <summary>
    /// Chế độ chiều rộng dropdown panel.
    /// <c>ContentOrEditorWidth</c> | <c>ContentWidth</c> | <c>EditorWidth</c>.
    /// Mặc định: <c>ContentOrEditorWidth</c>.
    /// </summary>
    [JsonPropertyName("dropDownWidthMode")]
    public string DropDownWidthMode { get; set; } = "ContentOrEditorWidth";

    /// <summary>
    /// Hiển thị nút xóa giá trị.
    /// <c>Hidden</c> = luôn ẩn; <c>Auto</c> = hiện khi có giá trị.
    /// Mặc định: <c>Auto</c>.
    /// </summary>
    [JsonPropertyName("clearButton")]
    public string ClearButton { get; set; } = "Auto";

    /// <summary>
    /// Tên field để group items trong dropdown — tùy chọn, chỉ dùng với data dynamic.
    /// Null = không group.
    /// </summary>
    [JsonPropertyName("groupFieldName")]
    public string? GroupFieldName { get; set; }

    /// <summary>
    /// Tên field bool trong data source để disable từng item.
    /// Null = không có item nào bị disable.
    /// </summary>
    [JsonPropertyName("disabledFieldName")]
    public string? DisabledFieldName { get; set; }

    // ── I18n Error Keys ────────────────────────────────────────────────

    /// <summary>
    /// Map các I18n key cho error message theo loại validation.
    /// Key: tên rule (Required, v.v.) — Value: I18n resource key.
    /// Ví dụ: <c>{"Required": "nhanvien.val.phong_ban.Required"}</c>.
    /// </summary>
    [JsonPropertyName("errorKeys")]
    public Dictionary<string, string>? ErrorKeys { get; set; }

    // ── Factory / Serialize ────────────────────────────────────────────

    /// <summary>
    /// Parse từ JSON string. Trả về instance với giá trị mặc định nếu <paramref name="json"/> null/rỗng/lỗi.
    /// </summary>
    /// <param name="json">Nội dung cột <c>Ui_Field.Control_Props_Json</c>.</param>
    public static ComboBoxControlProps Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<ComboBoxControlProps>(json, JsonOpts) ?? new();
        }
        catch (JsonException)
        {
            // JSON không hợp lệ → trả default, không crash runtime
            return new();
        }
    }

    /// <summary>Serialize thành JSON string để lưu vào <c>Ui_Field.Control_Props_Json</c>.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    // ── Internal ───────────────────────────────────────────────────────

    /// <summary>JsonSerializerOptions dùng chung — camelCase, bỏ qua null.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };
}
