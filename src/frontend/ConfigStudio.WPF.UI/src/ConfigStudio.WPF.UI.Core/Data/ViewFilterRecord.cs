// File    : ViewFilterRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : Model một dòng cấu hình control lọc Ui_View_Filter — editable trong lưới con.
//           MỖI THAM SỐ = 1 DÒNG (DateRange tách 2 dòng từ/đến).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Một control lọc trên panel trái của View (<c>dbo.Ui_View_Filter</c>). Kế thừa
/// <see cref="BindableBase"/> để chỉnh inline trong GridControl phản ánh ngay ra UI.
/// </summary>
public sealed class ViewFilterRecord : BindableBase
{
    public int FilterId { get; set; }

    private string _filterCode = "";
    /// <summary>Mã kỹ thuật control (unique/View) — client gửi giá trị theo code này.</summary>
    public string FilterCode { get => _filterCode; set => SetProperty(ref _filterCode, value); }

    private string _controlType = "Text";
    /// <summary>Text | Number | Date | Combo | MultiSelect | Checkbox | Radio.</summary>
    public string ControlType { get => _controlType; set => SetProperty(ref _controlType, value); }

    private string? _labelKey;
    /// <summary>Key i18n nhãn control (bắt buộc khi lưu).</summary>
    public string? LabelKey { get => _labelKey; set => SetProperty(ref _labelKey, value); }

    private string? _placeholderKey;
    public string? PlaceholderKey { get => _placeholderKey; set => SetProperty(ref _placeholderKey, value); }

    private string? _tooltipKey;
    public string? TooltipKey { get => _tooltipKey; set => SetProperty(ref _tooltipKey, value); }

    private string _paramName = "";
    /// <summary>Tên tham số SP/SQL — VD @MaBN, @TuNgay (literal, whitelist).</summary>
    public string ParamName { get => _paramName; set => SetProperty(ref _paramName, value); }

    private string _paramType = "string";
    /// <summary>string | int | decimal | date | bool.</summary>
    public string ParamType { get => _paramType; set => SetProperty(ref _paramType, value); }

    private string _operator = "=";
    /// <summary>= | LIKE | &gt;= | &lt;= | IN.</summary>
    public string Operator { get => _operator; set => SetProperty(ref _operator, value); }

    private string? _defaultValue;
    /// <summary>Giá trị/Item_Code mặc định (literal — KHÔNG i18n).</summary>
    public string? DefaultValue { get => _defaultValue; set => SetProperty(ref _defaultValue, value); }

    private bool _isRequired;
    public bool IsRequired { get => _isRequired; set => SetProperty(ref _isRequired, value); }

    private bool _isVisible = true;
    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private byte _colSpan = 1;
    /// <summary>Độ rộng trên panel (grid 4-col).</summary>
    public byte ColSpan { get => _colSpan; set => SetProperty(ref _colSpan, value); }

    private string? _lookupSource;
    /// <summary>NULL | static | dynamic.</summary>
    public string? LookupSource { get => _lookupSource; set => SetProperty(ref _lookupSource, value); }

    private string? _lookupCode;
    public string? LookupCode { get => _lookupCode; set => SetProperty(ref _lookupCode, value); }

    private string? _lookupSql;
    public string? LookupSql { get => _lookupSql; set => SetProperty(ref _lookupSql, value); }

    private string? _propsJson;
    public string? PropsJson { get => _propsJson; set => SetProperty(ref _propsJson, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
}
