// File    : LocalizationService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : i18n cho "chrome tĩnh" của shell (menu, layout, nút). Nguyên tắc:
//           - KEY thuộc về cấu trúc/code (suy từ slug), base tiếng Việt nằm ngay tại
//             chỗ gọi (fallback) → JSON chỉ là "lớp phủ giá trị", không khai báo key.
//           - Tải lười: chỉ nạp ngôn ngữ đang chọn; thêm ngôn ngữ = thả 1 file json.
//           - Gộp overlay từ nhiều nguồn (mỗi module RCL đăng ký nguồn riêng — #2).
//           - Đổi ngôn ngữ kéo theo CultureInfo (số/ngày + DevExpress tự dịch — #3).
//           - Hỗ trợ tham số {0}/{1} (#1) và pseudo-localization để soát i18n (#6).

using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ICare247.UI.Shared.Services.I18n;

/// <summary>Mô tả 1 ngôn ngữ trong manifest (languages.json).</summary>
/// <param name="Code">Mã ngôn ngữ (vd "vi", "en").</param>
/// <param name="Name">Tên hiển thị trong bộ chuyển ngôn ngữ.</param>
/// <param name="Rtl">True nếu viết phải-sang-trái (dành cho sau này).</param>
public sealed record LanguageInfo(string Code, string Name, bool Rtl = false);

/// <summary>
/// Dịch vụ ngôn ngữ cho shell. Đăng ký Scoped. Gọi <see cref="InitializeAsync"/> 1 lần
/// lúc khởi động; sau đó <see cref="L(string,string,object[])"/> tra cứu đồng bộ.
/// </summary>
public sealed class LocalizationService
{
    /// <summary>Ngôn ngữ gốc — base nằm ở fallback trong code, KHÔNG cần file json.</summary>
    public const string BaseLanguage = "vi";

    /// <summary>Mã giả lập (pseudo-localization) để soát chuỗi chưa i18n + tràn layout.</summary>
    public const string PseudoCode = "qps";

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new(System.Text.Json.JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;
    private readonly ILogger<LocalizationService> _logger;

    // Lớp phủ giá trị của ngôn ngữ hiện tại (gộp từ mọi nguồn đã đăng ký).
    private Dictionary<string, string> _overlay = new(StringComparer.OrdinalIgnoreCase);
    // Các nguồn i18n (đường dẫn tương đối base app). Host + mỗi module RCL đăng ký thêm.
    private readonly List<string> _sources = new();
    private CultureInfo _culture = new(BaseLanguage);

    /// <summary>Ngôn ngữ đang chọn (hoặc <see cref="PseudoCode"/> khi bật pseudo).</summary>
    public string CurrentLanguage { get; private set; } = BaseLanguage;

    /// <summary>Đang bật chế độ pseudo-localization?</summary>
    public bool PseudoEnabled { get; private set; }

    /// <summary>Danh sách ngôn ngữ từ manifest (đổ ra bộ chuyển ngôn ngữ).</summary>
    public IReadOnlyList<LanguageInfo> Available { get; private set; } = new List<LanguageInfo> { new(BaseLanguage, "Tiếng Việt") };

    /// <summary>Bắn khi đổi ngôn ngữ/pseudo để component vẽ lại.</summary>
    public event Action? OnChanged;

    public LocalizationService(HttpClient http, NavigationManager nav, IJSRuntime js, ILogger<LocalizationService> logger)
    {
        _http = http;
        _nav = nav;
        _js = js;
        _logger = logger;
        // Nguồn mặc định: static assets của RCL Shared.
        RegisterSource("_content/ICare247.UI.Shared/i18n");
    }

    /// <summary>
    /// Đăng ký thêm 1 nguồn i18n (module RCL gọi). Sự kiện theo sau: lần đổi ngôn ngữ
    /// kế tiếp sẽ gộp thêm file json từ nguồn này.
    /// </summary>
    /// <param name="basePath">Đường dẫn tương đối base app, vd "_content/ICare247.UI.Hr/i18n".</param>
    public void RegisterSource(string basePath)
    {
        if (!_sources.Contains(basePath)) _sources.Add(basePath);
    }

    /// <summary>
    /// Khởi tạo: nạp manifest + khôi phục ngôn ngữ đã lưu (localStorage) rồi áp dụng.
    /// Sự kiện theo sau: <see cref="OnChanged"/> bắn nếu ngôn ngữ khác base.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await LoadManifestAsync(ct);
        var saved = await GetSavedLanguageAsync();
        await SetLanguageAsync(string.IsNullOrWhiteSpace(saved) ? BaseLanguage : saved!, persist: false, ct);
    }

    /// <summary>
    /// Đổi ngôn ngữ hiện hành. Sự kiện theo sau: nạp overlay (nếu cần), set CultureInfo,
    /// lưu lựa chọn, bắn <see cref="OnChanged"/>.
    /// </summary>
    /// <param name="code">Mã ngôn ngữ hoặc <see cref="PseudoCode"/>.</param>
    /// <param name="persist">Có lưu vào localStorage không.</param>
    public async Task SetLanguageAsync(string code, bool persist = true, CancellationToken ct = default)
    {
        if (code == PseudoCode)
        {
            PseudoEnabled = true;
            CurrentLanguage = PseudoCode;
        }
        else
        {
            PseudoEnabled = false;
            CurrentLanguage = code;
            await LoadOverlayAsync(code, ct);
            ApplyCulture(code);
        }

        if (persist) await SaveLanguageAsync(code);
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Tra cứu chuỗi theo key. Quy tắc: overlay ngôn ngữ → <paramref name="fallback"/>
    /// (base vi nằm ngay tại chỗ gọi) → định dạng tham số → pseudo nếu đang bật.
    /// </summary>
    /// <param name="key">Khóa suy từ cấu trúc, vd "nav.module.organization".</param>
    /// <param name="fallback">Giá trị gốc tiếng Việt (luôn hiển thị nếu chưa dịch).</param>
    /// <param name="args">Tham số cho {0}/{1}… nếu có.</param>
    public string L(string key, string fallback, params object[] args)
    {
        var value = _overlay.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
        if (args is { Length: > 0 })
        {
            try { value = string.Format(_culture, value, args); }
            catch (FormatException ex) { _logger.LogWarning(ex, "i18n format lỗi cho key {Key}", key); }
        }
        return PseudoEnabled ? Pseudo(value) : value;
    }

    // ── Nội bộ ────────────────────────────────────────────────────────────

    /// <summary>Nạp manifest languages.json từ nguồn đầu tiên (Shared).</summary>
    private async Task LoadManifestAsync(CancellationToken ct)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<LanguageInfo>>(Url(_sources[0], "languages.json"), JsonOpts, ct);
            if (list is { Count: > 0 }) Available = list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "i18n: nạp manifest lỗi — dùng mặc định vi.");
        }
    }

    /// <summary>Nạp + gộp overlay {code}.json từ mọi nguồn. Base vi không cần file.</summary>
    private async Task LoadOverlayAsync(string code, CancellationToken ct)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (code != BaseLanguage)
        {
            foreach (var src in _sources)
            {
                try
                {
                    var map = await _http.GetFromJsonAsync<Dictionary<string, string>>(Url(src, $"{code}.json"), JsonOpts, ct);
                    if (map is not null)
                        foreach (var kv in map) merged[kv.Key] = kv.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "i18n: overlay {Code} từ {Src} lỗi (bỏ qua, dùng base).", code, src);
                }
            }
        }
        _overlay = merged;
    }

    /// <summary>Đặt CultureInfo theo ngôn ngữ → số/ngày + control DevExpress tự dịch.</summary>
    private void ApplyCulture(string code)
    {
        try
        {
            _culture = new CultureInfo(code);
            CultureInfo.DefaultThreadCurrentCulture = _culture;
            CultureInfo.DefaultThreadCurrentUICulture = _culture;
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _culture;
        }
        catch (CultureNotFoundException)
        {
            _culture = new CultureInfo(BaseLanguage);
        }
    }

    /// <summary>Bọc chuỗi kiểu pseudo: thêm ngoặc + kéo dài để lộ chuỗi chưa i18n / tràn.</summary>
    private static string Pseudo(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var pad = new string('·', Math.Max(1, s.Length / 3));
        return $"⟦{s}{pad}⟧";
    }

    /// <summary>Ghép URL tuyệt đối theo base app (không phụ thuộc BaseAddress API).</summary>
    private string Url(string src, string file) => $"{_nav.BaseUri}{src}/{file}";

    private async Task<string?> GetSavedLanguageAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", "ic247.lang"); }
        catch { return null; }
    }

    private async Task SaveLanguageAsync(string code)
    {
        try { await _js.InvokeVoidAsync("localStorage.setItem", "ic247.lang", code); }
        catch (Exception ex) { _logger.LogWarning(ex, "i18n: lưu ngôn ngữ lỗi."); }
    }
}
