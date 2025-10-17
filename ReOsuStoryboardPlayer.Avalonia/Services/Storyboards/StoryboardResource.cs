using System.Collections.Frozen;
using System.Collections.Generic;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public class StoryboardResource
{
    private IDictionary<string, SpriteResource> cacheDrawSpriteInstanceMap;

    public SpriteResource GetSprite(string key)
    {
        return cacheDrawSpriteInstanceMap.TryGetValue(key, out var group) ? group : null;
    }

    public void PinSpriteInstanceGroups(IDictionary<string, SpriteResource> map)
    {
        cacheDrawSpriteInstanceMap = map.ToFrozenDictionary();
    }
}