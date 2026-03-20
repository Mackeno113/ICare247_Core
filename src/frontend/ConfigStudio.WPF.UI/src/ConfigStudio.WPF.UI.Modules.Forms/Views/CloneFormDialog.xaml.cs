// File    : CloneFormDialog.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind cho CloneFormDialog — không chứa logic (đặt trong ViewModel).

using System.Windows.Controls;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

/// <summary>
/// Code-behind cho CloneFormDialog.
/// Logic xử lý đặt trong <see cref="ViewModels.CloneFormDialogViewModel"/>.
/// </summary>
public partial class CloneFormDialog : UserControl
{
    public CloneFormDialog()
    {
        InitializeComponent();
    }
}
