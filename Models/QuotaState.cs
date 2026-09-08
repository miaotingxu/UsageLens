namespace CodexQuotaFloat.Models;

public enum QuotaStatus
{
    Loading,
    Ready,
    Stale,
    Offline
}

public sealed record QuotaState(
    int? FiveHourRemaining,
    int? WeeklyRemaining,
    DateTimeOffset? FiveHourResetAt,
    DateTimeOffset? WeeklyResetAt,
    DateTimeOffset? LastUpdatedAt,
    QuotaStatus Status)
{
    public static QuotaState Loading() => new(
        FiveHourRemaining: null,
        WeeklyRemaining: null,
        FiveHourResetAt: null,
        WeeklyResetAt: null,
        LastUpdatedAt: null,
        Status: QuotaStatus.Loading);

    public static QuotaState Offline() => new(
        FiveHourRemaining: null,
        WeeklyRemaining: null,
        FiveHourResetAt: null,
        WeeklyResetAt: null,
        LastUpdatedAt: null,
        Status: QuotaStatus.Offline);

    public QuotaState MarkStale() => this with { Status = QuotaStatus.Stale };
}
