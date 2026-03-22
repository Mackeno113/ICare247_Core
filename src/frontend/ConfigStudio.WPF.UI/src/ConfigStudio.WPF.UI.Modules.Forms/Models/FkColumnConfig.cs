// File    : FkColumnConfig.cs
// Module  : Forms
// Layer   : Models
// Purpose : Cấu hình 1 cột hiển thị trong popup của LookupBox (FK lookup).
//           Mỗi cột xác định fieldName (tên cột DB), caption (tiêu đề), width (px).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Đại diện 1 cột được hiển thị trong dropdown popup của <c>LookupBox</c>.
/// Được serialize thành mảng JSON trong <c>Control_Props_Json.columns</c>.
/// </summary>
public sealed class FkColumnConfig : BindableBase
{
    private string _fieldName = "";
    /// <summary>Tên cột trong bảng DB nguồn. VD: "Ten_PhongBan".</summary>
    public string FieldName
    {
        get => _fieldName;
        set => SetProperty(ref _fieldName, value);
    }

    private string _caption = "";
    /// <summary>Tiêu đề cột hiển thị trong popup. VD: "Tên phòng ban".</summary>
    public string Caption
    {
        get => _caption;
        set => SetProperty(ref _caption, value);
    }

    private int _width = 150;
    /// <summary>Độ rộng cột (px). Mặc định 150.</summary>
    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }
}
