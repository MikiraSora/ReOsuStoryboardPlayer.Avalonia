using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ReOsuStoryboardPlayer.Avalonia.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.OpenStoryboard;

public partial class LoadingDialogViewModel : DialogViewModelBase, IDisposable
{
    private TaskCompletionSource waitForAttachedViewTokenSource = new();

    [ObservableProperty]
    private string message;

    public override string DialogIdentifier { get; } = nameof(LoadingDialogViewModel);

    public override string Title { get; } = "Loading";

    public void Dispose()
    {
        CloseDialog();
    }

    public Task WaitForAttachedView()
    {
        return waitForAttachedViewTokenSource.Task;
    }

    public override void OnViewBeforeUnload(Control view)
    {
        base.OnViewBeforeUnload(view);
        WeakReferenceMessenger.Default.Unregister<EventBoardcastLoggerProvider.LogEntry>(this);
        waitForAttachedViewTokenSource = new TaskCompletionSource();
    }

    public override void OnViewAfterLoaded(Control view)
    {
        base.OnViewAfterLoaded(view);
        WeakReferenceMessenger.Default.Register<EventBoardcastLoggerProvider.LogEntry>(this, OnLogRecordRecv);
        waitForAttachedViewTokenSource?.TrySetResult();
    }

    private void OnLogRecordRecv(object recipient, EventBoardcastLoggerProvider.LogEntry msg)
    {
        Message = msg.logRecord;
    }
}