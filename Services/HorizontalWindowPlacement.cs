namespace CodexQuotaFloat.Services;

public static class HorizontalWindowPlacement
{
    public static double ResolveInitialLeft(
        double? savedLeft,
        double workAreaLeft,
        double workAreaWidth,
        double windowWidth)
    {
        var requestedLeft = savedLeft is double value && double.IsFinite(value)
            ? value
            : workAreaLeft + (workAreaWidth - windowWidth) / 2;

        return ClampLeft(requestedLeft, workAreaLeft, workAreaWidth, windowWidth);
    }

    public static double ClampLeft(
        double requestedLeft,
        double workAreaLeft,
        double workAreaWidth,
        double windowWidth)
    {
        var maximumLeft = Math.Max(workAreaLeft, workAreaLeft + workAreaWidth - windowWidth);
        return Math.Clamp(requestedLeft, workAreaLeft, maximumLeft);
    }
}
