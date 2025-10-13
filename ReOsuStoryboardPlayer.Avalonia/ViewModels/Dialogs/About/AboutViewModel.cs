using System;
using CommunityToolkit.Mvvm.Input;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.About;

public partial class AboutViewModel : DialogViewModelBase
{
    public override string DialogIdentifier => nameof(AboutViewModel);

    public override string Title => "关于";

    public string ProgramCommitId => ThisAssembly.GitCommitId;
    public string ProgramCommitIdShort => ProgramCommitId[..7];
    public string AssemblyVersion => ThisAssembly.AssemblyVersion;
    public DateTime ProgramCommitDate => ThisAssembly.GitCommitDate + TimeSpan.FromHours(8);
    public string ProgramBuildConfiguration => ThisAssembly.AssemblyConfiguration;

    public string ProgramBuildTime
    {
        get
        {
            var type = typeof(AboutViewModel).Assembly.GetType("DpMapSubscribeTool.BuildTime");
            var prop = type?.GetField("Value")?.GetValue(null)
                ?.ToString();
            return prop;
        }
    }

    [RelayCommand]
    private void UserCloseDialog()
    {
        CloseDialog();
    }
}