using System;
using System.Threading;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Plaform;

[RegisterSingleton<IPlatform>]
public class BrowserPlatform(ILogger<BrowserPlatform> logger) : IPlatform
{
    private bool? supportMultiThread;
    public bool SupportMultiThread => supportMultiThread ??= CheckIfSupportMultiThread();

    private bool CheckIfSupportMultiThread()
    {
        try
        {
            var success = false;
            var ctid = Thread.CurrentThread.ManagedThreadId;
            var t = new Thread(() =>
            {
                var rtid = Thread.CurrentThread.ManagedThreadId;
                success = ctid != rtid;
            });
#pragma warning disable CA1416
            t.Start();
#pragma warning restore CA1416
            t.Join(100);
            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarningEx($"throw exception: {ex.Message}");
            return false;
        }
    }
}