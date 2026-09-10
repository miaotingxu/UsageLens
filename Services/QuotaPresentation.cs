namespace UsageLens.Services;

public enum QuotaColorBand
{
    Unknown,
    Green,
    Yellow,
    Amber,
    Red
}

public static class QuotaPresentation
{
    public static QuotaColorBand GetColorBand(int? remainingPercent) => remainingPercent switch
    {
        >= 81 and <= 100 => QuotaColorBand.Green,
        >= 50 and <= 80 => QuotaColorBand.Yellow,
        >= 20 and <= 49 => QuotaColorBand.Amber,
        >= 0 and <= 19 => QuotaColorBand.Red,
        _ => QuotaColorBand.Unknown
    };

    public static string FormatResetCountdown(DateTimeOffset? resetAt, DateTimeOffset now)
    {
        if (resetAt is null)
        {
            return "--";
        }

        var resetTime = resetAt.Value.ToLocalTime().ToString("MM/dd HH:mm");
        var remaining = resetAt.Value - now;
        if (remaining < TimeSpan.FromMinutes(1))
        {
            return $"<1m · {resetTime}";
        }

        if (remaining >= TimeSpan.FromDays(1))
        {
            return $"{remaining.Days}d {remaining.Hours}h · {resetTime}";
        }

        if (remaining >= TimeSpan.FromHours(1))
        {
            return $"{remaining.Hours}h {remaining.Minutes}m · {resetTime}";
        }

        return $"{remaining.Minutes}m · {resetTime}";
    }
}
