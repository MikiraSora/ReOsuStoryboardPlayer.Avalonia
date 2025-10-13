using Avalonia.Media;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface ISpriteResource
{
    string Name { get; }
    SKBitmap Image { get; }
}