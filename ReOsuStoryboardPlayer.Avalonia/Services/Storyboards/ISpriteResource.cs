using Avalonia.Media;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface ISpriteResource
{
    string Name { get; }
    SKImage Image { get; }
}