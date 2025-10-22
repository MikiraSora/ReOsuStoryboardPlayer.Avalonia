using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ReOsuStoryboardPlayer.Avalonia.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs.OpenStoryboard;

public partial class LoadingDialogViewModel : DialogViewModelBase, IDisposable
{
    [ObservableProperty]
    private string message;

    private TaskCompletionSource waitForAttachedViewTokenSource = new();

    public override string DialogIdentifier { get; } =
        nameof(LoadingDialogViewModel) + DateTime.UtcNow.Ticks;

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