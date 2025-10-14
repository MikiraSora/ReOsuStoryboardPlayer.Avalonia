using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Dialogs;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;

public class LoadingDialogViewModel : DialogViewModelBase, IDisposable
{
    private bool isRequestRollToEnd;
    private Control view;
    private TaskCompletionSource waitForAttachedViewTokenSource = new();

    public LoadingDialogViewModel()
    {
        Messages.CollectionChanged += MessagesOnCollectionChanged;
    }

    public ObservableCollection<string> Messages { get; set; } = new();

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
        this.view = default;
        WeakReferenceMessenger.Default.Unregister<EventBoardcastLoggerProvider.LogEntry>(this);
        waitForAttachedViewTokenSource = new TaskCompletionSource();
    }

    public override void OnViewAfterLoaded(Control view)
    {
        base.OnViewAfterLoaded(view);
        this.view = view;
        WeakReferenceMessenger.Default.Register<EventBoardcastLoggerProvider.LogEntry>(this, OnLogRecordRecv);
        waitForAttachedViewTokenSource?.TrySetResult();
    }

    private void OnLogRecordRecv(object recipient, EventBoardcastLoggerProvider.LogEntry message)
    {
        Messages.Add(message.logRecord);
    }

    private void MessagesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (isRequestRollToEnd)
            return;
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var scollViewer = view.FindDescendantOfType<ScrollViewer>();
            if (scollViewer is null)
                return;
            var isAtBottom = Math.Abs(scollViewer.Offset.Y - scollViewer.Extent.Height + scollViewer.Viewport.Height) <
                             10;
            if (isAtBottom)
            {
                isRequestRollToEnd = true;
                Dispatcher.UIThread.Post(() =>
                {
                    scollViewer.ScrollToEnd();
                    isRequestRollToEnd = false;
                });
            }
        }
    }
}