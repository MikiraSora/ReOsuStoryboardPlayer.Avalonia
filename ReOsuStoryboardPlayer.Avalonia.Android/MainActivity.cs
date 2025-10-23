using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Android.Utils.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Android;

[Activity(
    Label = "ReOsuStoryboardPlayer.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Landscape,
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
                    o.SetMinimumLevel(LogLevel.Debug);
                    o.AddProvider(new LogcatLoggerProvider());
                    o.AddDebug();
                });
            })
            .LogToTrace()
            .WithInterFont();
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
    }

    protected override void OnResume()
    {
        base.OnResume();
        EnterFullScreen();
    }

    private void EnterFullScreen()
    {
        // 隐藏状态栏和导航栏
        Window.AddFlags(WindowManagerFlags.Fullscreen);
        Window.DecorView.SystemUiVisibility =
            (StatusBarVisibility)(
                SystemUiFlags.LayoutStable |
                SystemUiFlags.LayoutHideNavigation |
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.Fullscreen |
                SystemUiFlags.ImmersiveSticky
            );
    }
}