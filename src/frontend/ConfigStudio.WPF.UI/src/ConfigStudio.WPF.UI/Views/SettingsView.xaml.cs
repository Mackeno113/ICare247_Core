// File    : SettingsView.xaml.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Code-behind cho SettingsView — chỉ xử lý PasswordBox (không bind được).

using System.Windows;
using System.Windows.Controls;
using ConfigStudio.WPF.UI.ViewModels;

namespace ConfigStudio.WPF.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        // ── Khi ViewModel load xong → đổ Password hiện tại vào PasswordBox ──
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Người dùng nhập password → cập nhật ViewModel (PasswordBox không bind được).
    /// </summary>
    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    /// <summary>
    /// ViewModel đổi (Prism navigate) → nạp lại Password vào PasswordBox.
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SettingsViewModel vm)
            DbPasswordBox.Password = vm.Password;
    }
}
