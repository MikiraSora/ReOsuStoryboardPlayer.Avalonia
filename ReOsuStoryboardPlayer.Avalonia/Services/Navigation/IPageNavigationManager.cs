using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Navigation;

public interface IPageNavigationManager
{
    PageViewModelBase CurrentPageViewModel { get; }

    Task<T> SetPage<T>(bool cleanNavigationStack = false) where T : PageViewModelBase;
    Task SetPage(PageViewModelBase pageViewModel, bool cleanNavigationStack = false);

    Task PushPage(PageViewModelBase pageViewModel);
    Task<T> PushPage<T>() where T : PageViewModelBase;

    Task<bool> TryPopPage();
    Task<bool> TryPopToPage<T>() where T : PageViewModelBase;
}