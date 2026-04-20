using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MetalLink.Desktop.Views.Controls;

public sealed class PieChartControl : Control
{
    static PieChartControl()
    {
        AffectsRender<PieChartControl>(SlicesProperty);
    }

    public static readonly StyledProperty<IReadOnlyList<(double fraction, IBrush brush)>?> SlicesProperty =
        AvaloniaProperty.Register<PieChartControl, IReadOnlyList<(double fraction, IBrush brush)>?>(nameof(Slices));

    public IReadOnlyList<(double fraction, IBrush brush)>? Slices
    {
        get => GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var slices = Slices;
        if (slices is null || slices.Count == 0)
            return;

        var bounds = Bounds;
        var size = Math.Min(bounds.Width, bounds.Height);
        if (size <= 0)
            return;

        // 3D-ish effect: draw a shadow ellipse and a slightly squashed pie.
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var radius = (size / 2) - 6;
        var yScale = 0.82; // squish for pseudo 3D

        // Shadow
        var shadowRect = new Rect(center.X - radius, center.Y - (radius * yScale) + 10, radius * 2, (radius * 2 * yScale));
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), null, shadowRect.Center, shadowRect.Width / 2, shadowRect.Height / 2);

        double startAngle = -90;
        foreach (var (fraction, brush) in slices)
        {
            var sweep = fraction * 360.0;
            if (sweep <= 0.1)
                continue;

            var geom = CreatePieSliceGeometry(center, radius, yScale, startAngle, startAngle + sweep);

            // Slight stroke for separation
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)), 1);
            context.DrawGeometry(brush, pen, geom);

            startAngle += sweep;
        }

        // Top gloss
        var gloss = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(70, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.6)
            }
        };
        context.DrawEllipse(gloss, null, center, radius, radius * yScale);
    }

    private static Geometry CreatePieSliceGeometry(Point center, double radius, double yScale, double startDeg, double endDeg)
    {
        var startRad = DegreesToRadians(startDeg);
        var endRad = DegreesToRadians(endDeg);

        var p1 = new Point(center.X + radius * Math.Cos(startRad), center.Y + (radius * yScale) * Math.Sin(startRad));
        var p2 = new Point(center.X + radius * Math.Cos(endRad), center.Y + (radius * yScale) * Math.Sin(endRad));

        var isLarge = (endDeg - startDeg) > 180;

        var segments = new PathSegments();
        segments.Add(new LineSegment { Point = p1 });
        segments.Add(new ArcSegment
        {
            Point = p2,
            Size = new Size(radius, radius * yScale),
            IsLargeArc = isLarge,
            SweepDirection = SweepDirection.Clockwise
        });

        var fig = new PathFigure { StartPoint = center, IsClosed = true, Segments = segments };

        return new PathGeometry { Figures = new PathFigures { fig } };
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
}
