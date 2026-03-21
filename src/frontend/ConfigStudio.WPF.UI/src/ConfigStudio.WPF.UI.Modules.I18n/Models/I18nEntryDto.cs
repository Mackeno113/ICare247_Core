// File    : I18nEntryDto.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : DTO hiển thị 1 dòng trong DataGrid i18n — key + các ngôn ngữ.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.I18n.Models;

/// <summary>
/// DTO đại diện cho 1 resource key với giá trị theo từng ngôn ngữ.
/// Cấu trúc: Key | Module | vi-VN | en-US | ja-JP.
/// </summary>
public class I18nEntryDto : BindableBase
{
    public int ResourceId { get; set; }

    private string _resourceKey = "";
    /// <summary>Key duy nhất (VD: lbl.soluong, err.fld.req).</summary>
    public string ResourceKey { get => _resourceKey; set => SetProperty(ref _resourceKey, value); }

    private string _module = "";
    /// <summary>Module sở hữu key (Form, Field, Rule, Event, System).</summary>
    public string Module { get => _module; set => SetProperty(ref _module, value); }

    private string _tablePrefix = "";
    /// <summary>Prefix table của key — segment đầu: "nhanvien.field.x" → "nhanvien".</summary>
    public string TablePrefix { get => _tablePrefix; set => SetProperty(ref _tablePrefix, value); }

    private string _viVn = "";
    /// <summary>Giá trị tiếng Việt.</summary>
    public string ViVn { get => _viVn; set => SetProperty(ref _viVn, value); }

    private string _enUs = "";
    /// <summary>Giá trị tiếng Anh.</summary>
    public string EnUs { get => _enUs; set => SetProperty(ref _enUs, value); }

    private string _jaJp = "";
    /// <summary>Giá trị tiếng Nhật.</summary>
    public string JaJp { get => _jaJp; set => SetProperty(ref _jaJp, value); }

    /// <summary>Kiểm tra có thiếu bản dịch nào không.</summary>
    public bool HasMissing => string.IsNullOrWhiteSpace(ViVn)
                           || string.IsNullOrWhiteSpace(EnUs)
                           || string.IsNullOrWhiteSpace(JaJp);
}
