using System;
using System.Collections.Generic;
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

[RegisterSingleton<ViewModelFactory>]
public static class TypeCollectedActivatorHelper<T>
{
    private static readonly Dictionary<string, ITypeCollectedActivator<T>> cacheClassToActivatorMap = new();

    public static bool TryCreateInstance(IServiceProvider serviceProvider, string fullClassName, out T instance)
    {
        if (cacheClassToActivatorMap.TryGetValue(fullClassName, out var cacheActivator))
            return cacheActivator.TryCreateInstance(fullClassName, out instance);

        var activitors = serviceProvider.GetServices<ITypeCollectedActivator<T>>();
        foreach (var activitor in activitors)
            if (activitor.TryCreateInstance(fullClassName, out instance))
            {
                cacheClassToActivatorMap[fullClassName] = activitor;
                return true;
            }

        var logger = serviceProvider.GetService<ILoggerFactory>()
            .CreateLogger(nameof(TypeCollectedActivatorHelper<T>));
        logger.LogErrorEx(
            $"Can't create instance of {fullClassName} for type {typeof(T).FullName}, please check if your project used/injected source generator attribute [CollectTypeForActivator(typeof({typeof(T).Name}))] or not");
        instance = default;
        return false;
    }
}