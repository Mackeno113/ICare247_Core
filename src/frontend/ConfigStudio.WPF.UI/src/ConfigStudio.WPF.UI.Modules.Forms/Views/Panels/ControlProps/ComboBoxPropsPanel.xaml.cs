// File    : ComboBoxPropsPanel.xaml.cs
// Module  : Forms / Views / Panels / ControlProps
// Layer   : Presentation
// Purpose : Code-behind tối giản cho ComboBoxPropsPanel UserControl.
//           Toàn bộ logic xử lý nằm trong FieldConfigViewModel (DataContext kế thừa).

namespace ConfigStudio.WPF.UI.Modules.Forms.Views.Panels.ControlProps;

/// <summary>
/// UserControl cấu hình props cho Editor_Type = <c>ComboBox</c> và <c>LookupComboBox</c>.
/// DataContext được kế thừa từ parent FieldConfigView → FieldConfigViewModel.
/// </summary>
public partial class ComboBoxPropsPanel : System.Windows.Controls.UserControl
{
    public ComboBoxPropsPanel()
    {
        InitializeComponent();
    }
}
