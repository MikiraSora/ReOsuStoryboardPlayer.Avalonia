using Android.App;
using Android.Content.PM;

using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Android.Utils.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using static ReOsuStoryboardPlayer.Core.Utils.Log;

namespace ReOsuStoryboardPlayer.Avalonia.Android;

[Activity(
    Label = "ReOsuStoryboardPlayer.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .AppendDependencyInject(collection =>
            {
                collection.AddReOsuStoryboardPlayerAvaloniaAndroid();

                collection.AddLogging(o =>
                {
                    o.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    o.AddProvider(new LogcatLoggerProvider());
                    o.AddDebug();
                });
            })
            .LogToTrace()
            .WithInterFont();
    }
}
