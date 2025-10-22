using System.ComponentModel;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Window;

public interface IWindowManager : INotifyPropertyChanged
{
    bool IsFullScreen { get; set; }

    string MainWindowTitle { get; set; }

    void OpenUrl(string url);
}