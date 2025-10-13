using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

public class DesktopSpriteResource : ISpriteResource
{
    public DesktopSpriteResource(string fixImage, SKImage image)
    {
        Name = fixImage;
        Image = image;
    }

    public string Name { get; }
    public SKImage Image { get; }
}