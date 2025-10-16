using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Navigation;

public interface IPageNavigationManager : INotifyPropertyChanged
{
    PageViewModelBase CurrentPageViewModel { get; }

    Task<T> SetPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]T>(bool cleanNavigationStack = false) where T : PageViewModelBase;
    Task SetPage(PageViewModelBase pageViewModel, bool cleanNavigationStack = false);

    Task PushPage(PageViewModelBase pageViewModel);
    Task<T> PushPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]T>() where T : PageViewModelBase;

    Task<bool> TryPopPage();
    Task<bool> TryPopToPage<T>() where T : PageViewModelBase;
}