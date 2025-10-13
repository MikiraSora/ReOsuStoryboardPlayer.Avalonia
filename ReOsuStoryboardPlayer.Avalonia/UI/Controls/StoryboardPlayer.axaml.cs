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
using ReOsuStoryboardPlayer.Core.PrimitiveValue;
using ReOsuStoryboardPlayer.Core.Utils;
using SkiaSharp;
using Vector = ReOsuStoryboardPlayer.Core.PrimitiveValue.Vector;

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

    private readonly SKPaint comonPaint;

    //private readonly SKBitmap debugBitmap;
    //private readonly SKImage debugImage;

    private readonly ILogger<StoryboardPlayer> logger;
    private readonly SKPaint sprintPaint;

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

        //debugBitmap = SKBitmap.Decode("C:\\Users\\mikir\\Desktop\\OngekiFumenEditor\\Resources\\editor\\BS.png");
        //debugImage = SKImage.FromBitmap(debugBitmap);

        comonPaint = new SKPaint
        {
            Color = SKColors.DarkGoldenrod,
            StrokeWidth = 2
        };

        sprintPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

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

        var isWidcreenStoryboard = storyboardInstance.Info.IsWidescreenStoryboard;

        var storyboardWidth = isWidcreenStoryboard ? 854f : 640f;
        var storyboardHeight = 480f;

        var screenWidth = (float) Bounds.Width;
        var screenHeight = (float) Bounds.Height;

        //垂直居中且按宽度等比例缩放
        var canvasScale = Math.Min(screenWidth / storyboardWidth, screenHeight / storyboardHeight);
        var widscreenOffsetX = isWidcreenStoryboard ? 107 : 0;
        var canvasOffsetX = (screenWidth / canvasScale - storyboardWidth) / 2;
        var canvasOffsetY = (screenHeight / canvasScale - storyboardHeight) / 2;

        canvas.Save();
        canvas.ResetMatrix();
        canvas.Scale(canvasScale);
        canvas.Translate(canvasOffsetX + widscreenOffsetX, canvasOffsetY);

        var clipRect = SKRect.Create(new SKPoint(-widscreenOffsetX, 0),
            new SKSize(storyboardWidth, storyboardHeight));
        canvas.ClipRect(clipRect);


        //DrawDebug(canvas);
        foreach (var updateStoryboard in storyboardUpdater.UpdatingStoryboardObjects)
            DrawStoryboardObject(canvas, updateStoryboard);

        comonPaint.Color = SKColors.DarkGoldenrod;
/*
        canvas.Save();
        canvas.ResetMatrix();
        canvas.Scale(canvasScale);
        canvas.Translate(canvasOffsetX, canvasOffsetY);
        canvas.DrawLine(new SKPoint(storyboardWidth, 0), new SKPoint(storyboardWidth, storyboardHeight), comonPaint);
        canvas.DrawLine(new SKPoint(0, storyboardHeight), new SKPoint(storyboardWidth, storyboardHeight), comonPaint);
        canvas.DrawLine(new SKPoint(0, 0), new SKPoint(0, storyboardHeight), comonPaint);
        comonPaint.Color = SKColors.Red;
        canvas.DrawLine(new SKPoint(storyboardWidth / 2 + 24, storyboardHeight / 2),
            new SKPoint(storyboardWidth / 2 - 24, storyboardHeight / 2), comonPaint);
        canvas.DrawLine(new SKPoint(storyboardWidth / 2, storyboardHeight / 2 + 24),
            new SKPoint(storyboardWidth / 2, storyboardHeight / 2 - 24), comonPaint);
        canvas.Restore();
*/
        canvas.Restore();

        /*
        comonPaint.Color = SKColors.Black;
        canvas.DrawRect(new SKRect(0, 0, viewOffsetX, screenHeight), comonPaint);
        canvas.DrawRect(new SKRect(screenWidth - viewOffsetX, 0, screenWidth, screenHeight), comonPaint);
        */
    }


    private void DrawDebug(SKCanvas skCanvas)
    {
        if (storyboardInstance.Resource.GetSprite(@"sb\diary\page1.png") is not ISpriteResource spriteResource)
            return;
        var obj = new StoryboardObject
        {
            OriginOffset = AnchorConvert.Convert(Anchor.Centre),
            IsHorizonFlip = true,
            IsVerticalFlip = false,
            IsAdditive = false,
            Postion = new Vector(320, 240),
            Scale = new Vector(0.3f, 0.4f),
            Color = new ByteVec4(255, 255, 255, 255),
            Rotate = 0.17453f
        };

        DrawStoryboardObject(skCanvas, obj, spriteResource.Image);
    }

    private void DrawStoryboardObject(SKCanvas skCanvas, StoryboardObject obj)
    {
        if (storyboardInstance.Resource.GetSprite(obj) is not ISpriteResource spriteResource)
            return;
        if (!obj.IsVisible) return;

        DrawStoryboardObject(skCanvas, obj, spriteResource.Image);
    }

    private void DrawStoryboardObject(SKCanvas skCanvas, StoryboardObject obj, SKImage tex)
    {
        skCanvas.Save();

        sprintPaint.BlendMode = obj.IsAdditive ? SKBlendMode.Plus : SKBlendMode.SrcOver;
        sprintPaint.ColorFilter = SKColorFilter.CreateBlendMode(
            new SKColor(obj.Color.X, obj.Color.Y, obj.Color.Z, obj.Color.W),
            SKBlendMode.Modulate);

        var origin = new SKPoint(-obj.OriginOffset.X * (obj.IsHorizonFlip ? -1 : 1),
                         obj.OriginOffset.Y * (obj.IsVerticalFlip ? -1 : 1)) -
                     new SKPoint(0.5f, 0.5f);

        var scaleX = obj.Scale.X * (obj.IsHorizonFlip ? -1 : 1);
        var scaleY = obj.Scale.Y * (obj.IsVerticalFlip ? -1 : 1);

        var translateX = obj.Postion.X;
        var translateY = obj.Postion.Y;

        var rotate = obj.Rotate;

        skCanvas.Translate(translateX, translateY);
        skCanvas.RotateRadians(rotate);
        skCanvas.Scale(scaleX, scaleY);

        var originOffsetX = tex.Width * origin.X;
        var originOffsetY = tex.Height * origin.Y;
        skCanvas.DrawImage(tex, originOffsetX, originOffsetY, sprintPaint);

        skCanvas.Restore();
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