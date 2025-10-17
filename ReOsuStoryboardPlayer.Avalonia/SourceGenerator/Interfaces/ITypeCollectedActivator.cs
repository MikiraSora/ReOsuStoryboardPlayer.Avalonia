using System;

namespace ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;

public interface ITypeCollectedActivator<T>
{
    bool TryCreateInstance(IServiceProvider serviceProvider, string fullName, out T obj);
}