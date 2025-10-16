using System;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Views;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

[RegisterSingleton<ViewFactory>]
public class ViewFactory
{
    private readonly ILogger<ViewFactory> logger;
    private readonly IServiceProvider serviceProvider;

    public ViewFactory(IServiceProvider serviceProvider, ILogger<ViewFactory> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public ViewBase CreateView(string fullClassName)
    {
        if (!TypeCollectedActivatorHelper<ViewBase>.TryCreateInstance(serviceProvider, fullClassName, out var instance))
        {
            logger.LogErrorEx($"failed creating view for {fullClassName}");
            return default;
        }

        return instance;
    }
}