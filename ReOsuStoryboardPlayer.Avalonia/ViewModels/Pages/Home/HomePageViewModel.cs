using System;
using CommunityToolkit.Mvvm.Input;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Home;

public partial class HomePageViewModel : PageViewModelBase
{
    private readonly IDialogManager dialogManager;

    public HomePageViewModel(IDialogManager dialogManager)
    {
        this.dialogManager = dialogManager;
    }

    public override string Title => "主页";

    public string ProgramCommitId => ThisAssembly.GitCommitId;
    public string ProgramCommitIdShort => ProgramCommitId[..7];
    public string AssemblyVersion => ThisAssembly.AssemblyVersion;
    public DateTime ProgramCommitDate => ThisAssembly.GitCommitDate + TimeSpan.FromHours(8);
    public string ProgramBuildConfiguration => ThisAssembly.AssemblyConfiguration;

    public string ProgramBuildTime
    {
        get
        {
            var type = typeof(HomePageViewModel).Assembly.GetType("DpMapSubscribeTool.BuildTime");
            var prop = type?.GetField("Value")?.GetValue(null)
                ?.ToString();
            return prop;
        }
    }

    [RelayCommand]
    private void OpenDialog()
    {
        dialogManager.ShowMessageDialog("Good Dialog!");
    }
}