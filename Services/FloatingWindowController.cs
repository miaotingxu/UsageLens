using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CodexQuotaFloat.Services;

public sealed class FloatingWindowController : IDisposable
{
    private static readonly TimeSpan CollapseDelay = TimeSpan.FromSeconds(1);
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(180));

    private readonly Window _window;
    private readonly Border _card;
    private readonly Border _hoverZone;
    private readonly DispatcherTimer _collapseTimer;
    private readonly double _expandedTop;
    private int _animationVersion;
    private bool _interactionLocked;
    private bool _disposed;

    public FloatingWindowController(Window window, Border card, Border hoverZone)
    {
        _window = window;
        _card = card;
        _hoverZone = hoverZone;
        _expandedTop = window.Top;
        _collapseTimer = new DispatcherTimer { Interval = CollapseDelay };

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

        if (_interactionLocked || _card.IsMouseOver || _hoverZone.IsMouseOver)
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

    private void HoverZoneOnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => Expand();

    private void HoverZoneOnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => ScheduleCollapse();

    private void CollapseTimerOnTick(object? sender, EventArgs e)
    {
        _collapseTimer.Stop();

        if (!_interactionLocked && !_card.IsMouseOver && !_hoverZone.IsMouseOver)
        {
            AnimateTop(CollapsedTop);
        }
    }

    private void AnimateTop(double targetTop)
    {
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
