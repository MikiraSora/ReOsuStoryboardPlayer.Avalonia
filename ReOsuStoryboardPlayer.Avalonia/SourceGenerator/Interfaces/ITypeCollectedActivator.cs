using System;

namespace ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;

public interface ITypeCollectedActivator<T>
{
    bool TryCreateInstance(string fullName, out T obj);
}