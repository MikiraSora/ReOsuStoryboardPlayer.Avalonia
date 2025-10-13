using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Audio;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Kernel;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.UI.Controls;

public partial class StoryboardPlayer : UserControl
{
    public static readonly DirectProperty<StoryboardPlayer, IStoryboardInstance> StoryboardInstanceProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, IStoryboardInstance>("StoryboardInstance",
            o => o.storyboardInstance,
            (o, v) =>
            {
                o.storyboardInstance = v;
                o.RebuildStoryboardUpdater();
            });

    public static readonly DirectProperty<StoryboardPlayer, IAudioPlayer> AudioPlayerProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, IAudioPlayer>("AudioPlayer", o => o.audioPlayer,
            (o, v) => { o.audioPlayer = v; });

    private readonly ILogger<StoryboardPlayer> logger;

    private readonly StoryboardDrawOperation storyboardDrawOperation;

    private IAudioPlayer audioPlayer;

    private volatile bool isRendering;

    private IStoryboardInstance storyboardInstance;

    private StoryboardUpdater storyboardUpdater;

    public StoryboardPlayer()
    {
        logger = (App.Current as App).RootServiceProvider.GetService<ILogger<StoryboardPlayer>>();
        storyboardDrawOperation = new StoryboardDrawOperation();
        storyboardDrawOperation.OnRender += StoryboardDrawOperationOnOnRender;

        InitializeComponent();
    }

    public IStoryboardInstance StoryboardInstance
    {
        get => GetValue(StoryboardInstanceProperty);
        set => SetValue(StoryboardInstanceProperty, value);
    }

    public IAudioPlayer AudioPlayer
    {
        get => GetValue(AudioPlayerProperty);
        set => SetValue(AudioPlayerProperty, value);
    }

    private void RebuildStoryboardUpdater()
    {
        storyboardUpdater = new StoryboardUpdater(storyboardInstance?.ObjectList ?? []);
    }

    private void UpdateStoryboard()
    {
        var curTime = (float) (audioPlayer?.CurrentTime.TotalMilliseconds ?? 0);
        //logger.LogDebugEx($"curTime: {curTime}");
        storyboardUpdater?.Update(curTime);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        storyboardDrawOperation.Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context?.Custom(storyboardDrawOperation);

        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    private void DrawStoryboards(ImmediateDrawingContext drawingContext, SKCanvas canvas)
    {
        if (storyboardUpdater is null || storyboardInstance is null)
            return;

        UpdateStoryboard();

        var storyboardWidth = /*storyboardInstance.Info.IsWidescreenStoryboard ? 854f :*/ 640f;
        var storyboardHeight = 480f;
        var screenWidth = (float) Bounds.Width;
        var screenHeight = (float) Bounds.Height;
        //垂直居中且按宽度等比例缩放
        var canvasScale = Math.Min(screenWidth / storyboardWidth, screenHeight / storyboardHeight);
        var canvasOffsetX = (screenWidth / canvasScale - storyboardWidth) / 2;
        var canvasOffsetY = (screenHeight / canvasScale - storyboardHeight) / 2;
        
        canvas.Save();
        canvas.ResetMatrix();
        canvas.Scale(canvasScale);
        canvas.Translate(canvasOffsetX, canvasOffsetY);

        foreach (var updateStoryboard in storyboardUpdater.UpdatingStoryboardObjects)
            DrawStoryboardObject(canvas, updateStoryboard);

        using var paint = new SKPaint
        {
            Color = SKColors.DarkGoldenrod,
            StrokeWidth = 2
        };
        canvas.DrawLine(new SKPoint(storyboardWidth, 0), new SKPoint(storyboardWidth, storyboardHeight), paint);
        canvas.DrawLine(new SKPoint(0, storyboardHeight), new SKPoint(storyboardWidth, storyboardHeight), paint);
        canvas.DrawLine(new SKPoint(0, 0), new SKPoint(0, storyboardHeight), paint);
        paint.Color = SKColors.Red;
        canvas.DrawLine(new SKPoint(storyboardWidth / 2 + 10, storyboardHeight / 2),
            new SKPoint(storyboardWidth / 2 - 10, storyboardHeight / 2), paint);
        canvas.DrawLine(new SKPoint(storyboardWidth / 2, storyboardHeight / 2 + 10),
            new SKPoint(storyboardWidth / 2, storyboardHeight / 2 - 10), paint);
        
        canvas.Restore();
        
        using var blackPaint = new SKPaint
        {
            Color = SKColors.Black
        };
        canvas.DrawRect(new SKRect(0, 0, canvasOffsetX, screenHeight), blackPaint);
        canvas.DrawRect(new SKRect(screenWidth - canvasOffsetX, 0, screenWidth, screenHeight), blackPaint);
    }

    private void StoryboardDrawOperationOnOnRender(ImmediateDrawingContext drawingContext)
    {
        if (isRendering)
            return;
        isRendering = true;

        var leaseFeature = drawingContext.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
        if (leaseFeature != null)
        {
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            canvas.Clear(SKColors.Black);

            DrawStoryboards(drawingContext, canvas);
        }

        isRendering = false;
    }

    private void DrawStoryboardObject(SKCanvas skCanvas, StoryboardObject obj)
    {
        if (storyboardInstance.Resource.GetSprite(obj) is not ISpriteResource spriteResource)
            return;
        if (!obj.IsVisible) return;
        skCanvas.Save();

        var origin = new SKPoint(-obj.OriginOffset.X, obj.OriginOffset.Y) - new SKPoint(0.5f, 0.5f);
        var bmp = spriteResource.Image;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            BlendMode = obj.IsAdditive ? SKBlendMode.Plus : SKBlendMode.SrcOver,
            ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(obj.Color.X, obj.Color.Y, obj.Color.Z, obj.Color.W),
                SKBlendMode.Modulate)
        };

        skCanvas.Translate(obj.Postion.X, obj.Postion.Y);
        skCanvas.Translate(spriteResource.Image.Width * origin.X * Math.Abs(obj.Scale.X),
            spriteResource.Image.Height * origin.Y * Math.Abs(obj.Scale.Y));
        if (obj.Rotate != 0)
            skCanvas.RotateDegrees(obj.Rotate);
        skCanvas.Scale(obj.Scale.X * (obj.IsHorizonFlip ? -1 : 1), obj.Scale.Y * (obj.IsVerticalFlip ? -1 : 1));

        var destRect = new SKRect(0, 0, bmp.Width, bmp.Height);
        skCanvas.DrawBitmap(bmp, destRect, paint);

        skCanvas.Restore();
    }

    private class StoryboardDrawOperation : ICustomDrawOperation
    {
        public void Dispose()
        {
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
            OnRender?.Invoke(context);
        }

        public Rect Bounds { get; set; }
        public event Action<ImmediateDrawingContext> OnRender;
    }
}