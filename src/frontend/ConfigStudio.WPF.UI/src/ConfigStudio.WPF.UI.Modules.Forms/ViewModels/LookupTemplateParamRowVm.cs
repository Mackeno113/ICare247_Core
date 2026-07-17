// File    : LookupTemplateParamRowVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : 1 dòng map tham số canonical của mẫu lookup (PICKER-P4) trên màn Cấu hình Field:
//           Name (từ Canonical_Params của mẫu) ← MappedValue (Field_Code / @Token / hằng số).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Dòng lưới "Map tham số" — admin điền MappedValue, các cột khác readonly từ mẫu.</summary>
public sealed class LookupTemplateParamRowVm : BindableBase
{
    /// <summary>Tên tham số canonical (không '@') — khớp @param trong Filter_Sql của mẫu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Kiểu dữ liệu khai trong mẫu (bigint/int/string/date/bool) — hiển thị gợi ý.</summary>
    public string? Type { get; init; }

    /// <summary>Bắt buộc map trước khi dùng mẫu.</summary>
    public bool Required { get; init; }

    /// <summary>Diễn giải từ mẫu (vd "Field Tỉnh/Thành trên form").</summary>
    public string? MoTa { get; init; }

    private string _mappedValue = "";
    /// <summary>
    /// Nguồn giá trị: Field_Code trên form (vd "TinhThanhPho_Id") · "@Token" đăng ký
    /// Sys_Context_Param (vd "@CongTyID_Active") · hằng số (vd "5", "true").
    /// </summary>
    public string MappedValue
    {
        get => _mappedValue;
        set => SetProperty(ref _mappedValue, value);
    }
}
