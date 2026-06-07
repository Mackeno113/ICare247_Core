// File    : FormTabItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Item tab (Ui_Tab) cho khu quản lý Tab trong FormEditor — editable, binding 2 chiều.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Đại diện một tab của form trong khu quản lý Tab (master list + editor).
/// Tách biệt với <see cref="FormTreeNode"/> vì tab không nằm trong TreeView cấu trúc.
/// Title hiển thị đa ngôn ngữ lưu trong Sys_Resource theo <see cref="TitleKey"/>.
/// </summary>
public sealed class FormTabItem : BindableBase
{
    private int _tabId;
    /// <summary>Ui_Tab.Tab_Id. = 0 khi tab mới chưa lưu.</summary>
    public int TabId { get => _tabId; set => SetProperty(ref _tabId, value); }

    private string _tabCode = "";
    /// <summary>Ui_Tab.Tab_Code — mã kỹ thuật, duy nhất trong form. [a-z0-9_].</summary>
    public string TabCode
    {
        get => _tabCode;
        set { if (SetProperty(ref _tabCode, value)) RaisePropertyChanged(nameof(DisplayLabel)); }
    }

    private string _titleKey = "";
    /// <summary>Resource key tiêu đề tab — {table_code}.tab.{tab_code}.title.</summary>
    public string TitleKey { get => _titleKey; set => SetProperty(ref _titleKey, value); }

    private string _resourceVi = "";
    /// <summary>Sys_Resource[TitleKey, 'vi'] — tiêu đề tiếng Việt.</summary>
    public string ResourceVi
    {
        get => _resourceVi;
        set { if (SetProperty(ref _resourceVi, value)) RaisePropertyChanged(nameof(DisplayLabel)); }
    }

    private string _resourceEn = "";
    /// <summary>Sys_Resource[TitleKey, 'en'] — tiêu đề tiếng Anh.</summary>
    public string ResourceEn { get => _resourceEn; set => SetProperty(ref _resourceEn, value); }

    private string _iconKey = "";
    /// <summary>Ui_Tab.Icon_Key — icon tùy chọn (không phải text dịch).</summary>
    public string IconKey { get => _iconKey; set => SetProperty(ref _iconKey, value); }

    private int _orderNo;
    /// <summary>Ui_Tab.Order_No — thứ tự trái→phải.</summary>
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private bool _isDefault;
    /// <summary>Ui_Tab.Is_Default — tab mở mặc định khi form load (max 1/form).</summary>
    public bool IsDefault { get => _isDefault; set => SetProperty(ref _isDefault, value); }

    private int _sectionCount;
    /// <summary>Số section đang gán vào tab này (chỉ đọc, để hiển thị).</summary>
    public int SectionCount { get => _sectionCount; set => SetProperty(ref _sectionCount, value); }

    /// <summary>Nhãn hiển thị trong list/combo: ưu tiên tên vi, fallback Tab Code.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(ResourceVi) ? TabCode : ResourceVi;
}
