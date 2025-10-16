using System;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

public interface IServiceProvideInjectable
{
    void InitalizeInjected(IServiceProvider serviceProvider);
}