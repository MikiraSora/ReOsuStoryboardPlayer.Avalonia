using System;
using System.Collections.Generic;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Core.Base;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public class StoryboardInstance(
    ISimpleDirectory fileSystemFolder,
    StoryboardInfo storyboardInfo,
    BeatmapFolderInfoEx info,
    List<StoryboardObject> objectList,
    StoryboardResource resource) : IDisposable
{
    private bool isDisposed;
    public ISimpleDirectory FileSystemFolder { get; } = fileSystemFolder;
    public StoryboardInfo StoryboardInfo { get; } = storyboardInfo;
    public BeatmapFolderInfoEx Info { get; private set; } = info;
    public List<StoryboardObject> ObjectList { get; private set; } = objectList;
    public StoryboardResource Resource { get; } = resource;

    public void Dispose()
    {
        if (isDisposed)
            return;

        Resource?.Dispose();
        FileSystemFolder?.Dispose();

        (Application.Current as App)?.RootServiceProvider.GetService<ILogger<StoryboardInstance>>()
            .LogInformationEx($"storyboard instance {StoryboardInfo} has been disposed");

        isDisposed = true;
    }
}