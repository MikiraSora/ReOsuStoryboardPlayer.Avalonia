using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using System;

namespace ReOsuStoryboardPlayer.Avalonia.Android.ServiceImplement.Parameters;

[Injectio.Attributes.RegisterSingleton<IParameterManager>]
public class AndroidParameterManager : IParameterManager
{
    public AndroidParameterManager()
    {
        Parameters = new AndroidParameters();
    }

    public IParameters Parameters { get; }
}