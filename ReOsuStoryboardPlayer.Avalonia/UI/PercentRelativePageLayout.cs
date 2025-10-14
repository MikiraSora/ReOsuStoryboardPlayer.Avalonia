using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ReOsuStoryboardPlayer.Avalonia.Views.Pages;

namespace ReOsuStoryboardPlayer.Avalonia.UI;

public class PercentRelativePageLayout
{
    public static readonly AttachedProperty<double> WidthPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("WidthPercentRelativePage", double.NaN);

    public static readonly AttachedProperty<double> HeightPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("HeightPercentRelativePage", double.NaN);

    static PercentRelativePageLayout()
    {
        WidthPercentRelativePageProperty.Changed.Subscribe(OnWidthPercentChanged);
        HeightPercentRelativePageProperty.Changed.Subscribe(OnHeightPercentChanged);
    }

    public static double GetWidthPercent(Control control)
    {
        return control.GetValue(WidthPercentRelativePageProperty);
    }

    public static void SetWidthPercent(Control control, double value)
    {
        control.SetValue(WidthPercentRelativePageProperty, value);
    }

    public static double GetHeightPercent(Control control)
    {
        return control.GetValue(HeightPercentRelativePageProperty);
    }

    public static void SetHeightPercent(Control control, double value)
    {
        control.SetValue(HeightPercentRelativePageProperty, value);
    }

    private static void OnWidthPercentChanged(AvaloniaPropertyChangedEventArgs<double> e)
    {
        if (e.Sender is Control control)
        {
            control.AttachedToVisualTree -= ControlOnAttachedToVisualTree;
            control.AttachedToVisualTree += ControlOnAttachedToVisualTree;
        }
    }

    private static void OnHeightPercentChanged(AvaloniaPropertyChangedEventArgs<double> e)
    {
        if (e.Sender is Control control)
        {
            control.AttachedToVisualTree -= ControlOnAttachedToVisualTree;
            control.AttachedToVisualTree += ControlOnAttachedToVisualTree;
        }
    }

    private static void ControlOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
            return;

        // 获取父级容器
        if (FindAncestor<PageViewBase>(control) is PageViewBase parent)
            parent.GetObservable(Control.BoundsProperty).Subscribe(bounds =>
            {
                var widthPercent = GetWidthPercent(control);
                var heightPercent = GetHeightPercent(control);

                if (!double.IsNaN(widthPercent))
                    control.Width = bounds.Width * widthPercent;

                if (!double.IsNaN(heightPercent))
                    control.Height = bounds.Height * heightPercent;
            });
    }

    private static T? FindAncestor<T>(Control control) where T : Control
    {
        Visual? parent = control;
        while (parent != null)
        {
            parent = parent.GetVisualParent();
            if (parent is T match)
                return match;
        }

        return null;
    }
}