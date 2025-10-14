using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Navigation;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> logger;

    [ObservableProperty]
    private IPageNavigationManager pageNavigationManager;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IPageNavigationManager pageNavigationManager,
        ViewModelFactory viewModelFactory)
    {
        this.logger = logger;
        PageNavigationManager = pageNavigationManager;

        ProcessInit();
    }

    private async void ProcessInit()
    {
        //load default page.
        await PageNavigationManager.SetPage<HomePageViewModel>(true);
    }
}