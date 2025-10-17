using System;

namespace ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Attributes;

/*
 * Collect/Register all public non-abstract classes for replace AOT-unfriendly codes like Activitor.CreateInstance(string)
 * usage search keyword: ViewTypeCollectedActivator
 */
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CollectTypeForActivatorAttribute : Attribute
{
    public CollectTypeForActivatorAttribute(Type targetBaseType)
    {
        TargetBaseType = targetBaseType;
    }

    public Type TargetBaseType { get; }
}