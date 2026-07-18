// File    : AddressBoxPropsPanel.xaml.cs
// Module  : Forms / Views / Panels / ControlProps
// Layer   : Presentation
// Purpose : Code-behind tối giản cho AddressBoxPropsPanel UserControl.
//           Toàn bộ logic nằm trong FieldConfigViewModel (DataContext kế thừa).

namespace ConfigStudio.WPF.UI.Modules.Forms.Views.Panels.ControlProps;

/// <summary>
/// UserControl cấu hình props cho Editor_Type = <c>AddressBox</c> (khối địa chỉ composite).
/// DataContext kế thừa từ parent FieldConfigView → FieldConfigViewModel.
/// </summary>
public partial class AddressBoxPropsPanel : System.Windows.Controls.UserControl
{
    public AddressBoxPropsPanel()
    {
        InitializeComponent();
    }
}
