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

    public static readonly AttachedProperty<double> MinWidthPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("MinWidthPercentRelativePage", double.NaN);

    public static readonly AttachedProperty<double> MinHeightPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("MinHeightPercentRelativePage", double.NaN);

    public static readonly AttachedProperty<double> MaxWidthPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("MaxWidthPercentRelativePage", double.NaN);

    public static readonly AttachedProperty<double> MaxHeightPercentRelativePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, double>("MaxHeightPercentRelativePage", double.NaN);

    static PercentRelativePageLayout()
    {
        WidthPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);
        HeightPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);

        MinWidthPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);
        MinHeightPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);

        MaxWidthPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);
        MaxHeightPercentRelativePageProperty.Changed.Subscribe(OnPropertyChanged);
    }

    public static double GetWidthPercent(Control control)
    {
        return control.GetValue(WidthPercentRelativePageProperty);
    }

    public static void SetWidthPercent(Control control, double value)
    {
        control.SetValue(WidthPercentRelativePageProperty, value);
    }

    public static void SetMinWidthPercent(Control control, double value)
    {
        control.SetValue(MinWidthPercentRelativePageProperty, value);
    }

    public static double GetMinWidthPercent(Control control)
    {
        return control.GetValue(MinWidthPercentRelativePageProperty);
    }

    public static void SetMaxWidthPercent(Control control, double value)
    {
        control.SetValue(MinWidthPercentRelativePageProperty, value);
    }

    public static double GetMaxWidthPercent(Control control)
    {
        return control.GetValue(MinWidthPercentRelativePageProperty);
    }

    public static double GetHeightPercent(Control control)
    {
        return control.GetValue(HeightPercentRelativePageProperty);
    }

    public static void SetHeightPercent(Control control, double value)
    {
        control.SetValue(HeightPercentRelativePageProperty, value);
    }

    public static double GetMinHeightPercent(Control control)
    {
        return control.GetValue(MinHeightPercentRelativePageProperty);
    }

    public static void SetMinHeightPercent(Control control, double value)
    {
        control.SetValue(MinHeightPercentRelativePageProperty, value);
    }

    public static double GetMaxHeightPercent(Control control)
    {
        return control.GetValue(MaxHeightPercentRelativePageProperty);
    }

    public static void SetMaxHeightPercent(Control control, double value)
    {
        control.SetValue(MaxHeightPercentRelativePageProperty, value);
    }

    private static void OnPropertyChanged(AvaloniaPropertyChangedEventArgs<double> e)
    {
        if (e.Sender is Control control)
        {
            control.AttachedToVisualTree -= ControlOnAttachedToVisualTree;
            control.AttachedToVisualTree += ControlOnAttachedToVisualTree;
        }
    }

    private static void ControlOnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
            return;

        var parent =
            FindAncestor<PageViewBase>(control) ??
            FindChildrenInVisualTreeRecursivly<PageViewBase>(TopLevel.GetTopLevel(control));

        // 获取父级容器
        if (parent != null)
            parent.GetObservable(Visual.BoundsProperty).Subscribe(bounds =>
            {
                var widthPercent = GetWidthPercent(control);
                var heightPercent = GetHeightPercent(control);
                var minWidthPercent = GetMinWidthPercent(control);
                var minHeightPercent = GetMinHeightPercent(control);
                var maxWidthPercent = GetMaxWidthPercent(control);
                var maxHeightPercent = GetMaxHeightPercent(control);

                if (!double.IsNaN(widthPercent))
                    control.Width = bounds.Width * widthPercent;

                if (!double.IsNaN(heightPercent))
                    control.Height = bounds.Height * heightPercent;

                if (!double.IsNaN(minWidthPercent))
                    control.MinWidth = bounds.Width * minWidthPercent;

                if (!double.IsNaN(minHeightPercent))
                    control.MinHeight = bounds.Height * minHeightPercent;

                if (!double.IsNaN(maxWidthPercent))
                    control.MaxWidth = bounds.Width * maxWidthPercent;

                if (!double.IsNaN(maxHeightPercent))
                    control.MaxHeight = bounds.Height * maxHeightPercent;
            });
    }

    private static T FindChildrenInVisualTreeRecursivly<T>(Visual control) where T : Visual
    {
        if (control == null)
            return null;
        if (control is T t)
            return t;
        foreach (var child in control.GetVisualChildren())
            if (FindChildrenInVisualTreeRecursivly<T>(child) is { } result)
                return result;
        return default;
    }

    private static T FindAncestor<T>(Control control) where T : Control
    {
        Visual parent = control;
        while (parent != null)
        {
            parent = parent.GetVisualParent();
            if (parent is T match)
                return match;
        }

        return null;
    }
}