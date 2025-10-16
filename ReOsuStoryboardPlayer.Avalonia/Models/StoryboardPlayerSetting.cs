using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using ReOsuStoryboardPlayer.Avalonia.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Models;

public partial class StoryboardPlayerSetting : ObservableObject
{
    [ObservableProperty]
    private int audioSampleRate = 48000;

    [ObservableProperty]
    private WideScreenOption wideScreenOption;

    public static JsonTypeInfo<StoryboardPlayerSetting> JsonTypeInfo =>
        JsonSourceGenerationContext.Default.StoryboardPlayerSetting;

    public bool AllowLog
    {
        get => Setting.AllowLog;
        set
        {
            Setting.AllowLog = value;
            OnPropertyChanged();
        }
    }

    public bool DebugMode
    {
        get => Setting.DebugMode;
        set
        {
            Setting.DebugMode = value;
            OnPropertyChanged();
        }
    }

    public bool EnableSplitMoveScaleCommand
    {
        get => Setting.EnableSplitMoveScaleCommand;
        set
        {
            Setting.EnableSplitMoveScaleCommand = value;
            OnPropertyChanged();
        }
    }

    public bool EnableLoopCommandUnrolling
    {
        get => Setting.EnableLoopCommandUnrolling;
        set
        {
            Setting.EnableLoopCommandUnrolling = value;
            OnPropertyChanged();
        }
    }

    public int UpdateThreadCount
    {
        get => Setting.UpdateThreadCount;
        set
        {
            Setting.UpdateThreadCount = value;
            OnPropertyChanged();
        }
    }

    public int ParallelParseCommandLimitCount
    {
        get => Setting.ParallelParseCommandLimitCount;
        set
        {
            Setting.ParallelParseCommandLimitCount = value;
            OnPropertyChanged();
        }
    }

    public int ParallelUpdateObjectsLimitCount
    {
        get => Setting.ParallelUpdateObjectsLimitCount;
        set
        {
            Setting.ParallelUpdateObjectsLimitCount = value;
            OnPropertyChanged();
        }
    }

    public bool FunReverseEasing
    {
        get => Setting.FunReverseEasing;
        set
        {
            Setting.FunReverseEasing = value;
            OnPropertyChanged();
        }
    }

    public bool ShowProfileSuggest
    {
        get => Setting.ShowProfileSuggest;
        set
        {
            Setting.ShowProfileSuggest = value;
            OnPropertyChanged();
        }
    }
}