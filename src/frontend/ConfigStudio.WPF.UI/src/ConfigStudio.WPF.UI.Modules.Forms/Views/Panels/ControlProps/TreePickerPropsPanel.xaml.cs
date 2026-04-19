// File    : TreePickerPropsPanel.xaml.cs
// Module  : Forms / Views / Panels / ControlProps
// Layer   : Presentation
// Purpose : Code-behind tối giản cho TreePickerPropsPanel UserControl.
//           Toàn bộ logic xử lý nằm trong FieldConfigViewModel (DataContext kế thừa).

namespace ConfigStudio.WPF.UI.Modules.Forms.Views.Panels.ControlProps;

/// <summary>
/// UserControl cấu hình props cho Editor_Type = <c>TreePicker</c> (dropdown cây phân cấp).
/// DataContext được kế thừa từ parent FieldConfigView → FieldConfigViewModel.
/// </summary>
public partial class TreePickerPropsPanel : System.Windows.Controls.UserControl
{
    public TreePickerPropsPanel()
    {
        InitializeComponent();
    }
}
