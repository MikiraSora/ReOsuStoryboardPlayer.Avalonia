using System;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JsonSourceGenerationSupportAttribute : Attribute
{
    public JsonSourceGenerationSupportAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    public Type TargetType { get; }
}