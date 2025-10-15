using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Plaform;

[Injectio.Attributes.RegisterSingleton<IPlatform>]
public class BrowserPlatform : IPlatform
{
    public bool SupportMultiThread => false;
}