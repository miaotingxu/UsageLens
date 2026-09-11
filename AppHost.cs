using System.Windows;
using UsageLens.Models;
using UsageLens.Services;
using UsageLens.ViewModels;

namespace UsageLens;

public sealed class AppHost : IDisposable
{
    private readonly AppSettingsStore _settingsStore = new();
    private UsageDataCoordinator? _coordinator;
    private MainWindow? _floatingWindow;
    private ControlCenterWindowManager? _controlCenter;
    private TrayIconController? _trayIcon;
    private StartupRegistrationService? _startupRegistration;
    private SettingsViewModel? _settingsViewModel;
    private AppSettings _settings = AppSettings.Default;
    private bool _started;
    private bool _exitRequested;
    private bool _disposed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            return;
        }

        _started = true;
        var settings = _settings = _settingsStore.Load();
        _startupRegistration = new StartupRegistrationService();
        if (settings.StartWithWindows)
        {
            // 便携程序可能被移动；每次启动时用当前路径修正 Run 项。
            _startupRegistration.Apply(enabled: true);
        }

        var quotaReader = new CodexQuotaReader();
        var tokenReader = new LocalTokenUsageReader();
        _coordinator = new UsageDataCoordinator(quotaReader, tokenReader);
        _coordinator.ApplySettings(settings);
        _coordinator.Start();
        _floatingWindow = new MainWindow(
            _settingsStore,
            () => _coordinator.RefreshAsync(RefreshScope.All),
            Exit)
        {
            Topmost = true
        };
        Application.Current.MainWindow = _floatingWindow;
        _floatingWindow.ApplySettings(settings);
        _floatingWindow.ApplySnapshot(_coordinator.Snapshot);
        _floatingWindow.Show();
        _coordinator.SnapshotChanged += CoordinatorOnSnapshotChanged;

        _settingsViewModel = new SettingsViewModel(
            settings,
            _settingsStore,
            PrepareSettings,
            ApplySettings,
            () => _coordinator.RefreshAsync(RefreshScope.All),
            () => _coordinator.Snapshot,
            _coordinator,
            ResetSettings);
        _controlCenter = new ControlCenterWindowManager(
            _coordinator,
            () => _coordinator.RefreshAsync(RefreshScope.All),
            settings,
            _settingsViewModel);
        _floatingWindow.OpenControlCenterAction = _controlCenter.Show;
        var router = new TrayCommandRouter(
            _controlCenter.Show,
            _floatingWindow.ToggleFloatingVisibility,
            () => _coordinator.RefreshAsync(RefreshScope.All),
            _controlCenter.ShowSettings,
            _floatingWindow.SelectStyle,
            Exit);
        _trayIcon = new TrayIconController(router, settings);
    }

    public void Exit()
    {
        if (_disposed || _exitRequested)
        {
            return;
        }

        _exitRequested = true;
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_coordinator is not null)
        {
            _coordinator.SnapshotChanged -= CoordinatorOnSnapshotChanged;
        }

        _trayIcon?.Dispose();
        _controlCenter?.Dispose();
        _floatingWindow?.Dispose();
        _settingsViewModel?.Dispose();
        _coordinator?.Dispose();
        _settingsViewModel = null;
        _trayIcon = null;
        _controlCenter = null;
        _floatingWindow = null;
        _coordinator = null;
    }

    private void CoordinatorOnSnapshotChanged(object? sender, UsageSnapshot snapshot)
    {
        _floatingWindow?.ApplySnapshot(snapshot);
    }

    private bool PrepareSettings(AppSettings settings)
    {
        if (_startupRegistration is not null && settings.StartWithWindows != _settings.StartWithWindows &&
            !_startupRegistration.Apply(settings.StartWithWindows))
        {
            return false;
        }

        return true;
    }

    private void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _coordinator?.ApplySettings(settings);
        _floatingWindow?.ApplySettings(settings);
        _controlCenter?.ApplySettings(settings);
        _trayIcon?.ApplySettings(settings);
    }

    private void ResetSettings()
    {
        _startupRegistration?.Apply(false);
        ApplySettings(AppSettings.Default);
    }
}
