namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public struct StoryboardInfo
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string DifficultyName { get; set; }
    public int BeatmapId { get; set; }
    public int BeatmapSetId { get; set; }
    public string Creator { get; set; }
    public string Source { get; set; }

    public string DisplayName => $"[{BeatmapSetId}]{Artist} - {Title}";
}