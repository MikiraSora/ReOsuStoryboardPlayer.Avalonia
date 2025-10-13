using System.Collections.Frozen;
using System.Collections.Generic;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

public class DesktopStoryboardResource : IStoryboardResource
{
    private IDictionary<string, DesktopSpriteResource> cacheDrawSpriteInstanceMap;

    public ISpriteResource GetSprite(string key)
    {
        return cacheDrawSpriteInstanceMap.TryGetValue(key, out var group) ? group : null;
    }

    public void PinSpriteInstanceGroups(Dictionary<string, DesktopSpriteResource> map)
    {
        cacheDrawSpriteInstanceMap = map.ToFrozenDictionary();
    }
}