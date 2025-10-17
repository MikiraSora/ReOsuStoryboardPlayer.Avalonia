using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface IStoryboardLoadDialog
{
    ValueTask<StoryboardInstance> OpenLoaderDialog();
}