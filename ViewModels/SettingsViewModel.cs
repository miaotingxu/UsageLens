using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using UsageLens.Models;
using UsageLens.Services;

namespace UsageLens.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsStore _store;
    private readonly Func<AppSettings, bool> _prepareSettings;
    private readonly Action<AppSettings> _applySettings;
    private readonly Func<Task> _refreshAsync;
    private readonly Func<UsageSnapshot> _getSnapshot;
    private readonly Action _resetSettings;
    private readonly UsageDataCoordinator _coordinator;
    private readonly DispatcherTimer _feedbackTimer;
    private AppSettings _settings;
    private UsageSnapshot _snapshot;
    private string _feedbackText = string.Empty;
    private bool _feedbackIsError;

    public SettingsViewModel(
        AppSettings settings,
        AppSettingsStore store,
        Func<AppSettings, bool> prepareSettings,
        Action<AppSettings> applySettings,
        Func<Task> refreshAsync,
        Func<UsageSnapshot> getSnapshot,
        UsageDataCoordinator coordinator,
        Action resetSettings)
    {
        _settings = settings;
        _store = store;
        _prepareSettings = prepareSettings;
        _applySettings = applySettings;
        _refreshAsync = refreshAsync;
        _getSnapshot = getSnapshot;
        _coordinator = coordinator;
        _snapshot = getSnapshot();
        _resetSettings = resetSettings;
        _feedbackTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1.8)
        };
        _feedbackTimer.Tick += FeedbackTimerOnTick;
        RefreshNowCommand = new RelayCommand(async _ => await RefreshNowAsync());
        ResetUiPreferencesCommand = new RelayCommand(_ => ResetUiPreferences());
        CopyDiagnosticsCommand = new RelayCommand(_ => CopyDiagnostics());
        ResetPositionCommand = new RelayCommand(_ => Update(_settings with { FloatingLeft = null }));
        _coordinator.SnapshotChanged += CoordinatorOnSnapshotChanged;
    }

    public AppSettings Settings => _settings;

    public FloatingStyleKind SelectedStyle
    {
        get => _settings.FloatingStyle;
        set => Update(_settings with { FloatingStyle = value });
    }

    public double FloatingOpacity
    {
        get => _settings.FloatingOpacity;
        set => Update(_settings with { FloatingOpacity = value });
    }

    public bool AutoCollapseEnabled
    {
        get => _settings.AutoCollapseEnabled;
        set => Update(_settings with { AutoCollapseEnabled = value });
    }

    public int CollapseDelaySeconds
    {
        get => (int)_settings.CollapseDelay.TotalSeconds;
        set => Update(_settings with { CollapseDelay = TimeSpan.FromSeconds(value) });
    }

    public bool ExpandOnHandleHover
    {
        get => _settings.ExpandOnHandleHover;
        set => Update(_settings with { ExpandOnHandleHover = value });
    }

    public int QuotaRefreshMinutes
    {
        get => (int)_settings.QuotaRefreshInterval.TotalMinutes;
        set => Update(_settings with { QuotaRefreshInterval = TimeSpan.FromMinutes(value) });
    }

    public int TokenRefreshMinutes
    {
        get => (int)_settings.TokenRefreshInterval.TotalMinutes;
        set => Update(_settings with { TokenRefreshInterval = TimeSpan.FromMinutes(value) });
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set => Update(_settings with { StartWithWindows = value });
    }

    public bool HideControlCenterOnClose
    {
        get => _settings.HideControlCenterOnClose;
        set => Update(_settings with { HideControlCenterOnClose = value });
    }

    public TrayPrimaryAction TrayPrimaryAction
    {
        get => _settings.TrayPrimaryAction;
        set => Update(_settings with { TrayPrimaryAction = value });
    }

    public string FeedbackText
    {
        get => _feedbackText;
        private set => SetProperty(ref _feedbackText, value);
    }

    public bool FeedbackIsError
    {
        get => _feedbackIsError;
        private set => SetProperty(ref _feedbackIsError, value);
    }

    public ICommand RefreshNowCommand { get; }
    public ICommand ResetUiPreferencesCommand { get; }
    public ICommand CopyDiagnosticsCommand { get; }
    public ICommand ResetPositionCommand { get; }

    public string CodexStatusText => _snapshot.Quota.Status switch
    {
        QuotaStatus.Ready => "Ready",
        QuotaStatus.Loading => "Loading",
        QuotaStatus.Stale => "Stale",
        _ => "Offline"
    };

    public string QuotaStatusText => _snapshot.Quota.Status.ToString();

    public string TokenStatusText => _snapshot.TokenUsage.Status.ToString();

    public string LastUpdateText => _snapshot.CapturedAt == default
        ? "--"
        : _snapshot.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public void Dispose()
    {
        _coordinator.SnapshotChanged -= CoordinatorOnSnapshotChanged;
        _feedbackTimer.Stop();
        _feedbackTimer.Tick -= FeedbackTimerOnTick;
    }

    public void ApplyCurrentSettings(AppSettings settings)
    {
        _settings = settings;
        RaiseAllProperties();
    }

    private void Update(AppSettings settings)
    {
        if (!_prepareSettings(settings))
        {
            SetFeedback("无法写入当前用户的开机启动项，设置未保存。", isError: true);
            return;
        }

        _store.Save(settings);
        _applySettings(settings);
        _settings = settings;
        SetFeedback("已保存");
        RaiseAllProperties();
    }

    private async Task RefreshNowAsync()
    {
        SetFeedback("正在刷新…", transient: false);
        await _refreshAsync();
        SetFeedback("已刷新");
    }

    private void ResetUiPreferences()
    {
        _store.Reset();
        _resetSettings();
        _settings = AppSettings.Default;
        SetFeedback("界面偏好已重置");
        RaiseAllProperties();
    }

    private void CopyDiagnostics()
    {
        var snapshot = _getSnapshot();
        var version = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        var lastUpdate = snapshot.CapturedAt == default
            ? "--"
            : snapshot.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        Clipboard.SetText(
            $"UsageLens {version}\n" +
            $"Codex connection: {snapshot.Quota.Status}\n" +
            $"Quota status: {snapshot.Quota.Status}\n" +
            $"Token status: {snapshot.TokenUsage.Status}\n" +
            $"Last update: {lastUpdate}");
        SetFeedback("脱敏诊断信息已复制");
    }

    private void SetFeedback(string text, bool isError = false, bool transient = true)
    {
        _feedbackTimer.Stop();
        FeedbackIsError = isError;
        FeedbackText = text;
        if (transient && !isError)
        {
            _feedbackTimer.Start();
        }
    }

    private void FeedbackTimerOnTick(object? sender, EventArgs e)
    {
        _feedbackTimer.Stop();
        FeedbackText = string.Empty;
    }

    private void RaiseAllProperties()
    {
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(SelectedStyle));
        OnPropertyChanged(nameof(FloatingOpacity));
        OnPropertyChanged(nameof(AutoCollapseEnabled));
        OnPropertyChanged(nameof(CollapseDelaySeconds));
        OnPropertyChanged(nameof(ExpandOnHandleHover));
        OnPropertyChanged(nameof(QuotaRefreshMinutes));
        OnPropertyChanged(nameof(TokenRefreshMinutes));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(HideControlCenterOnClose));
        OnPropertyChanged(nameof(TrayPrimaryAction));
    }

    private void CoordinatorOnSnapshotChanged(object? sender, UsageSnapshot snapshot)
    {
        _snapshot = snapshot;
        OnPropertyChanged(nameof(CodexStatusText));
        OnPropertyChanged(nameof(QuotaStatusText));
        OnPropertyChanged(nameof(TokenStatusText));
        OnPropertyChanged(nameof(LastUpdateText));
    }
}
