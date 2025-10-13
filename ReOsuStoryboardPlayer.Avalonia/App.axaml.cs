using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.UI.ValueConverters;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels;
using ReOsuStoryboardPlayer.Avalonia.Views;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia;

public class App : Application
{
    public IServiceProvider RootServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InitServiceProvider();
        //add ViewLocator
        DataTemplates.Add(ActivatorUtilities.CreateInstance<ViewLocator>(RootServiceProvider));

        var logger = RootServiceProvider.GetService<ILogger<App>>();
        var osblogger = RootServiceProvider.GetService<ILoggerFactory>().CreateLogger("OsuStoryboardPlayerCoreLog");
        Log.LogImplement = (caller, message, level) =>
        {
            var logLevl = level switch
            {
                Log.LogLevel.Debug => LogLevel.Debug,
                Log.LogLevel.None => LogLevel.Information,
                Log.LogLevel.Error => LogLevel.Error,
                Log.LogLevel.User => LogLevel.Information,
                Log.LogLevel.Warn => LogLevel.Warning
            };
            osblogger.Log(logLevl, $"{caller}(): {message}");
        };

        var injectableConverters = RootServiceProvider.GetServices<IInjectableValueConverter>();
        var injectableMultiValueConverters = RootServiceProvider.GetServices<IInjectableMultiValueConverter>();

        foreach (var converter in injectableConverters.AsEnumerable<object>().Concat(injectableMultiValueConverters))
        {
            var key = converter.GetType().Name /*.Replace("ValueConverter", string.Empty)*/;

            logger.LogInformationEx($"add injectable converter, key: {key} -> {converter.GetType().FullName}");
            Resources[key] = converter;
        }

        var mainViewModel = ActivatorUtilities.CreateInstance<MainViewModel>(RootServiceProvider);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = mainViewModel
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitServiceProvider()
    {
        if (RootServiceProvider is not null)
            throw new Exception("InitServiceProvider() has been called.");

        var serviceCollection = new ServiceCollection();

        /*
        //dialog
        serviceCollection.AddSingleton<IDialogService>(provider =>
        {
            var logger = provider.GetService<ILoggerFactory>().CreateLogger<DialogManager>();
            var viewLocator = new ViewLocator();
            var dialogFactory = new DialogFactory().AddMessageBox();

            var viewModelFactory = provider.GetService<ViewModelFactory>();

            var dialogService = new DialogService(
                new DialogManager(viewLocator, dialogFactory, logger),
                viewModelType => viewModelFactory.CreateViewModel(viewModelType));
            return dialogService;
        });
        */

        //logging
        serviceCollection.AddLogging(o => { o.SetMinimumLevel(LogLevel.Debug); });

        //others.
        serviceCollection.AddInjectsByAttributes(typeof(App).Assembly);

        //add other DI collection from other assemblies.
        AppBuilderMethodExtensions.AppBuilderStatic.injectConfigFunc?.Invoke(serviceCollection);

        RootServiceProvider = serviceCollection.BuildServiceProvider();
    }
}