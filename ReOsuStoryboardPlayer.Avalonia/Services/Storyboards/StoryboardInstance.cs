using System.Collections.Generic;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface IStoryboardInstance
{
    public StoryboardInfo StoryboardInfo { get; }
    public BeatmapFolderInfo Info { get; }
    public List<StoryboardObject> ObjectList { get; }
    public IStoryboardResource Resource { get; }
}