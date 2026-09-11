using UsageLens.Models;
using UsageLens.ViewModels;
using UsageLens.Views;

namespace UsageLens.Services;

public sealed class ControlCenterWindowManager : IDisposable
{
    private readonly UsageDataCoordinator _coordinator;
    private readonly Func<Task> _refreshAsync;
    private readonly ViewModels.SettingsViewModel? _settingsViewModel;
    private ControlCenterWindow? _window;
    private ControlCenterViewModel? _viewModel;
    private AppSettings _settings;
    private bool _disposed;

    public ControlCenterWindowManager(
        UsageDataCoordinator coordinator,
        Func<Task> refreshAsync,
        AppSettings settings,
        ViewModels.SettingsViewModel? settingsViewModel = null)
    {
        _coordinator = coordinator;
        _refreshAsync = refreshAsync;
        _settings = settings;
        _settingsViewModel = settingsViewModel;
    }

    public bool IsVisible => _window?.IsVisible == true;

    public void Show()
    {
        ThrowIfDisposed();
        EnsureWindow();

        if (_window!.WindowState == System.Windows.WindowState.Minimized)
        {
            _window.WindowState = System.Windows.WindowState.Normal;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
        _window.Focus();
    }

    public void ShowSettings()
    {
        EnsureWindow();
        _viewModel!.SelectSettings();
        Show();
    }

    public void Hide() => _window?.Hide();

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _settingsViewModel?.ApplyCurrentSettings(settings);
        if (_window is not null)
        {
            _window.HideOnClose = settings.HideControlCenterOnClose;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewModel?.Dispose();
        if (_window is not null)
        {
            _window.AllowClose();
            _window.Close();
        }

        _window = null;
        _viewModel = null;
    }

    private void EnsureWindow()
    {
        if (_window is not null)
        {
            return;
        }

        _viewModel = new ControlCenterViewModel(_coordinator, _refreshAsync, _settingsViewModel);
        _window = new ControlCenterWindow(_viewModel)
        {
            HideOnClose = _settings.HideControlCenterOnClose
        };
        _window.Closed += WindowOnClosed;
    }

    private void WindowOnClosed(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _window))
        {
            return;
        }

        _viewModel?.Dispose();
        _window = null;
        _viewModel = null;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
