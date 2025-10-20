using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    public static readonly DirectProperty<StoryboardPlayer, StoryboardPlayerSetting> PlayerSettingProperty =
        AvaloniaProperty.RegisterDirect<StoryboardPlayer, StoryboardPlayerSetting>(nameof(PlayerSetting),
            o => o.storyboardPlayerSetting,
            (o, v) =>
            {
                if (o.storyboardPlayerSetting is not null)
                    o.storyboardPlayerSetting.PropertyChanged -= o.StoryboardPlayerSettingOnPropertyChanged;
                o.storyboardPlayerSetting = v;
                if (o.storyboardPlayerSetting is not null)
                    o.storyboardPlayerSetting.PropertyChanged += o.StoryboardPlayerSettingOnPropertyChanged;
            });


    private readonly SKPaint comonPaint;

    //private readonly SKBitmap debugBitmap;
    //private readonly SKImage debugImage;

    private readonly ILogger<StoryboardPlayer> logger;

    private readonly SKPaint sprintPaint;

    private readonly Stopwatch stopwatch = new();

    private readonly StoryboardDrawOperation storyboardDrawOperation;

    private IAudioPlayer audioPlayer;

    private volatile bool isRendering;

    private StoryboardInstance storyboardInstance;
    private StoryboardPlayerSetting storyboardPlayerSetting = new();

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

    public StoryboardPlayerSetting PlayerSetting
    {
        get => GetValue(PlayerSettingProperty);
        set => SetValue(PlayerSettingProperty, value);
    }

    public IAudioPlayer AudioPlayer
    {
        get => GetValue(AudioPlayerProperty);
        set => SetValue(AudioPlayerProperty, value);
    }

    private void StoryboardPlayerSettingOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayerSetting.AntiAliasing):
            case nameof(PlayerSetting.FilterQuality):
                sprintPaint.FilterQuality = PlayerSetting.FilterQuality;
                sprintPaint.IsAntialias = PlayerSetting.AntiAliasing;
                break;
        }
    }

    private void RebuildStoryboardUpdater()
    {
        storyboardUpdater = new StoryboardUpdater(storyboardInstance?.ObjectList ?? []);
    }

    private void UpdateStoryboard()
    {
        var curTime = (float) (audioPlayer?.CurrentTime.TotalMilliseconds ?? 0);
        storyboardUpdater?.Update(curTime);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var scale = this.GetVisualRoot()?.RenderScaling ?? 1;
        storyboardDrawOperation.Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height) * scale;
        context?.Custom(storyboardDrawOperation);

        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    private void DrawStoryboards(ImmediateDrawingContext drawingContext, SKCanvas canvas)
    {
        if (storyboardUpdater is null || storyboardInstance is null)
            return;

#if DEBUG
        stopwatch.Restart();
#endif
        UpdateStoryboard();

#if DEBUG
        var storyboardUpdateCostTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();
#endif

        var settingWideScreenOption = storyboardPlayerSetting?.WideScreenOption ?? WideScreenOption.Auto;
        var isWidcreenStoryboard = settingWideScreenOption == WideScreenOption.Auto
            ? storyboardInstance.Info.IsWidescreenStoryboard
            : settingWideScreenOption is WideScreenOption.ForceWideScreen;

        var storyboardWidth = isWidcreenStoryboard ? 854f : 640f;
        var storyboardHeight = 480f;

        var screenWidth = (float) storyboardDrawOperation.Bounds.Width;
        var screenHeight = (float) storyboardDrawOperation.Bounds.Height;

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

#if DEBUG
        var storyboardRenderCostTime = stopwatch.ElapsedMilliseconds;
        var fps = 1000.0f / (storyboardRenderCostTime + storyboardUpdateCostTime);

        string[] lines =
        [
            $"Bounds: {storyboardDrawOperation.Bounds} Dpi: {this.GetVisualRoot()?.RenderScaling ?? 1:F2}x ClientSize:{this.GetVisualRoot()?.ClientSize}",
            $"FPS/Update/Render: {(float.IsInfinity(fps) ? "--" : fps.ToString("F2"))}/{storyboardUpdateCostTime:F2}ms/{storyboardRenderCostTime:F2}ms",
            $"Rendering Objs: {storyboardUpdater.UpdatingStoryboardObjects.Count}",
            $"Executing Cmds: {storyboardUpdater.UpdatingStoryboardObjects.Sum(x => x.ExecutedCommands.Count)}"
        ];
        DrawTextOverlay(canvas, lines);
#endif
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
        var r =SKColorFilter.CreateBlendMode(
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

#if DEBUG
    private readonly SKPaint textPaint = new()
    {
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                       SKFontStyleSlant.Upright)
                   ?? SKTypeface.FromFamilyName("Courier New")
                   ?? SKTypeface.Default,
        TextSize = 14,
        Color = SKColors.White,
        IsStroke = false
    };

    private readonly SKPaint bgPaint = new() {IsAntialias = true, Color = new SKColor(0, 0, 0, 160), IsStroke = false};

    public void DrawTextOverlay(
        SKCanvas canvas,
        params string[] lines)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));

        // Font metrics 用来计算行高
        var fm = textPaint.FontMetrics; // ascent is negative
        // 行高：ascent(-) + descent(+) + leading
        var lineHeight = MathF.Ceiling(MathF.Abs(fm.Ascent) + MathF.Abs(fm.Descent) + MathF.Abs(fm.Leading));

        // 计算最大宽度
        var maxWidth = 0f;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            var w = textPaint.MeasureText(line);
            if (w > maxWidth) maxWidth = w;
        }

        // 如果所有行都空（例如只是换行），用量度空格以确保最小可见宽度
        if (maxWidth == 0f)
            maxWidth = textPaint.MeasureText(" ");

        // 背景矩形（左上角）
        var x = 0f;
        var y = 0f;
        var padding = 6f;
        var cornerRadius = 6f;
        var bgLeft = x;
        var bgTop = y;
        var bgWidth = maxWidth + padding * 2f;
        var bgHeight = lineHeight * lines.Length + padding * 2f;

        // 绘制背景圆角矩形
        var rect = new SKRect(bgLeft, bgTop, bgLeft + bgWidth, bgTop + bgHeight);
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, bgPaint);

        // 绘制每一行文字（基线计算）
        // 首行基线位置： padding + (|ascent|)  (因为 ascent 是负数，baseline 从 top + padding + |ascent|)
        var baselineStart = bgTop + padding + MathF.Abs(fm.Ascent);
        var curBaseline = baselineStart;

        foreach (var line in lines)
        {
            // 注意：MeasureText 对于空字符串返回 0，我们仍然需要绘制空白占位（可忽略）
            if (!string.IsNullOrEmpty(line))
                canvas.DrawText(line, bgLeft + padding, curBaseline, textPaint);
            curBaseline += lineHeight;
        }
    }
#endif
}