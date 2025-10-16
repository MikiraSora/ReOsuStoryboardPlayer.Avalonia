using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.CommonMessage;
using ReOsuStoryboardPlayer.Avalonia.Views.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Dialog.DefaultImpl;

[Injectio.Attributes.RegisterSingleton<IDialogManager>]
public class DefaultDialogManager : IDialogManager
{
    private readonly Stack<global::Avalonia.Controls.Window> dialogWindowStack = new();
    private readonly ILogger<DefaultDialogManager> logger;
    private readonly ViewModelFactory viewModelFactory;

    public DefaultDialogManager(ViewModelFactory viewModelFactory, ILogger<DefaultDialogManager> logger)
    {
        this.viewModelFactory = viewModelFactory;
        this.logger = logger;
    }

    public Task<T> ShowDialog<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]T>() where T : DialogViewModelBase
    {
        var viewModel = viewModelFactory.CreateViewModel<T>();
        return ShowDialogInternal(viewModel);
    }

    public async Task ShowDialog(DialogViewModelBase dialogViewModel)
    {
        await ShowDialogInternal(dialogViewModel);
    }

    public async Task ShowMessageDialog(string content, DialogMessageType messageType = DialogMessageType.Info)
    {
        var vm = new CommonMessageDialogViewModel(messageType, content);
        await ShowDialogInternal(vm);
    }

    public async Task<bool> ShowComfirmDialog(string content, string yesButtonContent = "确认",
        string noButtonContent = "取消")
    {
        var vm = new CommonComfirmDialogViewModel(content, yesButtonContent, noButtonContent);
        await ShowDialogInternal(vm);
        return vm.ComfirmResult;
    }

    private Task<T> ShowDialogInternal<T>(T viewModel) where T : DialogViewModelBase
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            logger.LogInformationEx($"dialog {viewModel.DialogIdentifier} started.");
            var view = new TemplateDialogView(viewModel);
            await view.ShowAsync();
            logger.LogInformationEx($"dialog {viewModel.DialogIdentifier} finished");
            return viewModel;
        });
    }
}