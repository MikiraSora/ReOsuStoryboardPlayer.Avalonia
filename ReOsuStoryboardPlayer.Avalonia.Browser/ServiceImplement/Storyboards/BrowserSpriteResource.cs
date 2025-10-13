using Avalonia.Media;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

public class BrowserSpriteResource : ISpriteResource
{
    public BrowserSpriteResource(string fixImage, SKBitmap texture)
    {
        Name = fixImage;
        Image = texture;
    }

    public string Name { get; }
    public SKBitmap Image { get; }
}