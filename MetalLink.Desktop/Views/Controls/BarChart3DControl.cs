using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MetalLink.Desktop.Views.Controls;

public sealed class BarChart3DControl : Control
{
    public static readonly StyledProperty<int?> HoveredProductIdProperty =
        AvaloniaProperty.Register<BarChart3DControl, int?>(nameof(HoveredProductId));

    public int? HoveredProductId
    {
        get => GetValue(HoveredProductIdProperty);
        set => SetValue(HoveredProductIdProperty, value);
    }

    private readonly Avalonia.Threading.DispatcherTimer _animTimer;
    private double _hoverProgress; // 0..1
    private double _targetProgress;

    public BarChart3DControl()
    {
        _animTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animTimer.Tick += (_, __) => StepAnimation();

        this.GetObservable(HoveredProductIdProperty).Subscribe(_ =>
        {
            _targetProgress = HoveredProductId is null ? 0.0 : 1.0;
            if (!_animTimer.IsEnabled)
                _animTimer.Start();
        });
    }

    private void StepAnimation()
    {
        // Smoothly approach target
        var speed = 0.10; // slower/smoother
        _hoverProgress += (_targetProgress - _hoverProgress) * speed;

        if (Math.Abs(_targetProgress - _hoverProgress) < 0.001)
        {
            _hoverProgress = _targetProgress;
            _animTimer.Stop();
        }

        InvalidateVisual();
    }

    public sealed record BarItem(int ProductId, string ProductName, double ValueKg, IBrush Brush);

    public static readonly StyledProperty<IReadOnlyList<BarItem>?> ItemsProperty =
        AvaloniaProperty.Register<BarChart3DControl, IReadOnlyList<BarItem>?>(nameof(Items));

    static BarChart3DControl()
    {
        AffectsRender<BarChart3DControl>(ItemsProperty);
        AffectsRender<BarChart3DControl>(HoveredProductIdProperty);
    }

    public IReadOnlyList<BarItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var items = Items;
        if (items is null || items.Count == 0)
            return;

        var bounds = Bounds;
        var padding = 18;
        var chartRect = new Rect(bounds.X + padding, bounds.Y + padding, Math.Max(0, bounds.Width - padding * 2), Math.Max(0, bounds.Height - padding * 2));
        if (chartRect.Width <= 10 || chartRect.Height <= 10)
            return;

        var max = items.Max(i => i.ValueKg);
        if (max <= 0) max = 1;

        // Axes + gridlines (subtle)
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
        context.DrawLine(axisPen, new Point(chartRect.Left, chartRect.Bottom), new Point(chartRect.Right, chartRect.Bottom));
        context.DrawLine(axisPen, new Point(chartRect.Left, chartRect.Top), new Point(chartRect.Left, chartRect.Bottom));

        // horizontal increment lines (5 steps)
        var steps = 5;
        for (var s = 1; s <= steps; s++)
        {
            var y = chartRect.Bottom - (chartRect.Height * s / steps);
            context.DrawLine(gridPen, new Point(chartRect.Left, y), new Point(chartRect.Right, y));
        }

        var gap = 10;
        var barWidth = (chartRect.Width - gap * (items.Count - 1)) / items.Count;
        barWidth = Math.Max(12, barWidth);

        // 3D depth
        var dx = 8;
        var dy = 6;

        for (var idx = 0; idx < items.Count; idx++)
        {
            var item = items[idx];
            var label = item.ProductName;
            var value = item.ValueKg;
            var brush = item.Brush;

            var isHovered = HoveredProductId.HasValue && item.ProductId == HoveredProductId.Value;

            // Smooth hover: hovered bar grows, others shrink slightly when hovering.
            var t = _hoverProgress;
            var hoveredScale = Lerp(1.0, 1.22, t);
            var otherScale = Lerp(1.0, 0.96, t);
            var hoverScale = isHovered ? hoveredScale : otherScale;

            var h = (value / max) * (chartRect.Height - 24) * hoverScale;
            var x = chartRect.Left + idx * (barWidth + gap);
            var y = chartRect.Bottom - h;

            var w = barWidth * hoverScale;
            var front = new Rect(x - ((w - barWidth) / 2), y, w, h);

            // shadow
            var shadow = new Rect(front.X + 3, front.Y + 3, front.Width, front.Height);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), null, shadow);

            // front face
            context.DrawRectangle(brush, new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1), front);

            // right face
            var right = new StreamGeometry();
            using (var g = right.Open())
            {
                g.BeginFigure(new Point(front.Right, front.Bottom), true);
                g.LineTo(new Point(front.Right + dx, front.Bottom - dy));
                g.LineTo(new Point(front.Right + dx, front.Top - dy));
                g.LineTo(new Point(front.Right, front.Top));
                g.EndFigure(true);
            }

            var rightBrush = Darken(brush, 0.18);
            context.DrawGeometry(rightBrush, new Pen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), 1), right);

            // top face
            var top = new StreamGeometry();
            using (var g = top.Open())
            {
                g.BeginFigure(new Point(front.Left, front.Top), true);
                g.LineTo(new Point(front.Right, front.Top));
                g.LineTo(new Point(front.Right + dx, front.Top - dy));
                g.LineTo(new Point(front.Left + dx, front.Top - dy));
                g.EndFigure(true);
            }

            var topBrush = Lighten(brush, 0.14);
            context.DrawGeometry(topBrush, new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), 1), top);

            // value label
            var txt = new FormattedText(
                $"{value:0}" ,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                Brushes.White);
            context.DrawText(txt, new Point(front.Left, Math.Max(chartRect.Top, front.Top - 18)));

            // angled product name under bar
            var name = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                Brushes.White);

            var labelPoint = new Point(front.Left, chartRect.Bottom + 6);
            using (context.PushTransform(Matrix.CreateTranslation(-labelPoint.X, -labelPoint.Y) *
                                         Matrix.CreateRotation(-0.55) *
                                         Matrix.CreateTranslation(labelPoint.X, labelPoint.Y)))
            {
                context.DrawText(name, labelPoint);
            }
        }
    }

    protected override void OnPointerMoved(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var items = Items;
        if (items is null || items.Count == 0)
            return;

        var p = e.GetPosition(this);
        var bounds = Bounds;
        var padding = 18;
        var chartRect = new Rect(bounds.X + padding, bounds.Y + padding, Math.Max(0, bounds.Width - padding * 2), Math.Max(0, bounds.Height - padding * 2));
        var gap = 10;
        var barWidth = (chartRect.Width - gap * (items.Count - 1)) / items.Count;
        barWidth = Math.Max(12, barWidth);

        // Determine index by x-position
        var idx = (int)Math.Floor((p.X - chartRect.Left) / (barWidth + gap));
        if (idx >= 0 && idx < items.Count)
        {
            var item = items[idx];
            HoveredProductId = item.ProductId;
        }
        else
        {
            HoveredProductId = null;
        }
    }

    protected override void OnPointerExited(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerExited(e);
        HoveredProductId = null;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static IBrush Darken(IBrush brush, double amount)
    {
        if (brush is SolidColorBrush sc)
        {
            var c = sc.Color;
            byte d(byte v) => (byte)Math.Max(0, v * (1 - amount));
            return new SolidColorBrush(Color.FromArgb(c.A, d(c.R), d(c.G), d(c.B)));
        }
        return brush;
    }

    private static IBrush Lighten(IBrush brush, double amount)
    {
        if (brush is SolidColorBrush sc)
        {
            var c = sc.Color;
            byte l(byte v) => (byte)Math.Min(255, v + (255 - v) * amount);
            return new SolidColorBrush(Color.FromArgb(c.A, l(c.R), l(c.G), l(c.B)));
        }
        return brush;
    }
}
