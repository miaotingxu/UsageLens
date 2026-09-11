namespace UsageLens.Views;

/// <summary>
/// 为自动折叠状态提供视觉切换，避免卡片阴影投射到顶部把手区域。
/// </summary>
public interface ICollapsibleDashboard
{
    void SetCardShadowVisible(bool isVisible);
}

/// <summary>
/// 仅调整卡片底色，不让窗口整体透明度影响文字、进度条和交互控件。
/// </summary>
public interface IBackgroundOpacityDashboard
{
    void SetCardBackgroundOpacity(double opacity);
}
