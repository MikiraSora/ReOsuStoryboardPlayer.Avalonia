using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Browser.Views;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

internal sealed class Program
{
    private static Task Main(string[] args)
    {
        //Dispatcher.UIThread.UnhandledException += UIThreadOnUnhandledException;
        //Dispatcher.UIThread.UnhandledExceptionFilter+= UIThreadOnUnhandledExceptionFilter;

        return BuildAvaloniaApp()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback
                    {
                        FontFamily =
                            new FontFamily(
                                "avares://ReOsuStoryboardPlayer.Avalonia.Browser/Assets/Fonts/NotoSansSC-VariableFont_wght.ttf#Noto Sans SC")
                    }
                }
            })
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .AppendDependencyInject(collection =>
                {
                    collection.AddReOsuStoryboardPlayerAvaloniaBrowser();
#if DEBUG
                    if (DesignModeHelper.IsDesignMode)
                        return;
#endif
                    collection.AddLogging(o =>
                    {
                        o.AddProvider(new ConsoleLoggerProvider());
                        o.AddDebug();
                        o.SetMinimumLevel(LogLevel.Debug);
                    });

                    collection.AddTypeCollectedActivator(ViewTypeCollectedActivator.Default);
                }
            )
            .With(new BrowserPlatformOptions(){})
            .LogToTrace();
}