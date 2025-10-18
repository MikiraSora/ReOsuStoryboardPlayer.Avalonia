using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Models;
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
    public static readonly DirectProperty<StoryboardPlayer, StoryboardInstance> StoryboardInstanceProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, StoryboardInstance>(nameof(StoryboardInstance),
            o => o.storyboardInstance,
            (o, v) =>
            {
                o.storyboardInstance = v;
                o.RebuildStoryboardUpdater();
            });

    public static readonly DirectProperty<StoryboardPlayer, IAudioPlayer> AudioPlayerProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, IAudioPlayer>(nameof(AudioPlayer), o => o.audioPlayer,
            (o, v) => { o.audioPlayer = v; });

    public static readonly DirectProperty<StoryboardPlayer, WideScreenOption> WideScreenProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, WideScreenOption>(nameof(WideScreen), o => o.wideScreenOption,
            (o, v) => { o.wideScreenOption = v; });


    private readonly SKPaint comonPaint;

    //private readonly SKBitmap debugBitmap;
    //private readonly SKImage debugImage;

    private readonly ILogger<StoryboardPlayer> logger;
    private readonly SKPaint sprintPaint;

    private readonly StoryboardDrawOperation storyboardDrawOperation;

    private IAudioPlayer audioPlayer;

    private volatile bool isRendering;

    private StoryboardInstance storyboardInstance;

    private StoryboardUpdater storyboardUpdater;
    private WideScreenOption wideScreenOption = WideScreenOption.Auto;

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
            IsAntialias = false,
            FilterQuality = SKFilterQuality.Low
        };

        InitializeComponent();
    }

    public StoryboardInstance StoryboardInstance
    {
        get => GetValue(StoryboardInstanceProperty);
        set => SetValue(StoryboardInstanceProperty, value);
    }

    public WideScreenOption WideScreen
    {
        get => GetValue(WideScreenProperty);
        set => SetValue(WideScreenProperty, value);
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

        var isWidcreenStoryboard = wideScreenOption == WideScreenOption.Auto
            ? storyboardInstance.Info.IsWidescreenStoryboard
            : wideScreenOption is WideScreenOption.ForceWideScreen;

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

        DrawStoryboardObjectsImmediatly(canvas);

/*
        comonPaint.Color = SKColors.DarkGoldenrod;
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

    private void DrawStoryboardObjectsImmediatly(SKCanvas canvas)
    {
        foreach (var updateStoryboard in storyboardUpdater.UpdatingStoryboardObjects)
            DrawStoryboardObjectImmediatly(canvas, updateStoryboard);
    }

/* FUCK SKIA SHIT API DESIGN
    private void DrawStoryboardObjectsByBatch(SKCanvas canvas)
    {
        List<SKColor> batchColors = new();
        List<SKRect> batchSprites = new();
        List<SKRotationScaleMatrix> batchTramsforms = new();
        SKImage curImage = null;
        var curIsAdditive = false;
        using var print = new SKPaint();

        void flushDraw()
        {
            if (batchSprites.Count == 0)
                return;
            var blendMode = curIsAdditive ? SKBlendMode.Plus : SKBlendMode.SrcOver;

            canvas.DrawAtlas(curImage, batchSprites.ToArray(), batchTramsforms.ToArray(), batchColors.ToArray(),
                blendMode, print);

            batchSprites.Clear();
            batchTramsforms.Clear();
            batchColors.Clear();
        }

        void postDraw(SKImage image, bool isAdditive, SKRect spriteRect, SKRotationScaleMatrix matrix, SKColor color)
        {
            var needFlush = image != curImage || isAdditive != curIsAdditive;
            if (needFlush)
            {
                flushDraw();
                curImage = image;
                curIsAdditive = isAdditive;
            }

            batchSprites.Add(spriteRect);
            batchTramsforms.Add(matrix);
            batchColors.Add(color);
        }

        foreach (var obj in storyboardUpdater.UpdatingStoryboardObjects)
        {
            if (storyboardInstance.Resource.GetSprite(obj) is not ISpriteResource spriteResource)
                continue;
            if (!obj.IsVisible) continue;

            var color = new SKColor(obj.Color.X, obj.Color.Y, obj.Color.Z, obj.Color.W);

            var origin = new SKPoint(-obj.OriginOffset.X * (obj.IsHorizonFlip ? -1 : 1),
                             obj.OriginOffset.Y * (obj.IsVerticalFlip ? -1 : 1)) -
                         new SKPoint(0.5f, 0.5f);

            var scaleX = obj.Scale.X * (obj.IsHorizonFlip ? -1 : 1);
            var scaleY = obj.Scale.Y * (obj.IsVerticalFlip ? -1 : 1);

            var translateX = obj.Postion.X;
            var translateY = obj.Postion.Y;

            var rotate = obj.Rotate;

            skCanvas.Translate(translateX, translateY);

            var rotateScaleMatrix = SKRotationScaleMatrix.Create()
            skCanvas.RotateRadians(rotate);
            skCanvas.Scale(scaleX, scaleY);

            var originOffsetX = tex.Width * origin.X;
            var originOffsetY = tex.Height * origin.Y;
            skCanvas.DrawImage(tex, originOffsetX, originOffsetY, sprintPaint);
        }

        flushDraw();
    }
*/

    private void DrawDebug(SKCanvas skCanvas)
    {
        if (storyboardInstance.Resource.GetSprite(@"s\tex.jpg") is not SpriteResource spriteResource)
            return;
        var obj = new StoryboardObject
        {
            OriginOffset = AnchorConvert.Convert(Anchor.TopCentre),
            IsHorizonFlip = false,
            IsVerticalFlip = false,
            IsAdditive = false,
            Postion = new Vector(320, 0),
            Scale = new Vector(0.625f, -0.625f),
            Color = new ByteVec4(255, 255, 255, 255),
            Rotate = 0f
        };

        DrawStoryboardObjectImmediatly(skCanvas, obj, spriteResource.Image);
    }

    private void DrawStoryboardObjectImmediatly(SKCanvas skCanvas, StoryboardObject obj)
    {
        if (storyboardInstance.Resource.GetSprite(obj.ImageFilePath) is not SpriteResource spriteResource)
            return;
        if (!obj.IsVisible) return;

        DrawStoryboardObjectImmediatly(skCanvas, obj, spriteResource.Image);
    }

    private void DrawStoryboardObjectImmediatly(SKCanvas skCanvas, StoryboardObject obj, SKImage tex)
    {
        skCanvas.Save();

        sprintPaint.BlendMode = obj.IsAdditive ? SKBlendMode.Plus : SKBlendMode.SrcOver;
        sprintPaint.ColorFilter = SKColorFilter.CreateBlendMode(
            new SKColor(obj.Color.X, obj.Color.Y, obj.Color.Z, obj.Color.W),
            SKBlendMode.Modulate);

        var origin = new SKPoint(-obj.OriginOffset.X * (obj.IsHorizonFlip ? -1 : 1) * (obj.Scale.X < 0 ? -1 : 1),
                         obj.OriginOffset.Y * (obj.IsVerticalFlip ? -1 : 1) * (obj.Scale.Y < 0 ? -1 : 1)) -
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