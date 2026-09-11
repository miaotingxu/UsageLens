using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using H.NotifyIcon;
using UsageLens.Models;

namespace UsageLens.Services;

public sealed class TrayIconController : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private readonly TrayCommandRouter _router;
    private readonly DispatcherTimer _clickTimer;
    private readonly ContextMenu _contextMenu;
    private readonly Dictionary<FloatingStyleKind, MenuItem> _styleItems = new();
    private AppSettings _settings;
    private bool _disposed;

    public TrayIconController(TrayCommandRouter router, AppSettings settings)
    {
        _router = router;
        _settings = settings;
        _contextMenu = BuildContextMenu();
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "UsageLens",
            ContextMenu = _contextMenu,
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/UsageLens.ico"))
        };
        _clickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
        _clickTimer.Tick += SingleClickTimerOnTick;
        _taskbarIcon.TrayLeftMouseUp += TaskbarIconOnTrayLeftMouseUp;
        _taskbarIcon.TrayLeftMouseDoubleClick += TaskbarIconOnTrayLeftMouseDoubleClick;
        _taskbarIcon.ForceCreate();
        ApplySettings(settings);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        foreach (var pair in _styleItems)
        {
            pair.Value.IsChecked = pair.Key == settings.FloatingStyle;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _clickTimer.Stop();
        _clickTimer.Tick -= SingleClickTimerOnTick;
        _taskbarIcon.TrayLeftMouseUp -= TaskbarIconOnTrayLeftMouseUp;
        _taskbarIcon.TrayLeftMouseDoubleClick -= TaskbarIconOnTrayLeftMouseDoubleClick;
        _taskbarIcon.Dispose();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateItem("打开控制中心", (_, _) => _router.OpenControlCenter()));
        menu.Items.Add(CreateItem("显示/隐藏悬浮窗", (_, _) => _router.ToggleFloatingWindow()));
        menu.Items.Add(CreateRefreshItem());

        var styleMenu = new MenuItem { Header = "界面样式" };
        foreach (var style in Enum.GetValues<FloatingStyleKind>())
        {
            var item = CreateItem(GetStyleLabel(style), (_, _) => _router.SelectStyle(style));
            item.IsCheckable = true;
            _styleItems[style] = item;
            styleMenu.Items.Add(item);
        }

        menu.Items.Add(styleMenu);
        menu.Items.Add(CreateItem("设置", (_, _) => _router.OpenSettings()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("退出 UsageLens", (_, _) => _router.ExitApplication()));
        return menu;
    }

    private MenuItem CreateRefreshItem()
    {
        MenuItem? item = null;
        item = CreateItem("立即刷新", async (_, _) =>
        {
            item!.IsEnabled = false;
            try
            {
                await _router.RefreshAllAsync();
            }
            finally
            {
                item!.IsEnabled = true;
            }
        });
        return item!;
    }

    private static MenuItem CreateItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem
        {
            Header = header,
            Padding = new Thickness(12, 6, 24, 6),
            FontSize = 12
        };
        item.Click += handler;
        return item;
    }

    private void TaskbarIconOnTrayLeftMouseUp(object? sender, RoutedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _clickTimer.Stop();
        _clickTimer.Start();
    }

    private void TaskbarIconOnTrayLeftMouseDoubleClick(object? sender, RoutedEventArgs e)
    {
        _clickTimer.Stop();
        _router.ToggleFloatingWindow();
    }

    private void SingleClickTimerOnTick(object? sender, EventArgs e)
    {
        _clickTimer.Stop();
        if (_settings.TrayPrimaryAction == TrayPrimaryAction.ToggleFloatingWindow)
        {
            _router.ToggleFloatingWindow();
        }
        else
        {
            _router.OpenControlCenter();
        }
    }

    private static string GetStyleLabel(FloatingStyleKind style) => style switch
    {
        FloatingStyleKind.Instrument => "仪表 · A",
        FloatingStyleKind.Glass => "玻璃 · B",
        FloatingStyleKind.Timeline => "时间线 · C",
        FloatingStyleKind.Terminal => "终端 · D",
        _ => style.ToString()
    };
}
