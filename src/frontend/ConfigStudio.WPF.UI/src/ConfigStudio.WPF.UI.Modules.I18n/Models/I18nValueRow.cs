// File    : I18nValueRow.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : Một dòng bản dịch (1 ngôn ngữ) trong I18nEditorDialog.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.I18n.Models;

/// <summary>
/// Đại diện bản dịch của một resource key cho một ngôn ngữ cụ thể.
/// Danh sách lang lấy động từ Sys_Language → hỗ trợ thêm ngôn ngữ không cần sửa code.
/// </summary>
public sealed class I18nValueRow : BindableBase
{
    /// <summary>Mã ngôn ngữ (Sys_Language.Lang_Code) — vd "vi", "en", "ja".</summary>
    public string LangCode { get; init; } = "";

    /// <summary>Tên ngôn ngữ hiển thị (Sys_Language.Lang_Name).</summary>
    public string LangName { get; init; } = "";

    /// <summary>Ngôn ngữ mặc định của hệ thống — bắt buộc nhập bản dịch.</summary>
    public bool IsDefault { get; init; }

    private string _value = "";
    /// <summary>Bản dịch (Sys_Resource.Resource_Value) cho ngôn ngữ này.</summary>
    public string Value { get => _value; set => SetProperty(ref _value, value); }

    /// <summary>Nhãn hiển thị: "Tiếng Việt (vi)".</summary>
    public string DisplayLang => $"{LangName} ({LangCode})";
}
