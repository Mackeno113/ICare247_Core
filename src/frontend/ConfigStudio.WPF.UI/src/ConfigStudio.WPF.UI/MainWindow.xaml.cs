// File    : MainWindow.xaml.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Code-behind shell window + DataTemplateSelector cho custom sidebar.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConfigStudio.WPF.UI.Models;

namespace ConfigStudio.WPF.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

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

/// <summary>
/// Chọn DataTemplate phù hợp cho từng loại SidebarEntry:
/// header (section label), divider (đường kẻ ngang), item (nút điều hướng).
/// </summary>
public sealed class SidebarTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate  { get; set; }
    public DataTemplate? DividerTemplate { get; set; }
    public DataTemplate? ItemTemplate    { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is SidebarEntry entry)
        {
            if (entry.IsDivider) return DividerTemplate;
            if (entry.IsHeader)  return HeaderTemplate;
            return ItemTemplate;
        }
        return base.SelectTemplate(item, container);
    }
}
