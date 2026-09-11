using UsageLens.Models;
using UsageLens.Services;

var quotaReader = new FakeQuotaReader();
var tokenReader = new FakeTokenReader();
using var coordinator = new UsageDataCoordinator(quotaReader, tokenReader, synchronizationContext: null);

var readyQuota = new QuotaState(72, 61, DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow.AddDays(3), DateTimeOffset.UtcNow, QuotaStatus.Ready);
var readyToken = new TokenUsageState(
    new TokenUsagePeriod(100, 20),
    new TokenUsagePeriod(100, 20),
    new TokenUsagePeriod(100, 20),
    DateTimeOffset.UtcNow,
    TokenUsageStatus.Ready);
quotaReader.Next = readyQuota;
tokenReader.Next = readyToken;

await coordinator.RefreshAsync(RefreshScope.All);
AssertEqual(1, quotaReader.CallCount, "quota initial count");
AssertEqual(1, tokenReader.CallCount, "token initial count");
AssertEqual(QuotaStatus.Ready, coordinator.Snapshot.Quota.Status, "quota ready");
AssertEqual(TokenUsageStatus.Ready, coordinator.Snapshot.TokenUsage.Status, "token ready");

quotaReader.NextException = new IOException("offline");
await coordinator.RefreshAsync(RefreshScope.Quota);
AssertEqual(QuotaStatus.Stale, coordinator.Snapshot.Quota.Status, "quota stale");
AssertEqual(72, coordinator.Snapshot.Quota.FiveHourRemaining, "quota value preserved");
AssertEqual(1, tokenReader.CallCount, "quota-only does not read token");

tokenReader.NextException = new IOException("offline");
await coordinator.RefreshAsync(RefreshScope.TokenUsage);
AssertEqual(TokenUsageStatus.Stale, coordinator.Snapshot.TokenUsage.Status, "token stale");
AssertEqual(100L, coordinator.Snapshot.TokenUsage.Today.InputTokens, "token value preserved");

var routeLog = new List<string>();
var selectedStyle = FloatingStyleKind.Glass;
var router = new TrayCommandRouter(
    () => routeLog.Add("center"),
    () => routeLog.Add("floating"),
    () =>
    {
        routeLog.Add("refresh");
        return Task.CompletedTask;
    },
    () => routeLog.Add("settings"),
    style =>
    {
        selectedStyle = style;
        routeLog.Add("style");
    },
    () => routeLog.Add("exit"));

router.OpenControlCenter();
router.ToggleFloatingWindow();
await router.RefreshAllAsync();
router.OpenSettings();
router.SelectStyle(FloatingStyleKind.Terminal);
router.ExitApplication();
AssertEqual("center,floating,refresh,settings,style,exit", string.Join(',', routeLog), "tray command order");
AssertEqual(FloatingStyleKind.Terminal, selectedStyle, "tray style selection");

Console.WriteLine("Coordinator tests passed.");

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}");
    }
}

file sealed class FakeQuotaReader : IQuotaReader
{
    public int CallCount { get; private set; }
    public QuotaState? Next { get; set; }
    public Exception? NextException { get; set; }

    public Task<QuotaState> ReadAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        if (NextException is not null)
        {
            throw NextException;
        }

        return Task.FromResult(Next ?? QuotaState.Offline());
    }
}

file sealed class FakeTokenReader : ITokenUsageReader
{
    public int CallCount { get; private set; }
    public TokenUsageState? Next { get; set; }
    public Exception? NextException { get; set; }

    public Task<TokenUsageState> ReadAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        CallCount++;
        if (NextException is not null)
        {
            throw NextException;
        }

        return Task.FromResult(Next ?? TokenUsageState.Offline());
    }
}
