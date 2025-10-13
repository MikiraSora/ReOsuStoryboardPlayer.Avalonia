using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

[RegisterInjectable(typeof(IStoryboardLoader), ServiceLifetime.Singleton)]
public class BrowserStoryboardLoader(
    ILogger<BrowserStoryboardLoader> logger,
    IDialogManager dialogManager,
    IParameterManager parameterManager)
    : IStoryboardLoader
{
    private readonly IParameterManager parameterManager = parameterManager;

    public async ValueTask<IStoryboardInstance> OpenLoaderDialog()
    {
        var openDialog = await dialogManager.ShowDialog<BrowserOpenStoryboardDialogViewModel>();
        return default;
    }
}