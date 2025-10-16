using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Attributes;
using ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces;

namespace ReOsuStoryboardPlayer.Avalonia.Views;

[CollectTypeForActivator(typeof(ViewBase))]
public partial class ViewTypeCollectedActivator : ITypeCollectedActivator<ViewBase>
{
    
}
