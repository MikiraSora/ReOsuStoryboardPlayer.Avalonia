using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
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

    protected override void OnResume()
    {
        base.OnResume();
        EnterFullScreen();
    }

    private void EnterFullScreen()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            // Android 11+
            var controller = Window.InsetsController;
            if (controller != null)
            {
                // 隐藏状态栏和导航栏
                controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());

                // 设置行为：用户滑动时临时显示系统栏，之后自动隐藏
                controller.SystemBarsBehavior =
                    (int) WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
    }
}