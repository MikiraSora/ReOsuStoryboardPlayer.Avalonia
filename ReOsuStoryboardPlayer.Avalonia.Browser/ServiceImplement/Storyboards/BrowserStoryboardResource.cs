using System.Collections.Frozen;
using System.Collections.Generic;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

public class BrowserStoryboardResource : IStoryboardResource
{
    private IDictionary<string, BrowserSpriteResource> cacheDrawSpriteInstanceMap;

    public ISpriteResource GetSprite(string key)
    {
        return cacheDrawSpriteInstanceMap.TryGetValue(key, out var group) ? group : null;
    }

    public void PinSpriteInstanceGroups(Dictionary<string, BrowserSpriteResource> map)
    {
        cacheDrawSpriteInstanceMap = map.ToFrozenDictionary();
    }
}