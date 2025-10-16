using System;
using System.Diagnostics.CodeAnalysis;
using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

[RegisterSingleton<ViewModelFactory>]
public class ViewModelFactory
{
    private readonly IServiceProvider serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public T CreateViewModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : ViewModelBase
    {
        return (T) CreateViewModel(typeof(T));
    }

    public ViewModelBase CreateViewModel(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type viewModelType)
    {
        return (ViewModelBase) serviceProvider.Resolve(viewModelType);
    }
}