// File    : LookupBoxControlProps.cs
// Module  : Form
// Layer   : Domain
// Purpose : Typed model cho Control_Props_Json của Editor_Type = LookupBox.
//           Parse từ Ui_Field.Control_Props_Json khi runtime cần render DxDropDownBox.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ICare247.Domain.Entities.Form;

/// <summary>
/// Typed model cho <c>Control_Props_Json</c> của editor type <c>LookupBox</c>.
/// LookupBox dùng <c>DxDropDownBox</c> — khác hoàn toàn với <see cref="ComboBoxControlProps"/>:
/// <list type="bullet">
///   <item>Lưu FK (int) thay vì string code.</item>
///   <item>EditBox có thể hiển thị template phức tạp (code + tên).</item>
///   <item>Dropdown là popup grid (<c>DxGrid</c>) thay vì list đơn giản.</item>
/// </list>
/// Serialize/deserialize từ JSON stored trong cột <c>Ui_Field.Control_Props_Json</c>.
/// </summary>
public sealed class LookupBoxControlProps
{
    // ── EditBox Display ────────────────────────────────────────────────

    /// <summary>
    /// Chế độ hiển thị trong EditBox khi đã chọn giá trị.
    /// <c>TextOnly</c> = chỉ cột Display (mặc định).
    /// <c>CodeAndName</c> = mã code nhỏ + tên (dùng <see cref="CodeField"/>).
    /// <c>Custom</c> = template Blazor tùy chỉnh (cần RenderFragment riêng).
    /// </summary>
    [JsonPropertyName("editBoxMode")]
    public string EditBoxMode { get; set; } = "TextOnly";

    /// <summary>
    /// Tên cột code trong data source — chỉ dùng khi <see cref="EditBoxMode"/> = <c>CodeAndName</c>.
    /// Ví dụ: <c>"PhongBan_Code"</c>.
    /// </summary>
    [JsonPropertyName("codeField")]
    public string? CodeField { get; set; }

    // ── Popup Dimensions ───────────────────────────────────────────────

    /// <summary>
    /// Chiều rộng popup grid (px). Mặc định: 600.
    /// </summary>
    [JsonPropertyName("dropDownWidth")]
    public int DropDownWidth { get; set; } = 600;

    /// <summary>
    /// Chiều cao popup grid (px). Mặc định: 400.
    /// </summary>
    [JsonPropertyName("dropDownHeight")]
    public int DropDownHeight { get; set; } = 400;

    // ── Behavior ───────────────────────────────────────────────────────

    /// <summary>
    /// Bật tìm kiếm trong popup grid. Mặc định: <c>true</c>.
    /// </summary>
    [JsonPropertyName("searchEnabled")]
    public bool SearchEnabled { get; set; } = true;

    /// <summary>
    /// FieldCode của field khác trong form — khi field đó thay đổi giá trị,
    /// LookupBox tự động clear SelectedId và reload data.
    /// Null = không có cascading trigger.
    /// </summary>
    [JsonPropertyName("reloadTriggerField")]
    public string? ReloadTriggerField { get; set; }

    // ── Display ────────────────────────────────────────────────────────

    /// <summary>
    /// I18n key cho placeholder khi chưa chọn giá trị.
    /// Null = dùng fallback mặc định "-- Chọn --".
    /// </summary>
    [JsonPropertyName("nullTextKey")]
    public string? NullTextKey { get; set; }

    /// <summary>
    /// Hiển thị nút xóa giá trị.
    /// <c>Hidden</c> = luôn ẩn; <c>Auto</c> = hiện khi có giá trị.
    /// Mặc định: <c>Auto</c>.
    /// </summary>
    [JsonPropertyName("clearButton")]
    public string ClearButton { get; set; } = "Auto";

    // ── I18n Error Keys ────────────────────────────────────────────────

    /// <summary>
    /// Map các I18n key cho error message theo loại validation.
    /// LookupBox thường chỉ cần <c>Required</c>.
    /// Ví dụ: <c>{"Required": "nhanvien.val.phong_ban.Required"}</c>.
    /// </summary>
    [JsonPropertyName("errorKeys")]
    public Dictionary<string, string>? ErrorKeys { get; set; }

    // ── Factory / Serialize ────────────────────────────────────────────

    /// <summary>
    /// Parse từ JSON string. Trả về instance với giá trị mặc định nếu <paramref name="json"/> null/rỗng/lỗi.
    /// </summary>
    /// <param name="json">Nội dung cột <c>Ui_Field.Control_Props_Json</c>.</param>
    public static LookupBoxControlProps Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<LookupBoxControlProps>(json, JsonOpts) ?? new();
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
