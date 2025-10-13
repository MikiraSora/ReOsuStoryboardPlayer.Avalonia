using System;
using Microsoft.Extensions.DependencyInjection;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

public static class IServiceProviderEx
{
    public static T Resolve<T>(this IServiceProvider serviceProvider)
    {
        return ActivatorUtilities.CreateInstance<T>(serviceProvider);
    }
}