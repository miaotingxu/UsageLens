namespace UsageLens.Models;

public sealed record AppSettings(
    FloatingStyleKind FloatingStyle,
    double? FloatingLeft,
    double FloatingOpacity,
    bool AutoCollapseEnabled,
    TimeSpan CollapseDelay,
    bool ExpandOnHandleHover,
    TimeSpan QuotaRefreshInterval,
    TimeSpan TokenRefreshInterval,
    bool StartWithWindows,
    bool HideControlCenterOnClose,
    TrayPrimaryAction TrayPrimaryAction)
{
    public static AppSettings Default { get; } = new(
        FloatingStyleKind.Glass,
        FloatingLeft: null,
        FloatingOpacity: 0.70,
        AutoCollapseEnabled: true,
        CollapseDelay: TimeSpan.FromSeconds(1),
        ExpandOnHandleHover: true,
        QuotaRefreshInterval: TimeSpan.FromMinutes(1),
        TokenRefreshInterval: TimeSpan.FromMinutes(5),
        StartWithWindows: false,
        HideControlCenterOnClose: true,
        TrayPrimaryAction.OpenControlCenter);
}

public sealed record FloatingWindowBehavior(
    bool AutoCollapseEnabled,
    TimeSpan CollapseDelay,
    bool ExpandOnHandleHover)
{
    public static FloatingWindowBehavior From(AppSettings settings) => new(
        settings.AutoCollapseEnabled,
        settings.CollapseDelay,
        settings.ExpandOnHandleHover);
}
