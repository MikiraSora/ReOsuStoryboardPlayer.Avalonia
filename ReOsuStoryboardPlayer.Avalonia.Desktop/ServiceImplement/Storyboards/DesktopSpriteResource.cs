using Avalonia.Media;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

public class DesktopSpriteResource : ISpriteResource
{
    public DesktopSpriteResource(string fixImage, SKBitmap texture)
    {
        Name = fixImage;
        Image = texture;
    }

    public string Name { get; }
    public SKBitmap Image { get; }
}