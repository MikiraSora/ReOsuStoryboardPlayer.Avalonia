using System;

namespace ReOsuStoryboardPlayer.Avalonia.Views.Attributes;

/// <summary>
/// Mark that view is cachable and need not create them everytime.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CachableViewAttribute : Attribute
{
    
}