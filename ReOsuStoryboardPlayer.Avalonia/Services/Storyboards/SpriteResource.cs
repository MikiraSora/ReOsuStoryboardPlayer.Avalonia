using System;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public class SpriteResource(string name, SKImage image, Action<SpriteResource> disposeAction) : IDisposable
{
    private bool isDisposed;
    public string Name { get; } = name;
    public SKImage Image { get; set; } = image;

    public void Dispose()
    {
        if (isDisposed)
            return;
        disposeAction?.Invoke(this);
        isDisposed = true;
    }
}