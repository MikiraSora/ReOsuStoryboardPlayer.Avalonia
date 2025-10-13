using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ReOsuStoryboardPlayer.Avalonia.UI.ValueConverters;

public class TimeSpanToStringValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
            return ts.ToString(@"mm\:ss\.fff");

        return "00:00.000";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}