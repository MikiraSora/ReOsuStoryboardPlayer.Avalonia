using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Desktop.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using System;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Parameters;

[RegisterInjectable(typeof(IParameterManager), ServiceLifetime.Singleton)]
public class DesktopParameterManager : IParameterManager
{
    public DesktopParameterManager()
    {
        Parameters = CommandParser.Parse(Environment.GetCommandLineArgs(), out _);
    }

    public IParameters Parameters { get; }
}