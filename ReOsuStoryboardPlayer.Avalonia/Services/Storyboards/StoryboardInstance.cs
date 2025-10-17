using System.Collections.Generic;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Core.Base;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public record StoryboardInstance(
    ISimpleDirectory FileSystemFolder,
    StoryboardInfo StoryboardInfo,
    BeatmapFolderInfoEx Info,
    List<StoryboardObject> ObjectList,
    StoryboardResource Resource);