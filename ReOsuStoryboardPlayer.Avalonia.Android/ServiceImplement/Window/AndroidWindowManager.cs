using Android.Content;
using CommunityToolkit.Mvvm.ComponentModel;
using Injectio.Attributes;
using ReOsuStoryboardPlayer.Avalonia.Services.Window;
using System;

namespace ReOsuStoryboardPlayer.Avalonia.Android.ServiceImplement.Window;

[RegisterSingleton<IWindowManager>]
public class BrowserWindowManager : ObservableObject, IWindowManager
{
    public bool IsFullScreen
    {
        get => true;
        set
        {
            // No-op
        }
    }

    public void OpenUrl(string url)
    {
        try
        {
            var context =  global::Android.App.Application.Context;

            var intent = new Intent(Intent.ActionView);
            intent.SetData(global::Android.Net.Uri.Parse(url));
            intent.AddFlags(ActivityFlags.NewTask);

            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenUrl error: {ex}");
        }
    }
}