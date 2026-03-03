// File    : LevelToIndentConverter.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Chuyen level cua item thanh margin thut le cho sub-item.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConfigStudio.WPF.UI.Converters;

public sealed class LevelToIndentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value is int intValue ? intValue : 0;
        return new Thickness(level * 16, 0, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
