using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Desktop.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using System;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Parameters;

[Injectio.Attributes.RegisterSingleton<IParameterManager>]
public class DesktopParameterManager : IParameterManager
{
    public DesktopParameterManager()
    {
        Parameters = CommandParser.Parse(Environment.GetCommandLineArgs(), out _) ?? new DesktopParameters();
    }

    public IParameters Parameters { get; }
}