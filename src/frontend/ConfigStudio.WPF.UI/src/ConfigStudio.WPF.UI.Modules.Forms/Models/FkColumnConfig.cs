// File    : FkColumnConfig.cs
// Module  : Forms
// Layer   : Models
// Purpose : Cấu hình 1 cột hiển thị trong popup của LookupBox (FK lookup).
//           Mỗi cột xác định fieldName (tên cột DB), captionKey (i18n key), width (px).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Đại diện 1 cột được hiển thị trong dropdown popup của <c>LookupBox</c>.
/// Được serialize thành mảng JSON trong <c>Popup_Columns_Json</c>.
/// <para>
/// <c>CaptionKey</c> là i18n resource key tra trong <c>Sys_Resource</c>.
/// Backend API resolve key → text theo <c>langCode</c> trước khi trả Blazor.
/// VD: "phongban.col.ma_phong_ban" → "Mã phòng ban" (vi) / "Department Code" (en).
/// </para>
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

    private string _captionKey = "";
    /// <summary>
    /// I18n resource key cho tiêu đề cột. VD: "phongban.col.ten_phong_ban".
    /// Backend resolve → text theo ngôn ngữ hiện tại trước khi trả cho Blazor renderer.
    /// </summary>
    public string CaptionKey
    {
        get => _captionKey;
        set => SetProperty(ref _captionKey, value);
    }

    private int _width = 150;
    /// <summary>Độ rộng cột (px). Mặc định 150.</summary>
    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }
}
