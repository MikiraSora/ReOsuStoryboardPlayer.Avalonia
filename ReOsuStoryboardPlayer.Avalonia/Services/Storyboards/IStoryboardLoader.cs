using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

public interface IStoryboardLoader
{
    ValueTask<IStoryboardInstance> OpenLoaderDialog();
}