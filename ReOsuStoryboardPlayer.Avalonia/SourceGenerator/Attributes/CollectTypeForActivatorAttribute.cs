using System;

namespace ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CollectTypeForActivatorAttribute : Attribute
{
    public CollectTypeForActivatorAttribute(Type targetBaseType)
    {
        TargetBaseType = targetBaseType;
    }

    public Type TargetBaseType { get; }
}