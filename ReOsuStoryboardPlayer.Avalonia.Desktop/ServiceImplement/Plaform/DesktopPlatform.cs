using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Plaform;

[RegisterInjectable(typeof(IPlatform), ServiceLifetime.Singleton)]
public class DesktopPlatform : IPlatform
{
    public bool SupportMultiThread => true;
}