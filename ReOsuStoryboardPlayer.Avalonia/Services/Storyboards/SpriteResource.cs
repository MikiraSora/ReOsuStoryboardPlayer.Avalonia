using System;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public class SpriteResource(string name, SKImage image) : IDisposable
{
    public string Name { get; } = name;
    public SKImage Image { get; set; } = image;

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}