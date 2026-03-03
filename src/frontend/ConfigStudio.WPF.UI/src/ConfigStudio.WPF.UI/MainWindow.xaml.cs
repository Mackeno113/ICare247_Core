// File    : MainWindow.xaml.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Code-behind toi gian cho shell window.

using System.Windows;
using System.Windows.Input;

namespace ConfigStudio.WPF.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        DragMove();
    }
}
