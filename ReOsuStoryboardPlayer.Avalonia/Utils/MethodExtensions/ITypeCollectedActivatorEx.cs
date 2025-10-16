using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

public static class ITypeCollectedActivatorEx
{
    public static IServiceCollection AddTypeCollectedActivator<T>(this IServiceCollection services,
        ITypeCollectedActivator<T> activator)
    {
        services.AddSingleton(_ => activator);

        return services;
    }
}