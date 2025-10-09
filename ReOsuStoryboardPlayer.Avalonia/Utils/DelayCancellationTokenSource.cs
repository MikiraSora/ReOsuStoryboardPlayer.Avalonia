using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

public static class DelayCancellationTokenSource
{
    public static CancellationToken Create(TimeSpan delay, CancellationToken cancellation = default)
    {
        var source = new CancellationTokenSource();
        Task.Delay(delay, cancellation).ContinueWith(_ => source.Cancel(), cancellation).NoWait();
        return CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellation).Token;
    }
}