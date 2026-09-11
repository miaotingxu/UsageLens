using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UsageLens.Models;
using UsageLens.Services;

namespace UsageLens.ViewModels;

public enum ControlCenterPage
{
    Overview,
    Quota,
    TokenUsage,
    SessionSource,
    Settings
}

public sealed class ControlCenterViewModel : ObservableObject, IDisposable
{
    private readonly UsageDataCoordinator _coordinator;
    private readonly Func<Task> _refreshAsync;
    private readonly Dispatcher _dispatcher;
    private UsageSnapshot _snapshot;
    private ControlCenterPage _selectedPage;
    private bool _isRefreshing;
    private bool _disposed;

    public ControlCenterViewModel(
        UsageDataCoordinator coordinator,
        Func<Task> refreshAsync,
        SettingsViewModel? settings = null,
        Dispatcher? dispatcher = null)
    {
        _coordinator = coordinator;
        _refreshAsync = refreshAsync;
        _dispatcher = dispatcher ?? Application.Current.Dispatcher;
        _snapshot = coordinator.Snapshot;
        _selectedPage = ControlCenterPage.Overview;
        Settings = settings;

        SelectOverviewCommand = new RelayCommand(_ => SelectPage("overview"));
        SelectQuotaCommand = new RelayCommand(_ => SelectPage("quota"));
        SelectTokenUsageCommand = new RelayCommand(_ => SelectPage("tokens"));
        SelectSessionSourceCommand = new RelayCommand(_ => SelectPage("sessions"));
        SelectSettingsCommand = new RelayCommand(_ => SelectPage("settings"));
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsRefreshing);
        _coordinator.SnapshotChanged += CoordinatorOnSnapshotChanged;
    }

    public UsageSnapshot Snapshot => _snapshot;

    public SettingsViewModel? Settings { get; }

    public ControlCenterPage SelectedPage
    {
        get => _selectedPage;
        private set
        {
            if (SetProperty(ref _selectedPage, value))
            {
                OnPropertyChanged(nameof(SelectedPageTitle));
                OnPropertyChanged(nameof(SelectedPageSubtitle));
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value) && RefreshCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string AccountStatusText => _snapshot.Quota.Status == QuotaStatus.Loading
        ? "正在连接 Codex"
        : !_snapshot.IsCodexAvailable
            ? "未连接 Codex"
            : _snapshot.Quota.Status == QuotaStatus.Stale
                ? "使用最近额度缓存"
                : "已检测到 Codex 登录状态";

    public string AccountPrivacyText => "复用本机 Codex 登录态；不会读取或保存凭据。";

    public string SelectedPageTitle => SelectedPage switch
    {
        ControlCenterPage.Overview => "总览",
        ControlCenterPage.Quota => "额度窗口",
        ControlCenterPage.TokenUsage => "Token 用量",
        ControlCenterPage.SessionSource => "会话来源",
        _ => "设置"
    };

    public string SelectedPageSubtitle => SelectedPage switch
    {
        ControlCenterPage.Overview => "额度、Token 与本地状态一目了然",
        ControlCenterPage.Quota => "查看周期、进度与重置时间",
        ControlCenterPage.TokenUsage => "查看输入、输出、总量与估算价格",
        ControlCenterPage.SessionSource => "查看扫描范围与隐私边界",
        _ => "管理外观、刷新、托盘和隐私选项"
    };

    public string FiveHourText => FormatQuota(_snapshot.Quota.FiveHourRemaining, _snapshot.Quota.Status);
    public string WeeklyText => FormatQuota(_snapshot.Quota.WeeklyRemaining, _snapshot.Quota.Status);
    public string FiveHourResetText => QuotaPresentation.FormatResetCountdown(
        _snapshot.Quota.FiveHourResetAt, DateTimeOffset.Now);
    public string WeeklyResetText => QuotaPresentation.FormatResetCountdown(
        _snapshot.Quota.WeeklyResetAt, DateTimeOffset.Now);
    public Brush FiveHourBrush => GetQuotaBrush(_snapshot.Quota.FiveHourRemaining);
    public Brush WeeklyBrush => GetQuotaBrush(_snapshot.Quota.WeeklyRemaining);
    public string TodayTotalText => FormatTokenTotal(_snapshot.TokenUsage.Today);
    public string SevenDayTotalText => FormatTokenTotal(_snapshot.TokenUsage.Last7Days);
    public string ThirtyDayTotalText => FormatTokenTotal(_snapshot.TokenUsage.Last30Days);
    public string TodayInputText => FormatTokenPart(_snapshot.TokenUsage.Today.InputTokens);
    public string TodayOutputText => FormatTokenPart(_snapshot.TokenUsage.Today.OutputTokens);
    public string SevenDayInputText => FormatTokenPart(_snapshot.TokenUsage.Last7Days.InputTokens);
    public string SevenDayOutputText => FormatTokenPart(_snapshot.TokenUsage.Last7Days.OutputTokens);
    public string ThirtyDayInputText => FormatTokenPart(_snapshot.TokenUsage.Last30Days.InputTokens);
    public string ThirtyDayOutputText => FormatTokenPart(_snapshot.TokenUsage.Last30Days.OutputTokens);
    public string TodayTokenTooltip => TokenUsagePresentation.FormatTooltip(_snapshot.TokenUsage.Today);
    public string SevenDayTokenTooltip => TokenUsagePresentation.FormatTooltip(_snapshot.TokenUsage.Last7Days);
    public string ThirtyDayTokenTooltip => TokenUsagePresentation.FormatTooltip(_snapshot.TokenUsage.Last30Days);
    public string TodayInputTooltip => FormatInputTooltip(_snapshot.TokenUsage.Today);
    public string TodayOutputTooltip => FormatOutputTooltip(_snapshot.TokenUsage.Today);
    public string SevenDayInputTooltip => FormatInputTooltip(_snapshot.TokenUsage.Last7Days);
    public string SevenDayOutputTooltip => FormatOutputTooltip(_snapshot.TokenUsage.Last7Days);
    public string ThirtyDayInputTooltip => FormatInputTooltip(_snapshot.TokenUsage.Last30Days);
    public string ThirtyDayOutputTooltip => FormatOutputTooltip(_snapshot.TokenUsage.Last30Days);
    public string TodayPriceText => FormatPrice(_snapshot.TokenUsage.Today);
    public string SevenDayPriceText => FormatPrice(_snapshot.TokenUsage.Last7Days);
    public string ThirtyDayPriceText => FormatPrice(_snapshot.TokenUsage.Last30Days);
    public string TokenStatusText => _snapshot.TokenUsage.Status switch
    {
        TokenUsageStatus.Loading => "Loading…",
        TokenUsageStatus.Ready when _snapshot.TokenUsage.LastUpdatedAt is { } updated =>
            $"Updated {updated.ToLocalTime():HH:mm:ss}",
        TokenUsageStatus.Stale => "使用缓存数据",
        _ => "--"
    };
    public string SessionStatusText => _snapshot.TokenUsage.Status switch
    {
        TokenUsageStatus.Loading => "正在读取",
        TokenUsageStatus.Ready => "可用",
        TokenUsageStatus.Stale => "使用缓存数据",
        _ => "离线"
    };
    public string LastUpdatedText => _snapshot.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string LastScanText => _snapshot.CapturedAt.ToLocalTime().ToString("HH:mm:ss");

    public ICommand SelectOverviewCommand { get; }
    public ICommand SelectQuotaCommand { get; }
    public ICommand SelectTokenUsageCommand { get; }
    public ICommand SelectSessionSourceCommand { get; }
    public ICommand SelectSettingsCommand { get; }
    public ICommand RefreshCommand { get; }

    public void SelectPage(string pageKey)
    {
        SelectedPage = pageKey switch
        {
            "overview" => ControlCenterPage.Overview,
            "quota" => ControlCenterPage.Quota,
            "tokens" => ControlCenterPage.TokenUsage,
            "sessions" => ControlCenterPage.SessionSource,
            "settings" => ControlCenterPage.Settings,
            _ => throw new ArgumentException($"Unknown control-center page: {pageKey}", nameof(pageKey))
        };
    }

    public void SelectSettings() => SelectPage("settings");

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _coordinator.SnapshotChanged -= CoordinatorOnSnapshotChanged;
    }

    private async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            await _refreshAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void CoordinatorOnSnapshotChanged(object? sender, UsageSnapshot snapshot)
    {
        if (_dispatcher.CheckAccess())
        {
            ApplySnapshot(snapshot);
            return;
        }

        _dispatcher.BeginInvoke(() => ApplySnapshot(snapshot));
    }

    private void ApplySnapshot(UsageSnapshot snapshot)
    {
        if (_disposed)
        {
            return;
        }

        _snapshot = snapshot;
        OnPropertyChanged(nameof(Snapshot));
        OnPropertyChanged(nameof(AccountStatusText));
        OnPropertyChanged(nameof(FiveHourText));
        OnPropertyChanged(nameof(WeeklyText));
        OnPropertyChanged(nameof(FiveHourResetText));
        OnPropertyChanged(nameof(WeeklyResetText));
        OnPropertyChanged(nameof(FiveHourBrush));
        OnPropertyChanged(nameof(WeeklyBrush));
        OnPropertyChanged(nameof(TodayTotalText));
        OnPropertyChanged(nameof(SevenDayTotalText));
        OnPropertyChanged(nameof(ThirtyDayTotalText));
        OnPropertyChanged(nameof(TodayInputText));
        OnPropertyChanged(nameof(TodayOutputText));
        OnPropertyChanged(nameof(SevenDayInputText));
        OnPropertyChanged(nameof(SevenDayOutputText));
        OnPropertyChanged(nameof(ThirtyDayInputText));
        OnPropertyChanged(nameof(ThirtyDayOutputText));
        OnPropertyChanged(nameof(TodayTokenTooltip));
        OnPropertyChanged(nameof(SevenDayTokenTooltip));
        OnPropertyChanged(nameof(ThirtyDayTokenTooltip));
        OnPropertyChanged(nameof(TodayInputTooltip));
        OnPropertyChanged(nameof(TodayOutputTooltip));
        OnPropertyChanged(nameof(SevenDayInputTooltip));
        OnPropertyChanged(nameof(SevenDayOutputTooltip));
        OnPropertyChanged(nameof(ThirtyDayInputTooltip));
        OnPropertyChanged(nameof(ThirtyDayOutputTooltip));
        OnPropertyChanged(nameof(TodayPriceText));
        OnPropertyChanged(nameof(SevenDayPriceText));
        OnPropertyChanged(nameof(ThirtyDayPriceText));
        OnPropertyChanged(nameof(TokenStatusText));
        OnPropertyChanged(nameof(SessionStatusText));
        OnPropertyChanged(nameof(LastUpdatedText));
        OnPropertyChanged(nameof(LastScanText));
    }

    private static string FormatQuota(int? value, QuotaStatus status) => value is int number
        ? $"{number}%"
        : status == QuotaStatus.Loading ? "..." : "--";

    private static string FormatTokenTotal(TokenUsagePeriod period)
    {
        if (period.InputTokens == 0 && period.OutputTokens == 0)
        {
            return "0";
        }

        return TokenUsagePresentation.FormatCompact(period.InputTokens + period.OutputTokens);
    }

    private static string FormatTokenPart(long value) => TokenUsagePresentation.FormatCompact(value);

    private static string FormatInputTooltip(TokenUsagePeriod period) =>
        $"IN {period.InputTokens:N0} (includes cached input)";

    private static string FormatOutputTooltip(TokenUsagePeriod period) =>
        $"OUT {period.OutputTokens:N0}";

    private static string FormatPrice(TokenUsagePeriod period) =>
        $"{TokenUsagePresentation.FormatEstimatedPrice(period.EstimatedPriceUsd)}{(period.UnpricedTokens > 0 ? " · 未定价" : string.Empty)}";

    private static Brush GetQuotaBrush(int? remainingPercent) => QuotaPresentation.GetColorBand(remainingPercent) switch
    {
        QuotaColorBand.Green => Brushes.LightGreen,
        QuotaColorBand.Yellow => Brushes.Khaki,
        QuotaColorBand.Amber => Brushes.Orange,
        QuotaColorBand.Red => Brushes.LightCoral,
        _ => Brushes.LightSteelBlue
    };
}
