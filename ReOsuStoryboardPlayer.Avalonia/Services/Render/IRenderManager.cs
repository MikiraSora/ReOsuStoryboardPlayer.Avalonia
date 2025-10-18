using System;
using System.Threading.Tasks;
using Avalonia.Media;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Render;

public interface IRenderManager
{
    delegate void RenderAction(ImmediateDrawingContext context);

    bool NeedInvokeProcessMethod { get; }

    event Action OnRequestInvaildVisual;

    void InvokeInRender(RenderAction renderAction);
    void ProcessPendingInvokeRenderAction(ImmediateDrawingContext context);

    Task InvokeInRenderAsync(RenderAction renderAction)
    {
        var taskCompletionSource = new TaskCompletionSource();

        InvokeInRender(ctx =>
        {
            renderAction(ctx);
            taskCompletionSource.SetResult();
        });

        return taskCompletionSource.Task;
    }
}