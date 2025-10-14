using System;
using System.Globalization;
using Avalonia.Data;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.UI.ValueConverters;

[RegisterInjectable(typeof(IInjectableValueConverter))]
public class RadioButtonEnumToBoolConverter(ILogger<RadioButtonEnumToBoolConverter> logger) : IInjectableValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        logger.LogDebugEx($"value: {value}, targetType: {targetType.Name}, parameter: {parameter}, culture: {culture}");
        return value?.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        logger.LogDebugEx($"value: {value}, targetType: {targetType.Name}, parameter: {parameter}, culture: {culture}");
        return value?.Equals(true) == true ? parameter : BindingOperations.DoNothing;
    }
}