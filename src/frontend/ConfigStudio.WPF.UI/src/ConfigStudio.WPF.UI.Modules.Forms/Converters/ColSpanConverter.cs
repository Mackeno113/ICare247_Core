// File    : ColSpanConverter.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Convert byte ColSpan ↔ bool IsChecked cho RadioButton 1/2/3.

using System.Globalization;
using System.Windows.Data;

namespace ConfigStudio.WPF.UI.Modules.Forms.Converters;

/// <summary>
/// Dùng cho 3 RadioButton Col Span: IsChecked = (ColSpan == ConverterParameter).
/// ConverterParameter = "1" | "2" | "3".
/// </summary>
public sealed class ColSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte colSpan && parameter is string paramStr
            && byte.TryParse(paramStr, out var param))
            return colSpan == param;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Chỉ cập nhật khi RadioButton được chọn (IsChecked = true)
        if (value is true && parameter is string paramStr
            && byte.TryParse(paramStr, out var param))
            return param;
        return Binding.DoNothing;
    }
}
