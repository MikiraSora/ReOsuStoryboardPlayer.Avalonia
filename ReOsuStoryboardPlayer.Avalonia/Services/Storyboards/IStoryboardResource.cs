using System.Collections.Generic;
using ReOsuStoryboardPlayer.Core.Base;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface IStoryboardResource
{
    public ISpriteResource GetSprite(string key);
    public ISpriteResource GetSprite(StoryboardObject obj) => GetSprite(obj.ImageFilePath);
}