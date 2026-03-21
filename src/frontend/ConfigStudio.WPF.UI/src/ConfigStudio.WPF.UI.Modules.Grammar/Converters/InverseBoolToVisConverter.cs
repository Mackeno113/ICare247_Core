// File    : InverseBoolToVisConverter.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Converter đảo ngược: true → Collapsed, false → Visible.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Converters;

/// <summary>
/// Đảo ngược BooleanToVisibilityConverter:
/// true → Collapsed, false → Visible.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
