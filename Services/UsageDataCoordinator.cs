using UsageLens.Models;

namespace UsageLens.Services;

/// <summary>
/// 应用内唯一的数据刷新入口，避免悬浮窗和控制中心各自读取 Codex。
/// </summary>
public sealed class UsageDataCoordinator : IDisposable
{
    private readonly IQuotaReader _quotaReader;
    private readonly ITokenUsageReader _tokenReader;
    private readonly SemaphoreSlim _quotaGate = new(1, 1);
    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly object _snapshotGate = new();
    private Timer? _quotaTimer;
    private Timer? _tokenTimer;
    private AppSettings _settings = AppSettings.Default;
    private QuotaState? _lastSuccessfulQuota;
    private TokenUsageState? _lastSuccessfulTokenUsage;
    private UsageSnapshot _snapshot;
    private bool _started;
    private bool _disposed;

    public UsageDataCoordinator(
        IQuotaReader quotaReader,
        ITokenUsageReader tokenReader,
        SynchronizationContext? synchronizationContext = null)
    {
        _quotaReader = quotaReader;
        _tokenReader = tokenReader;
        _synchronizationContext = synchronizationContext ?? SynchronizationContext.Current;
        _snapshot = UsageSnapshot.Initial(DateTimeOffset.Now);
    }

    public UsageSnapshot Snapshot
    {
        get
        {
            lock (_snapshotGate)
            {
                return _snapshot;
            }
        }
    }

    public event EventHandler<UsageSnapshot>? SnapshotChanged;

    public void Start()
    {
        ThrowIfDisposed();
        if (_started)
        {
            return;
        }

        _started = true;
        ApplyTimers(_settings, startImmediately: true);
        _ = RefreshAsync(RefreshScope.All, _lifetimeCancellation.Token);
    }

    public void ApplySettings(AppSettings settings)
    {
        ThrowIfDisposed();
        _settings = settings;

        if (_started)
        {
            ApplyTimers(settings, startImmediately: false);
        }
    }

    public async Task RefreshAsync(
        RefreshScope scope,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token,
            cancellationToken);
        var token = linkedCancellation.Token;
        var tasks = new List<Task>(2);

        if (scope.HasFlag(RefreshScope.Quota))
        {
            tasks.Add(RefreshQuotaAsync(token));
        }

        if (scope.HasFlag(RefreshScope.TokenUsage))
        {
            tasks.Add(RefreshTokenUsageAsync(token));
        }

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _quotaTimer?.Dispose();
        _tokenTimer?.Dispose();
        _quotaTimer = null;
        _tokenTimer = null;
        DisposeReader(_quotaReader);
        DisposeReader(_tokenReader);
        _lifetimeCancellation.Dispose();
    }

    private async Task RefreshQuotaAsync(CancellationToken cancellationToken)
    {
        if (!await _quotaGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var state = await _quotaReader.ReadAsync(cancellationToken);
            _lastSuccessfulQuota = state;
            Publish(snapshot => snapshot with
            {
                Quota = state,
                IsCodexAvailable = true,
                CapturedAt = DateTimeOffset.Now
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 关闭或调用方取消不发布错误状态。
        }
        catch
        {
            var state = _lastSuccessfulQuota?.MarkStale() ?? QuotaState.Offline();
            Publish(snapshot => snapshot with
            {
                Quota = state,
                IsCodexAvailable = state.Status != QuotaStatus.Offline,
                CapturedAt = DateTimeOffset.Now
            });
        }
        finally
        {
            _quotaGate.Release();
        }
    }

    private async Task RefreshTokenUsageAsync(CancellationToken cancellationToken)
    {
        if (!await _tokenGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var state = await _tokenReader.ReadAsync(DateTimeOffset.Now, cancellationToken);
            _lastSuccessfulTokenUsage = state;
            Publish(snapshot => snapshot with
            {
                TokenUsage = state,
                CapturedAt = DateTimeOffset.Now
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 关闭或调用方取消不发布错误状态。
        }
        catch
        {
            var state = _lastSuccessfulTokenUsage?.MarkStale() ?? TokenUsageState.Offline();
            Publish(snapshot => snapshot with
            {
                TokenUsage = state,
                CapturedAt = DateTimeOffset.Now
            });
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private void ApplyTimers(AppSettings settings, bool startImmediately)
    {
        _quotaTimer ??= new Timer(
            static state => ((UsageDataCoordinator)state!).OnQuotaTimer(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
        _tokenTimer ??= new Timer(
            static state => ((UsageDataCoordinator)state!).OnTokenTimer(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        var quotaDue = startImmediately ? TimeSpan.Zero : settings.QuotaRefreshInterval;
        var tokenDue = startImmediately ? TimeSpan.Zero : settings.TokenRefreshInterval;
        _quotaTimer.Change(quotaDue, settings.QuotaRefreshInterval);
        _tokenTimer.Change(tokenDue, settings.TokenRefreshInterval);
    }

    private void OnQuotaTimer()
    {
        if (!_disposed)
        {
            _ = RefreshAsync(RefreshScope.Quota, _lifetimeCancellation.Token);
        }
    }

    private void OnTokenTimer()
    {
        if (!_disposed)
        {
            _ = RefreshAsync(RefreshScope.TokenUsage, _lifetimeCancellation.Token);
        }
    }

    private void Publish(Func<UsageSnapshot, UsageSnapshot> update)
    {
        if (_disposed)
        {
            return;
        }

        UsageSnapshot next;
        lock (_snapshotGate)
        {
            _snapshot = next = update(_snapshot);
        }

        if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
        {
            SnapshotChanged?.Invoke(this, next);
            return;
        }

        _synchronizationContext.Post(
            state => SnapshotChanged?.Invoke(this, (UsageSnapshot)state!),
            next);
    }

    private static void DisposeReader(object reader)
    {
        if (reader is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
