using System;
using System.Globalization;
using Avalonia;

namespace ReOsuStoryboardPlayer.Avalonia.UI.ValueConverters;

public class RadioButtonEnumToBoolConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
            return value.Equals(parameter);
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
            return (bool) value ? parameter : AvaloniaProperty.UnsetValue;
        return AvaloniaProperty.UnsetValue;
    }
}