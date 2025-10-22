using System;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Navigation;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels;

internal sealed class ConstructorInfoEx
{
    private readonly object[] _parameterKeys;
    public readonly ConstructorInfo Info;

    public readonly bool IsPreferred;

    public readonly ParameterInfo[] Parameters;

    public ConstructorInfoEx(ConstructorInfo constructor)
    {
        Info = constructor;
        Parameters = constructor.GetParameters();
        IsPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);
        for (var i = 0; i < Parameters.Length; i++)
        {
            var fromKeyedServicesAttribute =
                (FromKeyedServicesAttribute) Attribute.GetCustomAttribute(Parameters[i],
                    typeof(FromKeyedServicesAttribute), false);
            if (fromKeyedServicesAttribute != null)
            {
                if (_parameterKeys == null)
                    _parameterKeys = new object[Parameters.Length];
                _parameterKeys[i] = fromKeyedServicesAttribute.Key;
            }
        }
    }
}

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> logger;

    [ObservableProperty]
    private IPageNavigationManager pageNavigationManager;

    [ObservableProperty]
    private IWindowManager windowManager;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IPageNavigationManager pageNavigationManager, IWindowManager windowManager)
    {
        this.logger = logger;
        WindowManager = windowManager;
        PageNavigationManager = pageNavigationManager;

        ProcessInit();

        PageNavigationManager.PropertyChanged += PageNavigationManagerOnPropertyChanged;
    }

    private void PageNavigationManagerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        logger.LogInformationEx(
            $"OnPropertyChanged {e.PropertyName}, PageNavigationManager.CurrentPageViewModel={PageNavigationManager.CurrentPageViewModel}");
    }

    private async void ProcessInit()
    {
        //load default page.
        await PageNavigationManager.SetPage<HomePageViewModel>(true);
    }
}