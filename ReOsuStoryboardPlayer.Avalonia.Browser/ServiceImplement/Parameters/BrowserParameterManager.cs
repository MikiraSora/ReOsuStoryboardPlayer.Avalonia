using System;
using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Parameters;

[Injectio.Attributes.RegisterSingleton<IParameterManager>]
public class BrowserParameterManager : IParameterManager
{
    public BrowserParameterManager()
    {
        //todo parse from queryString
        Parameters = CommandParser.Parse(Environment.GetCommandLineArgs(), out _);
    }

    public IParameters Parameters { get; }
}