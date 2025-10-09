using Avalonia;
using AvaloniaDialogs.Views;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Views.Dialogs;

public partial class TemplateDialogView : BaseDialog
{
    public static readonly StyledProperty<DialogViewModelBase> MainPageContentProperty =
        AvaloniaProperty.Register<TemplateDialogView, DialogViewModelBase>(nameof(MainPageContent));

    public TemplateDialogView()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public TemplateDialogView(DialogViewModelBase dialogViewModelBase)
    {
        MainPageContent = dialogViewModelBase;
        dialogViewModelBase?.SetDialogView(this);
        InitializeComponent();
        DataContext = this;
    }

    public DialogViewModelBase MainPageContent
    {
        get => GetValue(MainPageContentProperty);
        set => SetValue(MainPageContentProperty, value);
    }
}