using System.Collections.Generic;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

public class BrowserStoryboardInstance : IStoryboardInstance
{
    public ISimpleDirectory StoryboardFileSystemRootDirectory { get; set; }

    public BeatmapFolderInfoEx InfoEx { get; set; }
    public StoryboardInfo StoryboardInfo { get; set; }
    public BeatmapFolderInfo Info => InfoEx;
    public List<StoryboardObject> ObjectList { get; set; }
    public IStoryboardResource Resource { get; set; }

    public static BrowserStoryboardInstance CreateInstance()
    {
        return new BrowserStoryboardInstance();
    }
}