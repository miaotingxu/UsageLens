using UsageLens.Models;

namespace UsageLens.Services;

public sealed class TrayCommandRouter
{
    private readonly Action _openControlCenter;
    private readonly Action _toggleFloatingWindow;
    private readonly Func<Task> _refreshAllAsync;
    private readonly Action _openSettings;
    private readonly Action<FloatingStyleKind> _selectStyle;
    private readonly Action _exitApplication;

    public TrayCommandRouter(
        Action openControlCenter,
        Action toggleFloatingWindow,
        Func<Task> refreshAllAsync,
        Action openSettings,
        Action<FloatingStyleKind> selectStyle,
        Action exitApplication)
    {
        _openControlCenter = openControlCenter;
        _toggleFloatingWindow = toggleFloatingWindow;
        _refreshAllAsync = refreshAllAsync;
        _openSettings = openSettings;
        _selectStyle = selectStyle;
        _exitApplication = exitApplication;
    }

    public void OpenControlCenter() => _openControlCenter();

    public void ToggleFloatingWindow() => _toggleFloatingWindow();

    public Task RefreshAllAsync() => _refreshAllAsync();

    public void OpenSettings() => _openSettings();

    public void SelectStyle(FloatingStyleKind style) => _selectStyle(style);

    public void ExitApplication() => _exitApplication();
}
