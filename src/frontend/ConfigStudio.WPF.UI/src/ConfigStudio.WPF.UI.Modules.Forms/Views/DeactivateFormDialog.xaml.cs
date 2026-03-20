// File    : DeactivateFormDialog.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind cho DeactivateFormDialog — không chứa logic (đặt trong ViewModel).

using System.Windows.Controls;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

/// <summary>
/// Code-behind cho DeactivateFormDialog.
/// Logic xử lý đặt trong <see cref="ViewModels.DeactivateFormDialogViewModel"/>.
/// </summary>
public partial class DeactivateFormDialog : UserControl
{
    public DeactivateFormDialog()
    {
        InitializeComponent();
    }
}
