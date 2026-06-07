// File    : I18nEditorDialogViewModel.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : ViewModel cho I18nEditorDialog — popup nhập bản dịch đa ngôn ngữ cho 1 resource key.
//           Dùng chung ở mọi nơi có i18n (Section, Tab, Field, Event...).

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.I18n.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.I18n.ViewModels;

/// <summary>
/// Popup nhập bản dịch cho một resource key cố định.
/// Tham số vào (DialogParameters):
///   - "key"          (string, bắt buộc) — resource key cần dịch.
///   - "contextLabel" (string, tùy chọn) — nhãn ngữ cảnh hiển thị ở header.
///   - "keyEditable"  (bool,   tùy chọn) — cho phép sửa key (mặc định false).
///   - "seedValue"    (string, tùy chọn) — giá trị mồi cho ngôn ngữ mặc định nếu key chưa có.
/// Trả về (ButtonResult.OK):
///   - "key"          — key cuối cùng (nếu user sửa).
///   - "primaryValue" — bản dịch ngôn ngữ mặc định (để caller cập nhật preview inline).
/// </summary>
public sealed class I18nEditorDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly II18nDataService? _i18n;

    public I18nEditorDialogViewModel(II18nDataService? i18n = null)
    {
        _i18n = i18n;
        SaveCommand   = new DelegateCommand(async () => await ExecuteSaveAsync(), () => !IsSaving)
            .ObservesProperty(() => IsSaving);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    // ── IDialogAware ─────────────────────────────────────────
    public DialogCloseListener RequestClose { get; set; }
    public string Title => "Dịch đa ngôn ngữ";
    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    // ── State ────────────────────────────────────────────────

    private string _resourceKey = "";
    /// <summary>Resource key đang chỉnh sửa bản dịch.</summary>
    public string ResourceKey { get => _resourceKey; set => SetProperty(ref _resourceKey, value); }

    private bool _keyEditable;
    /// <summary>Cho phép sửa key trực tiếp (nơi key tự sinh = false).</summary>
    public bool KeyEditable { get => _keyEditable; set => SetProperty(ref _keyEditable, value); }

    private string _contextLabel = "";
    /// <summary>Nhãn ngữ cảnh hiển thị ở header (vd "Tiêu đề Section").</summary>
    public string ContextLabel
    {
        get => _contextLabel;
        set { if (SetProperty(ref _contextLabel, value)) RaisePropertyChanged(nameof(HasContextLabel)); }
    }

    public bool HasContextLabel => !string.IsNullOrWhiteSpace(_contextLabel);

    /// <summary>Danh sách bản dịch theo từng ngôn ngữ.</summary>
    public ObservableCollection<I18nValueRow> Rows { get; } = [];

    private bool _isSaving;
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        private set { if (SetProperty(ref _errorMessage, value)) RaisePropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand SaveCommand   { get; }
    public DelegateCommand CancelCommand { get; }

    // ── IDialogAware open ────────────────────────────────────

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        ResourceKey = parameters.TryGetValue("key", out string? key) ? key ?? "" : "";
        ContextLabel = parameters.TryGetValue("contextLabel", out string? lbl) ? lbl ?? "" : "";
        KeyEditable = parameters.TryGetValue("keyEditable", out bool editable) && editable;
        var seedValue = parameters.TryGetValue("seedValue", out string? seed) ? seed ?? "" : "";

        await LoadLanguagesAsync(seedValue);
    }

    /// <summary>Load danh sách ngôn ngữ + bản dịch hiện có cho key.</summary>
    private async Task LoadLanguagesAsync(string seedValue)
    {
        Rows.Clear();

        // Lấy danh sách ngôn ngữ động; fallback vi/en nếu DB chưa cấu hình
        var langs = _i18n is not null ? await _i18n.GetLanguagesAsync() : [];
        // Fallback khi Sys_Language rỗng. Lang_Name lấy từ DB phải đảm bảo seed bằng N'...' (xem db/026).
        var langList = langs.Count > 0
            ? langs.Select(l => (l.LangCode, l.LangName, l.IsDefault)).ToList()
            : [("vi", "Tiếng Việt", true), ("en", "English", false)];

        foreach (var (code, name, isDefault) in langList)
        {
            string value = "";
            if (_i18n is not null && !string.IsNullOrEmpty(ResourceKey))
                value = await _i18n.ResolveKeyAsync(ResourceKey, code) ?? "";

            // Mồi giá trị cho ngôn ngữ mặc định nếu chưa có bản dịch
            if (string.IsNullOrEmpty(value) && isDefault && !string.IsNullOrEmpty(seedValue))
                value = seedValue;

            Rows.Add(new I18nValueRow
            {
                LangCode  = code,
                LangName  = name,
                IsDefault = isDefault,
                Value     = value
            });
        }
    }

    // ── Save ─────────────────────────────────────────────────

    private async Task ExecuteSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ResourceKey))
        {
            ErrorMessage = "Resource key không được để trống.";
            return;
        }

        // Ngôn ngữ mặc định bắt buộc có bản dịch
        var defaultRow = Rows.FirstOrDefault(r => r.IsDefault) ?? Rows.FirstOrDefault();
        if (defaultRow is not null && string.IsNullOrWhiteSpace(defaultRow.Value))
        {
            ErrorMessage = $"Bản dịch ngôn ngữ mặc định ({defaultRow.DisplayLang}) là bắt buộc.";
            return;
        }

        if (_i18n is null)
        {
            ErrorMessage = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
            return;
        }

        IsSaving = true;
        try
        {
            foreach (var row in Rows)
                if (!string.IsNullOrWhiteSpace(row.Value))
                    await _i18n.SaveResourceAsync(ResourceKey, row.LangCode, row.Value);

            var result = new DialogResult(ButtonResult.OK);
            result.Parameters.Add("key", ResourceKey);
            result.Parameters.Add("primaryValue", defaultRow?.Value ?? "");
            RequestClose.Invoke(result);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lưu bản dịch thất bại: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
