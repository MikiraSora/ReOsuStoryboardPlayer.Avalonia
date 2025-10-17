using System;
using Avalonia.Media;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Render;

public interface IRenderManager
{
    delegate void RenderAction(ImmediateDrawingContext context);

    event Action OnRequestInvaildVisual;

    void InvokeInRender(RenderAction renderAction);
    void ProcessPendingInvokeRenderAction(ImmediateDrawingContext context);
    
    bool NeedInvokeProcessMethod { get; }
}