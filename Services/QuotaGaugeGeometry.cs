using System.Windows;
using System.Windows.Media;

namespace CodexQuotaFloat.Services;

public static class QuotaGaugeGeometry
{
    public static Geometry CreateArc(int percent, double radius)
    {
        var clampedPercent = Math.Clamp(percent, 0, 100);
        if (clampedPercent == 0 || radius <= 0)
        {
            return Geometry.Empty;
        }

        var center = new Point(radius, radius);
        var start = new Point(radius, 0);
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(start, isFilled: false, isClosed: false);

            if (clampedPercent == 100)
            {
                context.ArcTo(new Point(radius, radius * 2), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, true);
                context.ArcTo(start, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, true);
            }
            else
            {
                var angle = clampedPercent * 3.6 - 90;
                var radians = angle * Math.PI / 180;
                var end = new Point(
                    center.X + radius * Math.Cos(radians),
                    center.Y + radius * Math.Sin(radians));
                context.ArcTo(end, new Size(radius, radius), 0, clampedPercent > 50, SweepDirection.Clockwise, true, true);
            }
        }

        geometry.Freeze();
        return geometry;
    }
}
