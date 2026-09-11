namespace UsageLens.Models;

public sealed record UsageSnapshot(
    QuotaState Quota,
    TokenUsageState TokenUsage,
    bool IsCodexAvailable,
    DateTimeOffset CapturedAt)
{
    public static UsageSnapshot Initial(DateTimeOffset now) => new(
        QuotaState.Loading(),
        TokenUsageState.Loading(),
        IsCodexAvailable: false,
        CapturedAt: now);
}
