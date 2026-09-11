using System.Windows.Media;

namespace UsageLens.Views;

internal static class DashboardBackgroundOpacity
{
    public static Brush WithOpacity(Brush original, double opacity)
    {
        var targetAlpha = (byte)Math.Round(
            Math.Clamp(opacity, 0.0, 1.0) * byte.MaxValue,
            MidpointRounding.AwayFromZero);
        return original switch
        {
            SolidColorBrush solid => new SolidColorBrush(WithAlpha(solid.Color, targetAlpha)),
            LinearGradientBrush gradient => CreateGradient(gradient, targetAlpha),
            _ => original
        };
    }

    private static LinearGradientBrush CreateGradient(LinearGradientBrush original, byte targetAlpha)
    {
        var copy = original.Clone();
        foreach (var stop in copy.GradientStops)
        {
            stop.Color = WithAlpha(stop.Color, targetAlpha);
        }

        return copy;
    }

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);
}
