using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using ReOsuStoryboardPlayer.Avalonia.Services.Render;

namespace ReOsuStoryboardPlayer.Avalonia.Views;

public partial class MainView : UserControl
{
    private readonly RenderActionInvoker renderInvoker;

    public MainView(IRenderManager renderManager)
    {
        renderInvoker = new RenderActionInvoker(renderManager);
        InitializeComponent();
        renderManager.OnRequestInvaildVisual += () => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.Custom(renderInvoker);
    }

    private class RenderActionInvoker : ICustomDrawOperation
    {
        private readonly IRenderManager renderManager;

        public RenderActionInvoker(IRenderManager renderManager)
        {
            this.renderManager = renderManager;
        }

        public void Dispose()
        {
            // TODO 在此释放托管资源
        }

        public bool Equals(ICustomDrawOperation other)
        {
            return this == other;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            renderManager.ProcessPendingInvokeRenderAction(context);
        }

        public Rect Bounds => renderManager.NeedInvokeProcessMethod ? new Rect(0, 0, 1, 1) : new Rect(0, 0, 0, 0);
    }
}