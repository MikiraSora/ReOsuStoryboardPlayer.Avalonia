using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Attributes;
using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;
using ReOsuStoryboardPlayer.Avalonia.Views;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.Views;

[CollectTypeForActivator(typeof(ViewBase))]
public partial class ViewTypeCollectedActivator : ITypeCollectedActivator<ViewBase>
{
    
}
