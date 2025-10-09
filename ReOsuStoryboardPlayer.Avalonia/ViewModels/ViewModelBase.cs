using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual void OnViewAfterLoaded(Control view)
    {

    }

    public virtual void OnViewBeforeUnload(Control view)
    {

    }
}
