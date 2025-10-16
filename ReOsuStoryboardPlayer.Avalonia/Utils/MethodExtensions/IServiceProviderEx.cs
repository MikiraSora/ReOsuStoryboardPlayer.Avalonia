using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

public static class IServiceProviderEx
{
    public static T Resolve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceProvider serviceProvider)
    {
        return ActivatorUtilities.CreateInstance<T>(serviceProvider);
    }

    public static object Resolve(this IServiceProvider serviceProvider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type resolveType)
    {
        return ActivatorUtilities.CreateInstance(serviceProvider, resolveType);
    }

    public static T ResolveAOT<T>(this IServiceProvider serviceProvider) where T : IServiceProvideInjectable, new()
    {
        var obj = new T();
        obj.InitalizeInjected(serviceProvider);
        return obj;
    }
}