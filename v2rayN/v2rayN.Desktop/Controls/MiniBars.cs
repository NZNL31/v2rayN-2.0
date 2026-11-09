using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace v2rayN.Desktop.Controls;

/// <summary>
/// 迷你条形指示器。
/// Segments = 1 时是一条进度条；Segments > 1 时是分段信号条（段越高越多表示越好）。
/// 颜色只取 Fill，轨道用同一颜色的低透明度，所以深浅主题都能自适应。
/// </summary>
public class MiniBars : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MiniBars, double>(nameof(Value));

    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<MiniBars, double>(nameof(Max), 100d);

    /// <summary>反向：值越小越好（用于延迟）</summary>
    public static readonly StyledProperty<bool> InvertProperty =
        AvaloniaProperty.Register<MiniBars, bool>(nameof(Invert));

    public static readonly StyledProperty<int> SegmentsProperty =
        AvaloniaProperty.Register<MiniBars, int>(nameof(Segments), 1);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<MiniBars, IBrush?>(nameof(Fill));

    static MiniBars()
    {
        AffectsRender<MiniBars>(ValueProperty, MaxProperty, InvertProperty, SegmentsProperty, FillProperty);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Max
    {
        get => GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }

    public bool Invert
    {
        get => GetValue(InvertProperty);
        set => SetValue(InvertProperty, value);
    }

    public int Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var brush = Fill;
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (brush is null || w <= 0 || h <= 0)
        {
            return;
        }

        var ratio = Max <= 0 ? 0 : Math.Clamp(Value / Max, 0, 1);
        if (Invert)
        {
            ratio = 1 - ratio;
        }

        var segs = Math.Max(1, Segments);
        var gap = segs > 1 ? 2d : 0d;
        var segW = Math.Max(1d, (w - gap * (segs - 1)) / segs);
        var filled = (int)Math.Round(ratio * segs);

        for (var i = 0; i < segs; i++)
        {
            var segH = segs == 1 ? h : h * (0.45 + 0.55 * i / (segs - 1));
            var rect = new Rect(i * (segW + gap), h - segH, segW, segH);
            var radius = Math.Min(2, segW / 2);

            using (context.PushOpacity(0.16))
            {
                context.DrawRectangle(brush, null, rect, radius, radius);
            }
            if (i < filled)
            {
                context.DrawRectangle(brush, null, rect, radius, radius);
            }
        }
    }
}
