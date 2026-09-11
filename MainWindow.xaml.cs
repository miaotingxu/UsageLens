using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShapePath = System.Windows.Shapes.Path;
using System.Windows.Threading;
using UsageLens.Models;
using UsageLens.Services;
using UsageLens.Views;

namespace UsageLens;

public partial class MainWindow : Window, IDisposable
{
    private static readonly FloatingStyleKind[] StyleOrder =
    [
        FloatingStyleKind.Instrument,
        FloatingStyleKind.Glass,
        FloatingStyleKind.Timeline,
        FloatingStyleKind.Terminal
    ];

    private readonly AppSettingsStore _settingsStore;
    private readonly Func<Task> _refreshAllAsync;
    private readonly Action _exitApplication;
    private readonly DispatcherTimer _countdownTimer;
    private readonly IReadOnlyDictionary<FloatingStyleKind, FrameworkElement> _styleViews;
    private Action? _openControlCenter;

    private FloatingWindowController? _floatingWindowController;
    private FloatingStyleKind _selectedStyle;
    private QuotaState _currentState = QuotaState.Loading();
    private TokenUsageState _tokenUsageState = TokenUsageState.Loading();
    private string _quotaStatusMessage = "正在连接 Codex…";
    private Brush _quotaStatusBrush = Brushes.Gainsboro;
    private string _tokenStatusMessage = "Loading…";
    private Brush _tokenStatusBrush = Brushes.Gainsboro;
    private double? _savedWindowLeft;
    private bool _isHorizontalDragging;
    private double _dragStartMouseScreenX;
    private double _dragStartWindowLeft;
    private AppSettings _settings;
    private bool _disposed;

    public MainWindow(
        AppSettingsStore? settingsStore = null,
        Func<Task>? refreshAllAsync = null,
        Action? exitApplication = null)
    {
        InitializeComponent();
        _settingsStore = settingsStore ?? new AppSettingsStore();
        _refreshAllAsync = refreshAllAsync ?? (() => Task.CompletedTask);
        _exitApplication = exitApplication ?? (() => { });
        _settings = _settingsStore.Load();
        _styleViews = new Dictionary<FloatingStyleKind, FrameworkElement>
        {
            [FloatingStyleKind.Instrument] = new InstrumentDashboard(),
            [FloatingStyleKind.Glass] = new GlassDashboard(),
            [FloatingStyleKind.Timeline] = new TimelineDashboard(),
            [FloatingStyleKind.Terminal] = new TerminalDashboard()
        };
        _selectedStyle = _settings.FloatingStyle;
        _savedWindowLeft = _settings.FloatingLeft;
        ApplyStyle(_selectedStyle, savePreference: false);

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _countdownTimer.Tick += CountdownTimerOnTick;

        SetQuotaState(_currentState);
        SetTokenUsageState(_tokenUsageState);
    }

    public void SetQuotaState(QuotaState state, string? statusMessage = null)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => SetQuotaState(state, statusMessage));
            return;
        }

        _currentState = state;
        UpdateQuotaPresentation();
        SetStatusText(statusMessage ?? GetDefaultStatusMessage(state), GetStatusBrush(state.Status));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _countdownTimer.Stop();
        _countdownTimer.Tick -= CountdownTimerOnTick;
        _floatingWindowController?.Dispose();
    }

    private void WindowOnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnPrimaryScreen();
        _floatingWindowController = new FloatingWindowController(
            this,
            Card,
            HoverZone,
            SetSelectedDashboardShadowVisible,
            SetStyleNavigationVisible);
        _floatingWindowController.ApplyBehavior(FloatingWindowBehavior.From(_settings));
        ApplyCardBackgroundOpacity(_settings.FloatingOpacity);
        _countdownTimer.Start();
        // 首次显示时也遵循自动折叠策略；若鼠标正停在卡片上，控制器会保留展开状态。
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => _floatingWindowController?.ScheduleCollapse()));
    }

    private void WindowOnClosed(object? sender, EventArgs e)
    {
        SaveAppearance();
        Dispose();

        if (!Application.Current.Dispatcher.HasShutdownStarted)
        {
            _exitApplication();
        }
    }

    private void CountdownTimerOnTick(object? sender, EventArgs e) => UpdateQuotaPresentation();

    private async void RefreshMenuItemOnClick(object sender, RoutedEventArgs e) => await _refreshAllAsync();

    private void OpenControlCenterMenuItemOnClick(object sender, RoutedEventArgs e) => _openControlCenter?.Invoke();

    private void ExitMenuItemOnClick(object sender, RoutedEventArgs e) => _exitApplication();

    private void PreviousStyleButtonOnClick(object sender, RoutedEventArgs e)
    {
        CycleStyle(-1);
    }

    private void NextStyleButtonOnClick(object sender, RoutedEventArgs e)
    {
        CycleStyle(1);
    }

    private void StyleNavigationOnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _floatingWindowController?.SetInteractionLocked(true);
    }

    private void StyleNavigationOnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _floatingWindowController?.SetInteractionLocked(false);
        _floatingWindowController?.ScheduleCollapse();
    }

    private void DragSurfaceOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _isHorizontalDragging)
        {
            return;
        }

        e.Handled = true;
        var workArea = SystemParameters.WorkArea;
        _dragStartMouseScreenX = GetMouseScreenX(e);
        _dragStartWindowLeft = Left;
        _isHorizontalDragging = true;
        _floatingWindowController?.BeginUserDrag(workArea.Top);
        Top = workArea.Top;

        if (!Mouse.Capture(this, CaptureMode.Element))
        {
            CompleteHorizontalDrag(releaseMouseCapture: false);
        }
    }

    private void WindowOnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isHorizontalDragging)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            CompleteHorizontalDrag();
            return;
        }

        var workArea = SystemParameters.WorkArea;
        var requestedLeft = _dragStartWindowLeft + GetMouseScreenX(e) - _dragStartMouseScreenX;
        Left = HorizontalWindowPlacement.ClampLeft(requestedLeft, workArea.Left, workArea.Width, Width);
        Top = workArea.Top;
        e.Handled = true;
    }

    private void WindowOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isHorizontalDragging || e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        CompleteHorizontalDrag();
    }

    private void WindowOnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isHorizontalDragging)
        {
            CompleteHorizontalDrag(releaseMouseCapture: false);
        }
    }

    private void CompleteHorizontalDrag(bool releaseMouseCapture = true)
    {
        if (!_isHorizontalDragging)
        {
            return;
        }

        _isHorizontalDragging = false;
        var workArea = SystemParameters.WorkArea;
        Left = HorizontalWindowPlacement.ClampLeft(Left, workArea.Left, workArea.Width, Width);
        Top = workArea.Top;
        _floatingWindowController?.CompleteUserDrag(Left, Top);
        SaveAppearance();

        if (releaseMouseCapture && Mouse.Captured == this)
        {
            Mouse.Capture(null);
        }
    }

    private double GetMouseScreenX(MouseEventArgs e)
    {
        var screenPoint = PointToScreen(e.GetPosition(this));
        var presentationSource = PresentationSource.FromVisual(this);
        return presentationSource?.CompositionTarget?.TransformFromDevice.Transform(screenPoint).X
               ?? screenPoint.X;
    }

    private void CycleStyle(int direction)
    {
        var currentIndex = Array.IndexOf(StyleOrder, _selectedStyle);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = (currentIndex + direction) % StyleOrder.Length;
        if (nextIndex < 0)
        {
            nextIndex += StyleOrder.Length;
        }

        ApplyStyle(StyleOrder[nextIndex], savePreference: true);
    }

    private void ApplyStyle(FloatingStyleKind style, bool savePreference)
    {
        _selectedStyle = style;
        StyleContent.Content = _styleViews[style];
        PreviousStyleButton.ToolTip = $"上一套：{GetStylePickerLabel(GetAdjacentStyle(-1))}";
        NextStyleButton.ToolTip = $"下一套：{GetStylePickerLabel(GetAdjacentStyle(1))}";

        if (savePreference)
        {
            SaveAppearance();
        }

        UpdateQuotaPresentation();
        SetTokenUsageState(_tokenUsageState);
        SetStatusText(_quotaStatusMessage, _quotaStatusBrush);
        SetTokenStatus(_tokenStatusMessage, _tokenStatusBrush);
    }

    private FloatingStyleKind GetAdjacentStyle(int direction)
    {
        var currentIndex = Array.IndexOf(StyleOrder, _selectedStyle);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = (currentIndex + direction) % StyleOrder.Length;
        if (nextIndex < 0)
        {
            nextIndex += StyleOrder.Length;
        }

        return StyleOrder[nextIndex];
    }

    private static string GetStylePickerLabel(FloatingStyleKind style) => style switch
    {
        FloatingStyleKind.Instrument => "INSTRUMENT · A",
        FloatingStyleKind.Glass => "GLASS · B",
        FloatingStyleKind.Timeline => "TIMELINE · C",
        FloatingStyleKind.Terminal => "TERMINAL · D",
        _ => "GLASS · B"
    };

    private void SetStyleNavigationVisible(bool isVisible)
    {
        StyleNavigation.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    private void SetSelectedDashboardShadowVisible(bool isVisible)
    {
        if (_styleViews[_selectedStyle] is ICollapsibleDashboard dashboard)
        {
            dashboard.SetCardShadowVisible(isVisible);
        }
    }

    private void ApplyCardBackgroundOpacity(double opacity)
    {
        // 窗口保持完全不透明，避免文字和控件随背景一起变淡。
        Opacity = 1.0;
        foreach (var dashboard in _styleViews.Values.OfType<IBackgroundOpacityDashboard>())
        {
            dashboard.SetCardBackgroundOpacity(opacity);
        }
    }

    public void ApplySnapshot(UsageSnapshot snapshot)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => ApplySnapshot(snapshot));
            return;
        }

        SetQuotaState(snapshot.Quota);
        SetTokenUsageState(snapshot.TokenUsage);
    }

    public void ApplySettings(AppSettings settings)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => ApplySettings(settings));
            return;
        }

        _settings = settings;
        _savedWindowLeft = settings.FloatingLeft;
        ApplyCardBackgroundOpacity(settings.FloatingOpacity);
        ApplyStyle(settings.FloatingStyle, savePreference: false);
        _floatingWindowController?.ApplyBehavior(FloatingWindowBehavior.From(settings));
        var workArea = SystemParameters.WorkArea;
        Left = HorizontalWindowPlacement.ResolveInitialLeft(
            _savedWindowLeft,
            workArea.Left,
            workArea.Width,
            Width);
        Top = workArea.Top;
    }

    public void ShowFloating()
    {
        if (_disposed)
        {
            return;
        }

        if (!IsVisible)
        {
            Show();
        }

        Top = SystemParameters.WorkArea.Top;
        Activate();
    }

    public void HideFloating() => Hide();

    public void ToggleFloatingVisibility()
    {
        if (IsVisible)
        {
            HideFloating();
        }
        else
        {
            ShowFloating();
        }
    }

    public void SelectStyle(FloatingStyleKind style) => ApplyStyle(style, savePreference: true);

    public Action? OpenControlCenterAction
    {
        get => _openControlCenter;
        set => _openControlCenter = value;
    }

    private void PositionOnPrimaryScreen()
    {
        var workArea = SystemParameters.WorkArea;
        Left = HorizontalWindowPlacement.ResolveInitialLeft(
            _savedWindowLeft,
            workArea.Left,
            workArea.Width,
            Width);
        Top = workArea.Top;
    }

    private void SaveAppearance()
    {
        var position = _floatingWindowController?.ExpandedPosition;
        var left = position?.X ?? Left;

        _savedWindowLeft = left;
        _settings = _settings with
        {
            FloatingStyle = _selectedStyle,
            FloatingLeft = left
        };
        _settingsStore.Save(_settings);
    }

    private static string FormatQuota(int? remainingPercent, QuotaStatus status) =>
        remainingPercent is int value
            ? $"{value}%"
            : status == QuotaStatus.Loading
                ? "..."
                : "--";

    private void UpdateQuotaPresentation()
    {
        SetStyleText("FiveHourValue", FormatQuota(_currentState.FiveHourRemaining, _currentState.Status));
        SetStyleText("WeeklyValue", FormatQuota(_currentState.WeeklyRemaining, _currentState.Status));
        SetStyleForeground("FiveHourValue", GetQuotaBrush(_currentState.FiveHourRemaining));
        SetStyleForeground("WeeklyValue", GetQuotaBrush(_currentState.WeeklyRemaining));
        UpdateQuotaProgress(
            FindStyleElement<Border>("FiveHourProgressFill"),
            _currentState.FiveHourRemaining,
            GetQuotaProgressTrackWidth());
        UpdateQuotaProgress(
            FindStyleElement<Border>("WeeklyProgressFill"),
            _currentState.WeeklyRemaining,
            GetQuotaProgressTrackWidth());
        UpdateQuotaGauge(FindStyleElement<ShapePath>("FiveHourGaugeArc"), _currentState.FiveHourRemaining);
        UpdateQuotaGauge(FindStyleElement<ShapePath>("WeeklyGaugeArc"), _currentState.WeeklyRemaining);

        var now = DateTimeOffset.Now;
        SetStyleText("FiveHourResetValue", QuotaPresentation.FormatResetCountdown(_currentState.FiveHourResetAt, now));
        SetStyleText("WeeklyResetValue", QuotaPresentation.FormatResetCountdown(_currentState.WeeklyResetAt, now));
    }

    private static void UpdateQuotaProgress(Border? fill, int? remainingPercent, double trackWidth)
    {
        if (fill is null)
        {
            return;
        }

        if (remainingPercent is not int value)
        {
            fill.Width = 0;
            fill.Visibility = Visibility.Collapsed;
            return;
        }

        fill.Visibility = Visibility.Visible;
        fill.Width = trackWidth * Math.Clamp(value, 0, 100) / 100d;
        fill.Background = GetQuotaBrush(value);
    }

    private static void UpdateQuotaGauge(ShapePath? gauge, int? remainingPercent)
    {
        if (gauge is null)
        {
            return;
        }

        if (remainingPercent is not int value)
        {
            gauge.Data = Geometry.Empty;
            gauge.Visibility = Visibility.Collapsed;
            return;
        }

        gauge.Visibility = Visibility.Visible;
        gauge.Data = QuotaGaugeGeometry.CreateArc(value, 20);
        gauge.Stroke = GetQuotaBrush(value);
    }

    private double GetQuotaProgressTrackWidth() => _selectedStyle switch
    {
        FloatingStyleKind.Glass => 112,
        FloatingStyleKind.Timeline => 178,
        FloatingStyleKind.Terminal => 154,
        _ => 200
    };

    private static Brush GetQuotaBrush(int? remainingPercent) => QuotaPresentation.GetColorBand(remainingPercent) switch
    {
        QuotaColorBand.Green => new SolidColorBrush(Color.FromRgb(0x55, 0xD6, 0xA4)),
        QuotaColorBand.Yellow => new SolidColorBrush(Color.FromRgb(0xF7, 0xDE, 0x6B)),
        QuotaColorBand.Amber => new SolidColorBrush(Color.FromRgb(0xF2, 0xB8, 0x5D)),
        QuotaColorBand.Red => new SolidColorBrush(Color.FromRgb(0xFF, 0x7C, 0x74)),
        _ => new SolidColorBrush(Color.FromRgb(0xD7, 0xDE, 0xE7))
    };

    private void SetTokenUsageState(TokenUsageState state)
    {
        _tokenUsageState = state;
        SetTokenPeriod(
            FindStyleElement<TextBlock>("TodayTotalValue"),
            FindStyleElement<TextBlock>("TodayPriceValue"),
            state.Today,
            state.Status);
        SetTokenPeriod(
            FindStyleElement<TextBlock>("SevenDayTotalValue"),
            FindStyleElement<TextBlock>("SevenDayPriceValue"),
            state.Last7Days,
            state.Status);
        SetTokenPeriod(
            FindStyleElement<TextBlock>("ThirtyDayTotalValue"),
            FindStyleElement<TextBlock>("ThirtyDayPriceValue"),
            state.Last30Days,
            state.Status);

        var statusMessage = state.Status switch
        {
            TokenUsageStatus.Loading => "Loading…",
            TokenUsageStatus.Ready when state.LastUpdatedAt is { } updated => $"Updated {updated.ToLocalTime():HH:mm:ss}",
            TokenUsageStatus.Stale => "Read failed · showing previous data",
            _ => "--"
        };
        var statusBrush = state.Status switch
        {
            TokenUsageStatus.Ready => Brushes.LightSteelBlue,
            TokenUsageStatus.Stale => Brushes.Khaki,
            TokenUsageStatus.Offline => Brushes.LightCoral,
            _ => Brushes.Gainsboro
        };
        SetTokenStatus(statusMessage, statusBrush);
    }

    private static void SetTokenPeriod(
        TextBlock? totalText,
        TextBlock? priceText,
        TokenUsagePeriod period,
        TokenUsageStatus status)
    {
        if (totalText is null || priceText is null)
        {
            return;
        }

        var placeholder = status == TokenUsageStatus.Loading ? "..." : "--";
        var totalTokens = period.InputTokens + period.OutputTokens;
        totalText.Text = status is TokenUsageStatus.Ready or TokenUsageStatus.Stale
            ? TokenUsagePresentation.FormatCompact(totalTokens)
            : placeholder;
        priceText.Text = status is TokenUsageStatus.Ready or TokenUsageStatus.Stale
            ? $"(约{TokenUsagePresentation.FormatEstimatedPrice(period.EstimatedPriceUsd)}{(period.UnpricedTokens > 0 ? "*" : string.Empty)})"
            : $"(约{placeholder})";

        var tooltip = status is TokenUsageStatus.Ready or TokenUsageStatus.Stale
            ? TokenUsagePresentation.FormatTooltip(period)
            : null;
        totalText.ToolTip = tooltip;
        priceText.ToolTip = tooltip;
    }

    private void SetStatusText(string message, Brush foreground)
    {
        _quotaStatusMessage = message;
        _quotaStatusBrush = foreground;
        SetStyleText("StatusValue", message);
        SetStyleForeground("StatusValue", foreground);
    }

    private void SetTokenStatus(string message, Brush foreground)
    {
        _tokenStatusMessage = message;
        _tokenStatusBrush = foreground;
        SetStyleText("TokenStatusValue", message);
        SetStyleForeground("TokenStatusValue", foreground);
    }

    private T? FindStyleElement<T>(string name)
        where T : FrameworkElement =>
        _styleViews[_selectedStyle].FindName(name) as T;

    private void SetStyleText(string name, string text)
    {
        var element = FindStyleElement<TextBlock>(name);
        if (element is not null)
        {
            element.Text = text;
        }
    }

    private void SetStyleForeground(string name, Brush foreground)
    {
        var element = FindStyleElement<TextBlock>(name);
        if (element is not null)
        {
            element.Foreground = foreground;
        }
    }

    private static string GetDefaultStatusMessage(QuotaState state) => state.Status switch
    {
        QuotaStatus.Loading => "正在连接 Codex…",
        QuotaStatus.Ready when state.LastUpdatedAt is { } updated =>
            $"已更新 {updated.ToLocalTime():HH:mm:ss}",
        QuotaStatus.Ready => "已更新",
        QuotaStatus.Stale => "读取失败，正在保留上次数据并重试…",
        _ => "无法读取额度，请确认 Codex 已登录；正在重试…"
    };

    private static Brush GetStatusBrush(QuotaStatus status) => status switch
    {
        QuotaStatus.Ready => Brushes.LightSteelBlue,
        QuotaStatus.Stale => Brushes.Khaki,
        QuotaStatus.Offline => Brushes.LightCoral,
        _ => Brushes.Gainsboro
    };

}
