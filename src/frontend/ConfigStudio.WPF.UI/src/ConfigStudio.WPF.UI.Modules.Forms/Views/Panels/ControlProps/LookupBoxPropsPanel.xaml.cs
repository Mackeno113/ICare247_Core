// File    : LookupBoxPropsPanel.xaml.cs
// Module  : Forms / Views / Panels / ControlProps
// Layer   : Presentation
// Purpose : Code-behind tối giản cho LookupBoxPropsPanel UserControl.
//           Toàn bộ logic xử lý nằm trong FieldConfigViewModel (DataContext kế thừa).

namespace ConfigStudio.WPF.UI.Modules.Forms.Views.Panels.ControlProps;

/// <summary>
/// UserControl cấu hình props cho Editor_Type = <c>LookupBox</c> (DxDropDownBox).
/// DataContext được kế thừa từ parent FieldConfigView → FieldConfigViewModel.
/// </summary>
public partial class LookupBoxPropsPanel : System.Windows.Controls.UserControl
{
    public LookupBoxPropsPanel()
    {
        InitializeComponent();
    }
}
