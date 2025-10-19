using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace ReOsuStoryboardPlayer.Avalonia.UI.ValueConverters;

public class AudioPlayerProgressConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.ElementAtOrDefault(0) is TimeSpan appliedLeadInCurrentTime &&
            values.ElementAtOrDefault(1) is TimeSpan duration &&
            values.ElementAtOrDefault(2) is TimeSpan leadInTime)
            return (appliedLeadInCurrentTime + leadInTime) / (duration + leadInTime);

        return 0;
    }
}