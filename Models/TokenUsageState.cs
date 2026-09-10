namespace UsageLens.Models;

public enum TokenUsageStatus
{
    Loading,
    Ready,
    Stale,
    Offline
}

public readonly record struct TokenUsagePeriod(
    long InputTokens,
    long OutputTokens,
    decimal EstimatedPriceUsd = 0m,
    long UnpricedTokens = 0);

public sealed record TokenUsageState(
    TokenUsagePeriod Today,
    TokenUsagePeriod Last7Days,
    TokenUsagePeriod Last30Days,
    DateTimeOffset? LastUpdatedAt,
    TokenUsageStatus Status)
{
    public static TokenUsageState Loading() => new(default, default, default, null, TokenUsageStatus.Loading);

    public static TokenUsageState Offline() => new(default, default, default, null, TokenUsageStatus.Offline);

    public TokenUsageState MarkStale() => this with { Status = TokenUsageStatus.Stale };
}
