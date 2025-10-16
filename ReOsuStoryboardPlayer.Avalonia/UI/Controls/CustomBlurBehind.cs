using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.UI.Controls;

public class CustomBlurBehind : Control
{
    public static readonly StyledProperty<ExperimentalAcrylicMaterial> MaterialProperty =
        AvaloniaProperty.Register<CustomBlurBehind, ExperimentalAcrylicMaterial>(
            "Material");

    private static readonly ImmutableExperimentalAcrylicMaterial DefaultAcrylicMaterial =
        (ImmutableExperimentalAcrylicMaterial) new ExperimentalAcrylicMaterial
        {
            MaterialOpacity = 0.5,
            TintColor = Colors.Azure,
            TintOpacity = 0.5,
            PlatformTransparencyCompensationLevel = 0
        }.ToImmutable();

    private static SKShader s_acrylicNoiseShader;

    static CustomBlurBehind()
    {
        AffectsRender<CustomBlurBehind>(MaterialProperty);
    }

    public ExperimentalAcrylicMaterial Material
    {
        get => GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var mat = Material != null
            ? (ImmutableExperimentalAcrylicMaterial) Material.ToImmutable()
            : DefaultAcrylicMaterial;
        context.Custom(new BlurBehindRenderOperation(mat, new Rect(default, Bounds.Size)));
    }

    private class BlurBehindRenderOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly ImmutableExperimentalAcrylicMaterial _material;

        public BlurBehindRenderOperation(ImmutableExperimentalAcrylicMaterial material, Rect bounds)
        {
            _material = material;
            _bounds = bounds;
        }

        public void Dispose()
        {
        }

        public bool HitTest(Point p)
        {
            return _bounds.Contains(p);
        }

        public void Render(ImmediateDrawingContext context)
        {
            var laser = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            using var skia = laser?.Lease();
            if (skia is null)
                return;

            if (!skia.SkCanvas.TotalMatrix.TryInvert(out var currentInvertedTransform))
                return;

            if (skia.SkSurface is null)
                return;

            using var backgroundSnapshot = skia.SkSurface.Snapshot();
            using var backdropShader = SKShader.CreateImage(backgroundSnapshot, SKShaderTileMode.Clamp,
                SKShaderTileMode.Clamp, currentInvertedTransform);

            if (skia.GrContext == null)
            {
                using (var filter = SKImageFilter.CreateBlur(3, 3, SKShaderTileMode.Clamp))
                using (var tmp = new SKPaint())
                {
                    tmp.Shader = backdropShader;
                    tmp.ImageFilter = filter;
                    skia.SkCanvas.DrawRect(0, 0, (float) _bounds.Width, (float) _bounds.Height, tmp);
                }

                return;
            }

            using var blurred = SKSurface.Create(skia.GrContext, false, new SKImageInfo(
                (int) Math.Ceiling(_bounds.Width),
                (int) Math.Ceiling(_bounds.Height), SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            using (var filter = SKImageFilter.CreateBlur(3, 3, SKShaderTileMode.Clamp))
            using (var blurPaint = new SKPaint())
            {
                blurPaint.Shader = backdropShader;
                blurPaint.ImageFilter = filter;
                blurred.Canvas.DrawRect(0, 0, (float) _bounds.Width, (float) _bounds.Height, blurPaint);
            }

            using (var blurSnap = blurred.Snapshot())
            using (var blurSnapShader = SKShader.CreateImage(blurSnap))
            using (var blurSnapPaint = new SKPaint())
            {
                blurSnapPaint.Shader = blurSnapShader;
                blurSnapPaint.IsAntialias = true;
                skia.SkCanvas.DrawRect(0, 0, (float) _bounds.Width, (float) _bounds.Height, blurSnapPaint);
            }

            //return;
            using var acrylliPaint = new SKPaint();
            acrylliPaint.IsAntialias = true;

            const double noiseOpacity = 0.0225;

            var tintColor = _material.TintColor;
            var tint = new SKColor(tintColor.R, tintColor.G, tintColor.B, tintColor.A);

            if (s_acrylicNoiseShader == null)
                using (var stream =
                       typeof(SkiaPlatform).Assembly.GetManifestResourceStream(
                           "Avalonia.Skia.Assets.NoiseAsset_256X256_PNG.png"))
                using (var bitmap = SKBitmap.Decode(stream))
                {
                    s_acrylicNoiseShader = SKShader
                        .CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
                        .WithColorFilter(CreateAlphaColorFilter(noiseOpacity));
                }

            using (var backdrop = SKShader.CreateColor(new SKColor(_material.MaterialColor.R, _material.MaterialColor.G,
                       _material.MaterialColor.B, _material.MaterialColor.A)))
            using (var tintShader = SKShader.CreateColor(tint))
            using (var effectiveTint = SKShader.CreateCompose(backdrop, tintShader))
            using (var compose = SKShader.CreateCompose(effectiveTint, s_acrylicNoiseShader))
            {
                acrylliPaint.Shader = compose;
                acrylliPaint.IsAntialias = true;
                skia.SkCanvas.DrawRect(0, 0, (float) _bounds.Width, (float) _bounds.Height, acrylliPaint);
            }
        }

        public Rect Bounds => _bounds.Inflate(4);

        public bool Equals(ICustomDrawOperation other)
        {
            return other is BlurBehindRenderOperation op && op._bounds == _bounds && op._material.Equals(_material);
        }


        private static SKColorFilter CreateAlphaColorFilter(double opacity)
        {
            if (opacity > 1)
                opacity = 1;
            var c = new byte[256];
            var a = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                c[i] = (byte) i;
                a[i] = (byte) (i * opacity);
            }

            return SKColorFilter.CreateTable(a, c, c, c);
        }
    }
}