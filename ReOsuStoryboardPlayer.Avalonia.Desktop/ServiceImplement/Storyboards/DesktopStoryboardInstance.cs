using System.Collections.Generic;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

public class DesktopStoryboardInstance : IStoryboardInstance
{
    public static DesktopStoryboardInstance CreateInstance()
    {
        return new DesktopStoryboardInstance();
    }

    public BeatmapFolderInfoEx InfoEx { get; set; }
    public StoryboardInfo StoryboardInfo { get; set; }
    public BeatmapFolderInfo Info => InfoEx;
    public List<StoryboardObject> ObjectList { get; set; }
    public IStoryboardResource Resource { get; set; }
}