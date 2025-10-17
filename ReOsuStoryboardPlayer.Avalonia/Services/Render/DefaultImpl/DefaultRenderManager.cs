using System;
using System.Collections.Concurrent;
using Avalonia.Media;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Render.DefaultImpl;

[RegisterSingleton<IRenderManager>]
public class DefaultRenderManager(ILogger<DefaultRenderManager> logger) : IRenderManager
{
    private readonly ConcurrentQueue<IRenderManager.RenderAction> pendingActions = new();
    private bool requestedInvalidateVisual;

    public event Action OnRequestInvaildVisual;

    public void InvokeInRender(IRenderManager.RenderAction renderAction)
    {
        pendingActions.Enqueue(renderAction);
        logger.LogDebugEx($"add action: {renderAction.GetHashCode()}");

        if (!requestedInvalidateVisual)
        {
            OnRequestInvaildVisual?.Invoke();
            requestedInvalidateVisual = true;
        }
    }

    public void ProcessPendingInvokeRenderAction(ImmediateDrawingContext context)
    {
        logger.LogDebugEx($"Begin process {pendingActions.Count} pending render actions");
        while (pendingActions.TryDequeue(out var action))
        {
            logger.LogDebugEx($"Executing action: {action.GetHashCode()}");
            action(context);
            logger.LogDebugEx("done");
        }

        requestedInvalidateVisual = false;
    }

    public bool NeedInvokeProcessMethod => !pendingActions.IsEmpty;
}