// File    : FieldI18nKeyService.cs
// Module  : Forms / Services
// Layer   : Presentation (logic thuần — không đụng UI state)
// Purpose : REFACTOR-B1 — tách khỏi FieldConfigViewModel: (1) sinh key i18n hiển thị/validation
//           theo spec 10 (chuỗi thuần), (2) orchestration ghi bản dịch sau khi lưu field
//           (upsert vi, lan Nhãn sang placeholder/tooltip, cascade các ngôn ngữ khác, caption
//           popup, key unique). VM chỉ chụp state vào Request và áp Result lên ô nhập
//           (SetResolvedValue — giữ nguyên hành vi không-dirty).

using ConfigStudio.WPF.UI.Core.Helpers;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Modules.Forms.Services;

/// <summary>Logic i18n key của màn Cấu hình Field. Hành vi giữ NGUYÊN từ trước refactor.</summary>
public static class FieldI18nKeyService
{
    // ── 1. Sinh key (spec 10 §1b) — thuần chuỗi ────────────────────────────────

    /// <summary>
    /// Key hiển thị field: <c>{tableCode}.field.{code}.{qualifier}</c>
    /// (qualifier = label / placeholder / tooltip). Trả rỗng nếu thiếu tableCode hoặc code.
    /// </summary>
    public static string BuildFieldKey(string tableCode, string effectiveCode, string qualifier)
    {
        var t = tableCode.ToLowerInvariant();
        var c = effectiveCode.ToLowerInvariant();
        return string.IsNullOrEmpty(t) || string.IsNullOrEmpty(c) ? "" : $"{t}.field.{c}.{qualifier}";
    }

    /// <summary>
    /// Key validation: <c>{tableCode}.val.{code}.{suffix}</c> (suffix = required / unique —
    /// khớp backend emit, xem SaveMasterDataCommandHandler). Trả rỗng nếu thiếu thành phần.
    /// </summary>
    public static string BuildValidationKey(string tableCode, string effectiveCode, string suffix)
    {
        var t = tableCode.ToLowerInvariant();
        var c = effectiveCode.ToLowerInvariant();
        return string.IsNullOrEmpty(t) || string.IsNullOrEmpty(c) ? "" : $"{t}.val.{c}.{suffix}";
    }

    // ── 2. Ghi bản dịch sau khi lưu field ─────────────────────────────────────

    /// <summary>Snapshot state VM cần cho việc ghi key — service không đọc ngược VM.</summary>
    public sealed class RegisterKeysRequest
    {
        public string LabelKey { get; init; } = "";
        public string? LabelValue { get; init; }
        public string PlaceholderKey { get; init; } = "";
        public string? PlaceholderValue { get; init; }
        public string TooltipKey { get; init; } = "";
        public string? TooltipValue { get; init; }

        /// <summary>Fallback default cho Nhãn khi ô trống (= mã cột — hành vi cũ).</summary>
        public string ColumnCode { get; init; } = "";
        public string TableCode { get; init; } = "";

        public bool IsRequired { get; init; }
        public string RequiredErrorKey { get; init; } = "";
        public string? RequiredErrorValue { get; init; }
        public string DefaultRequiredMessageVi { get; init; } = "";

        public bool IsUnique { get; init; }
        public string? UniqueErrorValue { get; init; }
        public string DefaultUniqueMessageVi { get; init; } = "";
        public string DefaultUniqueMessageEn { get; init; } = "";

        /// <summary>CaptionKey + FieldName của từng cột popup LookupBox (chỉ init default).</summary>
        public IReadOnlyList<(string CaptionKey, string FieldName)> PopupColumnKeys { get; init; } = [];

        /// <summary>Danh sách giá trị coi là "chưa dịch" (DisplayDefaultValues của VM).</summary>
        public IReadOnlyList<string> DisplayDefaults { get; init; } = [];
    }

    /// <summary>Giá trị đã ghi thay-cho-ô-trống — VM áp lại vào ô nhập KHÔNG kích hoạt dirty.</summary>
    public sealed class RegisterKeysResult
    {
        public string? PlaceholderApplied { get; init; }
        public string? TooltipApplied { get; init; }
    }

    /// <summary>
    /// Sau khi lưu field: ghi bản dịch (vi) cho các key hiển thị.
    /// Label/Placeholder/Tooltip/Required: user nhập thẳng → upsert (ghi đè); rỗng → init default nếu thiếu.
    /// captionKey popup + unique: chỉ init default khi chưa có (không ghi đè bản dịch đã sửa).
    /// Sự kiện theo sau: VM áp Result lên ô nhập placeholder/tooltip (không dirty).
    /// </summary>
    public static async Task<RegisterKeysResult> RegisterKeysAsync(
        II18nDataService i18n, RegisterKeysRequest r, CancellationToken ct)
    {
        // Label: user nhập THẲNG giá trị vi → upsert; rỗng → chỉ init default nếu chưa có.
        if (!string.IsNullOrWhiteSpace(r.LabelKey))
            await UpsertOrInitViAsync(i18n, r.LabelKey, r.LabelValue, r.ColumnCode, ct);

        // Placeholder/Tooltip thường CÙNG text với label: ô trống (hoặc còn giữ mặc định = mã cột) →
        // lấy text của label; user nhập KHÁC → tôn trọng giá trị user.
        var labelText = (r.LabelValue ?? "").Trim();

        var placeholderApplied = await SaveDisplayViAsync(
            i18n, r.PlaceholderKey, r.PlaceholderValue, labelText, r.DisplayDefaults, ct);
        var tooltipApplied = await SaveDisplayViAsync(
            i18n, r.TooltipKey, r.TooltipValue, labelText, r.DisplayDefaults, ct);

        // Các ngôn ngữ khác (bản dịch nhập qua popup của Nhãn) cũng lan sang placeholder/tooltip.
        await CascadeLabelToOtherLanguagesAsync(i18n, r, ct);

        if (r.IsRequired && !string.IsNullOrWhiteSpace(r.RequiredErrorKey))
            await UpsertOrInitViAsync(i18n, r.RequiredErrorKey, r.RequiredErrorValue,
                                      r.DefaultRequiredMessageVi, ct);

        // captionKey của từng cột popup LookupBox (chỉ init default, không có ô nhập riêng).
        foreach (var (captionKey, fieldName) in r.PopupColumnKeys)
            if (!string.IsNullOrWhiteSpace(captionKey))
                await i18n.InitResourceIfMissingAsync(captionKey, "vi", fieldName, ct);

        // Unique: auto-tạo key chống trùng (vi + en) khi bật cờ Duy nhất.
        if (r.IsUnique)
        {
            var uniqueKey = BuildValidationKey(r.TableCode, r.ColumnCode, "unique");
            if (!string.IsNullOrEmpty(uniqueKey))
            {
                // vi: user gõ thẳng → upsert (ghi đè); bỏ trống → init mẫu mặc định (token, không nhúng nhãn).
                await UpsertOrInitViAsync(i18n, uniqueKey, r.UniqueErrorValue, r.DefaultUniqueMessageVi, ct);
                // en: chỉ init mặc định nếu chưa có (nhập bản dịch khác qua nút Dịch).
                await i18n.InitResourceIfMissingAsync(uniqueKey, "en", r.DefaultUniqueMessageEn, ct);
            }
        }

        return new RegisterKeysResult
        {
            PlaceholderApplied = placeholderApplied,
            TooltipApplied = tooltipApplied
        };
    }

    /// <summary>Ghi bản dịch (vi) cho 1 key: có giá trị user nhập → upsert; rỗng → init default nếu chưa có.</summary>
    private static async Task UpsertOrInitViAsync(
        II18nDataService i18n, string key, string? value, string fallbackDefault, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(value))
            await i18n.SaveResourceAsync(key, "vi", value.Trim(), ct);
        else
            await i18n.InitResourceIfMissingAsync(key, "vi", fallbackDefault, ct);
    }

    /// <summary>
    /// Ghi bản dịch (vi) cho 1 ô hiển thị ăn theo Nhãn (Gợi ý nhập / Mô tả): ô trống hoặc còn giữ
    /// mặc định (= mã cột) → lấy labelText và trả về để VM phản ánh lại ô; user đã dịch riêng →
    /// ghi đúng giá trị user, trả null (không cần áp lại).
    /// </summary>
    private static async Task<string?> SaveDisplayViAsync(
        II18nDataService i18n, string key, string? currentValue, string labelText,
        IReadOnlyList<string> defaults, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var useLabel = I18nDefaults.IsUntranslated(currentValue, defaults);
        var text     = useLabel ? labelText : currentValue!.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;   // chưa có Nhãn → chưa ghi gì

        await i18n.SaveResourceAsync(key, "vi", text, ct);
        return useLabel ? text : null;
    }

    /// <summary>
    /// Lan bản dịch của Nhãn sang Gợi ý nhập / Mô tả ở CÁC NGÔN NGỮ NGOÀI vi: ngôn ngữ nào Nhãn đã
    /// dịch mà placeholder/tooltip còn rỗng hoặc giữ mặc định (= mã cột) thì ghi theo Nhãn. Bản dịch
    /// riêng của user giữ nguyên. Sự kiện theo sau: web đọc Sys_Resource thấy đủ 3 key mọi ngôn ngữ.
    /// </summary>
    private static async Task CascadeLabelToOtherLanguagesAsync(
        II18nDataService i18n, RegisterKeysRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.LabelKey)) return;

        var followKeys = new[] { r.PlaceholderKey, r.TooltipKey };
        if (followKeys.All(string.IsNullOrWhiteSpace)) return;

        foreach (var lang in await i18n.GetLanguagesAsync(ct))
        {
            if (string.Equals(lang.LangCode, "vi", StringComparison.OrdinalIgnoreCase)) continue;

            var labelValue = await i18n.ResolveKeyAsync(r.LabelKey, lang.LangCode, ct);
            if (string.IsNullOrWhiteSpace(labelValue)) continue;   // Nhãn chưa dịch → không có gì để lan

            foreach (var key in followKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var current = await i18n.ResolveKeyAsync(key, lang.LangCode, ct);
                if (I18nDefaults.IsUntranslated(current, r.DisplayDefaults))
                    await i18n.SaveResourceAsync(key, lang.LangCode, labelValue.Trim(), ct);
            }
        }
    }
}
