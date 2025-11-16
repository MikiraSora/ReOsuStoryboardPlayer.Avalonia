using Avalonia.Rendering.Composition;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages.Play;

namespace ReOsuStoryboardPlayer.Avalonia.Views.Pages.Play;

public partial class PlayPageView : PageViewBase
{
    private Compositor compositor;
    public PlayPageView()
    {
        InitializeComponent();

        this.Loaded += PlayPageView_Loaded;
    }

    private void PlayPageView_Loaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        compositor = ElementComposition.GetElementVisual(this).Compositor;
        if (DataContext is PlayPageViewModel pageViewModel)
        {
            compositor.RequestCompositionUpdate(() => { pageViewModel.UpdateCallbackEntryPoint(compositor); });
        }
    }
}