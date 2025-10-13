using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> logger;
    private readonly IPersistence persistence;

    private readonly ViewModelFactory viewModelFactory;

    [ObservableProperty]
    private bool enableNavigratable;

    [ObservableProperty]
    private bool isPaneOpen;

    [ObservableProperty]
    private ViewModelBase mainPageContent;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IPersistence persistence,
        ViewModelFactory viewModelFactory)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.viewModelFactory = viewModelFactory;

        ProcessInit();
    }

    public ValueTask NavigatePageAsync<T>(T existObj = default) where T : PageViewModelBase
    {
        var obj = existObj ?? viewModelFactory.CreateViewModel(typeof(T));
        var type = obj.GetType();

        MainPageContent = obj;

        return ValueTask.CompletedTask;
    }

    public ValueTask NavigatePageAsync(Type pageViewModelType)
    {
        return NavigatePageAsync(viewModelFactory.CreateViewModel(pageViewModelType) as PageViewModelBase);
    }

    private async void ProcessInit()
    {
        if (!DesignModeHelper.IsDesignMode)
        {
            var setting = await persistence.Load<ApplicationSettings>();
        }

        //load default page.
        await NavigatePageAsync<HomePageViewModel>();
    }
}