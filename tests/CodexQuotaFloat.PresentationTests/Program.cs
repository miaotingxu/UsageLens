using CodexQuotaFloat.Services;

var now = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero);

AssertEqual(QuotaColorBand.Green, QuotaPresentation.GetColorBand(81));
AssertEqual(QuotaColorBand.Green, QuotaPresentation.GetColorBand(100));
AssertEqual(QuotaColorBand.Yellow, QuotaPresentation.GetColorBand(80));
AssertEqual(QuotaColorBand.Yellow, QuotaPresentation.GetColorBand(50));
AssertEqual(QuotaColorBand.Amber, QuotaPresentation.GetColorBand(49));
AssertEqual(QuotaColorBand.Amber, QuotaPresentation.GetColorBand(20));
AssertEqual(QuotaColorBand.Red, QuotaPresentation.GetColorBand(19));
AssertEqual(QuotaColorBand.Red, QuotaPresentation.GetColorBand(0));
AssertEqual(QuotaColorBand.Unknown, QuotaPresentation.GetColorBand(null));

AssertEqual($"4d 12h · {now.ToLocalTime().AddDays(4).AddHours(12):MM/dd HH:mm}", QuotaPresentation.FormatResetCountdown(now.AddDays(4).AddHours(12), now));
AssertEqual($"2h 18m · {now.ToLocalTime().AddHours(2).AddMinutes(18):MM/dd HH:mm}", QuotaPresentation.FormatResetCountdown(now.AddHours(2).AddMinutes(18), now));
AssertEqual($"42m · {now.ToLocalTime().AddMinutes(42):MM/dd HH:mm}", QuotaPresentation.FormatResetCountdown(now.AddMinutes(42), now));
AssertEqual($"<1m · {now.ToLocalTime().AddSeconds(59):MM/dd HH:mm}", QuotaPresentation.FormatResetCountdown(now.AddSeconds(59), now));
AssertEqual($"<1m · {now.ToLocalTime().AddSeconds(-1):MM/dd HH:mm}", QuotaPresentation.FormatResetCountdown(now.AddSeconds(-1), now));
AssertEqual("--", QuotaPresentation.FormatResetCountdown(null, now));

AssertNear(824, HorizontalWindowPlacement.ResolveInitialLeft(null, 0, 2000, 352));
AssertNear(0, HorizontalWindowPlacement.ClampLeft(-50, 0, 2000, 352));
AssertNear(1648, HorizontalWindowPlacement.ClampLeft(1900, 0, 2000, 352));
AssertNear(640, HorizontalWindowPlacement.ResolveInitialLeft(640, 0, 2000, 352));
AssertNear(824, HorizontalWindowPlacement.ResolveInitialLeft(double.NaN, 0, 2000, 352));
AssertNear(-1920, HorizontalWindowPlacement.ClampLeft(-2100, -1920, 1920, 352));

Console.WriteLine("QuotaPresentation tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertNear(double expected, double actual)
{
    if (Math.Abs(expected - actual) > 0.001)
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}
