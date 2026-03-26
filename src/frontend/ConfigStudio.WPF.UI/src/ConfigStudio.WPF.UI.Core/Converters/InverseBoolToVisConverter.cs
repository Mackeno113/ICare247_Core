// File    : InverseBoolToVisConverter.cs
// Module  : Core
// Layer   : Shared
// Purpose : Converter dùng chung — đảo ngược: true → Collapsed, false → Visible.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConfigStudio.WPF.UI.Core.Converters;

/// <summary>
/// Đảo ngược <see cref="BooleanToVisibilityConverter"/>:
/// <c>true</c> → <see cref="Visibility.Collapsed"/>,
/// <c>false</c> → <see cref="Visibility.Visible"/>.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
