using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using UsageLens.Models;

namespace UsageLens.Services;

public sealed class FloatingWindowController : IDisposable
{
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(180));

    private readonly Window _window;
    private readonly Border _card;
    private readonly Border _hoverZone;
    private readonly Action<bool>? _setCardShadowVisible;
    private readonly Action<bool>? _setNavigationVisible;
    private readonly DispatcherTimer _collapseTimer;
    private double _expandedLeft;
    private double _expandedTop;
    private int _animationVersion;
    private bool _interactionLocked;
    private bool _isDragging;
    private bool _disposed;
    private FloatingWindowBehavior _behavior = FloatingWindowBehavior.From(AppSettings.Default);

    public FloatingWindowController(
        Window window,
        Border card,
        Border hoverZone,
        Action<bool>? setCardShadowVisible = null,
        Action<bool>? setNavigationVisible = null)
    {
        _window = window;
        _card = card;
        _hoverZone = hoverZone;
        _setCardShadowVisible = setCardShadowVisible;
        _setNavigationVisible = setNavigationVisible;
        _expandedLeft = window.Left;
        _expandedTop = window.Top;
        _collapseTimer = new DispatcherTimer { Interval = _behavior.CollapseDelay };

        _card.MouseEnter += CardOnMouseEnter;
        _card.MouseLeave += CardOnMouseLeave;
        _hoverZone.MouseEnter += HoverZoneOnMouseEnter;
        _hoverZone.MouseLeave += HoverZoneOnMouseLeave;
        _collapseTimer.Tick += CollapseTimerOnTick;
    }

    public void Expand()
    {
        ThrowIfDisposed();
        CancelCollapse();
        AnimateTop(_expandedTop);
    }

    public void ScheduleCollapse()
    {
        ThrowIfDisposed();

        if (!_behavior.AutoCollapseEnabled ||
            _interactionLocked || _isDragging || _card.IsMouseOver || _hoverZone.IsMouseOver)
        {
            return;
        }

        _collapseTimer.Stop();
        _collapseTimer.Start();
    }

    public void CancelCollapse()
    {
        if (!_disposed)
        {
            _collapseTimer.Stop();
        }
    }

    public void SetInteractionLocked(bool isLocked)
    {
        ThrowIfDisposed();
        _interactionLocked = isLocked;

        if (isLocked)
        {
            CancelCollapse();
            Expand();
        }
    }

    public void ApplyBehavior(FloatingWindowBehavior behavior)
    {
        ThrowIfDisposed();
        _behavior = behavior;
        _collapseTimer.Interval = behavior.CollapseDelay;

        if (!behavior.AutoCollapseEnabled)
        {
            CancelCollapse();
            Expand();
        }
    }

    public void BeginUserDrag(double expandedTop)
    {
        ThrowIfDisposed();
        _isDragging = true;
        _expandedTop = expandedTop;
        CancelCollapse();
        _setCardShadowVisible?.Invoke(true);
        _setNavigationVisible?.Invoke(true);
        _window.BeginAnimation(Window.TopProperty, null);
        _window.Top = _expandedTop;
    }

    public void CompleteUserDrag(double expandedLeft, double expandedTop)
    {
        ThrowIfDisposed();
        _window.BeginAnimation(Window.TopProperty, null);
        _expandedLeft = expandedLeft;
        _expandedTop = expandedTop;
        _window.Left = expandedLeft;
        _window.Top = expandedTop;
        _isDragging = false;

        if (!_interactionLocked && !_card.IsMouseOver && !_hoverZone.IsMouseOver)
        {
            ScheduleCollapse();
        }
    }

    public Point ExpandedPosition => new(_expandedLeft, _expandedTop);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _animationVersion++;
        _collapseTimer.Stop();
        _collapseTimer.Tick -= CollapseTimerOnTick;
        _card.MouseEnter -= CardOnMouseEnter;
        _card.MouseLeave -= CardOnMouseLeave;
        _hoverZone.MouseEnter -= HoverZoneOnMouseEnter;
        _hoverZone.MouseLeave -= HoverZoneOnMouseLeave;
        _window.BeginAnimation(Window.TopProperty, null);
    }

    private void CardOnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => Expand();

    private void CardOnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => ScheduleCollapse();

    private void HoverZoneOnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_behavior.ExpandOnHandleHover)
        {
            Expand();
        }
    }

    private void HoverZoneOnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => ScheduleCollapse();

    private void CollapseTimerOnTick(object? sender, EventArgs e)
    {
        _collapseTimer.Stop();

        if (!_interactionLocked && !_isDragging && !_card.IsMouseOver && !_hoverZone.IsMouseOver)
        {
            AnimateTop(CollapsedTop);
        }
    }

    private void AnimateTop(double targetTop)
    {
        var isExpanded = targetTop >= _expandedTop - 0.01;
        _setCardShadowVisible?.Invoke(isExpanded);
        _setNavigationVisible?.Invoke(isExpanded);

        var currentTop = _window.Top;
        _window.BeginAnimation(Window.TopProperty, null);
        _window.Top = currentTop;

        if (Math.Abs(currentTop - targetTop) < 0.01)
        {
            return;
        }

        var animationVersion = ++_animationVersion;
        var animation = new DoubleAnimation
        {
            From = currentTop,
            To = targetTop,
            Duration = SlideDuration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (_, _) =>
        {
            if (_disposed || animationVersion != _animationVersion)
            {
                return;
            }

            _window.Top = targetTop;
            _window.BeginAnimation(Window.TopProperty, null);
        };

        _window.BeginAnimation(Window.TopProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private double CollapsedTop => _expandedTop - (_card.ActualHeight > 0 ? _card.ActualHeight : _card.Height);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
